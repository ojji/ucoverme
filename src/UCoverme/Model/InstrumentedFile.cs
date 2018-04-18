using System.Threading;
using Newtonsoft.Json;

namespace UCoverme.Model
{
    public class InstrumentedFile
    {
        public int Id { get; }
        public string Path { get; }

        [JsonConstructor]
        private InstrumentedFile(int id, string path)
        {
            Id = id;
            Path = path;
        }

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