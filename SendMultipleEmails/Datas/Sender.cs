﻿using log4net;
using Panuon.UI.Silver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SendMultipleEmails.Datas
{
    public class Sender:Person
    {
        public string SMTP { get; set; }        
        public string Password { get; set; }

        public bool IsAsSender { get; set; } = true;

        public override bool Validate(ILog logger,Window window=null)
        {
            if (!base.Validate(logger)) return false;

            if (string.IsNullOrEmpty(SMTP))
            {
                if(logger==null) MessageBoxX.Show(window,"SMTP服务器不能为空", "温馨提示",MessageBoxButton.OK,MessageBoxIcon.Info);
                else logger.Warn(string.Format("第[{0}]条记录的SMTP服务器为空，姓名：{1}，邮箱：{2}", Order, UserId, Email));
                return false;
            }           

            if (string.IsNullOrEmpty(Password))
            {
                if(logger==null) MessageBoxX.Show(window,"密码不能为空", "温馨提示", MessageBoxButton.OK, MessageBoxIcon.Info);
                else logger.Warn(string.Format("第[{0}]条记录的密码为空，姓名：{1}，邮箱：{2}", Order, UserId, Email));
                return false;
            }

            return true;
        }
    }
}
