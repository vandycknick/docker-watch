using System;
using System.Reflection;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DockerWatch
{
    [Command(Name = "docker-watch", FullName = "docker-watch", Description = "Notify docker containers about changes in mounted volumes.")]
    [VersionOptionFromMember(MemberName = nameof(GetVersion))]
    [HelpOption]
    class Program
    {
        [Option(Description = "Enable more verbose and rich logging.", ShortName = "v")]
        public bool Verbose { get; set; } = false;

        public IHostBuilder CreateContainerMonitorHostBuilder()
        {
            var host = new HostBuilder()
                .UseConsoleLifetime()
                .ConfigureLogging((logging) =>
                {
                    logging.SetMinimumLevel(Verbose ? LogLevel.Trace : LogLevel.Information);
                    logging.AddConsole();
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

                    services.AddHostedService<ContainerMonitorHost>();

                    services.AddSingleton<IContainerNotifierFactory, ContainerNotifierFactory>();
                    services.AddSingleton<INotifierAction, NotifierAction>();
                    services.AddSingleton<DockerService>();
                });

            return host;
        }

        private Task OnExecuteAsync() =>
            CreateContainerMonitorHostBuilder().Build().RunAsync();

        public static Task Main(string[] args) =>
            CommandLineApplication.ExecuteAsync<Program>(args);

        private static string GetVersion() =>
            typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
