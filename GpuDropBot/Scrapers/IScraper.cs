using System.Collections.Generic;
using System.Threading.Tasks;

using GpuDropBot.Model;

namespace GpuDropBot.Scrapers
{
    interface IScraper
    {
        public ScraperSettings Settings { get; }
        Task<IEnumerable<ProductDetails>> GetProductDetails();
    }
}
