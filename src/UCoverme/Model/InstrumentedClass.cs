using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UCoverme.ModelBuilder.Filters;

namespace UCoverme.Model
{
    public class InstrumentedClass : ISkipable
    {
        public string Name { get; }
        public InstrumentedMethod[] Methods { get; }

        public bool IsSkipped => SkipReason != SkipReason.NoSkip;

        [JsonConverter(typeof(StringEnumConverter))]
        public SkipReason SkipReason { get; private set; }

        [JsonConstructor]
        private InstrumentedClass(string name, InstrumentedMethod[] methods, SkipReason skipReason)
        {
            Name = name;
            Methods = methods;
            SkipReason = skipReason;
        }

        public InstrumentedClass(string name, InstrumentedMethod[] methods)
        {
            Name = name;
            Methods = methods;
            SkipReason = SkipReason.NoSkip;
        }

        public override string ToString()
        {
            return $"{Name}";
        }

        public void SkipFromInstrumentation(SkipReason reason)
        {
            SkipReason = reason;
            foreach (var method in Methods)
            {
                method.SkipFromInstrumentation(reason);
            }
        }

        public void Unskip()
        {
            SkipReason = SkipReason.NoSkip;
            foreach (var method in Methods)
            {
                method.Unskip();
            }
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