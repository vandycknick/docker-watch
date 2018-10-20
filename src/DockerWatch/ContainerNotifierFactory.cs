namespace DockerWatch
{
    public class ContainerNotifierFactory : IContainerNotifierFactory
    {
        private readonly INotifierAction _notifierAction;
        private readonly ILoggerAdapter<ContainerNotifier> _logger;

        public ContainerNotifierFactory(INotifierAction notifierAction, ILoggerAdapter<ContainerNotifier> loggerFactory)
        {
            _notifierAction = notifierAction;
            _logger = loggerFactory;
        }

        public ContainerNotifier Create(string containerID, string hostPath, string containerPath)
        {
            return new ContainerNotifier(
                containerID: containerID,
                hostPath: hostPath,
                containerPath: containerPath,
                notify: _notifierAction,
                logger: _logger,
                fileSystemWatcher: new FileSystemWatcherWrapper()
            );
        }
    }
}
