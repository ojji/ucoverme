using System;
using System.Linq;

namespace UCoverme.DataCollector.DataCollectors
{
    public static class DataCollectors
    {
        public static IDataCollector CreateDataCollector()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(a => a.FullName).ToArray();
            
            if (assemblies.Any(a => a.StartsWith("NUnit3.TestAdapter")))
            {
                return new NUnitDataCollector();
            }
            if (assemblies.Any(a => a.StartsWith("xunit.abstractions")))
            {
                return new XUnitDataCollector();
            }

            throw new UnsupportedFrameworkException("Could not find a suitable test execution datacollector.");
        }
    }
}