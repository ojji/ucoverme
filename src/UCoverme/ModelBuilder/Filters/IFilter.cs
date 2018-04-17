using UCoverme.Model;

namespace UCoverme.ModelBuilder.Filters
{
    public interface IFilter
    {
        FilterType FilterType { get; }
        void ApplyTo(ISkipable skipable);
    }
}