using System;

namespace DockerWatch
{
    public interface IContainerNotifier : IDisposable
    {
        void Monitor(string containerID, string hostPath, string containerPath);
    }
}
