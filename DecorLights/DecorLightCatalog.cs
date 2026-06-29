using UnityEngine;

namespace DecorLights
{
	// Single source of truth for the 36 community-art lamps (14 floor 1x2, 20
	// ceiling/hanging 1x1, 2 wall 1x1): each lamp's spec lives here once,
	// referenced by its tiny Config subclass and looped over by the registration
	// patch (strings, build menu, tech unlock). Add a lamp = add a spec here + a
	// one-line subclass + its kanim folder under anim/assets.
	//
	// LightColor channels run 0..2 (HDR, matching the original Salt Lamp's Color(2,2,0)).
	// Anim is "<kanim build name>_kanim"; tall fixtures are 1x2, compact ones 1x1.
	public static class DecorLightCatalog
	{
		public static readonly DecorLightSpec ShinebugJar = new DecorLightSpec(
			id: "ShinebugJar", name: "Shinebug Jar",
			description: "A jar of Shine Bugs that never asked to be a lamp. Glows guilt-free.",
			anim: "decor_shinebug_kanim", width: 1, height: 2,
			lightColor: new Color(2f, 2f, 0.4f), lux: 1200, range: 5f);

		public static readonly DecorLightSpec PaperLantern = new DecorLightSpec(
			id: "PaperLantern", name: "Paper Lantern",
			description: "A paper lantern. Romantic, festive, and a fire marshal's nightmare.",
			anim: "decor_paper_lantern_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1f, 0.5f), lux: 1100, range: 5f,
			placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec EdisonCage = new DecorLightSpec(
			id: "EdisonCage", name: "Caged Edison Lamp",
			description: "A caged filament bulb. The cage is decorative. The bulb is judgmental.",
			anim: "decor_edison_cage_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1.6f, 0.8f), lux: 1200, range: 5f,
			placement: DecorPlacement.Wall);

		public static readonly DecorLightSpec MushroomCluster = new DecorLightSpec(
			id: "MushroomCluster", name: "Glowcap Cluster",
			description: "Bioluminescent mushrooms in a pot. Do not eat the lamp.",
			anim: "decor_mushroom_cluster_kanim", width: 1, height: 2,
			lightColor: new Color(0.6f, 1.6f, 2f), lux: 900, range: 4f);

		public static readonly DecorLightSpec AmethystGeode = new DecorLightSpec(
			id: "AmethystGeode", name: "Amethyst Geode",
			description: "A glowing amethyst. Allegedly aligns your chakras and your base lighting.",
			anim: "decor_amethyst_geode_kanim", width: 1, height: 1,
			lightColor: new Color(1.4f, 0.5f, 2f), lux: 1200, range: 5f);

		public static readonly DecorLightSpec PlasmaGlobe = new DecorLightSpec(
			id: "PlasmaGlobe", name: "Plasma Globe",
			description: "A plasma globe. Touch it for science; leave it for ambiance.",
			anim: "decor_plasma_globe_kanim", width: 1, height: 2,
			lightColor: new Color(1.5f, 0.8f, 2f), lux: 1400, range: 5.5f);

		public static readonly DecorLightSpec HurricaneLantern = new DecorLightSpec(
			id: "HurricaneLantern", name: "Hurricane Lantern",
			description: "A brass oil lantern, electrified for convenience. No oil, no soot, no romance lost.",
			anim: "decor_hurricane_lantern_kanim", width: 1, height: 2,
			lightColor: new Color(2f, 1.2f, 0.5f), lux: 1200, range: 5f);

		public static readonly DecorLightSpec JellyfishJar = new DecorLightSpec(
			id: "JellyfishJar", name: "Jellyfish Jar",
			description: "A jarred jellyfish that glows softly and asks for nothing. Model employee.",
			anim: "decor_jellyfish_jar_kanim", width: 1, height: 1,
			lightColor: new Color(0.6f, 1.4f, 2f), lux: 1000, range: 5f,
			placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec NeonCoil = new DecorLightSpec(
			id: "NeonCoil", name: "Neon Coil",
			description: "A coil of pink neon. Screams 'open' in a colony that never closes.",
			anim: "decor_neon_coil_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 0.4f, 1.4f), lux: 1200, range: 5f,
			placement: DecorPlacement.Wall);

		public static readonly DecorLightSpec BubbleTube = new DecorLightSpec(
			id: "BubbleTube", name: "Bubble Tube",
			description: "A bubble tube of drifting, color-shifting calm. Possibly load-bearing for morale.",
			anim: "decor_bubble_tube_kanim", width: 1, height: 2,
			lightColor: new Color(0.7f, 1.2f, 2f), lux: 1100, range: 5f);

		public static readonly DecorLightSpec CrystalChandelier = new DecorLightSpec(
			id: "CrystalChandelier", name: "Crystal Chandelier",
			description: "A crystal chandelier, because even an asteroid deserves a little class.",
			anim: "decor_crystal_chandelier_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1.8f, 1.4f), lux: 1600, range: 6f,
			placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec Candelabra = new DecorLightSpec(
			id: "Candelabra", name: "Candelabra",
			description: "A brass candelabra. Flickers convincingly; runs on electricity, not fire hazards.",
			anim: "decor_candelabra_kanim", width: 1, height: 2,
			lightColor: new Color(2f, 1.4f, 0.7f), lux: 900, range: 4.5f);

		public static readonly DecorLightSpec TerrariumOrb = new DecorLightSpec(
			id: "TerrariumOrb", name: "Terrarium Orb",
			description: "A glowing mossy terrarium. A tiny world that lights your bigger one.",
			anim: "decor_terrarium_orb_kanim", width: 1, height: 2,
			lightColor: new Color(1.3f, 2f, 0.8f), lux: 900, range: 4f);

		public static readonly DecorLightSpec WispOrb = new DecorLightSpec(
			id: "WispOrb", name: "Wisp Orb",
			description: "A captured wisp of light hovering on a pedestal. Definitely not haunted.",
			anim: "decor_wisp_orb_kanim", width: 1, height: 1,
			lightColor: new Color(1.7f, 1.8f, 2f), lux: 1300, range: 5.5f);

		public static readonly DecorLightSpec SputnikAtomic = new DecorLightSpec(
			id: "SputnikAtomic", name: "Atomic Sputnik Lamp",
			description: "A mid-century atomic lamp. The future, as imagined by the past.",
			anim: "decor_sputnik_atomic_kanim", width: 1, height: 2,
			lightColor: new Color(2f, 1.8f, 1.2f), lux: 1500, range: 6f);

		public static readonly DecorLightSpec Anglerfish = new DecorLightSpec(
			id: "Anglerfish", name: "Anglerfish Lamp",
			description: "An anglerfish lamp. Its lure works on duplicants too.",
			anim: "decor_anglerfish_kanim", width: 1, height: 1,
			lightColor: new Color(1f, 1.8f, 2f), lux: 1100, range: 5f,
			placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec AlgaeColumn = new DecorLightSpec(
			id: "AlgaeColumn", name: "Algae Column",
			description: "A column of glowing algae. Finally, a use for it that doesn't involve breathing.",
			anim: "decor_algae_column_kanim", width: 1, height: 2,
			lightColor: new Color(0.6f, 2f, 0.8f), lux: 900, range: 4f);

		public static readonly DecorLightSpec SaltRingTower = new DecorLightSpec(
			id: "SaltRingTower", name: "Salt Ring Tower",
			description: "A stack of glowing salt rings. Still not edible. Still do not lick.",
			anim: "decor_salt_ring_tower_kanim", width: 1, height: 2,
			lightColor: new Color(2f, 1.1f, 0.6f), lux: 1000, range: 5f);

		public static readonly DecorLightSpec StarProjector = new DecorLightSpec(
			id: "StarProjector", name: "Star Projector",
			description: "A star projector dome. Brings the stars indoors, minus the vacuum and dread.",
			anim: "decor_star_projector_kanim", width: 1, height: 1,
			lightColor: new Color(1.4f, 1.6f, 2f), lux: 1300, range: 5.5f);

		public static readonly DecorLightSpec LightningBottle = new DecorLightSpec(
			id: "LightningBottle", name: "Bottled Lightning",
			description: "Lightning in a bottle. Caught it once; mass-producing it now.",
			anim: "decor_lightning_bottle_kanim", width: 1, height: 1,
			lightColor: new Color(0.8f, 1.4f, 2f), lux: 1300, range: 5.5f);

		// ---- Dedicated hanging / pendant lamps (all ceiling-mounted) ----------------

		public static readonly DecorLightSpec PendantDome = new DecorLightSpec(
			id: "PendantDome", name: "Dome Pendant",
			description: "An industrial dome pendant. Honest light for an honest colony.",
			anim: "decor_pendant_dome_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1.6f, 0.8f), lux: 1200, range: 5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec GlobePendant = new DecorLightSpec(
			id: "GlobePendant", name: "Globe Pendant",
			description: "A glass globe on a cord. Simple, round, and quietly classy.",
			anim: "decor_glass_globe_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1.8f, 1.3f), lux: 1300, range: 5.5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec BareBulbPendant = new DecorLightSpec(
			id: "BareBulbPendant", name: "Bare Bulb Pendant",
			description: "A bare bulb on a wire. The minimalist's nightlight.",
			anim: "decor_bare_edison_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1.5f, 0.7f), lux: 1100, range: 5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec FestoonLights = new DecorLightSpec(
			id: "FestoonLights", name: "Festoon Lights",
			description: "A swag of party bulbs. Every day is a holiday underground.",
			anim: "decor_festoon_string_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1.6f, 0.9f), lux: 1100, range: 5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec HangingPaperLantern = new DecorLightSpec(
			id: "HangingPaperLantern", name: "Hanging Paper Lantern",
			description: "A paper lantern, suspended. Gravity does the decorating.",
			anim: "decor_paper_lantern_hang_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1f, 0.5f), lux: 1100, range: 5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec CagedPendant = new DecorLightSpec(
			id: "CagedPendant", name: "Caged Pendant Lantern",
			description: "A caged pendant lantern. The bulb is safe; your eyes are on their own.",
			anim: "decor_caged_pendant_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1.5f, 0.8f), lux: 1200, range: 5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec CrystalPendant = new DecorLightSpec(
			id: "CrystalPendant", name: "Crystal Pendant",
			description: "A faceted crystal on a chain. Spills violet light like a secret.",
			anim: "decor_crystal_pendant_kanim", width: 1, height: 1,
			lightColor: new Color(1.4f, 0.6f, 2f), lux: 1100, range: 5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec JellyPendant = new DecorLightSpec(
			id: "JellyPendant", name: "Jellyfish Pendant",
			description: "A jellyfish in a teardrop of glass. Drifts in place, glows on cue.",
			anim: "decor_jelly_pendant_kanim", width: 1, height: 1,
			lightColor: new Color(0.6f, 1.4f, 2f), lux: 1000, range: 5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec HangingTerrarium = new DecorLightSpec(
			id: "HangingTerrarium", name: "Hanging Terrarium",
			description: "A hanging globe of glowing moss. Low maintenance, high serenity.",
			anim: "decor_terrarium_hang_kanim", width: 1, height: 1,
			lightColor: new Color(1.3f, 2f, 0.8f), lux: 900, range: 4f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec HangingOilLamp = new DecorLightSpec(
			id: "HangingOilLamp", name: "Hanging Oil Lamp",
			description: "A hanging brass oil lamp. The shipwreck aesthetic, minus the shipwreck.",
			anim: "decor_oil_lamp_hang_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1.2f, 0.5f), lux: 1200, range: 5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec MoroccanLantern = new DecorLightSpec(
			id: "MoroccanLantern", name: "Moroccan Lantern",
			description: "A pierced-metal lantern that throws patterned light. Fancy, and it knows it.",
			anim: "decor_moroccan_lantern_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1.5f, 0.8f), lux: 1200, range: 5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec HangingFireflyJar = new DecorLightSpec(
			id: "HangingFireflyJar", name: "Hanging Firefly Jar",
			description: "A jar of fireflies, hung upside down. They've made their peace with it.",
			anim: "decor_firefly_jar_hang_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 2f, 0.5f), lux: 1100, range: 5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec PlasmaPendant = new DecorLightSpec(
			id: "PlasmaPendant", name: "Plasma Pendant",
			description: "A plasma orb on a cord. Arcs of light, no touching required.",
			anim: "decor_plasma_pendant_kanim", width: 1, height: 1,
			lightColor: new Color(1.5f, 0.8f, 2f), lux: 1300, range: 5.5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec VineBloom = new DecorLightSpec(
			id: "VineBloom", name: "Glowvine Bloom",
			description: "A trailing vine of glowing blooms. Nature's chandelier.",
			anim: "decor_vine_bloom_kanim", width: 1, height: 1,
			lightColor: new Color(0.6f, 2f, 0.8f), lux: 900, range: 4f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec StarlightOrb = new DecorLightSpec(
			id: "StarlightOrb", name: "Starlight Orb",
			description: "A frosted orb that scatters pinpricks of starlight. Pocket-sized cosmos.",
			anim: "decor_starlight_orb_kanim", width: 1, height: 1,
			lightColor: new Color(1.4f, 1.6f, 2f), lux: 1200, range: 5f, placement: DecorPlacement.Ceiling);

		public static readonly DecorLightSpec SpiralChandelier = new DecorLightSpec(
			id: "SpiralChandelier", name: "Spiral Chandelier",
			description: "A spiral chandelier of candle bulbs. Drama, suspended from the ceiling.",
			anim: "decor_spiral_chandelier_kanim", width: 1, height: 1,
			lightColor: new Color(2f, 1.7f, 1.2f), lux: 1500, range: 6f, placement: DecorPlacement.Ceiling);

		// Every lamp the mod adds through the data-driven base config. The original
		// four hand-written lamps register themselves separately.
		public static readonly DecorLightSpec[] All =
		{
			ShinebugJar, PaperLantern, EdisonCage, MushroomCluster, AmethystGeode,
			PlasmaGlobe, HurricaneLantern, JellyfishJar, NeonCoil, BubbleTube,
			CrystalChandelier, Candelabra, TerrariumOrb, WispOrb, SputnikAtomic,
			Anglerfish, AlgaeColumn, SaltRingTower, StarProjector, LightningBottle,
			PendantDome, GlobePendant, BareBulbPendant, FestoonLights, HangingPaperLantern,
			CagedPendant, CrystalPendant, JellyPendant, HangingTerrarium, HangingOilLamp,
			MoroccanLantern, HangingFireflyJar, PlasmaPendant, VineBloom, StarlightOrb,
			SpiralChandelier,
		};
	}
}
