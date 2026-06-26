using STRINGS;

namespace DecorLights
{
	// In-tree replacements for the three CaiLib helpers the original mod relied on,
	// written against the current game API (build 737790). Kept tiny and named for
	// the call site so the patch code reads as plain English.
	internal static class ModUtils
	{
		// Where these decor lamps live in the build menu / tech tree (current game IDs).
		public const string FurnitureCategory = "Furniture";
		public const string LightsSubcategory = "lights";
		public const string GlassFurnishingsTech = "GlassFurnishings";

		public static void AddBuildingStrings(string buildingId, string name, string description, string effect)
		{
			string key = buildingId.ToUpperInvariant();
			Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.NAME", UI.FormatAsLink(name, buildingId));
			Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.DESC", description);
			Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.EFFECT", effect);
		}

		public static void AddBuildingToFurnitureMenu(string buildingId)
		{
			ModUtil.AddBuildingToPlanScreen((HashedString)FurnitureCategory, buildingId, LightsSubcategory);
		}

		public static void UnlockBuildingWithGlassFurnishings(string buildingId)
		{
			Db.Get().Techs.Get(GlassFurnishingsTech).unlockedItemIDs.Add(buildingId);
		}
	}
}
