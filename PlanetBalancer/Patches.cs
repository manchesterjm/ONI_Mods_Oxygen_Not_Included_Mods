using System.Collections.Generic;
using Database;
using HarmonyLib;

namespace PlanetBalancer
{
    // Reimplementation of Blackstiff's dead Workshop mod "Planet Balancer" (1995546475):
    // guarantee every planet type from the vanilla starmap generator pool appears at least
    // once, backfilling missing ones at a random row between 80,000 and 170,000 km (the
    // original mod's range). Runs after the game restores/generates destinations; additions
    // serialize into the save, so an already-balanced save is a no-op on later loads.
    // Spaced Out uses the cluster starmap instead — deliberately untouched.
    [HarmonyPatch(typeof(SpacecraftManager), "OnSpawn")]
    public static class SpacecraftManager_OnSpawn_Patch
    {
        private const int MinRow = 7;  // OneBasedDistance 8  = 80,000 km
        private const int MaxRow = 16; // OneBasedDistance 17 = 170,000 km

        public static void Postfix(SpacecraftManager __instance)
        {
            if (DlcManager.FeatureClusterSpaceEnabled() || __instance.destinations == null)
            {
                return;
            }
            foreach (SpaceDestinationType planetType in VanillaGeneratorPool())
            {
                if (!HasDestinationOfType(__instance, planetType.Id) &&
                    __instance.AddDestination(planetType.Id,
                        SpacecraftManager.DestinationLocationSelectionType.Random, MinRow, MaxRow))
                {
                    Debug.Log("[PlanetBalancer] Added missing planet type " + planetType.Id);
                }
            }
        }

        private static bool HasDestinationOfType(SpacecraftManager manager, string typeId)
        {
            foreach (SpaceDestination destination in manager.destinations)
            {
                if (destination.type == typeId)
                {
                    return true;
                }
            }
            return false;
        }

        // The union of the tier lists in SpacecraftManager.GenerateRandomDestinations.
        // Earth and the Temporal Tear always exist; DLC mixing types (Ceres/Prehistoric/
        // Demolior/Aquatic) and the generator-unreachable SaltDesertPlanet are deliberately
        // excluded — forcing those into a base-game save is not this mod's promise.
        private static IEnumerable<SpaceDestinationType> VanillaGeneratorPool()
        {
            SpaceDestinationTypes types = Db.Get().SpaceDestinationTypes;
            return new[]
            {
                types.Satellite, types.MetallicAsteroid, types.RockyAsteroid,
                types.CarbonaceousAsteroid, types.IcyDwarf, types.OrganicDwarf,
                types.DustyMoon, types.TerraPlanet, types.VolcanoPlanet,
                types.GasGiant, types.IceGiant, types.SaltDwarf, types.RustPlanet,
                types.ForestPlanet, types.RedDwarf, types.GoldAsteroid,
                types.HydrogenGiant, types.OilyAsteroid, types.ShinyPlanet,
                types.ChlorinePlanet,
            };
        }
    }
}
