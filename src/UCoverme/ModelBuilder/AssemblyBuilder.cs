using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Mono.Cecil;
using UCoverme.Model;
using UCoverme.ModelBuilder.Filters;
using UCoverme.Utils;

namespace UCoverme.ModelBuilder
{
    public class AssemblyBuilder
    {
        public string FullyQualifiedAssemblyName { get; }
        public AssemblyPaths AssemblyPaths { get; }
        public string AssemblyHash { get; }
        public InstrumentedFile[] Files { get; set; }
        public InstrumentedClass[] Classes { get; set; }

        private readonly Dictionary<int, MethodDefinition> _methodMapping;
        private static int _assemblyId = 0;

        private AssemblyBuilder(AssemblyPaths assemblyPaths, bool shouldReadSymbols)
        {
            AssemblyPaths = assemblyPaths;
            AssemblyHash = GetAssemblyHash(assemblyPaths.OriginalAssemblyPath);

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

            if (shouldReadSymbols)
            {
                Files = GetFiles();
                Classes = GetClasses();
            }
            else
            {
                Files = new InstrumentedFile[0];
                Classes = new InstrumentedClass[0];
            }
        }

        private string GetAssemblyHash(string assemblyPath)
        {
            using (var reader = new StreamReader(assemblyPath))
            using (var shaProvider = new SHA1CryptoServiceProvider())
            {
                return BitConverter.ToString(shaProvider.ComputeHash(reader.BaseStream));
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

            var fileInMethod = methodDefinition.DebugInformation.SequencePoints.Select(sp => sp.Document.Url).Distinct().SingleOrDefault();

            var fileId = fileInMethod == null ? (int?) null : Files.Single(f => f.Path == fileInMethod).Id;

            var methodBuilder = MethodBuilder.Build(methodDefinition, fileId);
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
                new AssemblyBuilder(AssemblyPaths.GetAssemblyPaths(assemblyPath), isInstrumentable);

            var instrumentedAssembly = new InstrumentedAssembly(
                _assemblyId++,
                assemblyBuilder.FullyQualifiedAssemblyName,
                assemblyBuilder.AssemblyPaths,
                assemblyBuilder.AssemblyHash,
                assemblyBuilder.Files,
                assemblyBuilder.Classes);

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