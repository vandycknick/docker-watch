
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Xunit;
using static DockerWatch.DockerService;

namespace DockerWatch.Test
{
    public class NotifierActionTest
    {
        [Fact]
        public async Task Notify_ExecutesTheCorrectCommandInDocker()
        {
            // Assert
            var containerId = "123";
            var path = "path/to/file.js";
            var cmd = new string[] { "sh", "-c", String.Format("chmod $(stat -c %a {0}) {0}", path) };

            var docker = new Mock<IDockerService>();
            docker
                .Setup(s => s.Exec(containerId, cmd))
                .ReturnsAsync(new ContainerProcessResult()
                {
                    ExitCode = 0,
                });
            var logger = new Mock<ILoggerAdapter<NotifierAction>>();

            var notifier = new NotifierAction(docker.Object, logger.Object);

            // Act
            await notifier.Notify(containerId, path);

            // Arrange
            docker.Verify(d => d.Exec(containerId, cmd));
        }

        [Fact]
        public async Task Notify_LogsVerboseInformationmessage()
        {
            // Assert
            var containerId = "123";
            var path = "path/to/file.js";

            var docker = new Mock<IDockerService>();
            docker
                .Setup(s => s.Exec(containerId, It.IsAny<string[]>()))
                .ReturnsAsync(new ContainerProcessResult() { ExitCode = 0, });
            var logger = new Mock<ILoggerAdapter<NotifierAction>>();

            var notifier = new NotifierAction(docker.Object, logger.Object);

            // Act

            await notifier.Notify(containerId, path);

            // Arrange
            logger.Verify(l => l.LogTrace($"Syncing change for {path} inside container ({containerId}."));
        }

        [Fact]
        public async Task Notify_LogsErrorMessageWhenExecReturnNonZeroResult()
        {
            // Assert
            var error = "Some Error Message";
            var containerID = "123";
            var path = "path/to/file.js";

            var docker = new Mock<IDockerService>();
            docker
                .Setup(s => s.Exec(containerID, It.IsAny<string[]>()))
                .ReturnsAsync(new ContainerProcessResult()
                {
                    Stderr = new MemoryStream(Encoding.UTF8.GetBytes(error)),
                    ExitCode = 1,
                });
            var logger = new Mock<ILoggerAdapter<NotifierAction>>();

            var notifier = new NotifierAction(docker.Object, logger.Object);

            // Act
            await notifier.Notify(containerID, path);

            // Arrange
            logger.Verify(l => l.LogError($"Error syncing file in {containerID} for file {path}: {error}"));
        }
    }
}
