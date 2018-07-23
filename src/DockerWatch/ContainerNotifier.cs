using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DockerWatch
{
    public class ContainerNotifier : IDisposable
    {
        private readonly INotifierAction _Notify;
        private readonly ILogger _logger;

        private FileSystemWatcher _watcher;

        public string ContainerID { get; private set; }
        public string HostPath { get; private set; }
        public string ContainerPath { get; private set; }

        public ContainerNotifier(string containerID, string hostPath, string containerPath, INotifierAction notify, ILogger logger)
        {
            ContainerID = containerID;
            HostPath = hostPath;
            ContainerPath = containerPath;

            _logger = logger;
            _Notify = notify;

            WatchForVolumeChanges();
        }

        struct PathInfo
        {
            public bool IsValid;
            public bool IsDirectory;
            public bool IsFile;
        }

        private PathInfo ParsePath(string path)
        {
            var pathInfo = new PathInfo()
            {
                IsDirectory = Directory.Exists(path),
                IsFile = File.Exists(path),
            };

            pathInfo.IsValid = !pathInfo.IsDirectory && !pathInfo.IsFile;
            return pathInfo;
        }

        private string GetDockerPath(String source) {
            var relativePath = source.Replace(HostPath, "").Replace("\\", "/");
            return $"{ContainerPath}{relativePath}";
        }

        private DateTime _lastTimeFileWatcherEventRaised = DateTime.Now;

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // There is a bug in the FileSystemWatcher that causes this event
            // to sometimes be called twice. This is not an amazing workaround but
            // the best StackOverflow could provide me. 😝
            // https://stackoverflow.com/questions/449993/vb-net-filesystemwatcher-multiple-change-events/450046#450046
            if( e.ChangeType == WatcherChangeTypes.Changed )
            {
                if(DateTime.Now.Subtract (_lastTimeFileWatcherEventRaised).TotalMilliseconds < 500)
                    return;

                _logger.LogTrace($"File changed {e.FullPath} in mounted volume {ContainerPath}.");

                _lastTimeFileWatcherEventRaised = DateTime.Now;
                await _Notify.Notify(ContainerID, GetDockerPath(e.FullPath));
            }
        }

        private void WatchForVolumeChanges()
        {
            var pathInfo = ParsePath(HostPath);

            if (pathInfo.IsDirectory)
            {
                _watcher = new FileSystemWatcher()
                {
                    Path = HostPath,
                    Filter = "*.*",
                    IncludeSubdirectories = true,
                };
            }
            else if (pathInfo.IsFile)
            {
                var fileInfo = new FileInfo(HostPath);
                _watcher = new FileSystemWatcher()
                {
                    Path = fileInfo.Directory.FullName,
                    Filter = fileInfo.Name
                };
            }
            else
            {
                throw new ArgumentException("HostPath is not a valid file or directory.");
            }

            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.Changed += OnFileChanged;
            _watcher.EnableRaisingEvents = true;
        }
        public void Dispose()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnFileChanged;
                _watcher.Dispose();
                _watcher = null;
            }
        }
    }
}
