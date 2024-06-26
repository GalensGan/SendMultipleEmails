﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UZonMailService.Models.SQL.Base
{
    /// <summary>
    /// 所有数据库的基类
    /// </summary>
    public class SqlId
    {
        /// <summary>
        /// Id 值
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// 创建日期
        /// </summary>
        public DateTime CreateDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否被删除了
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 是否隐藏
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// 设置状态为正常
        /// </summary>
        public void SetStatusNormal()
        {
            IsDeleted = false;
            IsHidden = false;
        }
    }
}
