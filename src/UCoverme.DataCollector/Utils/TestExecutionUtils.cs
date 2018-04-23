using System;
using System.Diagnostics;
using System.Linq;

namespace UCoverme.DataCollector.Utils
{
    public static class TestExecutionUtils
    {
        public static string GetCurrentMethodFromStacktrace()
        {
            var stackTrace = new StackTrace();
            foreach (var frame in stackTrace.GetFrames())
            {
                var method = frame.GetMethod();
                var methodAttributes = method.GetCustomAttributes(false)
                    .Select(a => a.GetType().FullName)
                    .ToArray();
                
                if (methodAttributes
                    .Any(name => KnownTestAttributes.Contains(name)))
                {
                    return $"{method.ReflectedType.FullName}.{method.Name}";
                }
            }

            throw new InvalidOperationException("Could not find a test attribute in the stack trace.");
        }

        private static readonly string[] KnownTestAttributes =
        {
            "Xunit.FactAttribute",
            "Xunit.TheoryAttribute",
            "NUnit.Framework.TestAttribute",
            "NUnit.Framework.TestCaseAttribute"
        };
    }
}