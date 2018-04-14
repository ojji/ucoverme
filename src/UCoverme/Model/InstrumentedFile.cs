using System.Threading;

namespace UCoverme.Model
{
    public class InstrumentedFile
    {
        public int Id { get; }
        public string Path { get; }

        public InstrumentedFile(string path)
        {
            Id = Interlocked.Increment(ref _globalId);
            Path = path;
        }

        public override string ToString()
        {
            return $"{Id} - {Path}";
        }

        private static int _globalId;
    }
}