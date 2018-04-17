using System.Linq;
using System.Text;
using Mono.Cecil.Cil;
using UCoverme.Model;

namespace UCoverme.Utils
{
    public static class InstrumentedMethodExtensions
    {
        public static string Debug(this InstrumentedMethod method)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"{method.Name} (#{method.MethodId})");
            builder.AppendLine(
                $"- # of branches: {method.Branches.Length} - # of conditions: {method.Conditions.Length} - # of seq. points: {method.SequencePoints.Count(sp => !sp.IsHidden)} visible, {method.SequencePoints.Count(sp => sp.IsHidden)} hidden");

            foreach (var branch in method.Branches)
            {
                DebugBranch(builder, branch, method.Instructions, method.SequencePoints);
            }

            DebugConditions(builder, method);

            return builder.ToString();
        }

        private static void DebugConditions(StringBuilder builder, InstrumentedMethod method)
        {
            builder.AppendLine("\n----------\nConditions\n----------");
            var conditions = method.Conditions
                .OrderBy(lp => lp.Start.Instruction.Offset)
                .ThenBy(lp => lp.Target.Instruction.Offset)
                .Select((lp, idx) =>
                {
                    var startBranch = method.Branches.First(b => b.EndOffset == lp.Start.Instruction.Offset);
                    var targetBranch = method.Branches.First(b => b.StartOffset == lp.Target.Instruction.Offset);

                    return new
                    {
                        ConditionId = idx,
                        StartBranch = startBranch,
                        TargetBranch = targetBranch
                    };
                });

            foreach (var condition in conditions)
            {
                builder.AppendLine(
                    $"Condition #{condition.ConditionId + 1}: [branch #{condition.StartBranch.Id + 1} --> branch #{condition.TargetBranch.Id + 1}]");
            }
        }

        private static void DebugBranch(StringBuilder builder, Branch branch, Instruction[] instructions,
            InstrumentedSequencePoint[] sequencePoints)
        {
            builder.AppendLine(
                $"--- Branch #{branch.Id + 1}, start: {branch.StartOffset}, end: {branch.EndOffset} -------");
            var branchInstructions = instructions
                .SkipWhile(i => i.Offset < branch.StartOffset)
                .TakeWhile(i => i.Offset <= branch.EndOffset);

            var sequencePointsDictionary = sequencePoints.ToDictionary(sp => sp.StartOffset);
            foreach (var instruction in branchInstructions)
            {
                if (sequencePointsDictionary.ContainsKey(instruction.Offset))
                {
                    builder.AppendLine(sequencePointsDictionary[instruction.Offset].ToString());
                }

                builder.AppendLine($"[{instruction.Offset}, {instruction}]");
            }
        }
    }
}