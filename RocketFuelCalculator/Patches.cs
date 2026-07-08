using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RocketFuelCalculator
{
    // Rocket Fuel Calculator (ORIGINAL mod, 2026-07-08). Base-game rockets burn
    // EVERYTHING loaded in their fuel and oxidizer tanks on every launch, so any
    // load beyond what the trip needs is pure waste. This recreates the removed
    // Steam Workshop "Rocket Calculator" (2077848080) behavior Josh liked: when
    // a destination is assigned on the star map, compute the minimum fuel +
    // oxidizer load that still reaches it and write that into every tank's fill
    // slider (IUserControlledCapacity.UserMaxCapacity), then pop the numbers
    // over the command module.
    //
    // Flight model mirrored from RocketStats (decompiled build 740622):
    //   reachable = TotalThrust - ROCKETRY.CalculateMassWithPenalty(TotalMass)
    //   TotalThrust = min(fuelKg, oxidizerKg) * engine.efficiency * oxidizerQuality
    //                 (fuelKg * efficiency alone for engines without oxidizer)
    //                 + boosterEfficiency * min(ironKg, oxyliteKg) per booster
    //   destination reachable when reachable >= OneBasedDistance * 10000
    // TUNING values are read live so game retunes can't strand us. Boosters have
    // no player slider and auto-fill, so they are projected full. Oxidizer
    // quality comes from what the tanks currently hold (Oxylite 1.0 / LOX 1.33),
    // defaulting to Oxylite when empty - a conservative floor: better oxidizer
    // only overshoots, never strands.
    public static class RocketFuelCalculatorPatches
    {
        // Extra kilograms above the mathematical minimum so delivery rounding
        // can never leave the rocket a hair short of the destination.
        private const float SafetyMarginKg = 1f;

        private const float PopupSeconds = 15f;

        [HarmonyPatch(typeof(SpacecraftManager), nameof(SpacecraftManager.SetSpacecraftDestination))]
        public static class SpacecraftManager_SetSpacecraftDestination_Patch
        {
            public static void Postfix(SpacecraftManager __instance,
                LaunchConditionManager lcm, SpaceDestination destination)
            {
                if (lcm == null || destination == null)
                {
                    return;
                }
                Spacecraft craft = __instance.GetSpacecraftFromLaunchConditionManager(lcm);
                if (craft == null || craft.state != Spacecraft.MissionState.Grounded)
                {
                    return;
                }
                CommandModule commandModule = lcm.GetComponent<CommandModule>();
                if (commandModule == null)
                {
                    return;
                }
                RocketSurvey rocket = RocketSurvey.Of(commandModule);
                if (rocket == null)
                {
                    // No main engine yet - vanilla's launch checklist already nags.
                    return;
                }
                float neededDistance = destination.OneBasedDistance * 10000f;
                FuelSolution solution = rocket.CheapestLoadReaching(neededDistance);
                rocket.WriteSliders(solution);
                Announce(commandModule, rocket, solution, neededDistance);
            }
        }

        private static void Announce(CommandModule commandModule, RocketSurvey rocket,
            FuelSolution solution, float neededDistance)
        {
            string fuelLine = "Fuel: " + GameUtil.GetFormattedMass(solution.BurnableKg);
            if (rocket.NeedsOxidizer)
            {
                fuelLine += "\nOxidizer: " + GameUtil.GetFormattedMass(solution.BurnableKg);
            }
            string rangeLine = "Range: " + GameUtil.GetFormattedDistance(solution.ReachableDistance * 1000f)
                + " / needs " + GameUtil.GetFormattedDistance(neededDistance * 1000f);
            string verdict = solution.ReachesTarget
                ? "Tank sliders set for this destination"
                : "DESTINATION OUT OF RANGE - sliders set for max range";
            PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Resource,
                "Rocket Fuel Calculator\n" + verdict + "\n" + fuelLine + "\n" + rangeLine,
                commandModule.transform, PopupSeconds);
        }

        // Snapshot of one grounded rocket: its engine, tanks, boosters, and dry
        // mass, with the projections needed to plan a load before it is delivered.
        private sealed class RocketSurvey
        {
            private RocketEngine mainEngine;
            private readonly List<FuelTank> fuelTanks = new List<FuelTank>();
            private readonly List<OxidizerTank> oxidizerTanks = new List<OxidizerTank>();
            private float dryMassKg;
            private float boosterThrustWhenFull;
            private float boosterFuelKgWhenFull;
            private float fuelCapacityKg;
            private float oxidizerCapacityKg;
            private float oxidizerQuality;
            private float dataBankRangeCap = float.MaxValue;

            public bool NeedsOxidizer => mainEngine.requireOxidizer;

            public static RocketSurvey Of(CommandModule commandModule)
            {
                RocketSurvey survey = new RocketSurvey();
                foreach (GameObject module in AttachableBuilding.GetAttachedNetwork(
                    commandModule.GetComponent<AttachableBuilding>()))
                {
                    survey.TakeStockOf(module);
                }
                if (survey.mainEngine == null)
                {
                    return null;
                }
                survey.oxidizerQuality = survey.OxidizerQualityOnBoard();
                RoboPilotModule roboPilot = commandModule.GetComponent<RoboPilotModule>();
                if (roboPilot != null)
                {
                    survey.dataBankRangeCap = roboPilot.GetDataBankRange();
                }
                return survey;
            }

            private void TakeStockOf(GameObject module)
            {
                RocketModule rocketModule = module.GetComponent<RocketModule>();
                if (rocketModule != null)
                {
                    dryMassKg += rocketModule.GetComponent<PrimaryElement>().Mass;
                }
                RocketEngine engine = module.GetComponent<RocketEngine>();
                if (engine != null && engine.mainEngine && !(engine is SolidBooster))
                {
                    mainEngine = engine;
                }
                SolidBooster booster = module.GetComponent<SolidBooster>();
                if (booster != null)
                {
                    // Boosters auto-fill half iron / half oxylite with no player
                    // slider; thrust burns the matched pair.
                    float fullLoadKg = booster.fuelStorage.capacityKg;
                    boosterFuelKgWhenFull += fullLoadKg;
                    boosterThrustWhenFull += booster.efficiency * (fullLoadKg / 2f);
                }
                FuelTank fuelTank = module.GetComponent<FuelTank>();
                if (fuelTank != null)
                {
                    fuelTanks.Add(fuelTank);
                    fuelCapacityKg += fuelTank.physicalFuelCapacity;
                }
                OxidizerTank oxidizerTank = module.GetComponent<OxidizerTank>();
                if (oxidizerTank != null)
                {
                    oxidizerTanks.Add(oxidizerTank);
                    oxidizerCapacityKg += oxidizerTank.MaxCapacity;
                }
            }

            private float OxidizerQualityOnBoard()
            {
                float massKg = 0f;
                float weightedQuality = 0f;
                foreach (OxidizerTank tank in oxidizerTanks)
                {
                    foreach (KeyValuePair<Tag, float> oxidizer in tank.GetOxidizersAvailable())
                    {
                        if (RocketStats.oxidizerEfficiencies.TryGetValue(oxidizer.Key, out float quality))
                        {
                            massKg += oxidizer.Value;
                            weightedQuality += oxidizer.Value * quality;
                        }
                    }
                }
                if (massKg <= 0f)
                {
                    return TUNING.ROCKETRY.OXIDIZER_EFFICIENCY.LOW;
                }
                return weightedQuality / massKg;
            }

            // Distance the rocket reaches with burnableKg of fuel aboard (and the
            // same mass of oxidizer when the engine needs it), boosters full.
            public float ReachableDistance(float burnableKg)
            {
                float engineThrust = NeedsOxidizer
                    ? burnableKg * mainEngine.efficiency * oxidizerQuality
                    : burnableKg * mainEngine.efficiency;
                float loadedTankKg = NeedsOxidizer ? burnableKg * 2f : burnableKg;
                float totalMassKg = dryMassKg + loadedTankKg + boosterFuelKgWhenFull;
                float reach = engineThrust + boosterThrustWhenFull
                    - TUNING.ROCKETRY.CalculateMassWithPenalty(totalMassKg);
                return Mathf.Max(0f, Mathf.Min(reach, dataBankRangeCap));
            }

            // The minimum load that reaches neededDistance; when nothing does
            // (mass penalty makes range rise then fall), the load giving the
            // best range instead.
            public FuelSolution CheapestLoadReaching(float neededDistance)
            {
                float maxBurnableKg = NeedsOxidizer
                    ? Mathf.Min(fuelCapacityKg, oxidizerCapacityKg)
                    : fuelCapacityKg;
                float firstReachingKg = -1f;
                float bestKg = 0f;
                float bestDistance = ReachableDistance(0f);
                int wholeKgSteps = Mathf.CeilToInt(maxBurnableKg);
                for (int step = 0; step <= wholeKgSteps; step++)
                {
                    float kg = Mathf.Min(step, maxBurnableKg);
                    float distance = ReachableDistance(kg);
                    if (distance > bestDistance)
                    {
                        bestDistance = distance;
                        bestKg = kg;
                    }
                    if (distance >= neededDistance)
                    {
                        firstReachingKg = kg;
                        break;
                    }
                }
                if (firstReachingKg < 0f)
                {
                    return FuelSolution.OutOfRange(bestKg, bestDistance);
                }
                float minimumKg = RefineDownToMinimum(firstReachingKg, neededDistance);
                float loadKg = Mathf.Min(minimumKg + SafetyMarginKg, maxBurnableKg);
                return FuelSolution.Reaching(loadKg, ReachableDistance(loadKg));
            }

            // Bisect inside the 1 kg bracket the coarse scan landed in: lo is
            // always short of the target, hi always reaches it.
            private float RefineDownToMinimum(float reachingKg, float neededDistance)
            {
                float lo = Mathf.Max(0f, reachingKg - 1f);
                if (ReachableDistance(lo) >= neededDistance)
                {
                    return lo;
                }
                float hi = reachingKg;
                for (int i = 0; i < 25; i++)
                {
                    float mid = (lo + hi) / 2f;
                    if (ReachableDistance(mid) >= neededDistance)
                    {
                        hi = mid;
                    }
                    else
                    {
                        lo = mid;
                    }
                }
                return hi;
            }

            // Spread the load across tanks in proportion to each tank's physical
            // capacity (a share can never exceed its tank). Engines that burn no
            // oxidizer get their oxidizer tanks zeroed - dead mass otherwise.
            public void WriteSliders(FuelSolution solution)
            {
                foreach (FuelTank tank in fuelTanks)
                {
                    tank.UserMaxCapacity = fuelCapacityKg > 0f
                        ? solution.BurnableKg * (tank.physicalFuelCapacity / fuelCapacityKg)
                        : 0f;
                }
                foreach (OxidizerTank tank in oxidizerTanks)
                {
                    tank.UserMaxCapacity = NeedsOxidizer && oxidizerCapacityKg > 0f
                        ? solution.BurnableKg * (tank.MaxCapacity / oxidizerCapacityKg)
                        : 0f;
                }
            }
        }

        private readonly struct FuelSolution
        {
            public readonly float BurnableKg;
            public readonly float ReachableDistance;
            public readonly bool ReachesTarget;

            private FuelSolution(float burnableKg, float reachableDistance, bool reachesTarget)
            {
                BurnableKg = burnableKg;
                ReachableDistance = reachableDistance;
                ReachesTarget = reachesTarget;
            }

            public static FuelSolution Reaching(float burnableKg, float reachableDistance)
            {
                return new FuelSolution(burnableKg, reachableDistance, reachesTarget: true);
            }

            public static FuelSolution OutOfRange(float bestKg, float bestDistance)
            {
                return new FuelSolution(bestKg, bestDistance, reachesTarget: false);
            }
        }
    }
}
