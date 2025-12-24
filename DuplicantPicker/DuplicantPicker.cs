using HarmonyLib;
using KMod;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DuplicantPicker
{
    public class DuplicantPickerMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("DuplicantPicker loaded!");
            Debug.Log("  - Reshuffle cycles through duplicants in alphabetical order");
            Debug.Log("  - Press PageUp to go to previous duplicant");
            Debug.Log("  - Duplicate personalities allowed (can pick same dupe multiple times)");
        }
    }

    /// <summary>
    /// Central tracker for personality cycling
    /// </summary>
    public static class PersonalityTracker
    {
        private static List<Personality> sortedPersonalities = null;
        private static Dictionary<int, int> containerToIndex = new Dictionary<int, int>();
        public static Personality nextOverride = null;

        public static List<Personality> GetSortedPersonalities()
        {
            if (sortedPersonalities == null)
            {
                sortedPersonalities = Db.Get().Personalities.GetAll(true, false)
                    .OrderBy(p => p.Name)
                    .ToList();
                Debug.Log($"DuplicantPicker: Loaded {sortedPersonalities.Count} personalities");
            }
            return sortedPersonalities;
        }

        public static Personality CycleNext(int containerId, int direction = 1)
        {
            var list = GetSortedPersonalities();
            if (list.Count == 0) return null;

            if (!containerToIndex.ContainsKey(containerId))
                containerToIndex[containerId] = -1;

            containerToIndex[containerId] += direction;

            // Wrap around
            if (containerToIndex[containerId] >= list.Count)
                containerToIndex[containerId] = 0;
            if (containerToIndex[containerId] < 0)
                containerToIndex[containerId] = list.Count - 1;

            return list[containerToIndex[containerId]];
        }

        public static void Reset()
        {
            sortedPersonalities = null;
            containerToIndex.Clear();
        }
    }

    /// <summary>
    /// Patch IsCharacterInvalid to allow duplicate personalities
    /// The game normally prevents selecting the same personality twice
    /// </summary>
    [HarmonyPatch(typeof(CharacterContainer), "IsCharacterInvalid")]
    public static class CharacterContainer_IsCharacterInvalid_Patch
    {
        public static bool Prefix(ref bool __result)
        {
            // Always return false (character is valid) to allow duplicates
            __result = false;
            return false; // Skip original method
        }
    }

    /// <summary>
    /// Patch the MinionStartingStats constructor to use our override personality
    /// </summary>
    [HarmonyPatch(typeof(MinionStartingStats))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new System.Type[] { typeof(List<Tag>), typeof(bool), typeof(string), typeof(string), typeof(bool) })]
    public static class MinionStartingStats_ListTags_Ctor_Patch
    {
        public static void Postfix(MinionStartingStats __instance)
        {
            if (PersonalityTracker.nextOverride != null)
            {
                var p = PersonalityTracker.nextOverride;
                PersonalityTracker.nextOverride = null;

                __instance.personality = p;
                __instance.Name = p.Name;
                __instance.NameStringKey = p.nameStringKey;
                __instance.GenderStringKey = p.genderStringKey;

                Debug.Log($"DuplicantPicker: Selected {p.Name}");
            }
        }
    }

    /// <summary>
    /// Before reshuffle, set up the next personality to cycle to
    /// </summary>
    [HarmonyPatch(typeof(CharacterContainer), "Reshuffle")]
    public static class CharacterContainer_Reshuffle_Patch
    {
        public static void Prefix(CharacterContainer __instance)
        {
            int id = __instance.GetInstanceID();
            PersonalityTracker.nextOverride = PersonalityTracker.CycleNext(id, 1);
        }
    }

    /// <summary>
    /// Add component to handle PageUp for previous personality
    /// </summary>
    [HarmonyPatch(typeof(CharacterContainer), "OnSpawn")]
    public static class CharacterContainer_OnSpawn_Patch
    {
        public static void Postfix(CharacterContainer __instance, KButton ___reshuffleButton)
        {
            var handler = __instance.gameObject.AddComponent<DuplicantPickerInput>();
            handler.Init(__instance, ___reshuffleButton);
        }
    }

    public class DuplicantPickerInput : MonoBehaviour
    {
        private CharacterContainer container;
        private KButton reshuffleButton;
        private int instanceId;

        public void Init(CharacterContainer c, KButton btn)
        {
            container = c;
            reshuffleButton = btn;
            instanceId = c.GetInstanceID();
        }

        void Update()
        {
            if (container == null) return;

            // PageUp = go backwards through list
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                // Set override to previous personality (direction = -2 because Reshuffle will add +1)
                PersonalityTracker.nextOverride = PersonalityTracker.CycleNext(instanceId, -2);

                // Trigger reshuffle
                container.Reshuffle(true);
            }
        }
    }
}
