using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using GpuDropBot.Model;

namespace GpuDropBot.Notifiers
{
    public class PushbulletNotifier : INotifier
    {
        private ILogger<PushbulletNotifier> _logger;
        
        private string _apiKey;
        private string[] _devices;

        private HttpClient _httpClient = new HttpClient();

        public class PushRequest
        {
            public string device_iden { get; set; }
            public string type { get; set; }
            public string title { get; set; }
            public string body { get; set; }
        }

        public PushbulletNotifier(ILogger<PushbulletNotifier> logger, IConfiguration configuration)
        {
            _logger = logger;
            _apiKey = configuration.GetValue<string>("pushbullet:apiKey");

            _devices = configuration.GetSection("pushbullet:devices").Get<string[]>();
        }

        public async Task Notify(IEnumerable<ProductDetails> products)
        {
            _logger.LogInformation("Running pushbullet notifier for {ProductCount} products", products.Count());

            var requestData = new PushRequest
            {
                type = "note",
                title = "New GPU Products"
            };

            foreach(var product in products)
            {
                requestData.body += $"${product.Price} - {product.ProductName}\n{product.Link}\n\n";
            }

            foreach (var device in _devices)
            {
                requestData.device_iden = device;

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                content.Headers.Add("Access-Token", _apiKey);

                await _httpClient.PostAsync("https://api.pushbullet.com/v2/pushes", content);
            }
        }
    }
}
