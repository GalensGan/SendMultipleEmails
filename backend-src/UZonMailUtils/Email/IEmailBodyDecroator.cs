﻿using System;
using System.Threading.Tasks;
using UZonMail.DB.SQL;
using UZonMail.DB.SQL.EmailSending;
using UZonMail.DB.SQL.Settings;

namespace Uamazing.Utils.Email
{
    /// <summary>
    /// 邮件正文修饰器
    /// </summary>
    public interface IEmailBodyDecroator
    {
        /// <summary>
        /// 开始进行装饰
        /// </summary>
        /// <param name="decoratorParams"></param>
        /// <param name="originBody"></param>
        /// <returns></returns>
        public Task<string> StartDecorating(EmailBodyDecoratorParams decoratorParams, string originBody);
    }
}
