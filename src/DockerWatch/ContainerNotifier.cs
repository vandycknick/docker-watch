using System;
using System.IO;

namespace DockerWatch
{
    public class ContainerNotifier : IDisposable
    {
        private const string GITIGNORE = ".gitignore";

        private readonly INotifierAction _notify;
        private readonly ILoggerAdapter<ContainerNotifier> _logger;
        private readonly IFileSystemWatcher _fileSystemWatcher;
        private readonly GitIgnoreParser _gitignore;

        private PathInfo _hostPathInfo;

        public string ContainerID { get; private set; }
        public string HostPath { get; private set; }
        public string ContainerPath { get; private set; }

        public ContainerNotifier(
            string containerID, string hostPath, string containerPath,
            INotifierAction notify, ILoggerAdapter<ContainerNotifier> logger,
            IFileSystemWatcher fileSystemWatcher
        ) : this(notify, logger, fileSystemWatcher)
        {
            ContainerID = containerID;
            HostPath = hostPath;
            ContainerPath = containerPath;
        }

        public ContainerNotifier(INotifierAction notify, ILoggerAdapter<ContainerNotifier> logger, IFileSystemWatcher fileSystemWatcher)
        {
            _logger = logger;
            _notify = notify;
            _fileSystemWatcher = fileSystemWatcher;

            _gitignore = GitIgnoreParser.Compile(GITIGNORE);

            if (!File.Exists(GITIGNORE))
                _logger.LogTrace($"No {GITIGNORE} file found in the current directory, no files will get ignored!");
        }

        public void Start()
        {
            _hostPathInfo = ParsePath(HostPath);

            if (_hostPathInfo.IsDirectory)
            {
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

        struct PathInfo
        {
            public bool IsValid;
            public bool IsDirectory;
            public bool IsFile;
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
                var relativePath = GetRelativePath(path);
                var containerPath = _hostPathInfo.IsFile ? ContainerPath : GetDockerPath(path);

                if (!_gitignore.IsIgnored(relativePath))
                {
                    await _notify.Notify(ContainerID, containerPath);
                }
                else
                {
                    _logger.LogTrace($"Filepath ({relativePath}) is ignored by {GITIGNORE}, not triggering notifiers!");
                }
            }
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

        public void Dispose()
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;
                _fileSystemWatcher.Changed -= OnFileChanged;
                _fileSystemWatcher.Dispose();
            }
        }
    }
}
