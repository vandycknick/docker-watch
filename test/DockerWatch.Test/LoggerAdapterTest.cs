using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using Xunit;

namespace DockerWatch.Test
{
    public class LoggerAdapterTest
    {
        public class MockService { }

        [Fact]
        public void LogInformation_ShouldLogAnInformationMessage()
        {
            // Assert
            var logger = new Mock<ILogger<MockService>>();
            var adapter = new LoggerAdapter<MockService>(logger.Object);
            // Act
            adapter.LogInformation("information");

            // Arrange
            logger.Verify(m => m.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<FormattedLogValues>(v => v.ToString().Contains("information")),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()
            ));
        }

        [Fact]
        public void LogInformation_ShouldLogAnErrorMessage()
        {
            // Assert
            var logger = new Mock<ILogger<MockService>>();
            var adapter = new LoggerAdapter<MockService>(logger.Object);
            // Act
            adapter.LogError("error");

            // Arrange
            logger.Verify(m => m.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<FormattedLogValues>(v => v.ToString().Contains("error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()
            ));
        }

        [Fact]
        public void LogInformation_ShouldLogTraceMessages()
        {
            // Assert
            var logger = new Mock<ILogger<MockService>>();
            var adapter = new LoggerAdapter<MockService>(logger.Object);
            // Act
            adapter.LogTrace("trace");

            // Arrange
            logger.Verify(m => m.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<FormattedLogValues>(v => v.ToString().Contains("trace")),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()
            ));
        }
    }
}
