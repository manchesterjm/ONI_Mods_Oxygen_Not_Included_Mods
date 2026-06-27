using STRINGS;
using TUNING;
using UnityEngine;

namespace BuildableNaturalTile
{
    // The "Natural Tile" building: a 1x1 foundation tile you build from any solid
    // element. On completion the BuildingComplete.OnSpawn patch swaps the built
    // tile for a real natural block of that element (see BuildableNaturalTilePatches).
    public class NaturalTileConfig : IBuildingConfig
    {
        public const string Id = "NaturalTile";
        public const string DisplayName = "Natural Tile";
        public const string Description = "Fill that hole you dug out back in with any solid element.";
        public static readonly string Effect =
            "Fills a block in the world with " + UI.FormatAsLink("Solids", "ELEMENTS_SOLID") + ".";

        public override BuildingDef CreateBuildingDef()
        {
            string[] materials = { "Solid" };
            float[] mass = { BuildableNaturalTilePatches.Settings.BuildMass };
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                Id, 1, 1, "natural_tile_kanim", 100,
                BuildableNaturalTilePatches.Settings.BuildSpeed, mass, materials,
                1600f, BuildLocationRule.Anywhere, TUNING.BUILDINGS.DECOR.BONUS.TIER0,
                NOISE_POLLUTION.NONE, 0.2f);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            GeneratedBuildings.RemoveLoopingSounds(go);
            go.GetComponent<KPrefabID>().AddTag(GameTags.FloorTiles);
        }
    }
}
