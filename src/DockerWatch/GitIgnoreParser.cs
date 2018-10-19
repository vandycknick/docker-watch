using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DockerWatch
{
    public class GitIgnoreParser
    {
        public static GitIgnoreParser Compile(string filePath)
        {
            if (File.Exists(filePath))
            {
                using (var stream = File.OpenRead(filePath))
                    return Compile(stream);

            }
            return new GitIgnoreParser(Array.Empty<GitIgnoreEntry>());
        }

        public static GitIgnoreParser Compile(Stream stream)
        {
            string line;
            var ignores = new List<GitIgnoreEntry>();

            using (var reader = new StreamReader(stream))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith('#') || String.IsNullOrEmpty(line))
                        continue;

                    var isNegative = line.StartsWith('!');
                    var pattern = new Regex(
                        line.Replace("/", "\\/").Replace(".", "\\.").Replace("**", "(.+)").Replace("*", "([^\\/]+)"),
                        RegexOptions.Singleline
                    );

                    ignores.Add(new GitIgnoreEntry()
                    {
                        Pattern = pattern,
                        IsNegative = isNegative,
                    });
                }

                return new GitIgnoreParser(ignores.ToArray());
            }
        }

        private GitIgnoreEntry[] _ignores;

        private GitIgnoreParser(GitIgnoreEntry[] gitignorePath)
        {
            _ignores = gitignorePath;
        }

        public bool IsIgnored(string path)
        {
            Func<GitIgnoreEntry, bool> predicate = i => MatchesGitIgnorePath(i, path);
            var value = _ignores.FirstOrDefault(predicate);
            return value != null;
        }

        private bool MatchesGitIgnorePath(GitIgnoreEntry ignore, string path)
        {
            return ignore.IsNegative ? false : ignore.Pattern.IsMatch(path);
        }
    }

    class GitIgnoreEntry
    {
        public Regex Pattern { get; set; }
        public bool IsNegative { get; set; } = false;
    }
}
