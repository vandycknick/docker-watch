using System.IO;
using System.Threading;
using Moq;
using Xunit;

namespace DockerWatch.Test
{
    public class ContainerNotifierTest
    {
        [Fact]
        public void ContainerNotifier_CallsNotify_WhenDetectingChangesInMountedDirectory()
        {
            // Arrange
            var notify = new Mock<INotifierAction>();
            var logger = new Mock<ILoggerAdapter<ContainerNotifier>>();
            var watcher = new Mock<IFileSystemWatcher>();
            var gitingore = new Mock<IGitIgnoreParser>();

            gitingore.Setup(g => g.IsIgnored(It.IsAny<string>())).Returns(false);

            var containerId = "123";
            var hostPath = Path.GetTempPath();
            var containerPath = "/app";

            var notifier = new ContainerNotifier(notify.Object, logger.Object, watcher.Object, gitingore.Object);

            // Act
            notifier.Monitor(containerId, hostPath, containerPath);
            // Wait longer than 500 milliseconds before raising event because of throttling
            Thread.Sleep(501);

            var eventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, hostPath, "test.js");
            watcher.Raise(w => w.Changed += null, eventArgs);

            Thread.Sleep(501);
            eventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, $"{hostPath}\\subfolder", "test2.js");
            watcher.Raise(w => w.Changed += null, eventArgs);

            // Assert
            notify.Verify(n => n.Notify(containerId, $"{containerPath}/test.js"));
            notify.Verify(n => n.Notify(containerId, $"{containerPath}/subfolder/test2.js"));
        }

        [Fact]
        public void ContainerNotifier_CallsNotify_WhenDetectingChangesForMountedFile()
        {
            // Arrange
            var notify = new Mock<INotifierAction>();
            var logger = new Mock<ILoggerAdapter<ContainerNotifier>>();
            var watcher = new Mock<IFileSystemWatcher>();
            var gitingore = new Mock<IGitIgnoreParser>();

            gitingore.Setup(g => g.IsIgnored(It.IsAny<string>())).Returns(false);

            var containerId = "123";
            var filePath = Path.GetTempFileName();
            var containerPath = $"/app/{Path.GetFileName(filePath)}";

            var notifier = new ContainerNotifier(notify.Object, logger.Object, watcher.Object, gitingore.Object);

            // Act
            notifier.Monitor(containerId, filePath, containerPath);
            // Wait longer than 500 milliseconds before raising event because of throttling
            Thread.Sleep(501);

            var eventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
            watcher.Raise(w => w.Changed += null, eventArgs);

            // Assert
            notify.Verify(n => n.Notify(containerId, containerPath));
        }

        [Fact]
        public void ContainerNotifier_CallsNotify_OnlyOnceWhenMultipleEventsAreFiredInAShortTimeFrame()
        {
            // Arrange
            var notify = new Mock<INotifierAction>();
            var logger = new Mock<ILoggerAdapter<ContainerNotifier>>();
            var watcher = new Mock<IFileSystemWatcher>();
            var gitingore = new Mock<IGitIgnoreParser>();

            gitingore.Setup(g => g.IsIgnored(It.IsAny<string>())).Returns(false);

            var containerId = "123";
            var hostPath = Path.GetTempPath();
            var containerPath = "/app";

            var notifier = new ContainerNotifier(notify.Object, logger.Object, watcher.Object, gitingore.Object);

            // Act
            notifier.Monitor(containerId, hostPath, containerPath);

            var eventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, hostPath, "test1.js");
            watcher.Raise(w => w.Changed += null, eventArgs);

            eventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, hostPath, "test2.js");
            watcher.Raise(w => w.Changed += null, eventArgs);

            eventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, hostPath, "test3.js");
            watcher.Raise(w => w.Changed += null, eventArgs);

            Thread.Sleep(501);

            eventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, hostPath, "test4.js");
            watcher.Raise(w => w.Changed += null, eventArgs);

            // Assert
            notify.Verify(n => n.Notify(containerId, $"{containerPath}/test4.js"), Times.Once);
        }

        [Fact]
        public void ContainerNotifier_Dispose_RunsCleanupAndStopsListeningToEvents()
        {
            // Arrange
            var notify = new Mock<INotifierAction>();
            var logger = new Mock<ILoggerAdapter<ContainerNotifier>>();
            var watcher = new Mock<IFileSystemWatcher>();
            var gitingore = new Mock<IGitIgnoreParser>();

            gitingore.Setup(g => g.IsIgnored(It.IsAny<string>())).Returns(false);

            var containerId = "123";
            var hostPath = Path.GetTempPath();
            var containerPath = "/app";

            var notifier = new ContainerNotifier(notify.Object, logger.Object, watcher.Object, gitingore.Object);

            // Act
            notifier.Monitor(containerId, hostPath, containerPath);
            notifier.Dispose();

            var eventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, hostPath, "test.js");
            watcher.Raise(w => w.Changed += null, eventArgs);

            // Assert
            notify.Verify(n => n.Notify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.False(watcher.Object.EnableRaisingEvents);
        }

        [Fact]
        public void ContainerNotifier_Monitor_ConfiguresGitIgnoreCorrectly()
        {
            // Arrange
            var notify = new Mock<INotifierAction>();
            var logger = new Mock<ILoggerAdapter<ContainerNotifier>>();
            var watcher = new Mock<IFileSystemWatcher>();

            var gitingore1 = new Mock<IGitIgnoreParser>();
            var gitingore2 = new Mock<IGitIgnoreParser>();

            var containerId = "123";
            var hostPath = Path.GetTempPath();
            var filePath = Path.GetTempFileName();
            var containerPath = "/app";

            var notifier = new ContainerNotifier(notify.Object, logger.Object, watcher.Object, gitingore1.Object);
            var notifier2 = new ContainerNotifier(notify.Object, logger.Object, watcher.Object, gitingore2.Object);

            // Act
            notifier.Monitor(containerId, hostPath, containerPath);
            notifier2.Monitor(containerId, filePath, containerPath);

            // Assert
            gitingore1.VerifySet(g => g.Directory = hostPath);
            gitingore2.VerifySet(g => g.Directory = It.IsAny<string>(), Times.Never);
        }

        [Fact]
        public void ContainerNotifier_CallsNotify_OnlyWhenFileAreNotIgnoreByGitignore()
        {
            // Arrange
            var notify = new Mock<INotifierAction>();
            var logger = new Mock<ILoggerAdapter<ContainerNotifier>>();
            var watcher = new Mock<IFileSystemWatcher>();
            var gitingore = new Mock<IGitIgnoreParser>();

            gitingore.Setup(g => g.IsIgnored("bin/test.dll")).Returns(true);

            var containerId = "123";
            var hostPath = Path.GetTempPath();
            var containerPath = "/app";

            var notifier = new ContainerNotifier(notify.Object, logger.Object, watcher.Object, gitingore.Object);

            // Act
            notifier.Monitor(containerId, hostPath, containerPath);
            // Wait longer than 500 milliseconds before raising event because of throttling
            Thread.Sleep(501);

            var eventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, $"{hostPath}\\bin", "test.dll");
            watcher.Raise(w => w.Changed += null, eventArgs);

            Thread.Sleep(501);
            eventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, $"{hostPath}\\subfolder", "test2.js");
            watcher.Raise(w => w.Changed += null, eventArgs);

            // Assert
            notify.Verify(n => n.Notify(containerId, $"{containerPath}/subfolder/test2.js"));
            notify.VerifyNoOtherCalls();
        }

    }
}
