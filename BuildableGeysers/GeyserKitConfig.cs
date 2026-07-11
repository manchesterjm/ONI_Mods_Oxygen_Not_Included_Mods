using TUNING;
using UnityEngine;

namespace BuildableGeysers
{
    // One geyser kit building, parameterized by the geyser it unpacks into. The kit
    // wears the geyser's own kanim and footprint, costs refined metal + plastic, and
    // on construction complete is swapped for the real geyser prefab (see
    // BuildableGeysersPatches.BuildingComplete_OnSpawn_Patch).
    //
    // Instances are registered manually from the discovery pass, NOT by the vanilla
    // reflection pass in GeneratedBuildings.LoadGeneratedBuildings. That pass still
    // Activator.CreateInstance()s this class (parameterless, outside its try/catch),
    // so the parameterless instance declares a never-subscribed DLC id, which makes
    // BuildingConfigManager.RegisterBuilding skip it cleanly.
    public class GeyserKitConfig : IBuildingConfig
    {
        private const string NeverSubscribedDlc = "BuildableGeysers_Placeholder";

        private static readonly string[] KitMaterials = { MATERIALS.REFINED_METAL, MATERIALS.PLASTIC };
        private static readonly float[] KitMass = { 400f, 100f };
        private const float BuildTimeSeconds = 120f;

        private readonly GeyserKitSpec spec;

        public GeyserKitConfig()
        {
            spec = null;
        }

        public GeyserKitConfig(GeyserKitSpec spec)
        {
            this.spec = spec;
        }

        public override string[] GetRequiredDlcIds()
        {
            return spec == null ? new[] { NeverSubscribedDlc } : spec.RequiredDlcIds;
        }

        public override string[] GetForbiddenDlcIds()
        {
            return spec?.ForbiddenDlcIds;
        }

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                spec.KitId, spec.Width, spec.Height, spec.AnimName, 100,
                BuildTimeSeconds, KitMass, KitMaterials, 1600f,
                BuildLocationRule.OnFloor, TUNING.BUILDINGS.DECOR.BONUS.TIER1,
                NOISE_POLLUTION.NONE);
            def.DefaultAnimState = "inactive";
            def.AudioCategory = "HollowMetal";
            def.Floodable = false;
            def.Overheatable = false;
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            GeneratedBuildings.RemoveLoopingSounds(go);
        }

        // The drag preview and under-construction objects play the "place" anim,
        // which geyser kanims don't have - they'd render invisible. Point them at
        // "inactive" (the state every geyser kanim has); the preview still gets the
        // green/red placement tint automatically.
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            ShowGeyserArtInsteadOfPlaceAnim(go);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            ShowGeyserArtInsteadOfPlaceAnim(go);
        }

        private static void ShowGeyserArtInsteadOfPlaceAnim(GameObject go)
        {
            KBatchedAnimController anim = go.GetComponent<KBatchedAnimController>();
            if (anim != null)
            {
                anim.initialAnim = "inactive";
                anim.defaultAnim = "inactive";
            }
        }
    }
}
