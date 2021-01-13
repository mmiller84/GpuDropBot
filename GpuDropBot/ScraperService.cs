using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using GpuDropBot.Scrapers;
using GpuDropBot.Model;
using GpuDropBot.Notifiers;

namespace GpuDropBot
{
    public class ScraperService : IHostedService
    {
        private Dictionary<IScraper, IEnumerable<ProductDetails>> _watchedProducts = new Dictionary<IScraper, IEnumerable<ProductDetails>>();
        private IEnumerable<INotifier> _notifiers;

        private IEnumerable<ScraperSettings> _settings;
        private ILogger<ScraperService> _logger;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public ScraperService(ILogger<ScraperService> logger, IServiceProvider serviceProvider, List<ScraperSettings> settings, IEnumerable<INotifier> notifiers)
        {
            _logger = logger;
            _notifiers = notifiers;
            _settings = settings;

            foreach(var setting in settings)
            {
                _logger.LogInformation("Adding scraper \"{Name}\": {Type}", setting.Name, setting.Type);

                var scraperType = Type.GetType(setting.Type);
                var instance = ActivatorUtilities.CreateInstance(serviceProvider, scraperType, setting) as IScraper;

                _watchedProducts.Add(instance, new List<ProductDetails>());
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting the scraper service");

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            try
            {
                var tasks = new List<Task>();
                foreach (var scraper in _watchedProducts.Keys)
                {
                    var task = Task.Run(async () =>
                    {
                        _logger.LogInformation("Starting scraper task for \"{Name}\"", scraper.Settings.Name);

                        while (!cts.IsCancellationRequested)
                        {
                            try
                            {
                                var scrapedProducts = await scraper.GetProductDetails();
                                var newProducts = scrapedProducts.Except(_watchedProducts[scraper]);
                                _watchedProducts[scraper] = scrapedProducts;

                                _logger.LogInformation("Scraper \"{Name}\" got {ProductCount} products ({NewProductCount} new)", scraper.Settings.Name, scrapedProducts.Count(), newProducts.Count());

                                if (newProducts.Count() > 0)
                                {
                                    foreach (var notifier in _notifiers)
                                    {
                                        try
                                        {
                                            await notifier.Notify(newProducts);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "Error running notifier");
                                        }
                                    }
                                }

                            }
                            catch(Exception ex)
                            {
                                _logger.LogError(ex, "Error running scraper");
                            }

                            _logger.LogInformation("Scraper {Name} sleeping for {DelayDuration} minutes", scraper.Settings.Name, scraper.Settings.Frequency);
                            await Task.Delay(TimeSpan.FromMinutes(scraper.Settings.Frequency), cts.Token);
                        }
                    });

                    tasks.Add(task);
                }

                _logger.LogInformation("Starting scraper tasks");

                await Task.WhenAll(tasks);
            }
            catch (TaskCanceledException)
            {

            }

            _logger.LogInformation("Exiting the scraper service");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping the scraper service");

            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}
