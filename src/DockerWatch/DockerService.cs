using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace DockerWatch
{
    public class Container
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public IList<MountPoint> Mounts { get; set; }
    }

    public class DockerService
    {   
        const string DOCKER_URI = "npipe://./pipe/docker_engine";

        private readonly DockerClient _Client;

        public DockerService()
        {
            _Client = new DockerClientConfiguration(new Uri(DOCKER_URI))
                            .CreateClient();
        }

        public async Task<IList<Container>> GetRunningContainers()
        {
            var containers = await _Client.Containers.ListContainersAsync(
                new ContainersListParameters()
                {
                    All = true
                }
            );

            var running = (
                from container in containers
                where container.State == "running"
                select new Container()
                {
                    ID = container.ID,
                    Name = container.Image,
                    Mounts = container.Mounts,
                }
            );

            return running.ToList();
        }

        public async Task<MemoryStream> Exec(string containerId, string[] cmd)
        {
            MemoryStream output = new MemoryStream();

            var response = await _Client.Containers.ExecCreateContainerAsync(
                    id: containerId,
                    parameters: new ContainerExecCreateParameters {
                        AttachStderr = false,
                        AttachStdin = false,
                        AttachStdout = true,
                        Cmd = cmd,
                        Detach = false,
                        Tty = false,
                        User = "root",
                        Privileged = true
                    }
            );

            using (var stream = await _Client.Containers.StartAndAttachContainerExecAsync(
                id: response.ID,
                tty: false,
                cancellationToken: default(CancellationToken)
            ))
            {
                await stream.CopyOutputToAsync(null, output, null, default(CancellationToken));
            }

            output.Position = 0;

            return output;
        }
    }
}
