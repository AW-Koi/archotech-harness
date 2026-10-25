using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>The steps a scenario took, in order, with real time since the start, game tick and source line.</summary>
public sealed class ScenarioTimeline
{
    private readonly JsonArray _steps = new();
    private readonly List<TimelineStep> _list = new();
    private readonly DateTime _started = DateTime.UtcNow;

    /// <summary>The steps so far, in order, for display while the scenario runs.</summary>
    public IReadOnlyList<TimelineStep> Steps => _list;

    public double Elapsed => (DateTime.UtcNow - _started).TotalSeconds;

    /// <summary>Records a step; a wait passes when it started, so its duration is the time since then.</summary>
    public void Add(string kind, string text, int line = 0, double? startedAt = null, bool? passed = null)
    {
        var now = Elapsed;
        var tick = Find.TickManager?.TicksGame;
        _list.Add(new TimelineStep(Math.Round(startedAt ?? now, 2), tick, kind, text, line, passed));
        _steps.Add(new JsonObject
        {
            ["at"] = Math.Round(startedAt ?? now, 2),
            ["seconds"] = startedAt == null ? null : Math.Round(now - startedAt.Value, 2),
            ["tick"] = tick,
            ["kind"] = kind,
            ["text"] = text,
            ["line"] = line == 0 ? null : line,
            ["passed"] = passed,
        });
    }

    public JsonArray ToJson() => (JsonArray)_steps.DeepClone();
}
