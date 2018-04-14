using System;
using Mono.Cecil.Cil;

namespace UCoverme.Utils
{
    public static class ILProcessorExtensions
    {
        public static void InsertAllBefore(this ILProcessor processor, Instruction target,
            params Instruction[] instructions)
        {
            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (instructions == null)
            {
                throw new ArgumentNullException(nameof(instructions));
            }

            var currentTarget = target;
            for (int i = instructions.Length - 1; i >= 0; i--)
            {
                processor.InsertBefore(currentTarget, instructions[i]);
                currentTarget = instructions[i];
            }
        }

        public static void InsertAllAfter(this ILProcessor processor, Instruction target,
            params Instruction[] instructions)
        {
            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (instructions == null)
            {
                throw new ArgumentNullException(nameof(instructions));
            }

            var currentTarget = target;
            for (int i = 0; i < instructions.Length; i++)
            {
                processor.InsertAfter(currentTarget, instructions[i]);
                currentTarget = instructions[i];
            }
        }
    }
}