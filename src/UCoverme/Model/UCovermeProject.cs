using System;
using System.Collections.Generic;
using System.IO;
using UCoverme.DataCollector;
using UCoverme.Instrumentation;

namespace UCoverme.Model
{
    public class UCovermeProject
    {
        public Guid ProjectId { get; }
        public string CoverageDirectory { get; }
        public List<InstrumentedAssembly> Assemblies { get; }
        public HashSet<string> DataCollectorAssemblyPaths { get; set; }

        public UCovermeProject(Guid projectId)
        {
            ProjectId = projectId;
            CoverageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "coverage");
            Assemblies = new List<InstrumentedAssembly>();
            DataCollectorAssemblyPaths = new HashSet<string>();
        }

        public void AddAssembly(InstrumentedAssembly assembly)
        {
            Assemblies.Add(assembly);
        }

        public void Instrument()
        {
            foreach (var model in Assemblies)
            {
                if (model.IsSkipped)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"[DISABLED - {model.SkipReason.ToString()}] ");
                    Console.ResetColor();
                    Console.WriteLine($"{model.FullyQualifiedAssemblyName} - {model.AssemblyPaths.OriginalAssemblyPath}");
                }
                else
                {
                    Console.WriteLine($"{model.FullyQualifiedAssemblyName} - {model.AssemblyPaths.OriginalAssemblyPath}");

                    CopyDataCollectorAssembly(model.AssemblyPaths);
                    var instrumenter = new Instrumenter(model);
                    instrumenter.Instrument();
                }
            }
        }

        public void Uninstrument()
        {
            foreach (var model in Assemblies)
            {
                if (!model.IsSkipped)
                {
                    Console.WriteLine($"Uninstrumenting assembly - {model.AssemblyPaths.OriginalAssemblyPath}");

                    var uninstrumenter = new Uninstrumenter(model);
                    uninstrumenter.Uninstrument();
                }
            }

            DeleteDataCollectorAssemblies();
        }

        private void DeleteDataCollectorAssemblies()
        {
            foreach (var dataCollectorAssemblyPath in DataCollectorAssemblyPaths)
            {
                File.Delete(dataCollectorAssemblyPath);
            }
        }

        private void CopyDataCollectorAssembly(AssemblyPaths assemblyPaths)
        {
            var dataCollectorAssemblyPath = typeof(UCovermeDataCollector).Assembly.Location;
            var outputDirectory = Path.GetDirectoryName(assemblyPaths.OriginalAssemblyPath);
            var outputPath = Path.Combine(outputDirectory,
                Path.GetFileName(dataCollectorAssemblyPath));
            File.Copy(dataCollectorAssemblyPath,
                outputPath,
                true);
            if (!DataCollectorAssemblyPaths.Contains(outputPath))
            {
                DataCollectorAssemblyPaths.Add(outputPath);
            }
        }
    }
}