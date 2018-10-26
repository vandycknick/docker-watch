using System;
using System.IO;
using LibGit2Sharp;
using Xunit;

namespace DockerWatch.Test
{
    public class GitIngoreParserTest : IDisposable
    {
        private GitIgnoreParser gitignore;

        [Theory]
        [InlineData("somefile.js")]
        [InlineData("bin/some/nested/file.dll")]
        public void GitIgnoreParser_IsIgnored_ReturnsFalseWhenNoGitRepositoryIsFound(string relativePath)
        {
            // Arrange
            gitignore = new GitIgnoreParser();
            var directory = Path.GetTempPath();

            // Act
            gitignore.Directory = directory;
            var isIgnored = gitignore.IsIgnored(relativePath);

            Assert.False(isIgnored);
        }

        [Theory]
        [InlineData("some/nestedpath")]
        [InlineData("a/very/deeply/nested/filepath")]
        public void GitIgnoreParser_IsIgnored_ReturnsCorrectValueEvenIfGitRootIsInSomeParentDirectory(string path)
        {
            // Arrange
            var gitRoot = InitializeTempGitRepository();
            var full = Path.Join(gitRoot, path);
            var info = Directory.CreateDirectory(full);

            using (var writer = File.CreateText(Path.Join(gitRoot, ".gitignore")))
            {
                writer.Write("*.cs\n");
            }

            gitignore = new GitIgnoreParser()
            {
                Directory = full,
            };

            // Act
            var result = gitignore.IsIgnored("some/ignored/file.cs");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GitIgnoreParser_IsIgnored_ReturnsTheCorrectValueForIgnoredFiles()
        {
            // Arrange
            var gitRoot = InitializeTempGitRepository();

            using (var writer = File.CreateText(Path.Join(gitRoot, ".gitignore")))
            {
                writer.Write(@"
                    # Advanced patterns
                    [Bb]in/
                    [Bb]uild[Ll]og.*
                    [Ll]og/
                    *.pyc
                    **/Properties/launchSettings.json
                    *.tmp.*.js
                ");
            }

            gitignore = new GitIgnoreParser()
            {
                Directory = gitRoot,
            };

            // Assert
            Assert.True(gitignore.IsIgnored("Bin/somefile.dll"));
            Assert.True(gitignore.IsIgnored("bin/somefile.dll"));
            Assert.True(gitignore.IsIgnored("something/bin/something/test.dll"));
            Assert.True(gitignore.IsIgnored("Buildlog.something.txt"));
            Assert.True(gitignore.IsIgnored("src/docker/watcher.pyc"));
            Assert.True(gitignore.IsIgnored("in/some/nested/folder/Properties/launchSettings.json"));
            Assert.True(gitignore.IsIgnored("test.tmp.backup.js"));

            Assert.False(gitignore.IsIgnored("src/docker/watcher.py"));
            Assert.False(gitignore.IsIgnored("Blog/something/something.c"));
            Assert.False(gitignore.IsIgnored("test/Analog/something.c"));
        }

        private string _gitTmpPath = null;
        public string InitializeTempGitRepository()
        {
            var tmp = Path.GetTempPath();
            var gitroot = Guid.NewGuid().ToString();
            _gitTmpPath = Path.Join(tmp, gitroot);

            Directory.CreateDirectory(_gitTmpPath);
            Repository.Init(_gitTmpPath);

            return _gitTmpPath;
        }

        public void Dispose()
        {
            if (!String.IsNullOrEmpty(_gitTmpPath) && Directory.Exists(_gitTmpPath))
            {
                Directory.Delete(_gitTmpPath, true);
                _gitTmpPath = null;
            }

            gitignore?.Dispose();
        }
    }
}
