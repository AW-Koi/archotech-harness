using System.Linq;
using System.Text.Json.Nodes;
using Verse;

namespace DevHarness.Commands;

/// <summary>Sets work priorities, and turns on manual priorities for the colony.</summary>
/// <remarks>With "onlyWork" every work type not listed is set to 0.</remarks>
public static class SetWorkCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var pawn = Arguments.Pawn(args);
        var priorities = args["priorities"] as JsonObject ?? throw new HarnessException("Missing \"priorities\".");
        Apply(pawn, priorities, Arguments.Bool(args, "onlyWork", false));
        return CommandResult.Done(WorkPriorities(pawn));
    }

    public static void Apply(Pawn pawn, JsonObject priorities, bool onlyWork)
    {
        Find.PlaySettings.useWorkPriorities = true;
        pawn.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
        if (onlyWork)
        {
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(w => !pawn.WorkTypeIsDisabled(w)))
            {
                pawn.workSettings.SetPriority(workType, 0);
            }
        }

        foreach (var (name, priority) in priorities)
        {
            var workType = Arguments.Def<WorkTypeDef>(name);
            if (pawn.WorkTypeIsDisabled(workType))
            {
                throw new HarnessException($"{pawn.LabelShort} cannot do {workType.defName}.");
            }

            pawn.workSettings.SetPriority(workType, (int)priority!.GetValue<double>());
        }
    }

    public static JsonObject WorkPriorities(Pawn pawn)
    {
        var result = new JsonObject();
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(w => pawn.workSettings.GetPriority(w) > 0))
        {
            result[workType.defName] = pawn.workSettings.GetPriority(workType);
        }

        return result;
    }
}
