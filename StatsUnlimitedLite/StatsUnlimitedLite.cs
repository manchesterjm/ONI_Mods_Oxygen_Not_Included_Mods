using HarmonyLib;
using KMod;
using UnityEngine;

namespace StatsUnlimitedLite
{
    public class StatsUnlimitedLiteMod : UserMod2
    {
        public const int STATS_CAP = 200;  // Game default: 20

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log($"StatsUnlimitedLite loaded! Stats cap raised from 20 to {STATS_CAP}");
        }
    }

    /// <summary>
    /// Main patch - intercepts AddExperience to allow levels above 20
    /// </summary>
    [HarmonyPatch(typeof(Klei.AI.AttributeLevel), "AddExperience")]
    public static class AttributeLevel_AddExperience_Patch
    {
        public static bool Prefix(
            ref bool __result,
            Klei.AI.AttributeLevel __instance,
            Klei.AI.AttributeLevels levels,
            float experience)
        {
            // Check if already at our custom cap
            if (__instance.level >= StatsUnlimitedLiteMod.STATS_CAP)
            {
                __result = false;
                return false;  // Skip original
            }

            // Add experience
            __instance.experience += experience;
            __instance.experience = Mathf.Max(0f, __instance.experience);

            // Calculate experience needed for next level using game's formula
            float expForNextLevel = GetExperienceForNextLevel(__instance.level);

            // Check for level up
            if (__instance.experience >= expForNextLevel)
            {
                __instance.LevelUp(levels);
                __result = true;
            }
            else
            {
                __result = false;
            }

            return false;  // Skip original method
        }

        private static float GetExperienceForNextLevel(int level)
        {
            // Game's formula for experience per level. Read the tuning constants LIVE from the
            // game rather than hardcoding them, so the curve always matches vanilla even after
            // Klei retunes it (build 737790 changed EXPERIENCE_LEVEL_POWER 1.5 -> 1.7).
            float maxLevel = TUNING.DUPLICANTSTATS.ATTRIBUTE_LEVELING.MAX_GAINED_ATTRIBUTE_LEVEL;
            float expPower = TUNING.DUPLICANTSTATS.ATTRIBUTE_LEVELING.EXPERIENCE_LEVEL_POWER;
            float targetCycle = TUNING.DUPLICANTSTATS.ATTRIBUTE_LEVELING.TARGET_MAX_LEVEL_CYCLE;

            float currentExp = Mathf.Pow((float)level / maxLevel, expPower) * targetCycle * 600f;
            float nextExp = Mathf.Pow(((float)level + 1f) / maxLevel, expPower) * targetCycle * 600f;

            return nextExp - currentExp;
        }
    }
}
