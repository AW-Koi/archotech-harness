using System;
using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Makes a thing from "def", "stuff" and "quality" arguments.</summary>
public static class ThingFactory
{
    public static Thing Make(JsonObject args)
    {
        var def = Arguments.Def<ThingDef>(Arguments.RequiredString(args, "def"));
        var stuffName = Arguments.String(args, "stuff");
        var stuff = def.MadeFromStuff
            ? stuffName != null ? Arguments.Def<ThingDef>(stuffName) : GenStuff.DefaultStuffFor(def)
            : null;
        var thing = ThingMaker.MakeThing(def, stuff);
        var quality = Arguments.String(args, "quality");
        if (quality != null)
        {
            thing.TryGetComp<CompQuality>()?.SetQuality(Enum.Parse<QualityCategory>(quality, ignoreCase: true), ArtGenerationContext.Colony);
        }

        return thing;
    }
}
