using HarmonyLib;
using RimWorld;
using System.Reflection.Emit;
using Verse;

namespace DeathRattle.Harmony;

[HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDeadFromRequiredCapacity")]
public static class ShouldBeDeadFromRequiredCapacityPatch
{
    public static Dictionary<string, HediffDef> ailmentDictionary =
        new()
        {
            { "Metabolism", HediffDefOfDeathRattle.IntestinalFailure },
            { "BloodFiltration", null },
            { "BloodPumping", HediffDefOfDeathRattle.ClinicalDeathNoHeartbeat },
            { "Breathing", HediffDefOfDeathRattle.ClinicalDeathAsphyxiation },
            { "Consciousness", HediffDefOfDeathRattle.Coma }
        };

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> DeathRattleException(
        IEnumerable<CodeInstruction> instrs
    )
    {
        var trigger = false;
        foreach (var itr in instrs)
        {
            yield return itr;
            if (trigger)
            {
                trigger = false;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(
                    OpCodes.Ldfld,
                    AccessTools.Field(typeof(Pawn_HealthTracker), "pawn")
                );
                yield return new CodeInstruction(OpCodes.Ldloc_2);
                yield return new CodeInstruction(
                    OpCodes.Callvirt,
                    AccessTools.Method(
                        typeof(ShouldBeDeadFromRequiredCapacityPatch),
                        "AddCustomHediffs",
                        new[] { typeof(Pawn_HealthTracker), typeof(Pawn), typeof(PawnCapacityDef) }
                    )
                );
                yield return itr;
            }

            if (
                itr.opcode == OpCodes.Callvirt
                && itr.operand.Equals(
                    AccessTools.Method(
                        typeof(PawnCapacitiesHandler),
                        "CapableOf",
                        new[] { typeof(PawnCapacityDef) }
                    )
                )
            )
                trigger = true;
        }
    }

    public static bool AddCustomHediffs(
        Pawn_HealthTracker tracker,
        Pawn pawn,
        PawnCapacityDef pawnCapacityDef
    )
    {
        // Exit if pawn does not have brain
        if (pawn.health.hediffSet.GetBrain() == null)
            return false;

        // Exit if Biotech is active and pawn has Deathless gene
        if (
            ModsConfig.BiotechActive
            && pawn.genes != null
            && pawn.genes.HasGene(GeneDefOf.Deathless)
        )
            return false;

        // Check if pawn's race is flesh and if the capacity is lethal
        if (
            pawn.RaceProps.IsFlesh
            && pawnCapacityDef.lethalFlesh
            && !tracker.capacities.CapableOf(pawnCapacityDef)
        )
        {
            // Check if the ailment exists in dictionary
            if (ailmentDictionary.TryGetValue(pawnCapacityDef.defName, out var def))
            {
                var notMissingParts = pawn.health.hediffSet.GetNotMissingParts(
                    depth: BodyPartDepth.Inside
                );

                // Check for specific ailments like liver or kidney failure
                if (def == null)
                {
                    if (
                        !pawn.health.hediffSet.HasHediff(HediffDefOfDeathRattle.LiverFailure)
                        && !notMissingParts.Any(p => p.def.defName == "Liver")
                    )
                    {
                        def = HediffDefOfDeathRattle.LiverFailure;
                    }
                    else if (
                        !pawn.health.hediffSet.HasHediff(HediffDefOfDeathRattle.KidneyFailure)
                        && !notMissingParts.Any(p => p.def.defName.Contains("Kidney"))
                    )
                    {
                        def = HediffDefOfDeathRattle.KidneyFailure;
                    }
                }

                // If pawn does not have the hediff, add it
                if (def != null && !pawn.health.hediffSet.HasHediff(def))
                {
                    var ailment = (Hediff_DeathRattle)HediffMaker.MakeHediff(def, pawn);
                    ailment.cause = pawnCapacityDef;
                    pawn.health.AddHediff(ailment);
                }

                return true;
            }
        }

        return false;
    }
}
