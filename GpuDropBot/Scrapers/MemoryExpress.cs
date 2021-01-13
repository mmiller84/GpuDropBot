using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using AngleSharp;

using GpuDropBot.Model;

namespace GpuDropBot.Scrapers
{
    public class MemoryExpress : IScraper
    {
        private ILogger<MemoryExpress> _logger;
        private ScraperSettings _settings;
        private HttpClient _httpClient = new HttpClient();

        public MemoryExpress(ILogger<MemoryExpress> logger, ScraperSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public ScraperSettings Settings => _settings;

        public async Task<IEnumerable<ProductDetails>> GetProductDetails()
        {
            var response = await _httpClient.GetAsync(Settings.ScrapeUrl);

            if(response.IsSuccessStatusCode)
            {
                var baseUri = new Uri(Settings.ScrapeUrl);
                var content = await response.Content.ReadAsStringAsync();

                var config = Configuration.Default;
                var context = BrowsingContext.New(config);

                //Just get the DOM representation
                var document = await context.OpenAsync(req => req.Content(content));

                var products = document.GetElementsByClassName("c-shca-icon-item");

                var results = new List<ProductDetails>();
                foreach(var product in products)
                {
                    var details = product.GetElementsByClassName("c-shca-icon-item__body-name").First();

                    var name = details.GetElementsByTagName("a").First().TextContent.Replace("\n", " ").Trim();
                    name = Regex.Replace(name, " +", " ");

                    var url = $"{baseUri.Scheme}://{baseUri.Host}{details.GetElementsByTagName("a").First().GetAttribute("href")}";

                    var price = product.GetElementsByClassName("c-shca-icon-item__summary-list")
                        .First()
                        .TextContent
                        .Replace("\n", string.Empty)
                        .Replace("$", string.Empty)
                        .Trim();

                    results.Add(new ProductDetails
                    {
                        ProductName = name,
                        Price = Convert.ToDecimal(price),
                        Link = new Uri(url)
                    });
                }

                return results;
            }
            else
            {
                _logger.LogError("Receiver error {ResponseCode}: {Reason} from url {ScrapeUrl}", response.StatusCode, response.ReasonPhrase, Settings.ScrapeUrl);
            }

            return new List<ProductDetails>();
        }
    }
}
