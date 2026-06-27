using HarmonyLib;
using KMod;
using UnityEngine;

namespace HighPressurePumpTint
{
    // Companion to the High Pressure Mod (Steam 1816824573): its Pressure Gas Pump
    // and Pressure (liquid) Pump reuse the vanilla pump kanims, so a placed upgraded
    // pump is visually indistinguishable from the stock one. This tints the two
    // completed pumps a warm gold so you can tell them apart at a glance. Pure
    // visual; touches nothing else. Built for build 737790.
    public class HighPressurePumpTintMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
        }
    }

    [HarmonyPatch(typeof(BuildingComplete), "OnSpawn")]
    public static class BuildingComplete_OnSpawn_Patch
    {
        // Warm gold; change these RGB values to retint.
        private static readonly Color32 HighPressureTint = new Color32(255, 190, 60, 255);

        public static void Postfix(BuildingComplete __instance)
        {
            if (__instance.name != "PressureGasPumpComplete" && __instance.name != "PressurePumpComplete")
            {
                return;
            }
            KBatchedAnimController controller = __instance.GetComponent<KBatchedAnimController>();
            if (controller != null)
            {
                controller.TintColour = HighPressureTint;
            }
        }
    }
}
