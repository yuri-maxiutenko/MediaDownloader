using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaDownloader.Models
{
    public class DownloadOption
    {
        public DownloadFormat Format { get; set; }

        public string Name { get; set; }

        public string Option { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
