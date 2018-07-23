using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DockerWatch
{
    public class ContainerMonitorHost : IHostedService, IDisposable
    {
        static readonly Regex SOURCE_HOST = new Regex("^(?:/host_mnt)?/([a-zA-Z])/(.*)$", RegexOptions.Compiled);

        private readonly DockerService _dockerService;
        private readonly IContainerNotifierFactory _containerNotifierFactory;
        private readonly ILogger _logger;

        private Dictionary<string, List<ContainerNotifier>> _containerNotifiers;
        private ContainerEventsMonitor _monitor;
        public ContainerMonitorHost(DockerService dockerService, IContainerNotifierFactory containerNotifierFactory, ILogger<ContainerMonitorHost> logger)
        {
            _dockerService = dockerService;
            _containerNotifierFactory = containerNotifierFactory;
            _logger = logger;

            _containerNotifiers = new Dictionary<string, List<ContainerNotifier>>();
        }

        private string ToWindowsPath(string dockerSourcePath)
        {
            MatchCollection matches = SOURCE_HOST.Matches(dockerSourcePath);
            if (matches.Count != 1 || matches[0].Groups.Count != 3) { return null; }
            return $"{matches[0].Groups[1]}:/{matches[0].Groups[2]}";
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting monitor.");
            var containers = await _dockerService.GetRunningContainers();

            _logger.LogTrace($"Found {containers.Count} running container{(containers.Count > 1 ? "s" : "")}.");

            foreach (var container in containers)
            {
                AttachNotifiersForContainer(container);
            }

            _monitor = _dockerService.MonitorContainerEvents();
            _monitor.OnEvent = HandleEvent;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Stopping monitor...");
            _monitor.Stop();

            foreach (var entry in _containerNotifiers.ToList())
            {
                var containerID = entry.Key;
                var notifiersForContainer = entry.Value;

                _logger.LogTrace($"Removing all registered notifiers for '{containerID}'.");

                foreach (var notifier in notifiersForContainer)
                {
                    notifier.Dispose();
                }

                notifiersForContainer.Clear();
                _containerNotifiers.Remove(containerID);

                _logger.LogTrace($"Notifiers for {containerID} have been removed.");
            }

            _logger.LogInformation("Monitoring stopped...");
            return Task.CompletedTask;
        }

        public void HandleEvent(ContainerMonitorEvent containerEvent)
        {
            switch (containerEvent.Type)
            {
                case "start":
                    AttachNotifiersForContainer(containerEvent.Container);
                    break;
                case "die":
                    RemoveNotifiersForContainer(containerEvent.Container);
                    break;
                default:
                    Console.WriteLine("Unknown event");
                    break;
            }
        }

        public void RemoveNotifiersForContainer(Container container)
        {
            List<ContainerNotifier> notifiers;

            if (_containerNotifiers.TryGetValue(container.ID, out notifiers))
            {
                foreach (var notifier in notifiers)
                {
                    notifier.Dispose();
                }

                notifiers.Clear();
                _containerNotifiers.Remove(container.ID);

                _logger.LogTrace($"Notifiers for {container.Name} have been removed.");
            }
        }

        public void AttachNotifiersForContainer(Container container)
        {
            var n = new List<ContainerNotifier>();

            foreach (var mount in container.Mounts)
            {
                var hostPath = ToWindowsPath(mount.Source);
                try
                {
                    _logger.LogTrace($"Creating notifier for {container.Name} at {hostPath}");
                    var notifier = _containerNotifierFactory.Create(container.ID, hostPath, mount.Destination);
                    n.Add(notifier);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogCritical($"Error creating notifier for {container.Name} at {hostPath}. Reason: {ex.Message}");
                }
            }

            _containerNotifiers.Add(container.ID, n);
            _logger.LogInformation($"Registered notifiers for all mounted volumes in '{container.Name}'.");
        }

        public void Dispose()
        {
            _monitor.Dispose();
        }
    }
}
