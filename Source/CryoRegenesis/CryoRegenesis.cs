using RimWorld;
using System;
using System.Linq;
using Verse;

namespace CryoRegenesis
{
    public class Building_CryoRegenesis : Building_CryptosleepCasket
    {
        int cryptoHediffCooldown;
        int cryptoHediffCooldownBase = GenDate.TicksPerMonth / 2;
        long restoreCoolDown = 4294967295;
        int enterTime;
        int rate = 30;
        int fuelConsumption = 20;
        HediffDef cryosickness = HediffDef.Named("CryptosleepSickness");
        CompRefuelable refuelable;
        CompPowerTrader power;
        CompProperties_Power props;
        CompProperties_Refuelable fuelprops;

        public int AgeHediffs(Pawn pawn)
        {
            if (pawn != null)
            {
                bool hasCataracts = false;
                bool hasHearingLoss = false;
                int hediffs = 0;
                foreach (Hediff injury in pawn.health.hediffSet.GetHediffs<Hediff>().ToList())
                {
                    string injuryName = injury.def.label;
                    if (injuryName == "cataract" && !hasCataracts)
                    {
                        hediffs += 1;
                        hasCataracts = true;
                    }
                    else if (injuryName == "hearing loss" && !hasHearingLoss)
                    {
                        hediffs += 1;
                        hasHearingLoss = true;
                    }
                    else if (injuryName == "bad back" || injuryName == "frail" || injuryName == "dementia" || injuryName == "alzheimer's")
                        hediffs += 1;
                }
                return hediffs;
            }
            return 0;
        }

        protected int InjuryHediffs(Pawn pawn)
        {
            if (pawn != null)
            {
                int OldAgeHediffs = this.AgeHediffs(pawn);

                return pawn.health.hediffSet.GetHediffs<Hediff>().Count() - OldAgeHediffs;
            }

            return 0;
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            refuelable = GetComp<CompRefuelable>();
            power = GetComp<CompPowerTrader>();
            props = power.Props;
            fuelprops = refuelable.Props;
            fuelprops.fuelConsumptionRate = fuelConsumption / 60f;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>(ref cryptoHediffCooldown, "cryptoHediffCooldown");
            Scribe_Values.LookValue<int>(ref enterTime, "enterTime");
        }
        public override void Tick()
        {
            if (HasAnyContents && refuelable.HasFuel)
            {
                Pawn pawn = ContainedThing as Pawn;

                // Remove all health-related injuries if they're younger than age 21.
                if (restoreCoolDown > 0 &&  pawn.ageTracker.AgeBiologicalTicks <= GenDate.TicksPerYear * 24)
                {
                    // Heal brain injuries
                    if (power.PowerOn)
                    {
                        long ticksLeft = (pawn.ageTracker.AgeBiologicalTicks - restoreCoolDown);
                        refuelable.ConsumeFuel(fuelConsumption / GenDate.TicksPerYear);
                        if (ticksLeft % (rate * 100) == 0)
                        {
                            Log.Message("Restore Cooldown: " + ticksLeft);
                        }

                        if (restoreCoolDown >= pawn.ageTracker.AgeBiologicalTicks)
                        {
                            Log.Message("Cooled down");
                            string hediffName;
                            foreach (Hediff oldHediff in pawn.health.hediffSet.GetHediffs<Hediff>().ToList())
                            {
                                hediffName = oldHediff.def.label;

                                if (hediffName == "gunshot")
                                {
                                    pawn.health.RemoveHediff(oldHediff);
                                }
                                else
                                {
                                    pawn.health.RemoveHediff(oldHediff);
                                }

                                restoreCoolDown = pawn.ageTracker.AgeBiologicalTicks - GenDate.TicksPerYear;
                                //restoreCoolDown = pawn.ageTracker.AgeBiologicalTicks - GenDate.TicksPerSeason;
                                Log.Message("Cured HEDIFF: " + hediffName);
                                
                                break;
                            }
                        }
                    }
                }

                bool hasHediffs = AgeHediffs(pawn) > 0;
                if (hasHediffs || pawn.ageTracker.AgeBiologicalTicks > GenDate.TicksPerYear * 21)
                {
                    power.PowerOutput = -props.basePowerConsumption;
                    if (power.PowerOn)
                    {
                        refuelable.ConsumeFuel(fuelConsumption / GenDate.TicksPerYear);
                        cryptoHediffCooldown = Math.Max(cryptoHediffCooldown - 1, 0);
                        if (pawn.ageTracker.AgeBiologicalTicks > GenDate.TicksPerYear * 21)
                            pawn.ageTracker.AgeBiologicalTicks = Math.Max(pawn.ageTracker.AgeBiologicalTicks - rate, GenDate.TicksPerYear * 21);
                        if (hasHediffs && cryptoHediffCooldown == 0)
                        {
                            foreach (Hediff oldHediff in pawn.health.hediffSet.GetHediffs<Hediff>().ToList())
                            {
                                string hediffName = oldHediff.def.label;
                                if (hediffName == "bad back" && pawn.ageTracker.AgeBiologicalYears < 39)
                                {
                                    pawn.health.RemoveHediff(oldHediff);
                                    cryptoHediffCooldown = cryptoHediffCooldownBase;
                                    break;
                                }
                                else if (hediffName == "frail" && pawn.ageTracker.AgeBiologicalYears < 48)
                                {
                                    pawn.health.RemoveHediff(oldHediff);
                                    cryptoHediffCooldown = cryptoHediffCooldownBase;
                                    break;
                                }
                                else if (hediffName == "cataract" && pawn.ageTracker.AgeBiologicalYears < 52)
                                {
                                    foreach (Hediff cataractHediff in pawn.health.hediffSet.GetHediffs<Hediff>().ToList())
                                    {
                                        if (cataractHediff.def.label == "cataract")
                                        {
                                            pawn.health.RemoveHediff(cataractHediff);
                                        }
                                    }
                                    cryptoHediffCooldown = cryptoHediffCooldownBase;
                                    break;
                                }
                                else if (hediffName == "hearing loss" && pawn.ageTracker.AgeBiologicalYears < 52)
                                {
                                    foreach (Hediff hearingHediff in pawn.health.hediffSet.GetHediffs<Hediff>().ToList())
                                    {
                                        if (hearingHediff.def.label.ToString() == "hearing loss")
                                        {
                                            pawn.health.RemoveHediff(hearingHediff);
                                        }
                                    }
                                    cryptoHediffCooldown = cryptoHediffCooldownBase;
                                    break;
                                }
                                else if (hediffName == "dementia" && pawn.ageTracker.AgeBiologicalYears < 66)
                                {
                                    pawn.health.RemoveHediff(oldHediff);
                                    cryptoHediffCooldown = cryptoHediffCooldownBase;
                                    break;
                                }
                                else if (hediffName == "alzheimer's" && pawn.ageTracker.AgeBiologicalYears < 72)
                                {
                                    //oldHediff.Heal(1 / 7.5f);
                                    oldHediff.DirectHeal(1 / 7.5f);
                                    if (oldHediff.Severity > 0) cryptoHediffCooldown = GenDate.TicksPerDay;
                                    else
                                    {
                                        cryptoHediffCooldown = cryptoHediffCooldownBase;
                                        pawn.health.RemoveHediff(oldHediff);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                    power.PowerOutput = 0;
            }
            else
                power.PowerOutput = 0;
        }
        public override void EjectContents()
        {
            Pawn pawn = ContainedThing as Pawn;
            if (pawn.ageTracker.AgeBiologicalTicks >= GenDate.TicksPerYear * 21)
            {
                pawn.health.AddHediff(cryosickness);
            }
            power.PowerOutput = 0;
            base.EjectContents();
        }

        public override bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (base.TryAcceptThing(thing, allowSpecialEffects))
            {
                cryptoHediffCooldown = cryptoHediffCooldownBase;
                enterTime = Find.TickManager.TicksGame;
                if (refuelable.HasFuel && (AgeHediffs(thing as Pawn) > 0 || (thing as Pawn).ageTracker.AgeBiologicalTicks > GenDate.TicksPerYear * 21))
                {
                    power.PowerOutput = -props.basePowerConsumption;
                }

                return true;
            }

            return false;
        }

        public override string GetInspectString()
        {
            if (HasAnyContents)
            {
                Pawn pawn = ContainedThing as Pawn;
                pawn.ageTracker.AgeBiologicalTicks.TicksToPeriod(out int years, out int quadrums, out int days, out float hours);
                string bioTime = "AgeBiological".Translate(new object[]{years,quadrums,days});
                return base.GetInspectString() + ", " + AgeHediffs(pawn).ToString() + " Age Disabilities, " + InjuryHediffs(pawn).ToString() + " Injuries\n" + bioTime;
            }
            else return base.GetInspectString();
        }
    }
}