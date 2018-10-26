using LibGit2Sharp;

namespace DockerWatch
{
    public class GitIgnoreParser : IGitIgnoreParser
    {
        private IRepository _gitRepository = null;

        private string _path = null;

        public string Directory
        {
            get => _path;
            set {
                _path = value;
                var path = Repository.Discover(_path);

                if (_gitRepository != null)
                    _gitRepository.Dispose();

                if (path != null)
                    _gitRepository = new Repository(path);
            }
        }

        public bool IsIgnored(string relativePath)
        {
            if (_gitRepository == null)
                return false;

            return _gitRepository.Ignore.IsPathIgnored(relativePath);
        }

        public void Dispose()
        {
            _gitRepository?.Dispose();
        }
    }

    internal class NoGitignoreParser : IGitIgnoreParser
    {
        public string Directory { get; set; }
        public bool IsIgnored(string path) => false;
        public void Dispose() => new object {};
    }
}
