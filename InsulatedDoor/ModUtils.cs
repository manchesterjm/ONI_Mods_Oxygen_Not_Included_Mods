using System.Collections.Generic;
using STRINGS;
using TUNING;

namespace InsulatedDoor
{
    // Helpers for registering the insulated door buildings, written against the
    // current game API (build 737790). The original mod hardcoded tech IDs
    // ("TemperatureModulation"/"HVAC") and hand-edited TUNING.BUILDINGS.PLANORDER.data;
    // both drifted. Instead these look the placement up at runtime from the
    // matching VANILLA door, so the insulated variant always lands in the same
    // research node and menu subcategory even if Klei renames things.
    internal static class ModUtils
    {
        public const string BaseCategory = "Base";

        public static void AddBuildingStrings(string buildingId, string name, string description, string effect)
        {
            string key = buildingId.ToUpperInvariant();
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.NAME", UI.FormatAsLink(name, buildingId));
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.DESC", description);
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.EFFECT", effect);
        }

        // Insert the door into the Base build menu, right after `afterBuildingId`,
        // reusing that building's subcategory so they sit together.
        public static void AddDoorToBaseMenuAfter(string doorId, string afterBuildingId)
        {
            int order = TUNING.BUILDINGS.PLANORDER.FindIndex(x => x.category == (HashedString)BaseCategory);
            if (order < 0)
            {
                return;
            }
            List<KeyValuePair<string, string>> entries = TUNING.BUILDINGS.PLANORDER[order].buildingAndSubcategoryData;
            string subcategory = "uncategorized";
            foreach (KeyValuePair<string, string> entry in entries)
            {
                if (entry.Key == afterBuildingId)
                {
                    subcategory = entry.Value;
                    break;
                }
            }
            // 5-arg overload defaults ordering to After.
            ModUtil.AddBuildingToPlanScreen((HashedString)BaseCategory, doorId, subcategory, afterBuildingId);
        }

        // Unlock the door in whichever tech node already unlocks the vanilla door.
        public static void UnlockUnderSameTechAs(string doorId, string vanillaDoorId)
        {
            foreach (Tech tech in Db.Get().Techs.resources)
            {
                if (tech.unlockedItemIDs.Contains(vanillaDoorId))
                {
                    if (!tech.unlockedItemIDs.Contains(doorId))
                    {
                        tech.unlockedItemIDs.Add(doorId);
                    }
                    return;
                }
            }
        }
    }
}
