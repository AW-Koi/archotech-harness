using System.Collections.Generic;
using System.Linq;

namespace ArchotechHarness.Scripting;

/// <summary>Scenario builders started but not yet finished with their terminal call, such as .Spawn().</summary>
public sealed class PendingBuilders
{
    private readonly List<Token> _pending = new();

    public IReadOnlyList<string> Unfinished => _pending.Select(token => token.Description).ToList();

    public Token Track(string description)
    {
        var token = new Token(description);
        _pending.Add(token);
        return token;
    }

    public void Finish(Token token) => _pending.Remove(token);

    /// <summary>Identifies one tracked builder.</summary>
    public sealed class Token(string description)
    {
        public string Description { get; } = description;
    }
}
