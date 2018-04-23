using System.IO;

namespace UCoverme.DataCollector
{
    public class TestExecutionSummary
    {
        public static void WriteToFile(TestExecutionData data)
        {
            using (var writer = new StreamWriter(File.Open(data.GetTestCaseFilename(), FileMode.Create)))
            {
                foreach (var executionEvent in data.TestCaseEvents)
                {
                    writer.WriteLine($"[{data.TestCaseId} - {data.TestCaseName}] - {executionEvent}");
                }

                writer.WriteLine("\n--- Method executions ---");
                foreach (var method in data.MethodsExecuted)
                {
                    foreach (var executionEvent in method.ExecutionEvents)
                    {
                        writer.WriteLine($"[{method.MethodId}] - {executionEvent}");
                    }
                    writer.Write("\n\n");
                }
            }
        }
    }
}