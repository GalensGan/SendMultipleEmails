﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Uamazing.Utils.Web.Extensions;
using Uamazing.Utils.Web.ResponseModel;
using UZonMailService.Controllers.Emails.Models;
using UZonMailService.Models.SQL;
using UZonMailService.Models.SQL.EmailSending;
using UZonMailService.Services.Settings;
using UZonMailService.Utils.ASPNETCore.PagingQuery;

namespace UZonMailService.Controllers.Emails
{
    public class SendingItemController(SqlContext db, TokenService tokenService) : ControllerBaseV1
    {
        /// <summary>
        /// 获取邮件模板数量
        /// </summary>
        /// <param name="sendingGroupId"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet("filtered-count")]
        public async Task<ResponseResult<int>> GetEmailTemplatesCount(long sendingGroupId, string filter)
        {
            var userId = tokenService.GetUserDataId();
            // 只能获取自己的发件历史
            var sendingGroup = await db.SendingGroups.FirstOrDefaultAsync(x => x.Id == sendingGroupId && x.UserId == userId);
            if (sendingGroup == null)
            {
                return 0.ToSuccessResponse();
            }

            var dbSet = db.SendingItems.Where(x => x.SendingGroupId == sendingGroupId);
            if (!string.IsNullOrEmpty(filter))
            {
                dbSet = dbSet.Where(x => x.Subject.Contains(filter) || x.ToEmails.Contains(filter) || x.FromEmail.Contains(filter));
            }
            var count = await dbSet.CountAsync();
            return count.ToSuccessResponse();
        }

        /// <summary>
        /// 获取邮件模板数据
        /// </summary>
        /// <param name="sendingGroupId"></param>
        /// <param name="filter"></param>
        /// <param name="pagination"></param>
        /// <returns></returns>
        [HttpPost("filtered-data")]
        public async Task<ResponseResult<List<SendingItem>>> GetEmailTemplatesData(long sendingGroupId, string filter, Pagination pagination)
        {
            var userId = tokenService.GetUserDataId();
            // 只能获取自己的发件历史
            var sendingGroup = await db.SendingGroups.FirstOrDefaultAsync(x => x.Id == sendingGroupId && x.UserId == userId);
            if (sendingGroup == null)
            {
                return new List<SendingItem>().ToSuccessResponse();
            }

            var dbSet = db.SendingItems.Where(x => x.SendingGroupId == sendingGroupId);
            if (!string.IsNullOrEmpty(filter))
            {
                dbSet = dbSet.Where(x => x.Subject.Contains(filter) || x.ToEmails.Contains(filter) || x.FromEmail.Contains(filter));
            }

            var results = await dbSet.Page(pagination)
                .Select(x => new SendingItem()
                {
                    Id = x.Id,
                    Subject = x.Subject,
                    OutBoxId = x.OutBoxId,
                    FromEmail = x.FromEmail,
                    Inboxes = x.Inboxes,
                    Status = x.Status,
                    CreateDate = x.CreateDate,
                    SendDate = x.SendDate,
                    SendResult = x.SendResult
                })
                .ToListAsync();
            return results.ToSuccessResponse();
        }

        [HttpGet("{sendingItemId:long}/body")]
        public async Task<ResponseResult<string?>> GetSendingItemBody(long sendingItemId)
        {
            var userId = tokenService.GetUserDataId();
            var sendingItem = await db.SendingItems.FirstOrDefaultAsync(x => x.Id == sendingItemId && x.UserId == userId);
            if (sendingItem == null) return "".ToErrorResponse("邮件已被删除");
            return sendingItem.Content.ToSuccessResponse();
        }
    }
}
