using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UCoverme.ModelBuilder.Filters;

namespace UCoverme.Model
{
    public class InstrumentedAssembly : ISkipable
    {
        public string FullyQualifiedAssemblyName { get; }
        public AssemblyPaths AssemblyPaths { get; }
        public InstrumentedFile[] Files { get; }
        public InstrumentedClass[] Classes { get; }

        public bool IsSkipped => SkipReason != SkipReason.NoSkip;

        [JsonConverter(typeof(StringEnumConverter))]
        public SkipReason SkipReason { get; private set; }

        [JsonConstructor]
        private InstrumentedAssembly(string fullyQualifiedAssemblyName, AssemblyPaths assemblyPaths,
            InstrumentedFile[] files,
            InstrumentedClass[] classes,
            SkipReason skipReason)
        {
            FullyQualifiedAssemblyName = fullyQualifiedAssemblyName;
            AssemblyPaths = assemblyPaths;
            Files = files;
            Classes = classes;
            SkipReason = skipReason;
        }

        public InstrumentedAssembly(string fullyQualifiedAssemblyName, AssemblyPaths assemblyPaths,
            InstrumentedFile[] files,
            InstrumentedClass[] classes)
        {
            FullyQualifiedAssemblyName = fullyQualifiedAssemblyName;
            AssemblyPaths = assemblyPaths;
            Files = files;
            Classes = classes;
            SkipReason = SkipReason.NoSkip;
        }

        public override string ToString()
        {
            return $"{FullyQualifiedAssemblyName}";
        }

        public void SkipFromInstrumentation(SkipReason reason)
        {
            SkipReason = reason;
        }

        public void Unskip()
        {
            SkipReason = SkipReason.NoSkip;
        }

        public void ApplyFilter(IFilter filter)
        {
            if (filter is AssemblyFilter assemblyFilter &&
                assemblyFilter.MatchesAssemblyName(FullyQualifiedAssemblyName.Split(',').First()))
            {
                switch (assemblyFilter.FilterType)
                {
                    case FilterType.Exclusive when !ShouldSkipWholeAssembly(assemblyFilter):
                        break;
                    case FilterType.Exclusive when ShouldSkipWholeAssembly(assemblyFilter):
                    {
                        if (!IsSkipped)
                        {
                            SkipFromInstrumentation(SkipReason.Filter);
                        }
                        break;
                    }
                    case FilterType.Inclusive:
                        Unskip();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                foreach (var instrumentedClass in Classes)
                {
                    instrumentedClass.ApplyFilter(filter);
                }
            }
        }

        private bool ShouldSkipWholeAssembly(AssemblyFilter assemblyFilter)
        {
            return assemblyFilter.TypenameFilterText == "*";
        }
    }
}