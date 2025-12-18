using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateHalconLicense.Models
{
    public class AppConfig
    {
        public string HalconPath { get; set; }
        public string DownloadPath { get; set; }
        public bool AutoUpdateEnabled { get; set; }
        public bool UseProxy { get; set; } = true; // 默认启用代理
        public int UpdateIntervalIndex { get; set; }
    }
}
