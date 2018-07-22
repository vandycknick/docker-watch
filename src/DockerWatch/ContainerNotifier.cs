using System;
using System.IO;
using System.Threading.Tasks;

namespace DockerWatch
{
    public class ContainerNotifier : IDisposable
    {
        private FileSystemWatcher _Watcher;

        private readonly INotifierAction _Notify;

        public string ContainerID { get; private set; }
        public string HostPath { get; private set; }
        public string ContainerPath { get; private set; }

        public ContainerNotifier(string containerID, string hostPath, string containerPath, INotifierAction notify)
        {
            ContainerID = containerID;
            HostPath = hostPath;
            ContainerPath = containerPath;

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

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            await _Notify.Notify(ContainerID, GetDockerPath(e.FullPath));
        }

        private void WatchForVolumeChanges()
        {
            var pathInfo = ParsePath(HostPath);

            if (pathInfo.IsDirectory)
            {
                _Watcher = new FileSystemWatcher()
                {
                    Path = HostPath,
                    Filter = "*.*",
                    IncludeSubdirectories = true,
                };
            }
            else if (pathInfo.IsFile)
            {
                var fileInfo = new FileInfo(HostPath);
                _Watcher = new FileSystemWatcher()
                {
                    Path = fileInfo.Directory.FullName,
                    Filter = fileInfo.Name
                };
            }
            else
            {
                throw new ArgumentException("HostPath is not a valid file or directory.");
            }

            _Watcher.NotifyFilter = NotifyFilters.LastWrite;
            _Watcher.Changed += OnFileChanged;
            _Watcher.EnableRaisingEvents = true;
        }
        public void Dispose()
        {
            if (_Watcher != null)
            {
                _Watcher.EnableRaisingEvents = false;
                _Watcher.Changed -= OnFileChanged;
                _Watcher.Dispose();
                _Watcher = null;
            }
        }
    }
}
