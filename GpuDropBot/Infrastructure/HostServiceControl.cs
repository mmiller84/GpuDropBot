using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Topshelf;

using GpuDropBot.Notifiers;
using GpuDropBot.Scrapers;

namespace GpuDropBot.Infrastructure
{
    public class HostServiceControl : ServiceControl
    {
        private CancellationTokenSource _hostCancellationSource = new CancellationTokenSource();

        public HostServiceControl()
        {

        }

        public bool Start(HostControl hostControl)
        {
            using IHost host = CreateHostBuilder().Build();

            host.RunAsync(_hostCancellationSource.Token);

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _hostCancellationSource.Cancel();
            return true;
        }

        private IHostBuilder CreateHostBuilder() =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                configuration.Sources.Clear();

                IHostEnvironment env = hostingContext.HostingEnvironment;

                configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

                var config = configuration.Build();
            })
            .ConfigureServices((context, services) =>
            {
                var config = context.Configuration;
                services.AddSingleton(config);

                services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

                var scraperSettings = new List<ScraperSettings>();
                config.GetSection("Scrapers").Bind(scraperSettings);
                services.AddSingleton(scraperSettings);

                var scrapers = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(i => !i.IsInterface && !i.IsAbstract && i.IsAssignableFrom(typeof(IScraper)));

                foreach(var scraper in scrapers)
                {
                    services.AddScoped(scraper);
                }

                services.AddScoped<INotifier, PushbulletNotifier>();

                services.AddHostedService<ScraperService>();
            });
    }
}
