using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DockerWatch
{
    public class ContainerMonitor
    {
        static readonly Regex DockerSourceHost = new Regex("^(?:/host_mnt)?/([a-zA-Z])/(.*)$", RegexOptions.Compiled);

        private readonly DockerService _DockerService;

        private readonly INotifierAction _NotifyAction;

        private List<ContainerNotifier> _Notifiers = new List<ContainerNotifier>();

        public ContainerMonitor(DockerService dockerService, INotifierAction iNotify)
        {
            _DockerService = dockerService;
            _NotifyAction = iNotify;
        }

        public async Task Start()
        {
            var containers = await _DockerService.GetRunningContainers();

            foreach (var container in containers)
            {
                Console.WriteLine($"Image {container.Name}.");
                Console.WriteLine("---");
                
                foreach(var mount in container.Mounts)
                {
                    var hostPath = ToWindowsPath(mount.Source);
                    try
                    {
                        var notify = new ContainerNotifier(
                            containerID: container.ID,
                            hostPath: hostPath,
                            containerPath: mount.Destination,
                            notify: _NotifyAction
                        );

                        _Notifiers.Add(notify);
                    }
                    catch(ArgumentException ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}. {hostPath}");
                    }
                }

                Console.WriteLine("---");
            }
        }

        private string ToWindowsPath(string dockerSourcePath)
        {
            MatchCollection matches = DockerSourceHost.Matches(dockerSourcePath);
            if (matches.Count != 1 || matches[0].Groups.Count != 3) { return null; }
            return $"{matches[0].Groups[1]}:/{matches[0].Groups[2]}";
        }

    }
}
