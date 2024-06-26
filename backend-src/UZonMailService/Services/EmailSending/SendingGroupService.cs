﻿using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Quartz;
using Uamazing.Utils.Json;
using Uamazing.Utils.Web.Service;
using UZonMailService.Jobs;
using UZonMailService.Models.SQL;
using UZonMailService.Models.SQL.EmailSending;
using UZonMailService.Models.SQL.Settings;
using UZonMailService.Services.EmailSending.Sender;
using UZonMailService.Services.EmailSending.WaitList;
using UZonMailService.Services.Settings;
using UZonMailService.Utils.Database;

namespace UZonMailService.Services.EmailSending
{
    /// <summary>
    /// 发送组服务
    /// </summary>
    public class SendingGroupService(SqlContext db
        , TokenService tokenService
        , SystemTasksService tasksService
        , SystemSendingWaitListService waitList
        , ISchedulerFactory schedulerFactory
        ) : IScopedService
    {
        /// <summary>
        /// 创建发送组
        /// </summary>
        /// <param name="sendingGroup"></param>
        /// <returns></returns>
        public async Task<SendingGroup> CreateSendingGroup(SendingGroup sendingGroup)
        {
            var userId = tokenService.GetUserDataId();
            // 格式化 Excel 数据
            sendingGroup.Data = await FormatExcelData(sendingGroup.Data, userId);

            // 使用事务
            await db.RunTransaction(async ctx =>
            {
                // 添加数据
                // 跟踪数据，将数据转换成 EF 对象
                if (sendingGroup.Templates != null)
                {
                    var templates = ctx.EmailTemplates.Where(x => sendingGroup.Templates.Select(t => t.Id).Contains(x.Id)).ToList();
                    sendingGroup.Templates = templates;
                }
                if (sendingGroup.Outboxes != null)
                {
                    var outboxes = ctx.Outboxes.Where(x => sendingGroup.Outboxes.Select(t => t.Id).Contains(x.Id)).ToList();
                    sendingGroup.Outboxes = outboxes;
                }
                if (sendingGroup.Attachments != null)
                {
                    var fileUsageIds = sendingGroup.Attachments.Select(x => x.__fileUsageId).Where(x => x > 0).ToList();
                    if (fileUsageIds.Count > 0)
                    {
                        var attachmenets = await ctx.FileUsages.Where(x => fileUsageIds.Contains(x.Id)).ToListAsync();
                        sendingGroup.Attachments = attachmenets;
                    }
                    else sendingGroup.Attachments = [];
                }
                // 增加数据
                sendingGroup.Status = SendingGroupStatus.Created;
                // 解析总数
                sendingGroup.TotalCount = sendingGroup.Inboxes.Count;
                sendingGroup.UserId = userId;

                ctx.SendingGroups.Add(sendingGroup);
                // 保存 group，从而获取 Id
                await ctx.SaveChangesAsync();

                // 获取用户设置
                var userSettings = await UserSettingsCache.GetUserSettings(ctx, sendingGroup.UserId);

                // 将数据组装成 SendingItem 保存
                // 要确保数据已经通过验证
                var builder = new SendingItemsBuilder(db, sendingGroup, userSettings, tokenService);
                List<SendingItem> items = await builder.GenerateAndSave();

                // 更新发件数量
                sendingGroup.TotalCount = items.Count;

                // 增加附件使用记录
                if (sendingGroup.Attachments != null && sendingGroup.Attachments.Count > 0)
                {
                    var attachmentObjectIds = sendingGroup.Attachments.Select(x => x.FileObjectId).ToList();
                    await ctx.FileObjects.UpdateAsync(x => attachmentObjectIds.Contains(x.Id), obj => obj.SetProperty(x => x.LinkCount, y => y.LinkCount + 1));
                }

                return await ctx.SaveChangesAsync();
            });

            return sendingGroup;
        }

        /// <summary>
        /// 格式化 Excel 数据
        /// 只保留属于自己的数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task<JArray?> FormatExcelData(JArray? data, long userId)
        {
            if (data == null || data.Count == 0)
            {
                return data;
            }

            // 获取发件箱,只能使用自己名下的发件箱
            var outboxEmails = data.Select(x => x.SelectTokenOrDefault("outbox", "")).Where(x => !string.IsNullOrEmpty(x)).ToList();
            var outboxes = await db.Outboxes.Where(x => x.UserId == userId && outboxEmails.Contains(x.Email)).ToListAsync();

            var templateIds = data.Select(x => x.SelectTokenOrDefault("templateId", 0L)).Where(x => x > 0).ToList();
            var templateNames = data.Select(x => x.SelectTokenOrDefault("templateName", "")).Where(x => !string.IsNullOrEmpty(x)).ToList();
            var templates = await db.EmailTemplates.Where(x => x.UserId == userId && (templateIds.Contains(x.Id) || templateNames.Contains(x.Name))).ToListAsync();

            // 重新更新数据
            JArray results = [];
            foreach (var token in data)
            {
                var outboxEmail = token.SelectTokenOrDefault<string>("outbox", "");
                if (!string.IsNullOrEmpty(outboxEmail))
                {
                    // 获取 outboxId
                    var outboxEntity = outboxes.FirstOrDefault(x => x.Email == outboxEmail);
                    if (outboxEntity != null)
                    {
                        token["outboxId"] = outboxEntity.Id;
                    }
                    else
                    {
                        token["outboxId"] = 0;
                        token["outbox"] = string.Empty;
                    }
                }

                var templateId = token.SelectTokenOrDefault<int>("templateId", 0);
                var templateName = token.SelectTokenOrDefault<string>("templateName", "");
                var templateEntity = templates.FirstOrDefault(x => x.Id == templateId || x.Name == templateName);

                if (templateEntity == null)
                {
                    token["templateId"] = 0;
                }
                else
                {
                    token["templateId"] = templateEntity.Id;
                }
                results.Add(token);
            }

            return results;
        }

        /// <summary>
        /// 立即发件
        /// </summary>
        /// <param name="sendingGroup"></param>
        /// <param name="sendItemIds">若有值，则只会发送这部分邮件</param>
        /// <returns></returns>
        public async Task SendNow(SendingGroup sendingGroup, List<long>? sendItemIds = null)
        {
            // 添加到发件列表
            await waitList.AddSendingGroup(sendingGroup, sendItemIds);
            // 开始发件
            tasksService.StartSending();
        }

        /// <summary>
        /// 计划发件
        /// </summary>
        /// <param name="sendingGroup"></param>
        /// <returns></returns>
        public async Task SendSchedule(SendingGroup sendingGroup)
        {
            var scheduler = await schedulerFactory.GetScheduler();
            var jobKey = new JobKey($"emailSending-{sendingGroup.Id}", "sendingGroup");

            var job = JobBuilder.Create<EmailSendingJob>()
                        .WithIdentity(jobKey)
                        .SetJobData(new JobDataMap
                        {
                            { "sendingGroupId", sendingGroup.Id },
                            { "smtpPasswordSecretKeys", string.Join(',',sendingGroup.SmtpPasswordSecretKeys) }
                        })
                        .Build();

            var trigger = TriggerBuilder.Create()
                .ForJob(jobKey)
                .StartAt(new DateTimeOffset(sendingGroup.ScheduleDate))
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }
    }
}
