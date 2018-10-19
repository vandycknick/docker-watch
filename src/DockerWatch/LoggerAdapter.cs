using Microsoft.Extensions.Logging;

namespace DockerWatch
{
    public class LoggerAdapter<T> : ILoggerAdapter<T>
    {
        private readonly ILogger<T> _logger;

        public LoggerAdapter(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogTrace(string message)
        {
            _logger.LogTrace(message);
        }

        public void LogError(string message)
        {
            _logger.LogError(message);
        }
    }
}
