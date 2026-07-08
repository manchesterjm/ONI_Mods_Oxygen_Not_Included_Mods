using System.Collections.Generic;
using HarmonyLib;
using Klei.AI;
using UnityEngine;

namespace SkillStatsProgressRevived
{
    // Optional rolling-window XP tracking (the old mod's "complex feature"):
    // sample every duplicant's XP on a timer, keep per-sample gains inside the
    // configured window, and let the panel show "(+N)" per stat. Attribute XP
    // resets to zero on a level-up, so negative per-sample diffs are clamped
    // to zero exactly like the original did. Skill XP uses the monotonic
    // MinionResume.TotalExperienceGained, which never resets.
    internal static class XpDeltas
    {
        public const string SkillKey = "__skillxp";

        private sealed class Sample
        {
            public float Time;
            public Dictionary<string, float> Gains;
        }

        private static readonly Dictionary<MinionIdentity, Queue<Sample>> History =
            new Dictionary<MinionIdentity, Queue<Sample>>();

        private static readonly Dictionary<MinionIdentity, Dictionary<string, float>> LastSeen =
            new Dictionary<MinionIdentity, Dictionary<string, float>>();

        private static float sinceLastSample;

        public static float GainedInWindow(MinionIdentity dupe, string attributeId)
        {
            if (!History.TryGetValue(dupe, out Queue<Sample> samples))
            {
                return 0f;
            }
            float total = 0f;
            foreach (Sample sample in samples)
            {
                if (sample.Gains.TryGetValue(attributeId, out float gain))
                {
                    total += gain;
                }
            }
            return total;
        }

        [HarmonyPatch(typeof(GameClock), "Render1000ms")]
        internal static class SamplerPatch
        {
            public static void Postfix(float dt)
            {
                Options options = SkillStatsProgressRevivedMod.Options;
                if (!options.EnableDeltas || SpeedControlScreen.Instance == null
                    || SpeedControlScreen.Instance.IsPaused)
                {
                    return;
                }
                sinceLastSample += dt * Time.timeScale;
                if (sinceLastSample < options.SampleEverySeconds)
                {
                    return;
                }
                sinceLastSample = 0f;
                float now = GameClock.Instance.GetTime();
                foreach (MinionIdentity dupe in Components.LiveMinionIdentities)
                {
                    SampleDuplicant(dupe, now);
                    PruneOldSamples(dupe, now - options.DeltaWindowSeconds);
                }
            }
        }

        private static void SampleDuplicant(MinionIdentity dupe, float now)
        {
            Dictionary<string, float> current = ReadCurrentXp(dupe);
            if (current == null)
            {
                return;
            }
            if (!LastSeen.TryGetValue(dupe, out Dictionary<string, float> previous))
            {
                LastSeen[dupe] = current;
                History[dupe] = new Queue<Sample>();
                return;
            }
            Dictionary<string, float> gains = new Dictionary<string, float>(current.Count);
            foreach (KeyValuePair<string, float> entry in current)
            {
                previous.TryGetValue(entry.Key, out float before);
                gains[entry.Key] = Mathf.Max(0f, entry.Value - before);
            }
            LastSeen[dupe] = current;
            History[dupe].Enqueue(new Sample { Time = now, Gains = gains });
        }

        private static void PruneOldSamples(MinionIdentity dupe, float cutoff)
        {
            if (!History.TryGetValue(dupe, out Queue<Sample> samples))
            {
                return;
            }
            while (samples.Count > 0 && samples.Peek().Time <= cutoff)
            {
                samples.Dequeue();
            }
        }

        private static Dictionary<string, float> ReadCurrentXp(MinionIdentity dupe)
        {
            AttributeLevels levels = dupe.GetComponent<AttributeLevels>();
            MinionResume resume = dupe.GetComponent<MinionResume>();
            if (levels == null || resume == null)
            {
                return null;
            }
            Dictionary<string, float> xp = new Dictionary<string, float>
            {
                [SkillKey] = resume.TotalExperienceGained,
            };
            foreach (AttributeInstance attribute in dupe.gameObject.GetAttributes().AttributeTable)
            {
                if (attribute.Attribute.ShowInUI != Klei.AI.Attribute.Display.Skill)
                {
                    continue;
                }
                AttributeLevel level = levels.GetAttributeLevel(attribute.Id);
                if (level != null)
                {
                    xp[attribute.Id] = level.experience;
                }
            }
            return xp;
        }
    }
}
