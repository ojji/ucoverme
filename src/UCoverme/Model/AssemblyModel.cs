namespace UCoverme.Model
{
    public class AssemblyModel : ISkipable
    {
        public string AssemblyName { get; }
        public AssemblyPaths AssemblyPaths { get; }
        public InstrumentedFile[] Files { get; }
        public InstrumentedClass[] Classes { get; }
        public bool IsSkipped => SkipReason != SkipReason.NoSkip;
        public SkipReason SkipReason { get; private set; }


        public AssemblyModel(string assemblyName, AssemblyPaths assemblyPaths, InstrumentedFile[] files,
            InstrumentedClass[] classes)
        {
            AssemblyName = assemblyName;
            AssemblyPaths = assemblyPaths;
            Files = files;
            Classes = classes;
        }

        public override string ToString()
        {
            return $"{AssemblyName}";
        }

        public void SkipFromInstrumentation(SkipReason reason)
        {
            SkipReason = reason;
        }
    }
}