using HarmonyLib;
using static DecorLights.ModUtils;

namespace DecorLights
{
	public class DecorLightsPatches
	{
		[HarmonyPatch(typeof(GeneratedBuildings))]
		[HarmonyPatch(nameof(GeneratedBuildings.LoadGeneratedBuildings))]
		public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
		{
			public static void Prefix()
			{
				AddBuildingStrings(LavaLampConfig.Id, LavaLampConfig.DisplayName, LavaLampConfig.Description, LavaLampConfig.Effect);
				AddBuildingStrings(SaltLampConfig.Id, SaltLampConfig.DisplayName, SaltLampConfig.Description, SaltLampConfig.Effect);
				AddBuildingStrings(CeilingLampConfig.Id, CeilingLampConfig.DisplayName, CeilingLampConfig.Description, CeilingLampConfig.Effect);
				AddBuildingStrings(LuminiferousSphereConfig.Id, LuminiferousSphereConfig.DisplayName, LuminiferousSphereConfig.Description, LuminiferousSphereConfig.Effect);

				AddBuildingToFurnitureMenu(LavaLampConfig.Id);
				AddBuildingToFurnitureMenu(SaltLampConfig.Id);
				AddBuildingToFurnitureMenu(CeilingLampConfig.Id);
				AddBuildingToFurnitureMenu(LuminiferousSphereConfig.Id);
			}
		}

		[HarmonyPatch(typeof(Db))]
		[HarmonyPatch("Initialize")]
		public static class Db_Initialize_Patch
		{
			public static void Postfix()
			{
				UnlockBuildingWithGlassFurnishings(LavaLampConfig.Id);
				UnlockBuildingWithGlassFurnishings(SaltLampConfig.Id);
				UnlockBuildingWithGlassFurnishings(CeilingLampConfig.Id);
				UnlockBuildingWithGlassFurnishings(LuminiferousSphereConfig.Id);
			}
		}
	}
}
