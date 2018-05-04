using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UCoverme.DataCollector;
using UCoverme.DataCollector.Summary;
using UCoverme.Instrumentation;
using UCoverme.Report;

namespace UCoverme.Model
{
    public class UCovermeProject
    {
        public Guid ProjectId { get; }
        public string ProjectPath { get; }
        public List<InstrumentedAssembly> Assemblies { get; }
        public HashSet<string> DataCollectorAssemblyPaths { get; set; }

        public UCovermeProject(Guid projectId, string projectPath)
        {
            ProjectId = projectId;
            ProjectPath = projectPath;
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
                    Console.WriteLine(
                        $"{model.FullyQualifiedAssemblyName} - {model.AssemblyPaths.OriginalAssemblyPath}");
                }
                else
                {
                    Console.WriteLine(
                        $"{model.FullyQualifiedAssemblyName} - {model.AssemblyPaths.OriginalAssemblyPath}");

                    CopyDataCollectorAssembly(model.AssemblyPaths);
                    var instrumenter = new Instrumenter(model);
                    instrumenter.Instrument(ProjectPath);
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

        public OpenCoverReport GetCoverageReport(IReadOnlyList<TestExecutionSummary> executionSummaries)
        {
            return new OpenCoverReport(this, executionSummaries);
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

        public void WriteToFile()
        {
            var jsonSerializer = new JsonSerializer();
            using (var sw = new StreamWriter(File.Create(ProjectPath)))
            using (var jsonWriter = new JsonTextWriter(sw))
            {
                jsonSerializer.Serialize(jsonWriter, this);
            }

            Console.WriteLine($"Project file created: {ProjectPath}");
        }
    }
}