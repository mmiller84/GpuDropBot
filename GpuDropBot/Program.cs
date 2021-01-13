using System;

using Topshelf;
using Topshelf.HostConfigurators;
using Serilog;

using GpuDropBot.Infrastructure;

namespace GpuDropBot
{
    class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .WriteTo.Console()
              .WriteTo.Trace()
              .WriteTo.File(AppDomain.CurrentDomain.BaseDirectory + @"\Logs\Log.txt", rollingInterval: RollingInterval.Day, shared: true, retainedFileCountLimit: 2)
              .CreateLogger();
            
            return (int)HostFactory.Run(host => Configuration(host));
        }

        private static void Configuration(HostConfigurator host)
        {
            host.Service((settings) => new HostServiceControl());
            host.RunAsNetworkService();
            host.SetDisplayName("GpuDropBot");
            host.SetServiceName("GpuDropBot");
        }
    }
}
