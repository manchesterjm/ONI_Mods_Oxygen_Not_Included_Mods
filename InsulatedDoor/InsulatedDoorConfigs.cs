using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace InsulatedDoor
{
    // The insulation behaviour: when an insulated door spawns, set the sim
    // insulation flag on its cells so heat barely passes through them.
    internal class InsulatingDoor : KMonoBehaviour
    {
        [MyCmpGet]
        public Door door;

        public void ApplyInsulation(float insulation)
        {
            IList<int> cells = GetComponent<Building>().PlacementCells;
            for (int i = 0; i < cells.Count; i++)
            {
                SimMessages.SetInsulation(cells[i], insulation);
            }
        }
    }

    // Shared wiring for the door buildings.
    internal static class InsulatedDoorParts
    {
        public static void WireDoor(GameObject go, Door.DoorType doorType,
            float unpoweredAnimSpeed, float poweredAnimSpeed,
            string openSound, string closeSound)
        {
            Door door = go.AddOrGet<Door>();
            door.hasComplexUserControls = true;
            door.doorType = doorType;
            door.unpoweredAnimSpeed = unpoweredAnimSpeed;
            if (poweredAnimSpeed > 0f)
            {
                door.poweredAnimSpeed = poweredAnimSpeed;
            }
            if (openSound != null)
            {
                door.doorOpeningSoundEventName = openSound;
            }
            if (closeSound != null)
            {
                door.doorClosingSoundEventName = closeSound;
            }
            go.AddOrGet<Insulator>();
            go.AddOrGet<ZoneTile>();
            go.AddOrGet<AccessControl>();
            go.AddOrGet<KBoxCollider2D>();
            Prioritizable.AddRef(go);
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Door;
            go.AddOrGet<Workable>().workTime = 5f;
            Object.DestroyImmediate(go.GetComponent<BuildingEnabledButton>());
        }

        public static void PostConfigure(GameObject go)
        {
            go.GetComponent<AccessControl>().controlEnabled = true;
            go.GetComponent<KBatchedAnimController>().initialAnim = "closed";
        }

        public static BuildingDef MakeDoorDef(string id, int height, string anim, float buildTime,
            float[] mass, string[] materials, EffectorValues decor)
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(id, 1, height, anim, 30, buildTime,
                mass, materials, 1600f, BuildLocationRule.Tile, decor, NOISE_POLLUTION.NONE, 1f);
            def.ThermalConductivity = 0.01f;
            def.Overheatable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.IsFoundation = true;
            def.TileLayer = ObjectLayer.FoundationTile;
            def.AudioCategory = "Metal";
            def.PermittedRotations = PermittedRotations.R90;
            def.SceneLayer = Grid.SceneLayer.TileMain;
            def.ForegroundLayer = Grid.SceneLayer.InteriorWall;
            return def;
        }
    }

    public class InsulatedManualPressureDoorConfig : IBuildingConfig
    {
        public const string Id = "InsulatedManualPressureDoor";

        public override BuildingDef CreateBuildingDef()
        {
            float[] mass = { BUILDINGS.CONSTRUCTION_MASS_KG.TIER5[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0] };
            return InsulatedDoorParts.MakeDoorDef(Id, 2, "Insulated_door_manual_kanim", 60f,
                mass, new[] { "BuildableRaw", "Metal" }, BUILDINGS.DECOR.PENALTY.TIER2);
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            InsulatedDoorParts.WireDoor(go, Door.DoorType.ManualPressure, 1f, 0f, null, null);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            InsulatedDoorParts.PostConfigure(go);
        }
    }

    public class TinyInsulatedManualPressureDoorConfig : IBuildingConfig
    {
        public const string Id = "TinyInsulatedManualPressureDoor";

        public override BuildingDef CreateBuildingDef()
        {
            float[] mass = { BUILDINGS.CONSTRUCTION_MASS_KG.TIER4[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0] };
            return InsulatedDoorParts.MakeDoorDef(Id, 1, "Tiny_Insulated_door_manual_kanim", 60f,
                mass, new[] { "BuildableRaw", "Metal" }, BUILDINGS.DECOR.PENALTY.TIER2);
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            InsulatedDoorParts.WireDoor(go, Door.DoorType.ManualPressure, 1f, 0f, null, null);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            InsulatedDoorParts.PostConfigure(go);
        }
    }

    public class InsulatedPressureDoorConfig : IBuildingConfig
    {
        public const string Id = "InsulatedPressureDoor";

        public override BuildingDef CreateBuildingDef()
        {
            float[] mass = { BUILDINGS.CONSTRUCTION_MASS_KG.TIER5[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0] };
            BuildingDef def = InsulatedDoorParts.MakeDoorDef(Id, 2, "Insulated_door_external_kanim", 60f,
                mass, new[] { "BuildableRaw", "RefinedMetal" }, BUILDINGS.DECOR.PENALTY.TIER1);
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 180f;
            def.ViewMode = OverlayModes.Power.ID;
            def.LogicInputPorts = DoorConfig.CreateSingleInputPortList(new CellOffset(0, 0));
            return def;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            InsulatedDoorParts.WireDoor(go, Door.DoorType.Pressure, 0.65f, 5f,
                "MechanizedAirlock_opening", "MechanizedAirlock_closing");
            InsulatedDoorParts.PostConfigure(go);
        }
    }

    public class TinyInsulatedPressureDoorConfig : IBuildingConfig
    {
        public const string Id = "TinyInsulatedPressureDoor";

        public override BuildingDef CreateBuildingDef()
        {
            float[] mass = { BUILDINGS.CONSTRUCTION_MASS_KG.TIER4[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0] };
            BuildingDef def = InsulatedDoorParts.MakeDoorDef(Id, 1, "Tiny_Insulated_door_external_kanim", 30f,
                mass, new[] { "BuildableRaw", "RefinedMetal" }, BUILDINGS.DECOR.PENALTY.TIER1);
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 90f;
            def.ViewMode = OverlayModes.Power.ID;
            def.LogicInputPorts = DoorConfig.CreateSingleInputPortList(new CellOffset(0, 0));
            return def;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            InsulatedDoorParts.WireDoor(go, Door.DoorType.Pressure, 0.65f, 5f,
                "MechanizedAirlock_opening", "MechanizedAirlock_closing");
            InsulatedDoorParts.PostConfigure(go);
        }
    }
}
