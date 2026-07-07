using HarmonyLib;

namespace InsulatedDoor
{
    // Insulated Door (Davkas, Steam 2054432034). Revived from the shipped DLL and
    // rebuilt for build 737790; the original (2020-2022) broke on the Aquatic update.
    // Adds four insulated door buildings (manual + mechanized, full + tiny) whose
    // very low thermal conductivity blocks heat between rooms.
    //
    // 737790 port notes:
    //   * The original Door.OnPrefabInit anim patch is back, reworked (see
    //     Door_OnPrefabInit_Patch below). The 2026-06-27 rebuild dropped it because
    //     the base game now sets overrideAnims itself — but the game does it from a
    //     static array that goes null when a mod's Harmony patch on Door runs the
    //     class constructor before the anim database loads, crashing any dupe sent
    //     to operate a door (seen live 2026-07-07).
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

        // Door.OnPrefabInit copies the static Door.OVERRIDE_ANIMS array — the dupe's
        // anim_use_remote "operate this door" animation — onto every door. That static
        // is initialized the first time the Door class is touched, and Harmony's
        // documented behaviour is that patching a method runs its class constructor;
        // a mod patching Door at load time (our Self-sealing Airlocks fork does) fires
        // it before the anim database exists, so Assets.GetAnim returns null and every
        // door carries a null override. StandardWorker.AttachOverrideAnims null-checks
        // the array but not its elements → NullReferenceException in
        // KAnimControllerBase.AddAnimOverrides the moment a dupe starts operating a
        // door (crash seen live 2026-07-07, dupe Pei vs InsulatedPressureDoor).
        // Repair at prefab-init time, when the anim database is loaded. This runs for
        // vanilla doors too — they share the same poisoned static array.
        [HarmonyPatch(typeof(Door), "OnPrefabInit")]
        public static class Door_OnPrefabInit_Patch
        {
            public static void Postfix(Door __instance)
            {
                KAnimFile[] anims = __instance.overrideAnims;
                if (anims == null || !System.Array.Exists(anims, anim => anim == null))
                {
                    return;
                }
                KAnimFile useRemoteAnim = Assets.GetAnim("anim_use_remote_kanim");
                __instance.overrideAnims =
                    useRemoteAnim != null ? new[] { useRemoteAnim } : null;
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
