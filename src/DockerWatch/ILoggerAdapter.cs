namespace DockerWatch
{
    public interface ILoggerAdapter<T>
    {
        void LogInformation(string message);

        void LogTrace(string message);

        void LogError(string message);
    }
}
