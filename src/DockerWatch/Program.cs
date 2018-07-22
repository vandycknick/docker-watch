using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DockerWatch
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .UseConsoleLifetime()
                .ConfigureServices((context, services) =>
                {
                    services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

                    services.AddHostedService<ContainerMonitorHost>();

                    services.AddSingleton<INotifierAction, NotifierAction>();
                    services.AddSingleton<DockerService>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
