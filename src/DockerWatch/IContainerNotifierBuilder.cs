namespace DockerWatch
{
    public interface IContainerNotifierFactory
    {
        ContainerNotifier Create(string containerID, string hostPath, string containerPath);
    }
}
