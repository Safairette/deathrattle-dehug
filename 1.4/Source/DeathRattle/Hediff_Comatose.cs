using Verse;

namespace DeathRattle;

public class Hediff_Comatose : HediffWithComps
{
    public override void ExposeData()
    {
        base.ExposeData();
        /*if (cause != null) {
            Scribe_Defs.Look(ref cause, "cause");
        }*/
    }

    public float calcSeverityForHediff(HediffWithComps item)
    {
        var change = 0f;

        foreach (var comp in item.comps)
            if (comp is HediffComp_SeverityPerDay sev)
                sev.CompPostTick(ref change);
        return change;
    }

    public override void PostTick()
    {
        var debug = false;
        base.PostTick();
        //string resStr = "Tick: Hediff_DeathRattle: ";
        if (pawn.IsHashIntervalTick(200))
        {
            //bool artificialComa = false;
            var QE_LifeSupport = false;
            var wakeUpHigh = false;
            var deathRattleHediff = false;

            foreach (var item in pawn.health.hediffSet.hediffs)
                if (item.def.defName == "QE_LifeSupport")
                    QE_LifeSupport = true;
                /*else if(item.def.defName == HediffDefOfComatose.ArtificialComa.defName) {
                    artificialComa = true;
                }*/
                else if (item.def.defName == "WakeUpHigh") wakeUpHigh = true;

            if (def.defName == HediffDefOfComatose.ArtificialComa.defName)
            {
                if (debug) Log.Message("Artificial coma detected");

                var change = 0f;
                foreach (var comp in comps)
                    if (comp is HediffComp_SeverityPerDay sev)
                    {
                        sev.CompPostTick(ref change);
                        if (debug) Log.Message(string.Format("Tick: Logging change: {0}", change));
                    }

                if (wakeUpHigh)
                {
                    if (debug) Log.Message("WakeUp changed state!");
                    pawn.health.RemoveHediff(this);
                }
                else
                {
                    foreach (var item in pawn.health.hediffSet.hediffs)
                    {
                        deathRattleHediff = false;
                        if (item.def.defName == HediffDefOfDeathRattleStrings.LiverFailure)
                        {
                            deathRattleHediff = true;


                            change = calcSeverityForHediff((HediffWithComps)item);
                            if (QE_LifeSupport)
                            {
                                if (debug)
                                    Log.Message(string.Format("{0}, stabilizing slowly to 0.4", item.def.defName));
                                if (change > 0 && item.Severity > 0.4f)
                                {
                                    if (debug) Log.Message(string.Format("{0}, is stabilizing", item.def.defName));
                                    item.Severity -= Math.Min(item.Severity, change + change * 1);
                                }
                            }
                        }
                        else if (item.def.defName == HediffDefOfDeathRattleStrings.KidneyFailure)
                        {
                            deathRattleHediff = true;


                            change = calcSeverityForHediff((HediffWithComps)item);
                            if (QE_LifeSupport)
                            {
                                if (debug) Log.Message(string.Format("{0}, dialysis in progress", item.def.defName));
                                if (change > 0) item.Severity -= Math.Min(item.Severity, change + change * 2);
                            }
                        }
                        else if (item.def.defName == HediffDefOfDeathRattleStrings.IntestinalFailure)
                        {
                            deathRattleHediff = true;

                            if (debug) Log.Message(string.Format("{0}, no way to stabilize", item.def.defName));
                        }
                        else if (item.def.defName == HediffDefOfDeathRattleStrings.ClinicalDeathAsphyxiation
                                 || item.def.defName == HediffDefOfDeathRattleStrings.ClinicalDeathNoHeartbeat)
                        {
                            deathRattleHediff = true;


                            change = calcSeverityForHediff((HediffWithComps)item);
                            if (QE_LifeSupport)
                            {
                                if (debug)
                                    Log.Message(string.Format("{0}, heart-lung machine stabilizing rapidly",
                                        item.def.defName));
                                if (change > 0) item.Severity -= Math.Min(item.Severity, change + change * 10);
                            }
                        }
                        else if (item.def.defName != HediffDefOfComatose.ArtificialComa.defName)
                        {
                        }

                        if (deathRattleHediff)
                        {
                            if (debug) Log.Message(string.Format("Slowing down illness by half", item.def.defName));
                            if (change > 0) item.Severity -= Math.Min(item.Severity, change / 2);
                        }
                    }
                }
            }
            else
            {
                Log.Message("Strange! Other type detected!");
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
}