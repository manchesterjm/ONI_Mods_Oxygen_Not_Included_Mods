using HarmonyLib;

namespace CustomizeYourPaints
{
    public class ArtablePatches
    {
        [HarmonyPatch(typeof(Artable), "OnSpawn")]
        public class Artable_OnSpawn_Patch
        {
            public static void Prefix(Artable __instance, ref string ___currentStage)
            {
                ArtHelper.RestoreStage(__instance, ref ___currentStage);
            }
        }

        [HarmonyPatch(typeof(Artable), "SetStage")]
        public class Artable_SetStage_Patch
        {
            public static void Prefix(Artable __instance, ref string stage_id)
            {
                ArtHelper.UpdateOverride(__instance, stage_id);
            }
        }

        [HarmonyPatch(typeof(Artable), nameof(Artable.OnDeserialized))]
        public class Artable_OnDeserialized_Patch
        {
            // Prevent invalid stages from breaking the game
            public static void Postfix(ref string ___currentStage)
            {
                if (Db.GetArtableStages().TryGet(___currentStage) == null)
                {
                    ___currentStage = Artable.defaultArtworkId;
                }
            }
        }
    }
}
