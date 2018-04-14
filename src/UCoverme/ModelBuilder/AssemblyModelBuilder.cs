using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using UCoverme.Model;
using UCoverme.Utils;

namespace UCoverme.ModelBuilder
{
    public class AssemblyModelBuilder
    {
        public string AssemblyName { get; }
        public AssemblyPaths AssemblyPaths { get; }

        private readonly Dictionary<int, MethodDefinition> _methodMapping;

        private AssemblyModelBuilder(AssemblyPaths assemblyPaths, bool shouldReadSymbols)
        {
            AssemblyPaths = assemblyPaths;

            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPaths.OriginalAssemblyPath,
                new ReaderParameters
                {
                    ReadSymbols = shouldReadSymbols
                }))
            {
                AssemblyName = assemblyDefinition.FullName;
                _methodMapping = new Dictionary<int, MethodDefinition>();
                BuildMethodMappings(assemblyDefinition);
            }
        }

        private void BuildMethodMappings(AssemblyDefinition assemblyDefinition)
        {
            BuildMethodMappings(assemblyDefinition.MainModule.Types);
        }

        private void BuildMethodMappings(IEnumerable<TypeDefinition> typeDefinitions)
        {
            foreach (var methodDefinition
                in typeDefinitions.SelectMany(td =>
                    td.GetInstrumentableMethods()))
            {
                _methodMapping.Add(
                    methodDefinition.MetadataToken.ToInt32(),
                    methodDefinition);
            }
        }

        private InstrumentedFile[] GetFiles()
        {
            return _methodMapping.Values
                .SelectMany(
                    m => m.DebugInformation.SequencePoints,
                    (m, s) => s.Document.Url)
                .Distinct()
                .Select(path => new InstrumentedFile(path))
                .ToArray();
        }

        private InstrumentedClass[] GetClasses()
        {
            return _methodMapping.Values
                .Select(methodDefinition =>
                    new {DeclaringClass = methodDefinition.DeclaringType, Method = methodDefinition})
                .GroupBy(classWithMethods => classWithMethods.DeclaringClass,
                    classWithMethods => classWithMethods.Method)
                .Select(group =>
                {
                    var methods = group.Select(GetInstrumentedMethod).ToArray();

                    return new InstrumentedClass(group.Key.FullName, methods);
                }).ToArray();
        }

        private InstrumentedMethod GetInstrumentedMethod(MethodDefinition methodDefinition)
        {
            var methodName = methodDefinition.FullName;
            var methodId = methodDefinition.MetadataToken.ToInt32();

            var methodGraph = MethodGraph.Build(methodDefinition);
            var sequencePoints = GetSequencePointsForMethod(methodId);
            return new InstrumentedMethod(
                methodName,
                methodId,
                methodGraph.Branches,
                methodGraph.Conditions,
                sequencePoints,
                methodGraph.Instructions);
        }

        private InstrumentedSequencePoint[] GetSequencePointsForMethod(int methodId)
        {
            var sequencePoints = new InstrumentedSequencePoint[0];
            if (_methodMapping.ContainsKey(methodId) && _methodMapping[methodId].DebugInformation.HasSequencePoints)
            {
                sequencePoints = _methodMapping[methodId]
                    .DebugInformation
                    .SequencePoints
                    .Select(sp =>
                        new InstrumentedSequencePoint(
                            sp.Document.Url,
                            sp.Offset,
                            sp.StartLine,
                            sp.EndLine,
                            sp.StartColumn,
                            sp.EndColumn))
                    .OrderBy(sp => sp.Offset)
                    .ToArray();
            }

            return sequencePoints;
        }

        public static AssemblyModel Build(string assemblyPath, bool disableDefaultFilters)
        {
            var shouldReadSymbols = IsInstrumentable(assemblyPath, disableDefaultFilters, out var skipReason);
            var assemblyBuilder =
                new AssemblyModelBuilder(AssemblyPaths.GetAssemblyPaths(assemblyPath), shouldReadSymbols);

            var assemblyModel = new AssemblyModel(
                assemblyBuilder.AssemblyName,
                assemblyBuilder.AssemblyPaths,
                assemblyBuilder.GetFiles(),
                assemblyBuilder.GetClasses());

            if (skipReason != SkipReason.NoSkip)
            {
                assemblyModel.SkipFromInstrumentation(skipReason);
            }

            return assemblyModel;
        }

        private static bool IsInstrumentable(string assemblyPath, bool disableDefaultFilters, out SkipReason skipReason)
        {
            
            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath))
            {
                // its a test framework assembly
                if (TestFrameworkAssemblies.Any(testFrameworkAssemblyName => assembly.FullName.StartsWith(testFrameworkAssemblyName)))
                {
                    skipReason = SkipReason.TestAssembly;
                    return false;
                }

                // its one of the default disabled assemblies
                if (!disableDefaultFilters && MatchesDisabledAssemblies(assembly.FullName))
                {
                    skipReason = SkipReason.Filter;
                    return false;
                }

                // its a temp file, or a file with no symbol files
                if (Path.GetFileNameWithoutExtension(assemblyPath).EndsWith($"{AssemblyPaths.TempFilenameString}") ||
                    !File.Exists(Path.ChangeExtension(assemblyPath, "pdb")))
                {
                    skipReason = SkipReason.NoPdb;
                    return false;
                }

                skipReason = SkipReason.NoSkip;
                return true;
            }
        }

        private static bool MatchesDisabledAssemblies(string fullName)
        {
            var shortAssemblyName = fullName.Split(',').First();
            return DefaultDisabledAssemblies
                .Any(pattern =>
                    pattern.EndsWith(".*")
                        ? shortAssemblyName.StartsWith(pattern.Substring(0, pattern.Length - 1),
                            StringComparison.InvariantCultureIgnoreCase)
                        : shortAssemblyName.Equals(pattern, StringComparison.InvariantCultureIgnoreCase));
        }

        private static HashSet<string> TestFrameworkAssemblies =>
            new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                "NUnit3.TestAdapter"
            };

        private static HashSet<string> DefaultDisabledAssemblies =>
            new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                "mscorlib",
                "System",
                "System.*",
                "Microsoft.*"
            };
    }
}