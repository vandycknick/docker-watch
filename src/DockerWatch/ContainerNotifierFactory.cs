using Microsoft.Extensions.Logging;

namespace DockerWatch
{
    public class ContainerNotifierFactory : IContainerNotifierFactory
    {
        private readonly INotifierAction _notifierAction;
        private readonly ILoggerFactory _loggerFactory;

        public ContainerNotifierFactory(INotifierAction notifierAction, ILoggerFactory loggerFactory)
        {
            _notifierAction = notifierAction;
            _loggerFactory = loggerFactory;
        }

        public ContainerNotifier Create(string containerID, string hostPath, string containerPath)
        {
            var logger = _loggerFactory.CreateLogger($"ContainerNotifier.{containerID}");
            return new ContainerNotifier(
                containerID: containerID,
                hostPath: hostPath,
                containerPath: containerPath,
                notify: _notifierAction,
                logger: logger
            );
        }
    }
}
