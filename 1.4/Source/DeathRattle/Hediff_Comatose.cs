using System.Diagnostics;
using Verse;

namespace DeathRattle;

public class Hediff_Comatose : HediffWithComps
{
    Dictionary<string, Action<HediffWithComps, bool, float>> dispatchTable = new Dictionary<
        string,
        Action<HediffWithComps, bool, float>
    >
    {
        ["HediffDefOfDeathRattleStrings.LiverFailure"] = HandleLiverFailure,
        ["HediffDefOfDeathRattleStrings.KidneyFailure"] = HandleKidneyFailure,
        ["HediffDefOfDeathRattleStrings.IntestinalFailure"] = HandleIntestinalFailure,
        ["HediffDefOfDeathRattleStrings.ClinicalDeathAsphyxiation"] =
            HandleClinicalDeathAsphyxiation,
        ["HediffDefOfDeathRattleStrings.ClinicalDeathNoHeartbeat"] = HandleClinicalDeathNoHeartbeat,
    };
    bool QE_LifeSupport = false;
    bool wakeUpHigh = false;

    public override void ExposeData()
    {
        base.ExposeData();
        /*if (cause != null) {
            Scribe_Defs.Look(ref cause, "cause");
        }*/
    }

    public override void PostTick()
    {
        base.PostTick();
        //string resStr = "Tick: Hediff_DeathRattle: ";

        if (pawn.IsHashIntervalTick(200))
        {
            //bool artificialComa = false;
            QE_LifeSupport = HasHediff("QE_LifeSupport");
            wakeUpHigh = HasHediff("WakeUpHigh");

            if (def.defName == HediffDefOfComatose.ArtificialComa.defName)
            {
                DebugMessage("Artificial coma detected");

                var change = 0f;
                foreach (var comp in comps)
                    if (comp is HediffComp_SeverityPerDay sev)
                    {
                        sev.CompPostTick(ref change);
                        DebugMessage(string.Format("Tick: Logging change: {0}", change));
                    }

                if (wakeUpHigh)
                {
                    DebugMessage("WakeUp changed state!");
                    pawn.health.RemoveHediff(this);
                }
                else
                {
                    ProcessHediffs(change);
                }
            }
            else
            {
                DebugMessage("Strange! Other type detected!");
            }
        }
        //Log.Message(resStr);

        /*
        if(cause != null) {
            if (pawn.health.capacities.CapableOf(cause)) {
                pawn.health.RemoveHediff(this);
            }
        }
        */
    }

    //public PawnCapacityDef cause = null;

    private void ProcessHediffs(float change)
    {
        foreach (var item in pawn.health.hediffSet.hediffs)
        {
            var deathRattleHediff = false;

            if (item is HediffWithComps hediffWithComps)
            {
                if (dispatchTable.ContainsKey(item.def.defName))
                {
                    deathRattleHediff = true;
                    change = HandleDeathRattleHediff(hediffWithComps);
                }
            }

            if (deathRattleHediff && change > 0)
            {
                DebugMessage($"Slowing down illness by half for {item.def.defName}");
                item.Severity -= Math.Min(item.Severity, change / 2);
            }
        }
    }

    private float HandleDeathRattleHediff(HediffWithComps item)
    {
        var change = calcSeverityForHediff(item);
        DebugMessage($"Processing {item.def.defName}");
        dispatchTable[item.def.defName](item, QE_LifeSupport, change);
        return change;
    }

    private static void HandleLiverFailure(HediffWithComps item, bool QE_LifeSupport, float change)
    {
        if (QE_LifeSupport)
        {
            DebugMessage(string.Format("{0}, stabilizing slowly to 0.4", item.def.defName));
            if (change > 0 && item.Severity > 0.4f)
            {
                DebugMessage(string.Format("{0}, is stabilizing", item.def.defName));
                item.Severity -= Math.Min(item.Severity, change + change * 1);
            }
        }
    }

    private static void HandleIntestinalFailure(
        HediffWithComps item,
        bool QE_LifeSupport,
        float change
    )
    {
        DebugMessage(string.Format("{0}, no way to stabilize", item.def.defName));
    }

    private static void HandleKidneyFailure(HediffWithComps item, bool QE_LifeSupport, float change)
    {
        if (QE_LifeSupport)
        {
            DebugMessage(string.Format("{0}, dialysis in progress", item.def.defName));
            if (change > 0)
                item.Severity -= Math.Min(item.Severity, change + change * 2);
        }
    }

    private static void HandleClinicalDeathAsphyxiation(
        HediffWithComps item,
        bool QE_LifeSupport,
        float change
    )
    {
        if (QE_LifeSupport)
        {
            DebugMessage(
                string.Format("{0}, heart-lung machine stabilizing rapidly", item.def.defName)
            );
            if (change > 0)
                item.Severity -= Math.Min(item.Severity, change + change * 10);
        }
    }

    private static void HandleClinicalDeathNoHeartbeat(
        HediffWithComps item,
        bool QE_LifeSupport,
        float change
    )
    {
        if (QE_LifeSupport)
        {
            DebugMessage(
                string.Format("{0}, heart-lung machine stabilizing rapidly", item.def.defName)
            );
            if (change > 0)
                item.Severity -= Math.Min(item.Severity, change + change * 10);
        }
    }

    private bool HasHediff(string hediffName)
    {
        return pawn.health.hediffSet.hediffs.Any(h => h.def.defName == hediffName);
    }

    private static float calcSeverityForHediff(HediffWithComps item)
    {
        var change = 0f;

        foreach (var comp in item.comps)
            if (comp is HediffComp_SeverityPerDay sev)
                sev.CompPostTick(ref change);
        return change;
    }

    private static void DebugMessage(string message)
    {
        if (Prefs.DevMode)
            Log.Message(message);
    }
}
