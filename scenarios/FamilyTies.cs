namespace Scenarios;

/// <summary>A colony with history: a leader, an engaged pair, an ex-lover, a parent and their child, a friend and a rival.</summary>
[Dlc("ideology", "biotech")]
public sealed class FamilyTies : HarnessScenario
{
    public override async Task Run()
    {
        // A calm world with a cleared yard
        Clock.Speed = TimeSpeed.Paused;
        World.Calm();
        World.RemoveHostiles();
        var c = World.Center;
        var yard = new CellRect(c.x - 8, c.z - 6, 17, 13);
        Build.Clearing(yard);

        // Six colonists of different ages, the child a Hussar
        var bride = Spawn.Colonist().WithName("Bride").At(c + new IntVec3(-3, 0, 2)).Aged(28).Spawn();
        var groom = Spawn.Colonist().WithName("Groom").At(c + new IntVec3(-1, 0, 2)).Aged(30).Spawn();
        var ex = Spawn.Colonist().WithName("Ex").At(c + new IntVec3(1, 0, 2)).Aged(31).Spawn();
        var parent = Spawn.Colonist().WithName("Parent").At(c + new IntVec3(3, 0, 2)).Aged(58).Spawn();
        var child = Spawn.Colonist().WithName("Child").At(c + new IntVec3(-3, 0, -2)).Aged(9).Xenotype(IdentityDefOf.Hussar).Spawn();
        var stranger = Spawn.Colonist().WithName("Stranger").At(c + new IntVec3(3, 0, -2)).Aged(40).Spawn();
        Colonists.RemoveAllExcept(bride, groom, ex, parent, child, stranger);

        // The bride leads, is engaged to the groom, once loved the ex and is the parent's child; the ex resents her and the stranger likes the groom
        bride.AssignRole(PreceptDefOf.IdeoRole_Leader);
        bride.Relate(groom, PawnRelationDefOf.Fiance);
        bride.Relate(ex, PawnRelationDefOf.ExLover);
        bride.Relate(parent, PawnRelationDefOf.Parent);
        child.Relate(groom, PawnRelationDefOf.Parent);
        ex.SetOpinionOf(bride, -40);
        stranger.SetOpinionOf(groom, 30);

        // Every role, relation, opinion, age and xenotype holds
        Check(bride.Ideo.GetRole(bride)?.def == PreceptDefOf.IdeoRole_Leader);
        Check(bride.relations.DirectRelationExists(PawnRelationDefOf.Fiance, groom) && groom.relations.DirectRelationExists(PawnRelationDefOf.Fiance, bride));
        Check(bride.relations.DirectRelationExists(PawnRelationDefOf.ExLover, ex));
        Check(bride.GetMother() == parent || bride.GetFather() == parent);
        Check(child.GetFather() == groom || child.GetMother() == groom);
        Check(ex.relations.OpinionOf(bride) == -40);
        Check(stranger.relations.OpinionOf(groom) == 30);
        Check(child.DevelopmentalStage == DevelopmentalStage.Child && child.ageTracker.AgeBiologicalYears == 9);
        Check(child.genes.Xenotype == IdentityDefOf.Hussar);
        Check(parent.ageTracker.AgeBiologicalYears == 58);

        // The family together
        Camera.Frame(yard);
        await Shot("family");
    }
}
