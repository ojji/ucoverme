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
                .OrderBy(condition => condition.StartOffset)
                .ThenBy(condition => condition.EndOffset)
                .Select((condition, idx) =>
                {
                    var startBranch = method.Branches.First(branch => branch.EndOffset == condition.StartOffset);
                    var targetBranch = method.Branches.First(branch => branch.StartOffset == condition.EndOffset);

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
                    $"Condition #{condition.ConditionId + 1}: [branch #{condition.StartBranch.Id} --> branch #{condition.TargetBranch.Id}]");
            }
        }

        private static void DebugBranch(StringBuilder builder, Branch branch, Instruction[] instructions,
            InstrumentedSequencePoint[] sequencePoints)
        {
            builder.AppendLine(
                $"--- Branch #{branch.Id}, start: {branch.StartOffset}, end: {branch.EndOffset} -------");
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