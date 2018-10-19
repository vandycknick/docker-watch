using System.IO;
using System.Text;

namespace DockerWatch.Test.Extensions
{
    public static class StringExtensions
    {
        public static Stream ToStream(this string text)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text ?? ""));
        }
    }
}
