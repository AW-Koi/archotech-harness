using System;
using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Makes a thing from a def, stuff and quality, or from "def", "stuff" and "quality" arguments.</summary>
public static class ThingFactory
{
    public static Thing Make(JsonObject args) =>
        Make(
            Arguments.Def<ThingDef>(Arguments.RequiredString(args, "def")),
            Arguments.String(args, "stuff") is { } stuff ? Arguments.Def<ThingDef>(stuff) : null,
            Arguments.String(args, "quality") is { } quality ? Enum.Parse<QualityCategory>(quality, ignoreCase: true) : null);

    /// <summary>Without stuff, a thing made from stuff uses the def's default stuff. Art gets its title and description.</summary>
    public static Thing Make(ThingDef def, ThingDef stuff = null, QualityCategory? quality = null)
    {
        var thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? stuff ?? GenStuff.DefaultStuffFor(def) : null);
        if (quality is { } category)
        {
            thing.TryGetComp<CompQuality>()?.SetQuality(category, ArtGenerationContext.Colony);
        }

        thing.TryGetComp<CompArt>()?.InitializeArt(ArtGenerationContext.Colony);

        return thing;
    }
}
