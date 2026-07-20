using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Database;
using HarmonyLib;

namespace PlanetAdder
{
    // Port of Schalasoft's Planet-Adder (github.com/Schalasoft/Planet-Adder) rebuilt for the
    // current game build. Manual, config-driven planet injection for the base-game starmap:
    // each "TypeName,row,slot" line in config.txt (next to the DLL) becomes one destination at
    // that distance row (0-based, range needed = (row+1) x 10,000 km) and orbit slot (0-9).
    // After a successful apply the config is renamed config.txt.old so planets are added to the
    // save exactly once. Upstream's logging scaffolding is dropped in favor of Player.log.
    [HarmonyPatch(typeof(StarmapScreen), "LoadPlanets")]
    public static class StarmapScreen_LoadPlanets_Patch
    {
        public static void Prefix()
        {
            SpacecraftManager manager = Game.Instance == null ? null : Game.Instance.spacecraftManager;
            if (manager == null || manager.destinations == null)
            {
                return;
            }
            string configPath = Path.Combine(ModFolder(), "config.txt");
            if (!File.Exists(configPath))
            {
                return;
            }
            int added = 0;
            foreach (string line in File.ReadAllLines(configPath))
            {
                if (TryParsePlanet(line, out SpaceDestinationType planetType, out int row, out int slot))
                {
                    SpaceDestination destination =
                        new SpaceDestination(manager.destinations.Count, planetType.Id, row)
                        {
                            startingOrbitPercentage = slot / 10f,
                        };
                    manager.destinations.Add(destination);
                    added++;
                    Debug.Log("[PlanetAdder] Added " + planetType.Id + " at row " + row + " slot " + slot);
                }
            }
            if (added > 0)
            {
                RetireConfig(configPath);
            }
        }

        private static bool TryParsePlanet(string line, out SpaceDestinationType planetType, out int row, out int slot)
        {
            planetType = null;
            row = 0;
            slot = 0;
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                return false;
            }
            string[] parts = trimmed.Split(',');
            if (parts.Length != 3 ||
                !ConfigNames().TryGetValue(parts[0].Trim(), out planetType) ||
                !int.TryParse(parts[1].Trim(), out row) ||
                !int.TryParse(parts[2].Trim(), out slot))
            {
                Debug.LogWarning("[PlanetAdder] Skipping malformed config line: " + trimmed);
                return false;
            }
            return true;
        }

        // Upstream's config vocabulary, kept verbatim for config compatibility ("DustyDwarf" is
        // its alias for DustyMoon). Typed Db fields dodge Klei's internal-ID quirks
        // (OilyAsteroid's ID is the typo "OilyAsteriod"; HydrogenGiant's is "HeliumGiant").
        private static Dictionary<string, SpaceDestinationType> ConfigNames()
        {
            SpaceDestinationTypes types = Db.Get().SpaceDestinationTypes;
            return new Dictionary<string, SpaceDestinationType>
            {
                { "Satellite", types.Satellite },
                { "MetallicAsteroid", types.MetallicAsteroid },
                { "RockyAsteroid", types.RockyAsteroid },
                { "CarbonaceousAsteroid", types.CarbonaceousAsteroid },
                { "IcyDwarf", types.IcyDwarf },
                { "OrganicDwarf", types.OrganicDwarf },
                { "TerraPlanet", types.TerraPlanet },
                { "VolcanoPlanet", types.VolcanoPlanet },
                { "GasGiant", types.GasGiant },
                { "IceGiant", types.IceGiant },
                { "DustyDwarf", types.DustyMoon },
                { "Wormhole", types.Wormhole },
                { "SaltDwarf", types.SaltDwarf },
                { "RustPlanet", types.RustPlanet },
                { "ForestPlanet", types.ForestPlanet },
                { "RedDwarf", types.RedDwarf },
                { "GoldAsteroid", types.GoldAsteroid },
                { "HydrogenGiant", types.HydrogenGiant },
                { "OilyAsteroid", types.OilyAsteroid },
                { "ShinyPlanet", types.ShinyPlanet },
                { "ChlorinePlanet", types.ChlorinePlanet },
                { "SaltDesertPlanet", types.SaltDesertPlanet },
                { "Earth", types.Earth },
            };
        }

        private static string ModFolder()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private static void RetireConfig(string configPath)
        {
            string retiredPath = configPath + ".old";
            if (File.Exists(retiredPath))
            {
                File.Delete(retiredPath);
            }
            File.Move(configPath, retiredPath);
        }
    }
}
