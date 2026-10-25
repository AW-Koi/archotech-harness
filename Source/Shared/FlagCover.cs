namespace ArchotechHarness.Shared;

/// <summary>Picks a candidate whose bit flags include every required flag and none of the forbidden ones.</summary>
public static class FlagCover
{
    public static T? FirstCovering<T>(T[] candidates, FlagsOf<T> flags, long required, long forbidden)
        where T : class
    {
        foreach (var candidate in candidates)
        {
            var bits = flags(candidate);
            if ((bits & required) == required && (bits & forbidden) == 0)
            {
                return candidate;
            }
        }

        return null;
    }
}
