using System;
using System.Collections.Generic;
using System.Text;

namespace GpuDropBot
{
    [Serializable]
    public sealed class ScraperSettings
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string ScrapeUrl { get; set; }
        public int Frequency { get; set; }
    }
}
