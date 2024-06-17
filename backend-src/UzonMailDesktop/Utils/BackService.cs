﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace UzonMailDesktop.Utils
{
    /// <summary>
    /// 后台服务
    /// </summary>
    internal class BackService
    {
        private readonly string ServiceName = "UzonMailService";

        private List<Process> GetUzonMailProcesses()
        {
            return Process.GetProcesses().Where(x => x.ProcessName == ServiceName).ToList();
        }

        public void StartBackService()
        {
            // 关闭非当前目录中的后台服务
            CloseBackServiceIfNotSelf();

            // 判断是否有后台服务，若有，则不再启动
            var processes = GetUzonMailProcesses();
            if (processes.Count > 0)
            {
                return;
            }

            // 启动服务
            var startInfo = new ProcessStartInfo
            {
                FileName = "service/UZonMailService.exe",
                WorkingDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "service"),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(startInfo);
        }

        /// <summary>
        /// 关闭非当前目录中的后台服务
        /// </summary>
        public void CloseBackServiceIfNotSelf()
        {
            var processes = GetUzonMailProcesses();
            string servicePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "service");

            foreach (var process in processes)
            {
                string query = $"SELECT ProcessId,ExecutablePath FROM Win32_Process WHERE ProcessId = {process.Id}";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    ManagementObject mo = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    if (mo == null)
                        continue;

                    var executablePath = mo["ExecutablePath"]?.ToString();
                    if (string.IsNullOrEmpty(executablePath))
                        continue;

                    if (!executablePath.StartsWith(servicePath))
                    {
                        // 非当前进程目录下的进程服务，关闭
                        process.Kill();
                    }
                }
            }
        }

        /// <summary>
        /// 关闭后台服务
        /// </summary>
        public void CloseAllBackService()
        {
            // 查找进程名为 UZonMailService 的进程并杀死
            var processes = GetUzonMailProcesses();
            foreach (var item in processes)
            {
                item.Kill();
            }
        }

        /// <summary>
        /// 当窗口关闭时调用
        /// </summary>
        public void OnWindowsClosing()
        {
            // 获取配置
            var keepBackService = ConfigurationManager.AppSettings["keepBackService"];
            if(bool.TryParse(keepBackService, out bool keep) && keep)
            {
                CloseBackServiceIfNotSelf();
            }
            else
            {
                CloseAllBackService();
            }
        }
    }
}
