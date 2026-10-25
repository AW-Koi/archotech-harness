using System.Collections.Generic;
using System.Linq;

namespace ArchotechHarness.Scripting;

/// <summary>The game errors a scenario's [AllowGameErrors] attributes let it pass with.</summary>
public sealed class GameErrorAllowList
{
    private readonly bool _all;
    private readonly IReadOnlyList<string> _containing;

    private GameErrorAllowList(bool all, IReadOnlyList<string> containing)
    {
        _all = all;
        _containing = containing;
    }

    public static GameErrorAllowList None { get; } = new(false, []);

    public static GameErrorAllowList From(IEnumerable<AllowGameErrorsAttribute> attributes)
    {
        var list = attributes.ToList();
        return new GameErrorAllowList(list.Any(a => a.Containing == null), list.Where(a => a.Containing != null).Select(a => a.Containing).ToList());
    }

    public bool Allows(string error) => _all || _containing.Any(text => error.Contains(text, System.StringComparison.Ordinal));
}
