using UCoverme.ModelBuilder.Filters;

namespace UCoverme.Model
{
    public interface ISkipable
    {
        bool IsSkipped { get; }
        SkipReason SkipReason { get; }
        void SkipFromInstrumentation(SkipReason reason);
        void Unskip();
        void ApplyFilter(IFilter filter);
    }
}