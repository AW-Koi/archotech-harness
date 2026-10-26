using System.Collections.Generic;

namespace ArchotechHarness.Scripting;

/// <summary>Keeps every error the game logs from the harness's start until the first read, up to its capacity.</summary>
/// <remarks>Unlike the game's log, it never drops its oldest entries.</remarks>
public sealed class LoadErrorLog(int capacity)
{
    private readonly object _lock = new();
    private readonly List<string> _errors = new();
    private bool _closed;

    public static LoadErrorLog Current { get; } = new(100_000);

    public void Add(string text)
    {
        lock (_lock)
        {
            if (!_closed && _errors.Count < capacity)
            {
                _errors.Add(text);
            }
        }
    }

    /// <summary>Stops recording and returns the errors recorded so far; later calls return the same errors.</summary>
    public IReadOnlyList<string> Close()
    {
        lock (_lock)
        {
            _closed = true;
            return _errors.ToArray();
        }
    }
}
