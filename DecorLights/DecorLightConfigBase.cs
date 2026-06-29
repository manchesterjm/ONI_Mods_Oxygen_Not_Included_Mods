using TUNING;
using UnityEngine;

namespace DecorLights
{
	// Shared wiring for the community-art decorative lamps. A subclass supplies a
	// DecorLightSpec; this base turns it into a floor-standing, glass-built decor
	// light that emits a soft circular glow — the same recipe as the original Salt
	// Lamp, lifted here so each new lamp is data, not duplicated plumbing.
	public abstract class DecorLightConfigBase : IBuildingConfig
	{
		public abstract DecorLightSpec Spec { get; }

		public string Effect => STRINGS.BUILDINGS.PREFABS.CEILINGLIGHT.EFFECT;

		private static BuildLocationRule LocationRule(DecorPlacement placement)
		{
			switch (placement)
			{
				case DecorPlacement.Ceiling: return BuildLocationRule.OnCeiling;
				case DecorPlacement.Wall: return BuildLocationRule.OnWall;
				default: return BuildLocationRule.OnFloor;
			}
		}

		public override BuildingDef CreateBuildingDef()
		{
			var spec = Spec;
			var buildingDef = BuildingTemplates.CreateBuildingDef(
				id: spec.Id,
				width: spec.Width,
				height: spec.Height,
				anim: spec.Anim,
				hitpoints: BUILDINGS.HITPOINTS.TIER0,
				construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER1,
				construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER1,
				construction_materials: MATERIALS.GLASSES,
				melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER0,
				build_location_rule: LocationRule(spec.Placement),
				decor: DECOR.BONUS.TIER3,
				noise: NOISE_POLLUTION.NONE);

			buildingDef.RequiresPowerInput = true;
			buildingDef.EnergyConsumptionWhenActive = 4f;
			buildingDef.SelfHeatKilowattsWhenActive = 0.3f;
			buildingDef.ViewMode = OverlayModes.Light.ID;
			buildingDef.AudioCategory = "Metal";

			return buildingDef;
		}

		// Where the glow sits relative to the building origin. Floor lamps light from
		// just above the base; ceiling lamps are 1x1 fixtures, glow at cell origin; wall
		// lamps push the glow out from the wall.
		private static Vector2 LightOffset(DecorPlacement placement)
		{
			switch (placement)
			{
				case DecorPlacement.Ceiling: return new Vector2(0f, 0.4f);
				case DecorPlacement.Wall: return new Vector2(0f, 0.5f);
				default: return new Vector2(0.05f, 1f);
			}
		}

		public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
		{
			var lightShapePreview = go.AddComponent<LightShapePreview>();
			lightShapePreview.lux = Spec.Lux;
			lightShapePreview.radius = Spec.Range;
			lightShapePreview.shape = LightShape.Circle;
			lightShapePreview.offset = new CellOffset(
				(int)def.BuildingComplete.GetComponent<Light2D>().Offset.x,
				(int)def.BuildingComplete.GetComponent<Light2D>().Offset.y);
		}

		public override void DoPostConfigureComplete(GameObject go)
		{
			var spec = Spec;
			go.AddOrGet<EnergyConsumer>();
			go.AddOrGet<LoopingSounds>();

			var light2D = go.AddOrGet<Light2D>();
			light2D.overlayColour = LIGHT2D.FLOORLAMP_OVERLAYCOLOR;
			light2D.Color = spec.LightColor;
			light2D.Range = spec.Range;
			light2D.Lux = spec.Lux;
			light2D.Angle = 0.0f;
			light2D.Direction = LIGHT2D.FLOORLAMP_DIRECTION;
			light2D.Offset = LightOffset(spec.Placement);
			light2D.shape = LightShape.Circle;
			light2D.drawOverlay = true;

			go.AddOrGetDef<LightController.Def>();
		}
	}
}
