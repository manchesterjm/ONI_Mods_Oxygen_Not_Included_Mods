using STRINGS;

namespace BuildableNaturalTile
{
    // In-tree replacements for the CaiLib helpers the original mod relied on,
    // written against the current game API (build 737790). Named for the call
    // site so the patch code reads as plain English.
    internal static class ModUtils
    {
        // The "Natural Tile" building lives in the Base build menu and is unlocked
        // by the same tech that unlocks the Ration Box (Basic Farming), matching
        // the original CoolAzura mod.
        public const string BaseCategory = "Base";
        public const string UncategorizedSubcategory = "uncategorized";
        public const string FarmingTech = "FarmingTech";

        public static void AddBuildingStrings(string buildingId, string name, string description, string effect)
        {
            string key = buildingId.ToUpperInvariant();
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.NAME", UI.FormatAsLink(name, buildingId));
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.DESC", description);
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.EFFECT", effect);
        }

        public static void AddBuildingToBaseMenu(string buildingId)
        {
            ModUtil.AddBuildingToPlanScreen((HashedString)BaseCategory, buildingId, UncategorizedSubcategory);
        }

        public static void UnlockBuildingWithBasicFarming(string buildingId)
        {
            Db.Get().Techs.Get(FarmingTech).unlockedItemIDs.Add(buildingId);
        }
    }
}
