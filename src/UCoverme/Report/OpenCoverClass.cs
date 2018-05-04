using System.Linq;
using UCoverme.Model;

namespace UCoverme.Report
{
    public class OpenCoverClass
    {
        public Summary Summary { get; }
        public string Name { get; }
        public OpenCoverMethod[] Methods { get; }

        public OpenCoverClass(OpenCoverReport report, InstrumentedClass instrumentedClass)
        {
            Summary = report.GetSummaryForClass(instrumentedClass);
            Name = instrumentedClass.Name;
            Methods = instrumentedClass
                .Methods
                .Select(method => 
                    new OpenCoverMethod(report, method))
                .ToArray();
        }
    }
}