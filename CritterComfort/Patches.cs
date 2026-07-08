using HarmonyLib;

namespace CritterComfort
{
    // Critter Comfort (ORIGINAL mod, 2026-07-08). Removes every comfort/mood
    // negative from every critter of every species (Josh's ask via a Pacu
    // status panel showing red "Crowded" / "Cramped" / "Mood: Glum"):
    //
    //   Crowded   - happiness penalty per excess critter   \  all three applied by
    //   Confined  - happiness -10 + reproduction x0         > OvercrowdingMonitor.
    //   Cramped   - reproduction x0 (eggs will overfill)   /  AlignTagsAndEffects
    //   Glum      - metabolism -15 wild / -80 tame         \  entered via
    //   Miserable - metabolism + reproduction/growth x0     > HappinessMonitor
    //
    // Both systems are global chokepoints every critter runs through (land
    // critters per room cavity, fish per pond), so no species list is needed
    // and future critters are covered automatically. Survival mechanics
    // (starvation, temperature, drowning) are deliberately untouched.
    //
    // Positive moods still work: happiness now only ever sums upward, so
    // grooming/feeder bonuses and the tame Happy reproduction boost behave
    // exactly as vanilla.
    public static class CritterComfortPatches
    {
        // Replaces the once-a-second routine that applies/removes the three
        // crowding effects and their tags. Our version only ever clears:
        // effects off, Overcrowded/Confined tags off, and Expecting on for
        // every adult (vanilla withholds it while the room is Cramped).
        [HarmonyPatch(typeof(OvercrowdingMonitor), "AlignTagsAndEffects")]
        public static class OvercrowdingMonitor_AlignTagsAndEffects_Patch
        {
            public static bool Prefix(OvercrowdingMonitor.Instance smi)
            {
                ClearEffect(smi, smi.confined.Effect);
                ClearEffect(smi, smi.overcrowded.Effect);
                ClearEffect(smi, smi.futureOvercrowded.Effect);
                AlignTag(smi, GameTags.Creatures.Overcrowded, present: false);
                AlignTag(smi, GameTags.Creatures.Confined, present: false);
                AlignTag(smi, GameTags.Creatures.Expecting, present: !smi.isBaby);
                return false;
            }

            private static void ClearEffect(OvercrowdingMonitor.Instance smi, Klei.AI.Effect effect)
            {
                if (smi.effects.HasEffect(effect))
                {
                    smi.effects.Remove(effect);
                }
            }

            private static void AlignTag(OvercrowdingMonitor.Instance smi, Tag tag, bool present)
            {
                if (smi.kpid.HasTag(tag) != present)
                {
                    smi.kpid.SetTag(tag, present);
                }
            }
        }

        // Mood floor: a critter can never test as Glum, so the mood state
        // machine can only sit at Neutral or Happy.
        [HarmonyPatch(typeof(HappinessMonitor), "IsGlum")]
        public static class HappinessMonitor_IsGlum_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        // Same floor for Miserable ("IsMisirable" is the game's own typo).
        [HarmonyPatch(typeof(HappinessMonitor), "IsMisirable")]
        public static class HappinessMonitor_IsMisirable_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                __result = false;
                return false;
            }
        }
    }
}
