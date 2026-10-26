namespace ArchotechHarness.Scripting;

/// <summary>A calm world from World.Calm(), already in effect: each call lets one part of the world carry on.</summary>
public sealed class CalmBuilder
{
    private readonly ScriptSession _session;

    internal CalmBuilder(ScriptSession session) => _session = session;

    /// <summary>Lets storyteller, queued and quest incidents fire.</summary>
    public CalmBuilder AllowIncidents()
    {
        _session.NoIncidents = false;
        _session.Timeline.Add("world", "incidents allowed");
        return this;
    }

    /// <summary>Lets dormant pawns, buildings and sleeping lords wake, and signal actions such as ambushes act.</summary>
    public CalmBuilder AllowWakeUps()
    {
        _session.KeepAsleep = false;
        _session.Timeline.Add("world", "wake-ups allowed");
        return this;
    }

    /// <summary>Lets letters arrive, such as a raid's.</summary>
    public CalmBuilder AllowLetters()
    {
        _session.NoLetters = false;
        _session.Timeline.Add("world", "letters allowed");
        return this;
    }

    /// <summary>Lets the weather change from the clear weather calm set.</summary>
    public CalmBuilder AllowWeather()
    {
        _session.HoldWeather = false;
        _session.Timeline.Add("world", "weather allowed");
        return this;
    }
}
