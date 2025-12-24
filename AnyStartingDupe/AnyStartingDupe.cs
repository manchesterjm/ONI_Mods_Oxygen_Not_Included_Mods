using HarmonyLib;
using KMod;
using UnityEngine;

namespace AnyStartingDupe
{
    public class AnyStartingDupeMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("AnyStartingDupe loaded - All duplicants available at start!");
        }
    }

    /// <summary>
    /// Patches the Personality constructor to set isStartingMinion = true for all personalities
    /// This allows any duplicant to be selected when starting a new game
    /// Updated for build 704096 - constructor now has 27 parameters
    /// </summary>
    [HarmonyPatch(typeof(Personality))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new System.Type[] {
        typeof(string),  // name_string_key
        typeof(string),  // name
        typeof(string),  // Gender
        typeof(string),  // PersonalityType
        typeof(string),  // StressTrait
        typeof(string),  // JoyTrait
        typeof(string),  // StickerType
        typeof(string),  // CongenitalTrait
        typeof(int),     // headShape
        typeof(int),     // mouth
        typeof(int),     // neck
        typeof(int),     // eyes
        typeof(int),     // hair
        typeof(int),     // body
        typeof(int),     // belt
        typeof(int),     // cuff
        typeof(int),     // foot
        typeof(int),     // hand
        typeof(int),     // pelvis
        typeof(int),     // leg
        typeof(int),     // arm_skin
        typeof(int),     // leg_skin
        typeof(string),  // description
        typeof(bool),    // isStartingMinion
        typeof(string),  // graveStone
        typeof(Tag),     // model
        typeof(int)      // SpeechMouth
    })]
    public static class Personality_Ctor_Patch
    {
        public static void Prefix(ref bool isStartingMinion)
        {
            // Force all personalities to be valid starting minions
            isStartingMinion = true;
        }
    }
}
