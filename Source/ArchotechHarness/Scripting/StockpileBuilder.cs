using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A stockpile zone to make with Build.Stockpile(area); finish with Build().</summary>
public sealed class StockpileBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly CellRect _area;
    private readonly string _label;
    private StorageSettingsPreset _preset = StorageSettingsPreset.DefaultStockpile;

    internal StockpileBuilder(CellRect area, string caller)
    {
        _area = area;
        _label = $"Build.Stockpile() at {caller}";
        _token = ScriptSession.Current?.Builders.Track($"{_label} ends with .Build()");
    }

    /// <summary>Without it, the default stockpile settings.</summary>
    public StockpileBuilder Preset(StorageSettingsPreset preset)
    {
        _preset = preset;
        return this;
    }

    /// <summary>Takes cells from older zones; cells off the map or under buildings are left out and noted.</summary>
    public Zone_Stockpile Build()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        var map = World.Map;
        var zone = ZonePlacement.Fill(new Zone_Stockpile(_preset, map.zoneManager), _area, map, out var skipped);
        PresetNote.Record(_label, "zone cells", _area.Area, _area.Area - skipped, "off the map or under buildings");
        return zone;
    }
}
