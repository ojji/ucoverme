using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using ProtoBuf;
using UCoverme.DataCollector.Summary;
using UCoverme.Model;
using UCoverme.Report;

namespace UCoverme.Commands
{
    public class CreateReportCommand : UCovermeCommand
    {
        public override string Name => "createreport";
        public override string Description => "Collect the tests sample data and generate a report.";

        private int _moduleCounter;

        public override void Execute()
        {
            var coverageDirectory = GetCoverageDirectory();
            var outputFile = Path.Combine(coverageDirectory, "opencover-report.xml");

            var projectsWithSummaries = GetProjectsWithSummaries(coverageDirectory);

            foreach (var projectWithSummaries in projectsWithSummaries)
            {
                var coverageReport = projectWithSummaries.Key.GetCoverageReport(projectWithSummaries.Value);

                XDocument document = new XDocument(new XDeclaration("1.0", "utf-8", ""));
                XElement coverageSessionElement = new XElement(XName.Get("CoverageSession"));

                var projectSummary = coverageReport.GetSummaryForProject();
                coverageSessionElement.Add(CreateSummaryElement(projectSummary));

                XElement modules = new XElement(XName.Get("Modules"));

                foreach (var module in coverageReport.Project.Assemblies)
                {
                    _moduleCounter = 0;
                    XElement moduleElement = CreateModuleElement(coverageReport, module);
                    modules.Add(moduleElement);
                }

                coverageSessionElement.Add(modules);
                document.Add(coverageSessionElement);

                using (var file = File.Create(outputFile))
                {
                    document.Save(file);
                }
            }
        }

        private XElement CreateSummaryElement(Summary summary)
        {
            return new XElement(
                XName.Get("Summary"),
                new XAttribute(XName.Get("numSequencePoints"), summary.NumSequencePoints),
                new XAttribute(XName.Get("visitedSequencePoints"), summary.VisitedSequencePoints),
                new XAttribute(XName.Get("numBranchPoints"), summary.NumBranchPoints),
                new XAttribute(XName.Get("visitedBranchPoints"), summary.VisitedBranchPoints),
                new XAttribute(XName.Get("sequenceCoverage"), summary.SequenceCoverage),
                new XAttribute(XName.Get("branchCoverage"), summary.BranchCoverage),
                new XAttribute(XName.Get("maxCyclomaticComplexity"), summary.MaxCyclomaticComplexity),
                new XAttribute(XName.Get("minCyclomaticComplexity"), summary.MinCyclomaticComplexity),
                new XAttribute(XName.Get("visitedClasses"), summary.VisitedClasses),
                new XAttribute(XName.Get("numClasses"), summary.NumClasses),
                new XAttribute(XName.Get("visitedMethods"), summary.VisitedMethods),
                new XAttribute(XName.Get("numMethods"), summary.NumMethods)
                );
        }

        private XElement CreateModuleElement(CoverageReport report, InstrumentedAssembly assembly)
        {
            var moduleElement = new XElement(XName.Get("Module"),
                            new XAttribute(
                                XName.Get("hash"),
                                assembly.Hash));

            moduleElement.Add(new XElement(XName.Get("ModulePath"), assembly.AssemblyPaths.OriginalAssemblyPath));
            // todo: fix this
            moduleElement.Add(new XElement(XName.Get("ModuleTime"), DateTime.Now.ToString("o")));
            moduleElement.Add(new XElement(XName.Get("ModuleName"), assembly.FullyQualifiedAssemblyName.Split(',').First()));

            if (assembly.IsSkipped)
            {
                moduleElement.Add(new XAttribute(XName.Get("skippedDueTo"), assembly.SkipReason.ToString()));
            }
            else
            {
                moduleElement.Add(CreateSummaryElement(report.GetSummaryForAssembly(assembly)));
                moduleElement.Add(CreateFilesElement(assembly.Files));
            }

            var classesElement = new XElement(XName.Get("Classes"));
            if (!assembly.IsSkipped)
            {
                foreach (var instrumentedClass in assembly.Classes)
                {
                    var classElement = CreateClassElement(report, instrumentedClass);
                    classesElement.Add(classElement);
                }
            }
            
            moduleElement.Add(classesElement);

            return moduleElement;
        }

        private XElement CreateFilesElement(InstrumentedFile[] files)
        {
            var filesElement = new XElement(XName.Get("Files"));

            foreach (var file in files)
            {
                filesElement.Add(
                    new XElement(
                        XName.Get("File"), 
                        new XAttribute(XName.Get("uid"), file.Id),
                        new XAttribute(XName.Get("fullPath"), file.Path)));
            }

            return filesElement;
        }

        private XElement CreateClassElement(CoverageReport report, InstrumentedClass instrumentedClass)
        {
            var classElement = new XElement(XName.Get("Class"));
            classElement.Add(CreateSummaryElement(report.GetSummaryForClass(instrumentedClass)));
            classElement.Add(new XElement(XName.Get("FullName"), instrumentedClass.Name));
            
            var methodsElement = new XElement(XName.Get("Methods"));
            foreach (var method in instrumentedClass.Methods)
            {
                methodsElement.Add(CreateMethodElement(report, method));
            }

            classElement.Add(methodsElement);
            return classElement;
        }

        private XElement CreateMethodElement(CoverageReport report, InstrumentedMethod method)
        {
            var summary = report.GetSummaryForMethod(method);

            var methodElement = new XElement(XName.Get("Method"));
            // todo: method attributes
            methodElement.Add(new XAttribute(XName.Get("visited"), summary.VisitedMethods == 1 ? "true" : "false"));
            methodElement.Add(new XAttribute(XName.Get("cyclomaticComplexity"), summary.MinCyclomaticComplexity));
            methodElement.Add(new XAttribute(XName.Get("nPathComplexity"), CalculateNPathComplexity(method)));
            methodElement.Add(new XAttribute(XName.Get("sequenceCoverage"), summary.SequenceCoverage));
            methodElement.Add(new XAttribute(XName.Get("branchCoverage"), summary.BranchCoverage));
            var isConstructor = method.Name.EndsWith("::.ctor()") || method.Name.EndsWith("::.cctor()");
            methodElement.Add(new XAttribute(XName.Get("isConstructor"), 
                isConstructor ? "true" : "false"));
            methodElement.Add(new XAttribute(XName.Get("isStatic"), "false")); // todo
            methodElement.Add(new XAttribute(XName.Get("isGetter"), "false")); // todo
            methodElement.Add(new XAttribute(XName.Get("isSetter"), "false")); // todo
            // ----

            methodElement.Add(CreateSummaryElement(summary));
            methodElement.Add(new XElement(XName.Get("MetadataToken"), method.MethodId));
            methodElement.Add(new XElement(XName.Get("Name"), method.Name));

            if (method.HasVisibleSequencePoint())
            {
                methodElement.Add(new XElement(XName.Get("FileRef"),
                new XAttribute(XName.Get("uid"), method.SequencePoints.First(sp => sp.FileId.HasValue).FileId)));
            }

            var sequencePointsElement = new XElement(XName.Get("SequencePoints"));

            int localCounter = 0;
            int methodPointId = -1;
            foreach (var sequencePoint in method.SequencePoints.Where(sequencePoint => !sequencePoint.IsHidden).OrderBy(sp => sp.StartOffset))
            {
                _moduleCounter++;
                if (localCounter == 0)
                {
                    methodPointId = _moduleCounter;
                }
                var sequencePointElement = new XElement(
                    XName.Get("SequencePoint"),
                    new XAttribute(XName.Get("vc"), sequencePoint.VisitCount),
                    new XAttribute(XName.Get("uspid"), _moduleCounter),
                    new XAttribute(XName.Get("ordinal"), localCounter++),
                    new XAttribute(XName.Get("offset"), sequencePoint.StartOffset),
                    new XAttribute(XName.Get("sl"), sequencePoint.StartLine),
                    new XAttribute(XName.Get("sc"), sequencePoint.StartColumn),
                    new XAttribute(XName.Get("el"), sequencePoint.EndLine),
                    new XAttribute(XName.Get("ec"), sequencePoint.EndColumn),
                    new XAttribute(XName.Get("bec"), 0), // TODO
                    new XAttribute(XName.Get("bev"), 0), // TODO
                    new XAttribute(XName.Get("fileid"), sequencePoint.FileId));

                sequencePointsElement.Add(sequencePointElement);
            }
            methodElement.Add(sequencePointsElement);

            // todo branching points
            var branchPointsElement = new XElement(XName.Get("BranchPoints"));
            methodElement.Add(branchPointsElement);

            XElement methodPointElement;
            var methodPoint = method.SequencePoints.FirstOrDefault();

            if (methodPoint != null && !methodPoint.IsHidden)
            {
                // TODO xsi:type
                methodPointElement = new XElement(
                    XName.Get("MethodPoint"),
                    new XAttribute(XName.Get("vc"), methodPoint.VisitCount),
                    new XAttribute(XName.Get("uspid"), methodPointId),
                    new XAttribute(XName.Get("ordinal"), 0),
                    new XAttribute(XName.Get("offset"), methodPoint.StartOffset),
                    new XAttribute(XName.Get("sl"), methodPoint.StartLine),
                    new XAttribute(XName.Get("sc"), methodPoint.StartColumn),
                    new XAttribute(XName.Get("el"), methodPoint.EndLine),
                    new XAttribute(XName.Get("ec"), methodPoint.EndColumn),
                    new XAttribute(XName.Get("bec"), 0), // TODO
                    new XAttribute(XName.Get("bev"), 0), // TODO
                    new XAttribute(XName.Get("fileid"), methodPoint.FileId)
                    );
            }
            else
            {
                methodPointElement = new XElement(
                    XName.Get("MethodPoint"),
                    new XAttribute(XName.Get("vc"), methodPoint?.VisitCount ?? method.VisitCount),
                    new XAttribute(XName.Get("uspid"), ++_moduleCounter),
                    new XAttribute(XName.Get("ordinal"), 0),
                    new XAttribute(XName.Get("offset"), 0)
                    );
            }
            methodElement.Add(methodPointElement);

            return methodElement;
        }

        private int CalculateNPathComplexity(InstrumentedMethod method)
        {
            // todo
            return 0;
        }

        private Dictionary<UCovermeProject, List<TestExecutionSummary>> GetProjectsWithSummaries(
            string coverageDirectory)
        {
            var testExecutionFiles = Directory.EnumerateFiles(coverageDirectory, "*.ucovermetest");
            var summariesDictionary = new Dictionary<string, (UCovermeProject project, List<TestExecutionSummary> testExecutionSummaries)>();

            foreach (var testExecutionFile in testExecutionFiles)
            {
                using (var reader = File.OpenRead(testExecutionFile))
                {
                    var summary = Serializer.Deserialize<TestExecutionSummary>(reader);
                    var projectFile = summary.ProjectPath;

                    if (!summariesDictionary.ContainsKey(projectFile))
                    {
                        var jsonSerializer = new JsonSerializer();

                        using (var file = new StreamReader(File.OpenRead(projectFile)))
                        using (var jsonReader = new JsonTextReader(file))
                        {
                            var project = jsonSerializer.Deserialize<UCovermeProject>(jsonReader);
                            summariesDictionary.Add(projectFile, (project, new List<TestExecutionSummary> { summary }));
                        }
                    }
                    else
                    {
                        summariesDictionary[projectFile].testExecutionSummaries.Add(summary);
                    }
                }
            }
            return summariesDictionary.ToDictionary(d => d.Value.project, d => d.Value.testExecutionSummaries);
        }
    }
}