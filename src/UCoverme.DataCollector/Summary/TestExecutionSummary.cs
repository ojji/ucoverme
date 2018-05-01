using System;
using ProtoBuf;
using UCoverme.DataCollector.Events;

namespace UCoverme.DataCollector.Summary
{
    [ProtoContract]
    public class TestExecutionSummary
    {
        [ProtoMember(1, IsRequired = true)]
        public string FileName { get; set; }
        [ProtoMember(2, IsRequired = true)]
        public Guid TestCaseId { get; set; }
        [ProtoMember(3, IsRequired = true)]
        public string TestCaseName { get; set; }
        [ProtoMember(4, IsRequired = true)]
        public ExecutionEvent[] TestCaseEvents { get; set; }
        [ProtoMember(5, IsRequired = true)]
        public MethodExecutionSummary[] MethodsExecuted { get; set; }
        [ProtoMember(6, IsRequired = true)]
        public string ProjectPath { get; set; }
    }
}