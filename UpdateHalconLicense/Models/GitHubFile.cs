using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateHalconLicense.Models
{
    public class GitHubFile
    {
        public string name { get; set; }
        public string download_url { get; set; }
        public string type { get; set; }
        public long size { get; set; }
    }
}
