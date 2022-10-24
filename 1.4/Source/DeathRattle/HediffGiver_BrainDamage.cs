using Verse;

namespace DeathRattle;

public class HediffGiver_BrainDamage : HediffGiver
{
    public float baseMtbDays;
    public float minSeverity;
    public float severityAmount;

    public override void OnIntervalPassed(Pawn pawn, Hediff cause)
    {
        if (cause.Severity < (double)minSeverity || !Rand.MTBEventOccurs(baseMtbDays, 60000f, 60f))
            return;
        if (TryApply(pawn))
            SendLetter(pawn, cause);
        pawn.health.hediffSet.GetFirstHediffOfDef(hediff).Severity += severityAmount;
    }
}