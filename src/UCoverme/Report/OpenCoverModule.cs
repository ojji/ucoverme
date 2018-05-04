using System;
using System.Linq;
using UCoverme.Model;

namespace UCoverme.Report
{
    public class OpenCoverModule
    {
        public Summary Summary { get; }
        public string Hash { get; }
        public string ModulePath { get; }
        public string ModuleTime { get; }
        public string ModuleName { get; }
        public bool IsSkipped => SkipReason != SkipReason.NoSkip;
        public SkipReason SkipReason { get; }
        public InstrumentedFile[] Files { get; }
        public OpenCoverClass[] Classes { get; }

        public OpenCoverModule(OpenCoverReport report, InstrumentedAssembly assembly)
        {
            Hash = assembly.Hash;
            ModulePath = assembly.AssemblyPaths.OriginalAssemblyPath;
            ModuleTime = DateTime.Now.ToString("o"); // todo fix this
            SkipReason = assembly.SkipReason;
            ModuleName = assembly.FullyQualifiedAssemblyName.Split(',')[0];
            if (!assembly.IsSkipped)
            {
                Summary = report.GetSummaryForAssembly(assembly);
                Files = assembly.Files;
                Classes = assembly
                    .Classes
                    .Select(instrumentedClass =>
                        new OpenCoverClass(report, instrumentedClass))
                    .ToArray();
            }
        }
    }
}