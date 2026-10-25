namespace Scenarios;

// ReSharper disable InconsistentNaming, UnassignedField.Global
/// <summary>Titles, nobles, backstories and xenotypes the identity samples use; DLC defs bind only when the DLC is active.</summary>
[DefOf]
public static class IdentityDefOf
{
    [MayRequireRoyalty]
    public static RoyalTitleDef Knight;

    [MayRequireRoyalty]
    public static PawnKindDef Empire_Royal_Count;

    [MayRequireBiotech]
    public static XenotypeDef Hussar;

    public static BackstoryDef GlitterworldSurgeon15;
    public static BackstoryDef TribeChild19;
    public static BackstoryDef Hunter73;
    public static BackstoryDef Pirate56;
}
