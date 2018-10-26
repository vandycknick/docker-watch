using System;
using System.IO;

namespace DockerWatch
{
    public class ContainerNotifier : IContainerNotifier
    {

        private readonly INotifierAction _notify;
        private readonly ILoggerAdapter<ContainerNotifier> _logger;
        private readonly IFileSystemWatcher _fileSystemWatcher;
        private readonly IGitIgnoreParser _gitignore;

        private PathInfo _hostPathInfo;

        private string ContainerID { get; set; }
        private string HostPath { get; set; }
        private string ContainerPath { get; set; }

        public ContainerNotifier(
            INotifierAction notify,
            ILoggerAdapter<ContainerNotifier> logger,
            IFileSystemWatcher fileSystemWatcher,
            IGitIgnoreParser gitignore
        )
        {
            _logger = logger;
            _notify = notify;
            _fileSystemWatcher = fileSystemWatcher;

            _gitignore = gitignore;
        }

        public void Monitor(string containerID, string hostPath, string containerPath)
        {
            ContainerID = containerID;
            HostPath = hostPath;
            ContainerPath = containerPath;

            _hostPathInfo = ParsePath(HostPath);

            if (_hostPathInfo.IsDirectory)
            {
                _gitignore.Directory = HostPath;
                _fileSystemWatcher.Path = HostPath;
                _fileSystemWatcher.Filter = "*.*";
                _fileSystemWatcher.IncludeSubdirectories = true;
            }
            else if (_hostPathInfo.IsFile)
            {
                var fileInfo = new FileInfo(HostPath);
                _fileSystemWatcher.Path = fileInfo.Directory.FullName;
                _fileSystemWatcher.Filter = fileInfo.Name;
            }
            else
            {
                throw new ArgumentException("HostPath is not a valid file or directory.");
            }

            _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _fileSystemWatcher.Changed += OnFileChanged;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private DateTime _lastTimeFileWatcherEventRaised = DateTime.Now;

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {            // There is a bug in the FileSystemWatcher that causes this event
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
                var relativePath = Path.GetRelativePath(HostPath, path).Replace("\\", "/");
                var containerPath = _hostPathInfo.IsFile ? ContainerPath : $"{ContainerPath}/{relativePath}";

                if (!_gitignore.IsIgnored(relativePath))
                {
                    await _notify.Notify(ContainerID, containerPath);
                }
                else
                {
                    _logger.LogTrace($"Filepath ({relativePath}) is ignored, not triggering notifiers!");
                }
            }
        }

        struct PathInfo
        {
            public bool IsDirectory;
            public bool IsFile;
        }

        private PathInfo ParsePath(string path)
        {
            return new PathInfo()
            {
                IsDirectory = Directory.Exists(path),
                IsFile = File.Exists(path),
            };
        }

        public void Dispose()
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;
                _fileSystemWatcher.Changed -= OnFileChanged;
                _fileSystemWatcher.Dispose();
            }

            _gitignore.Dispose();
        }
    }
}
