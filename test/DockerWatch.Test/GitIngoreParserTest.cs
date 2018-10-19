using DockerWatch.Test.Extensions;
using Xunit;

namespace DockerWatch.Test
{
    public class GitIngoreParserTest
    {
        [Fact]
        public void IsIgnored_ReturnsTrueForIgnoredFiles()
        {
            // Arrange
            string ignoreFile = @"
                # This is a comment
                bin
                hello/test.js
            ";
            var stream = ignoreFile.ToStream();

            // Act
            var gitignore = GitIgnoreParser.Compile(stream);

            // Assert
            Assert.True(gitignore.IsIgnored("hello/test.js"));
            Assert.True(gitignore.IsIgnored("bin/somefile.dll"));
            Assert.True(gitignore.IsIgnored("bin/Debug/resource.txt"));
        }

        [Fact]
        public void IsIgnored_ReturnsFalseForFileThatAreNotIgnored()
        {
            // Arrange
            string ignoreFile = @"
                # This is a comment
                bin
                hello/test.js
            ";
            var stream = ignoreFile.ToStream();

            // Act
            var gitignore = GitIgnoreParser.Compile(stream);

            // Assert
            Assert.False(gitignore.IsIgnored("server/index.js"));
            Assert.False(gitignore.IsIgnored("src/Magic/Program.cs"));
            Assert.False(gitignore.IsIgnored("src/Resources"));
        }

        [Fact]
        public void IsIngore_ReturnsCorrectResultForAdvancedGitPatterns()
        {
            // Arrange
            string ignoreFile = @"
                # Advanced patterns
                [Bb]in/
                [Bb]uild[Ll]og.*
                *.pyc
                **/Properties/launchSettings.json
                *.tmp.*.js
            ";
            var stream = ignoreFile.ToStream();

            // Act
            var gitignore = GitIgnoreParser.Compile(stream);

            // Assert
            Assert.True(gitignore.IsIgnored("Bin/somefile.dll"));
            Assert.True(gitignore.IsIgnored("bin/somefile.dll"));

            Assert.True(gitignore.IsIgnored("Buildlog.something.txt"));

            Assert.True(gitignore.IsIgnored("src/docker/watcher.pyc"));
            Assert.False(gitignore.IsIgnored("src/docker/watcher.py"));

            Assert.True(gitignore.IsIgnored("in/some/nested/folder/Properties/launchSettings.json"));

            Assert.True(gitignore.IsIgnored("test.tmp.backup.js"));
        }
    }
}
