using System;
using System.IO;

namespace DockerWatch
{
    public interface IFileSystemWatcher : IDisposable
    {
        bool EnableRaisingEvents { get; set; }
        string Filter { get; set; }
        bool IncludeSubdirectories { get; set; }
        int InternalBufferSize { get; set; }
        NotifyFilters NotifyFilter { get; set; }
        string Path { get; set; }

        WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType);
        WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType, int timeout);

        event FileSystemEventHandler Changed;
        event FileSystemEventHandler Created;
        event FileSystemEventHandler Deleted;
        event ErrorEventHandler Error;
        event RenamedEventHandler Renamed;
    }
}
