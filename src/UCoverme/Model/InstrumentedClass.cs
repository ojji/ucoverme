using System;
using UCoverme.ModelBuilder.Filters;

namespace UCoverme.Model
{
    public class InstrumentedClass : ISkipable
    {
        public InstrumentedAssembly ContainingAssembly { get; private set; }
        public string Name { get; }
        public InstrumentedMethod[] Methods { get; }

        public bool IsSkipped => SkipReason != SkipReason.NoSkip;
        public SkipReason SkipReason { get; private set; }

        public InstrumentedClass(string name, InstrumentedMethod[] methods)
        {
            Name = name;
            Methods = methods;
            SkipReason = SkipReason.NoSkip;

            foreach (var method in methods)
            {
                method.SetContainingClass(this);
            }
        }

        public void SetContainingAssembly(InstrumentedAssembly instrumentedAssembly)
        {
            ContainingAssembly = instrumentedAssembly;
        }

        public override string ToString()
        {
            return $"{Name}";
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
            if (filter is AssemblyFilter assemblyFilter && assemblyFilter.MatchesTypeName(Name))
            {
                switch (assemblyFilter.FilterType)
                {
                    case FilterType.Exclusive:
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
            }
        }
    }
}