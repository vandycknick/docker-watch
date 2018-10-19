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

        [Option(Description = "Glob pattern to filter containers. Without providing a pattern, notifiers will get attached to each running container.", ShortName = "c")]
        public string Container { get; set; } = "*";

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
                    services.Configure<ContainerMonitorHostOptions>(options => options.ContainerGlob = Container);

                    services.AddHostedService<ContainerMonitorHost>();
                    services.AddSingleton<IContainerNotifierFactory, ContainerNotifierFactory>();
                    services.AddSingleton<INotifierAction, NotifierAction>();
                    services.AddSingleton<IDockerService, DockerService>();
                    services.AddSingleton<DockerService>();
                    services.AddSingleton(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));
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
