using System;

namespace UCoverme.Model
{
    public class InstrumentedClass : ISkipable
    {
        public string Name { get; }
        public InstrumentedMethod[] Methods { get; }

        public InstrumentedClass(string name, InstrumentedMethod[] methods)
        {
            Name = name;
            Methods = methods;
        }

        public override string ToString()
        {
            return $"{Name}";
        }

        public bool IsSkipped => SkipReason != SkipReason.NoSkip;
        public SkipReason SkipReason { get; private set; }

        public void SkipFromInstrumentation(SkipReason reason)
        {
            throw new NotImplementedException();
        }
    }
}