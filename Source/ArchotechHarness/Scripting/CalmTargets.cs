using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace ArchotechHarness.Scripting;

/// <summary>Finds a virtual method on a base type and every override of it, so a calm patch covers subclasses too.</summary>
internal static class CalmTargets
{
    private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    public static IEnumerable<MethodBase> DeclaredOverrides(Type baseType, string method) =>
        AccessTools.AllTypes()
            .Where(baseType.IsAssignableFrom)
            .Select(t => t.GetMethod(method, Declared, null, Type.EmptyTypes, null) ?? t.GetMethods(Declared).FirstOrDefault(m => m.Name == method))
            .Where(m => m != null && !m.IsAbstract)
            .Distinct();
}
