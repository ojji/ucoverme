using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace UCoverme.Utils
{
    public static class TypeDefinitionExtensions
    {
        public static IEnumerable<MethodDefinition> GetInstrumentableMethods(this TypeDefinition typeDefinition)
        {
            foreach (var methodDefinition
                in typeDefinition.Methods
                    .Where(m => m.HasBody &&
                                m.Body.Instructions.Any()))
            {
                yield return methodDefinition;
            }

            if (!typeDefinition.HasNestedTypes) yield break;

            foreach (var nestedDefinition in
                typeDefinition.NestedTypes.SelectMany(n =>
                    n.GetInstrumentableMethods()))
            {
                yield return nestedDefinition;
            }
        }
    }
}