using HarmonyLib;
using KMod;
using UnityEngine;

namespace StatsUnlimitedLite
{
    public class StatsUnlimitedLiteMod : UserMod2
    {
        public static Settings Settings { get; private set; } = new Settings();

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PeterHan.PLib.Core.PUtil.InitLibrary(false);
            new PeterHan.PLib.Options.POptions().RegisterOptions(this, typeof(Settings));
            Settings = Settings.Load();
            Debug.Log($"StatsUnlimitedLite loaded! Stats cap {Settings.MaxStatLevel} (vanilla 20); " +
                $"attribute XP x{Settings.AttributeXpMultiplier}, skill XP x{Settings.SkillXpMultiplier}");
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
            // Check if already at the configured cap
            if (__instance.level >= StatsUnlimitedLiteMod.Settings.MaxStatLevel)
            {
                __result = false;
                return false;  // Skip original
            }

            // Add experience, scaled by the configured learning speed
            __instance.experience += experience * StatsUnlimitedLiteMod.Settings.AttributeXpMultiplier;
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

    /// <summary>
    /// Learning-speed knob for skill points (hats): scale the experience amount
    /// before the game's own bookkeeping runs.
    /// </summary>
    [HarmonyPatch(typeof(MinionResume), nameof(MinionResume.AddExperience))]
    public static class MinionResume_AddExperience_Patch
    {
        public static void Prefix(ref float amount)
        {
            amount *= StatsUnlimitedLiteMod.Settings.SkillXpMultiplier;
        }
    }
}
