using System;
using System.Linq;
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
        public SkipReason SkipReason { get; private set; }

        public InstrumentedAssembly(string fullyQualifiedAssemblyName, AssemblyPaths assemblyPaths,
            InstrumentedFile[] files,
            InstrumentedClass[] classes)
        {
            FullyQualifiedAssemblyName = fullyQualifiedAssemblyName;
            AssemblyPaths = assemblyPaths;
            Files = files;
            Classes = classes;
            SkipReason = SkipReason.NoSkip;
            foreach (var instrumentedClass in classes)
            {
                instrumentedClass.SetContainingAssembly(this);
            }
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