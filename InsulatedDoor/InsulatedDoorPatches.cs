using HarmonyLib;

namespace InsulatedDoor
{
    // Insulated Door (Davkas, Steam 2054432034). Revived from the shipped DLL and
    // rebuilt for build 737790; the original (2020-2022) broke on the Aquatic update.
    // Adds four insulated door buildings (manual + mechanized, full + tiny) whose
    // very low thermal conductivity blocks heat between rooms.
    //
    // 737790 port notes:
    //   * Dropped the original Door.OnPrefabInit anim patch (set overrideAnims to
    //     anim_use_remote_kanim) — the base game does this itself now and the field
    //     changed type, same as Self-sealing Airlocks.
    //   * Replaced the hardcoded tech IDs and hand-edited BUILDINGS.PLANORDER.data
    //     with runtime lookups off the matching vanilla door (see ModUtils).
    //   * Dropped the .po localization machinery; English strings registered inline.
    public static class InsulatedDoorPatches
    {
        private const string Desc =
            "The lowered thermal conductivity of an insulated door slows heat passing through it.";
        private const string Effect = "Maintains the temperature difference between two rooms.";

        [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
        public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
        {
            public static void Prefix()
            {
                ModUtils.AddBuildingStrings(InsulatedManualPressureDoorConfig.Id,
                    "Insulated Manual Airlock", Desc, Effect);
                ModUtils.AddBuildingStrings(TinyInsulatedManualPressureDoorConfig.Id,
                    "Tiny Insulated Manual Airlock", Desc, Effect);
                ModUtils.AddBuildingStrings(InsulatedPressureDoorConfig.Id,
                    "Insulated Mechanized Airlock", Desc, Effect);
                ModUtils.AddBuildingStrings(TinyInsulatedPressureDoorConfig.Id,
                    "Tiny Insulated Mechanized Airlock", Desc, Effect);

                ModUtils.AddDoorToBaseMenuAfter(InsulatedManualPressureDoorConfig.Id, "ManualPressureDoor");
                ModUtils.AddDoorToBaseMenuAfter(TinyInsulatedManualPressureDoorConfig.Id,
                    InsulatedManualPressureDoorConfig.Id);
                ModUtils.AddDoorToBaseMenuAfter(InsulatedPressureDoorConfig.Id, "PressureDoor");
                ModUtils.AddDoorToBaseMenuAfter(TinyInsulatedPressureDoorConfig.Id,
                    InsulatedPressureDoorConfig.Id);
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        public static class Db_Initialize_Patch
        {
            public static void Postfix()
            {
                ModUtils.UnlockUnderSameTechAs(InsulatedManualPressureDoorConfig.Id, "ManualPressureDoor");
                ModUtils.UnlockUnderSameTechAs(TinyInsulatedManualPressureDoorConfig.Id, "ManualPressureDoor");
                ModUtils.UnlockUnderSameTechAs(InsulatedPressureDoorConfig.Id, "PressureDoor");
                ModUtils.UnlockUnderSameTechAs(TinyInsulatedPressureDoorConfig.Id, "PressureDoor");
            }
        }

        [HarmonyPatch(typeof(BuildingComplete), "OnSpawn")]
        public static class BuildingComplete_OnSpawn_Patch
        {
            public static void Postfix(BuildingComplete __instance)
            {
                switch (__instance.name)
                {
                    case InsulatedManualPressureDoorConfig.Id + "Complete":
                    case TinyInsulatedManualPressureDoorConfig.Id + "Complete":
                    case InsulatedPressureDoorConfig.Id + "Complete":
                    case TinyInsulatedPressureDoorConfig.Id + "Complete":
                        InsulatingDoor insulator = __instance.gameObject.AddOrGet<InsulatingDoor>();
                        insulator.ApplyInsulation(__instance.Def.ThermalConductivity);
                        break;
                }
            }
        }
    }
}
