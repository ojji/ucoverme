using System.IO;

namespace UCoverme.DataCollector.Utils
{
    public static class LogExtensions {
        public static void Log(this string path, string message)
        {
            File.AppendAllText(path, $"{message}\n");
        }

        public static void Empty(this string path)
        {
            File.WriteAllText(path, "");
        }
    }
}