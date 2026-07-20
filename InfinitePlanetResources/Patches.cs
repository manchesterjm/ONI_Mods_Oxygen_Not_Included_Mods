using Database;
using HarmonyLib;

namespace InfinitePlanetResources
{
    // Port of Schalasoft's Infinite-Planet-Resources (github.com/Schalasoft/Infinite-Planet-Resources)
    // rebuilt for the current game build. Every Sim1000ms tick the game calls Replenish on each
    // starmap destination; topping availableMass back up here means mining rockets never deplete
    // a planet. Upstream wrote availableMass = maxiumMass, which overfills: CurrentMass is
    // minimumMass + availableMass, so "full" is the difference (what the game's own constructor uses).
    [HarmonyPatch(typeof(SpaceDestination), nameof(SpaceDestination.Replenish))]
    public static class SpaceDestination_Replenish_Patch
    {
        private static readonly AccessTools.FieldRef<SpaceDestination, float> AvailableMass =
            AccessTools.FieldRefAccess<SpaceDestination, float>("availableMass");

        public static void Postfix(SpaceDestination __instance)
        {
            SpaceDestinationType destinationType = __instance.GetDestinationType();
            AvailableMass(__instance) = destinationType.maxiumMass - destinationType.minimumMass;
        }
    }
}
