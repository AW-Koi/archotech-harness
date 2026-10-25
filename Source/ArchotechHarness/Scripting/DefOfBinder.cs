using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Binds [DefOf] classes in assemblies loaded after startup, such as scenarios, with RimWorld's DefOfHelper.</summary>
public static class DefOfBinder
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    private static readonly MethodInfo BindDefsFor = typeof(DefOfHelper).GetMethod("BindDefsFor", PrivateStatic)
        ?? throw new MissingMethodException(nameof(DefOfHelper), "BindDefsFor");

    private static readonly FieldInfo BindingNow = typeof(DefOfHelper).GetField("bindingNow", PrivateStatic)
        ?? throw new MissingFieldException(nameof(DefOfHelper), "bindingNow");

    private static readonly FieldInfo EarlyTry = typeof(DefOfHelper).GetField("earlyTry", PrivateStatic)
        ?? throw new MissingFieldException(nameof(DefOfHelper), "earlyTry");

    public static IReadOnlyList<Type> DefOfClasses(Assembly assembly) =>
        assembly.GetTypes().Where(type => type.IsDefined(typeof(DefOf), inherit: false)).ToList();

    /// <summary>Throws UnknownDefException naming every field whose def is not loaded.</summary>
    public static void Bind(IEnumerable<Type> defOfClasses)
    {
        var classes = defOfClasses.ToList();
        var earlyTry = EarlyTry.GetValue(null);
        BindingNow.SetValue(null, true);
        EarlyTry.SetValue(null, true);
        try
        {
            foreach (var type in classes)
            {
                BindDefsFor.Invoke(null, BindingFlags.DoNotWrapExceptions, null, [type], null);
            }
        }
        finally
        {
            EarlyTry.SetValue(null, earlyTry);
            BindingNow.SetValue(null, false);
        }

        var unbound = classes.SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static)).Where(IsUnbound).ToList();
        if (unbound.Count > 0)
        {
            throw new UnknownDefException(unbound);
        }
    }

    private static bool IsUnbound(FieldInfo field) =>
        field.GetValue(null) is null or SoundDef { isUndefined: true }
        && !field.IsDefined(typeof(MayRequireAttribute))
        && !field.IsDefined(typeof(MayRequireAnyOfAttribute));
}
