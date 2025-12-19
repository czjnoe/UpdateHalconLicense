using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateHalconLicense.Models
{
    public class Appsetting
    {
        public string HalconDownloadUrl { get; set; }

        public List<string> Proxys { get; set; }

        public string HalconPath { get; set; }

        /// <summary>
        /// 下载目录
        /// </summary>
        public string DownloadPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HalconLicenses");

        public bool AutoUpdateEnabled { get; set; }

        /// <summary>
        /// 默认启用代理
        /// </summary>
        public bool UseProxy { get; set; } = true; 

        /// <summary>
        /// 默认每天
        /// </summary>
        public int UpdateIntervalIndex { get; set; } = 2;
    }
}
