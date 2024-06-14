﻿using System.ComponentModel.DataAnnotations;

namespace UZonMailService.Controllers.Files
{
    public class StaticFileUploaderBody
    {
        /// <summary>
        /// 子路径
        /// </summary>
        public string SubPath { get; set; } = "default-upload";

        [Display(Name = "File")]
        public IFormFile File { get; set; }
    }
}
