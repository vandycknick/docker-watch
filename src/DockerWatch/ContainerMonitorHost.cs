using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace DockerWatch
{
    public class ContainerMonitorHost : IHostedService, IDisposable
    {
        static readonly Regex SOURCE_HOST = new Regex("^(?:/host_mnt)?/([a-zA-Z])/(.*)$", RegexOptions.Compiled);

        private readonly DockerService _dockerService;
        private readonly INotifierAction _notifyAction;

        private Timer _timer;
        private Dictionary<string, List<ContainerNotifier>> _Notifiers;

        public ContainerMonitorHost(DockerService dockerService, INotifierAction notifyAction)
        {
            _dockerService = dockerService;
            _notifyAction = notifyAction;

            _Notifiers = new Dictionary<string, List<ContainerNotifier>>();
        }

        private string ToWindowsPath(string dockerSourcePath)
        {
            MatchCollection matches = SOURCE_HOST.Matches(dockerSourcePath);
            if (matches.Count != 1 || matches[0].Groups.Count != 3) { return null; }
            return $"{matches[0].Groups[1]}:/{matches[0].Groups[2]}";
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var containers = await _dockerService.GetRunningContainers();

            WatchContainerVolumes(containers);

            _timer = new Timer(
                callback: async (object state) => await SyncContainerNotifiers(),
                state: null,
                dueTime: TimeSpan.Zero,
                period: TimeSpan.FromSeconds(1)
            );
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public async Task SyncContainerNotifiers()
        {
            var containers = await _dockerService.GetRunningContainers();

            foreach (var item in _Notifiers.ToList())
            {
                if (containers.Count(p => p.ID == item.Key) == 0)
                {
                    foreach (var notifier in item.Value.ToList())
                    {
                        notifier.Dispose();
                    }
                    item.Value.Clear();
                    _Notifiers.Remove(item.Key);
                }
            }

            var newContainers = containers.Where(c => !_Notifiers.ContainsKey(c.ID));
            WatchContainerVolumes(newContainers.ToList());
        }

        public void WatchContainerVolumes(IList<Container> containers)
        {
            foreach (var container in containers)
            {
                Console.WriteLine($"Image {container.Name}.");
                Console.WriteLine("---");

                var n = new List<ContainerNotifier>();

                foreach (var mount in container.Mounts)
                {
                    var hostPath = ToWindowsPath(mount.Source);
                    try
                    {
                        var notify = new ContainerNotifier(
                            containerID: container.ID,
                            hostPath: hostPath,
                            containerPath: mount.Destination,
                            notify: _notifyAction
                        );

                        n.Add(notify);
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}. {hostPath}");
                    }
                }

                _Notifiers.Add(container.ID, n);

                Console.WriteLine("---");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
