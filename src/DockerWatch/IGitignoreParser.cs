using System;

namespace DockerWatch
{
    public interface IGitIgnoreParser : IDisposable
    {
        string Directory { get; set; }
        bool IsIgnored(string path);
    }
}
