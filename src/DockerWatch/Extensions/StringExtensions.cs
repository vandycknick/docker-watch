using System.Text.RegularExpressions;

namespace DockerWatch.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Compares the string against a given pattern.
        /// Stolen from: https://stackoverflow.com/questions/188892/glob-pattern-matching-in-net
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="pattern">The pattern to match, where "*" means any sequence of characters, and "?" means any single character.</param>
        /// <returns><c>true</c> if the string matches the given pattern; otherwise <c>false</c>.</returns>
        public static bool MatchesGlob(this string str, string pattern)
        {
            return new Regex(
                "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            ).IsMatch(str);
        }
    }
}
