using UnityEngine;

namespace DecorLights
{
	// Where a lamp mounts. Drives the build location rule, the light offset/direction,
	// and how its kanim art is anchored (floor sits on the ground, ceiling hangs down,
	// wall brackets to one side).
	public enum DecorPlacement
	{
		Floor,
		Ceiling,
		Wall,
	}

	// Everything that distinguishes one decorative lamp from another. The shared
	// building wiring lives in DecorLightConfigBase; each new lamp is just one of
	// these specs. Keeps the community-art lamps to a few readable lines each.
	public readonly struct DecorLightSpec
	{
		public readonly string Id;
		public readonly string Name;
		public readonly string Description;
		public readonly string Anim;
		public readonly int Width;
		public readonly int Height;
		public readonly Color LightColor;
		public readonly int Lux;
		public readonly float Range;
		public readonly DecorPlacement Placement;

		public DecorLightSpec(string id, string name, string description, string anim,
			int width, int height, Color lightColor, int lux, float range,
			DecorPlacement placement = DecorPlacement.Floor)
		{
			Id = id;
			Name = name;
			Description = description;
			Anim = anim;
			Width = width;
			Height = height;
			LightColor = lightColor;
			Lux = lux;
			Range = range;
			Placement = placement;
		}
	}
}
