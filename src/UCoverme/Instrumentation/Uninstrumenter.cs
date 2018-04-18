using System;
using System.IO;
using UCoverme.Model;

namespace UCoverme.Instrumentation
{
    public class Uninstrumenter
    {
        private readonly InstrumentedAssembly _assembly;

        public Uninstrumenter(InstrumentedAssembly assembly)
        {
            _assembly = assembly;
        }

        public void Uninstrument()
        {
            RestoreOriginalAssembliesFromBackup();
        }

        private void RestoreOriginalAssembliesFromBackup()
        {
            if (!File.Exists(_assembly.AssemblyPaths.TempAssemblyPath))
            {
                throw new InvalidOperationException($"Cannot find the original assembly: {_assembly.AssemblyPaths.TempAssemblyPath}");
            }
            if (!File.Exists(_assembly.AssemblyPaths.TempPdbPath))
            {
                throw new InvalidOperationException($"Cannot find the original symbol file: {_assembly.AssemblyPaths.TempPdbPath}");
            }

            File.Copy(
                _assembly.AssemblyPaths.TempAssemblyPath,
                _assembly.AssemblyPaths.OriginalAssemblyPath,
                true);

            File.Copy(
                _assembly.AssemblyPaths.TempPdbPath,
                _assembly.AssemblyPaths.OriginalPdbPath,
                true);

            File.Delete(_assembly.AssemblyPaths.TempAssemblyPath);
            File.Delete(_assembly.AssemblyPaths.TempPdbPath);
        }
    }
}