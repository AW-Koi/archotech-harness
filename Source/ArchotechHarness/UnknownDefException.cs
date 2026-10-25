using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;

namespace ArchotechHarness;

/// <summary>[DefOf] fields that name defs the game has not loaded.</summary>
public sealed class UnknownDefException(IReadOnlyList<FieldInfo> fields) : HarnessException(
    "No def matches these [DefOf] fields: " + string.Join(", ", fields.Select(Describe)) + ".")
{
    public IReadOnlyList<string> Fields { get; } = fields.Select(FullName).ToList();

    private static string FullName(FieldInfo field) => $"{field.DeclaringType!.Name}.{field.Name}";

    private static string Describe(FieldInfo field) =>
        $"{FullName(field)} ({field.FieldType.Name} '{field.GetCustomAttribute<DefAliasAttribute>()?.defName ?? field.Name}')";
}
