﻿using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UZonMail.Core.Services.EmailSending.Base;
using UZonMail.Core.Services.EmailSending.Event;
using UZonMail.Core.Services.EmailSending.Event.Commands;
using UZonMail.Core.Services.EmailSending.Pipeline;
using UZonMail.Core.Services.EmailSending.Utils;

namespace UZonMail.Core.Services.EmailSending.OutboxPool
{
    /// <summary>
    /// 单个用户的发件箱池
    /// 每个邮箱账号共用冷却池
    /// key: 邮箱 userId+邮箱号 ，value: 发件箱列表
    /// </summary>
    public class UserOutboxesPool : ConcurrentDictionary<string, OutboxEmailAddress>, IWeight, ISendingComplete
    {
        private readonly static ILog _logger = LogManager.GetLogger(typeof(UserOutboxesPool));
        private readonly IServiceScopeFactory _ssf;

        public UserOutboxesPool(IServiceScopeFactory ssf, long userId, int weight)
        {
            _ssf = ssf;
            UserId = userId;
            Weight = weight > 0 ? weight : 1;
        }

        #region 自定义参数
        public long UserId { get; }
        #endregion

        #region 接口实现
        /// <summary>
        /// 权重
        /// </summary>
        public int Weight { get; private set; }

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool Enable
        {
            get
            {
                if (this.Count == 0) return false;
                return this.Values.Any(x => x.Enable);
            }
        }
        #endregion

        /// <summary>
        /// 添加发件箱
        /// </summary>
        /// <param name="outbox"></param>
        public async Task<bool> AddOutbox(OutboxEmailAddress outbox)
        {
            if (this.TryGetValue(outbox.Email, out var existValue))
            {
                existValue.Update(outbox);
                return true;
            }

            // 验证发件箱是否有效
            if (!outbox.Validate())
            {
                return false;
            }


            // 不存在则添加
            this.TryAdd(outbox.Email, outbox);
            return true;
        }

        /// <summary>
        /// 按用户设置的权重获取发件箱
        /// </summary>
        /// <returns></returns>
        public async Task<FuncResult<OutboxEmailAddress>> GetOutboxByWeight(SendingContext scopeServices)
        {
            var data = this.GetDataByWeight();
            if (data.NotOk) return new FuncResult<OutboxEmailAddress>()
            {
                Message = data.Message,
                Ok = data.Ok,
                Status = data.Status,
                Data = data.Data as OutboxEmailAddress
            };

            // outbox
            // 判断是否可用
            if (data.Data is not OutboxEmailAddress outbox)
            {
                return new FuncResult<OutboxEmailAddress>()
                {
                    Ok = false,
                    Status = PoolResultStatus.EmptyError,
                    Message = $"未能从{UserId}池中获取发件箱"
                };
            }

            if (!outbox.LockUsing())
            {
                // 获取使用权失败
                return new FuncResult<OutboxEmailAddress>()
                {
                    Ok = false,
                    Status = PoolResultStatus.LockError,
                    Message = $"发件箱 {outbox.Email} 已被其它线程使用，锁定失败"
                };
            }

            // 保存当前引用
            scopeServices.UserOutboxesPool = this;
            return new FuncResult<OutboxEmailAddress>()
            {
                Data = outbox,
                Ok = true
            };
        }

        public async Task EmailItemSendCompleted(SendingContext sendingContext)
        {
            // 移除发件箱
            if (sendingContext.OutboxEmailAddress.ShouldDispose)
            {
                this.TryRemove(sendingContext.OutboxEmailAddress.Email, out _);
                _logger.Info($"{sendingContext.OutboxEmailAddress.Email} 被标记为释放，从发件池中移除");
            }

            // 回调父级
            await sendingContext.UserOutboxesPoolManager.EmailItemSendCompleted(sendingContext);
        }
    }
}
