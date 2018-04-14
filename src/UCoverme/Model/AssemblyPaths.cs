using System.IO;
using System.Runtime;

namespace UCoverme.Model
{
    public class AssemblyPaths
    {
        public string TempAssemblyPath { get; }
        public string TempPdbPath { get; }
        public string OriginalAssemblyPath { get; }
        public string OriginalPdbPath { get; }

        public const string TempFilenameString = ".ucovermebackup";

        private AssemblyPaths(string originalAssemblyPath, string originalPdbPath, string tempAssemblyPath, string tempPdbPath)
        {
            TempAssemblyPath = tempAssemblyPath;
            TempPdbPath = tempPdbPath;
            OriginalAssemblyPath = originalAssemblyPath;
            OriginalPdbPath = originalPdbPath;
        }

        public static AssemblyPaths GetAssemblyPaths(string assemblyPath)
        {
            var originalAssemblyPath = assemblyPath;
            var originalPdbPath = Path.ChangeExtension(originalAssemblyPath, "pdb");
            var directory = Path.GetDirectoryName(originalAssemblyPath);

            var tempAssemblyPath = Path.Combine(
                directory,
                $"{Path.GetFileNameWithoutExtension(originalAssemblyPath)}{TempFilenameString}{Path.GetExtension(originalAssemblyPath)}");
            var tempPdbPath = Path.ChangeExtension(tempAssemblyPath, "pdb");

            return new AssemblyPaths(
                originalAssemblyPath, 
                File.Exists(originalPdbPath) ? originalPdbPath : string.Empty,
                tempAssemblyPath,
                File.Exists(originalPdbPath) ? tempPdbPath : string.Empty);
        }
    }
}