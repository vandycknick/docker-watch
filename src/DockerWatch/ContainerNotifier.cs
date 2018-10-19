using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace DockerWatch
{
    public class ContainerNotifier : IDisposable
    {
        private const string GITIGNORE = ".gitignore";

        private readonly INotifierAction _Notify;
        private readonly ILogger _logger;
        private readonly GitIgnoreParser _gitignore;

        private FileSystemWatcher _watcher;

        private PathInfo _hostPathInfo;

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

            _hostPathInfo = ParsePath(HostPath);
            _gitignore = GitIgnoreParser.Compile(GITIGNORE);

            if (!File.Exists(GITIGNORE))
                _logger.LogTrace($"No {GITIGNORE} file found in the current directory, no files will get ignored!");

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

        private string GetRelativePath(string source)
        {
            var path = source.Replace(HostPath, "").Replace("\\", "/");
            if (path.StartsWith('/'))
                return path.Substring(1);

            return path;
        }

        private string GetDockerPath(string source)
        {
            var relativePath = GetRelativePath(source);
            return $"{ContainerPath}/{relativePath}";
        }

        private DateTime _lastTimeFileWatcherEventRaised = DateTime.Now;

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // There is a bug in the FileSystemWatcher that causes this event
            // to sometimes be called twice. This is not an amazing workaround but
            // the best StackOverflow could provide me. 😝
            // https://stackoverflow.com/questions/449993/vb-net-filesystemwatcher-multiple-change-events/450046#450046
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                if (DateTime.Now.Subtract(_lastTimeFileWatcherEventRaised).TotalMilliseconds < 500)
                    return;

                _logger.LogTrace($"File changed {e.FullPath} in mounted volume {ContainerPath}.");

                _lastTimeFileWatcherEventRaised = DateTime.Now;

                var path = e.FullPath;
                var relativePath = GetRelativePath(path);
                var containerPath = _hostPathInfo.IsFile ? ContainerPath : GetDockerPath(path);

                if (!_gitignore.IsIgnored(relativePath))
                {
                    await _Notify.Notify(ContainerID, containerPath);
                }
                else
                {
                    _logger.LogTrace($"Filepath ({relativePath}) is ignored by {GITIGNORE}, not triggering notifiers!");
                }
            }
        }

        private void WatchForVolumeChanges()
        {
            if (_hostPathInfo.IsDirectory)
            {
                _watcher = new FileSystemWatcher()
                {
                    Path = HostPath,
                    Filter = "*.*",
                    IncludeSubdirectories = true,
                };
            }
            else if (_hostPathInfo.IsFile)
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
