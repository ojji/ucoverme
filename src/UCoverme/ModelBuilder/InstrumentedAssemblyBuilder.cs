using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using UCoverme.Model;
using UCoverme.ModelBuilder.Filters;
using UCoverme.Utils;

namespace UCoverme.ModelBuilder
{
    public class InstrumentedAssemblyBuilder
    {
        public string FullyQualifiedAssemblyName { get; }
        public AssemblyPaths AssemblyPaths { get; }

        private readonly Dictionary<int, MethodDefinition> _methodMapping;

        private InstrumentedAssemblyBuilder(AssemblyPaths assemblyPaths, bool shouldReadSymbols)
        {
            AssemblyPaths = assemblyPaths;

            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPaths.OriginalAssemblyPath,
                new ReaderParameters
                {
                    ReadSymbols = shouldReadSymbols
                }))
            {
                FullyQualifiedAssemblyName = assemblyDefinition.FullName;
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

            var methodBuilder = InstrumentedMethodBuilder.Build(methodDefinition);
            return new InstrumentedMethod(
                methodName,
                methodId,
                methodBuilder.Branches,
                methodBuilder.Conditions,
                methodBuilder.SequencePoints,
                methodBuilder.Instructions);
        }

        public static InstrumentedAssembly Build(string assemblyPath, List<IFilter> filters)
        {
            var isInstrumentable = IsInstrumentable(assemblyPath, out var skipReason);
            var assemblyBuilder =
                new InstrumentedAssemblyBuilder(AssemblyPaths.GetAssemblyPaths(assemblyPath), isInstrumentable);

            InstrumentedAssembly instrumentedAssembly;

            if (isInstrumentable)
            {
                instrumentedAssembly = new InstrumentedAssembly(
                    assemblyBuilder.FullyQualifiedAssemblyName,
                    assemblyBuilder.AssemblyPaths,
                    assemblyBuilder.GetFiles(),
                    assemblyBuilder.GetClasses());
            }
            else
            {
                instrumentedAssembly = new InstrumentedAssembly(
                    assemblyBuilder.FullyQualifiedAssemblyName,
                    assemblyBuilder.AssemblyPaths,
                    new InstrumentedFile[0],
                    new InstrumentedClass[0]);
            }

            if (skipReason != SkipReason.NoSkip)
            {
                instrumentedAssembly.SkipFromInstrumentation(skipReason);
                return instrumentedAssembly;
            }

            var exclusionFilters = filters.Where(f => f.FilterType == FilterType.Exclusive);
            foreach (var exclusionFilter in exclusionFilters)
            {
                exclusionFilter.ApplyTo(instrumentedAssembly);
            }

            var inclusionFilters = filters.Where(f => f.FilterType == FilterType.Inclusive);
            foreach (var inclusionFilter in inclusionFilters)
            {
                inclusionFilter.ApplyTo(instrumentedAssembly);
            }

            return instrumentedAssembly;
        }

        private static bool IsInstrumentable(string assemblyPath, out SkipReason skipReason)
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath))
            {
                // its a test framework assembly
                if (TestFrameworkAssemblies.Any(testFrameworkAssemblyName =>
                    assembly.FullName.StartsWith(testFrameworkAssemblyName)))
                {
                    skipReason = SkipReason.TestAssembly;
                    return false;
                }

                // its a temp file, or a file with no symbol files
                if (Path.GetFileNameWithoutExtension(assemblyPath).EndsWith($"{AssemblyPaths.TempFilenameString}"))
                {
                    skipReason = SkipReason.BackupFile;
                    return false;
                }

                if (!File.Exists(Path.ChangeExtension(assemblyPath, "pdb")))
                {
                    skipReason = SkipReason.NoPdb;
                    return false;
                }

                skipReason = SkipReason.NoSkip;
                return true;
            }
        }

        private static HashSet<string> TestFrameworkAssemblies =>
            new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                "NUnit3.TestAdapter"
            };
    }
}