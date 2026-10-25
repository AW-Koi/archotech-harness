using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>The developmental stage a human has at an age, so a pawn generated at that age is allowed it.</summary>
public static class LifeStage
{
    private const float ChildFromYears = 3;
    private const float AdultFromYears = 13;

    public static DevelopmentalStage ForAge(float years) =>
        years < ChildFromYears ? DevelopmentalStage.Baby
        : years < AdultFromYears ? DevelopmentalStage.Child
        : DevelopmentalStage.Adult;
}
