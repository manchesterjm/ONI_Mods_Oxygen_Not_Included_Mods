using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KMod;
using UnityEngine;

namespace CritterCondoAnywhere
{
    // Original mod. The three base-game Critter Condos only work inside a Stable
    // (CreaturePen) room. Each config gives its building a RoomTracker that REQUIRES the
    // Stable room, and the building's operational callback refuses to run unless
    // RoomTracker.IsInCorrectRoom() is true. This mod lets all three condos be built and
    // used anywhere, while preserving each condo's OTHER conditions (the Aquatic condo
    // still needs its cells submerged in liquid; the land condo still needs to be un-flooded).
    public class CritterCondoAnywhere : UserMod2
    {
        // The condo configs override this IBuildingConfig method to set up the Stable-room
        // requirement; it is the seam this mod relaxes.
        private const string ConfigureCompleteMethod = "DoPostConfigureComplete";

        // The three base-game condo building IDs, taken straight from the game's own configs
        // so they track any rename Klei makes.
        private static readonly HashSet<Tag> CondoBuildingIds = new HashSet<Tag>
        {
            new Tag(CritterCondoConfig.ID),
            new Tag(UnderwaterCritterCondoConfig.ID),
            new Tag(AirBorneCritterCondoConfig.ID)
        };

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
        }

        // Stop the condo configs from REQUIRING a Stable room. Demoting the RoomTracker to
        // "TrackingOnly" drops the "Dedicated Room: Stable" requirement line, the
        // "not in required room" warning, and the must-be-built-inside-a-room placement rule —
        // while leaving requiredRoomType set, so the engine's RoomTracker assert still passes.
        [HarmonyPatch]
        public static class StableRoomNotRequiredPatch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(CritterCondoConfig), ConfigureCompleteMethod);
                yield return AccessTools.Method(typeof(UnderwaterCritterCondoConfig), ConfigureCompleteMethod);
                yield return AccessTools.Method(typeof(AirBorneCritterCondoConfig), ConfigureCompleteMethod);
            }

            public static void Postfix(GameObject go)
            {
                RoomTracker roomTracker = go.GetComponent<RoomTracker>();
                if (roomTracker != null)
                {
                    roomTracker.requirement = RoomTracker.Requirement.TrackingOnly;
                }
            }
        }

        // The condos' operational callbacks gate on RoomTracker.IsInCorrectRoom() before their
        // own checks. Forcing the room check to pass for the three condos only lets them operate
        // anywhere, leaving the liquid / un-flooded / operational checks that follow it intact.
        [HarmonyPatch(typeof(RoomTracker), nameof(RoomTracker.IsInCorrectRoom))]
        public static class CondoCountsAsInRoomPatch
        {
            public static void Postfix(RoomTracker __instance, ref bool __result)
            {
                if (__result || !IsCondo(__instance))
                {
                    return;
                }
                __result = true;
            }

            private static bool IsCondo(RoomTracker roomTracker)
            {
                KPrefabID prefabId = roomTracker.GetComponent<KPrefabID>();
                return prefabId != null && CondoBuildingIds.Contains(prefabId.PrefabTag);
            }
        }
    }
}
