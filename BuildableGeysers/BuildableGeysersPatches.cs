using System.Collections.Generic;
using HarmonyLib;
using STRINGS;
using UnityEngine;

namespace BuildableGeysers
{
    // Wires the geyser kits into the game: discovers every geyser prefab in the
    // loaded game (vanilla + any mod's), registers one kit building per geyser,
    // and swaps a completed kit for the real geyser.
    public static class BuildableGeysersPatches
    {
        // Discovery must run AFTER every entity prefab exists. Building load order
        // at 740622 (decompiled): Db.Initialize -> LoadGeneratedBuildings ->
        // LoadGeneratedEntities (geyser prefabs created here, including other mods'
        // GenerateConfigs additions) -> ConfigurePost -> Db.PostProcess (tech items
        // resolve). A postfix on LoadGeneratedEntities is the earliest moment every
        // geyser is visible and still early enough for buildings + tech to register.
        [HarmonyPatch(typeof(EntityConfigManager), nameof(EntityConfigManager.LoadGeneratedEntities))]
        public static class EntityConfigManager_LoadGeneratedEntities_Patch
        {
            public static void Postfix()
            {
                GeyserKitCatalog.DiscoverGeysersAndRegisterKits();
            }
        }

        // When a kit finishes construction, replace it with the real geyser prefab
        // on the exact same cells (building and placed-entity origin conventions
        // match), then delete the kit. BuildableNaturalTile pattern, dictionary-gated.
        [HarmonyPatch(typeof(BuildingComplete), "OnSpawn")]
        public static class BuildingComplete_OnSpawn_Patch
        {
            public static void Postfix(BuildingComplete __instance)
            {
                KPrefabID kitId = __instance.GetComponent<KPrefabID>();
                if (kitId == null ||
                    !GeyserKitCatalog.GeyserByKit.TryGetValue(kitId.PrefabTag, out Tag geyserTag))
                {
                    return;
                }

                GameObject kit = __instance.gameObject;
                int cell = Grid.PosToCell(kit);
                Vector3 geyserPosition = Grid.CellToPosCBC(cell, Grid.SceneLayer.BuildingBack);
                GameUtil.KInstantiate(Assets.GetPrefab(geyserTag), geyserPosition,
                    Grid.SceneLayer.BuildingBack).SetActive(value: true);
                Util.KDestroyGameObject(kit);
            }
        }
    }

    // The live catalog: one kit per geyser prefab found in the loaded game.
    internal static class GeyserKitCatalog
    {
        public const string KitIdPrefix = "GeyserKit_";
        public const string BaseMenuCategory = "Base";
        public const string GeysersSubcategory = "geysers";

        // Thematic mid-game unlock: whatever tech already contains the Geotuner
        // (runtime lookup so tech-tree renames can't break us; InsulatedDoor pattern).
        private const string UnlockSiblingItem = "GeoTuner";
        private const string FallbackUnlockSiblingItem = "OilRefinery";

        public static readonly Dictionary<Tag, Tag> GeyserByKit = new Dictionary<Tag, Tag>();

        public static void DiscoverGeysersAndRegisterKits()
        {
            AddSubcategoryTitle();
            Tech unlockTech = FindUnlockTech();

            // Snapshot first: RegisterBuilding adds the kit's own prefabs to
            // Assets.Prefabs, which would invalidate a live iteration.
            List<KPrefabID> geysers = FindGeyserPrefabs();
            foreach (KPrefabID geyser in geysers)
            {
                GeyserKitSpec spec = BuildSpec(geyser);
                if (spec == null)
                {
                    continue;
                }

                AddKitStrings(spec);
                BuildingConfigManager.Instance.RegisterBuilding(new GeyserKitConfig(spec));
                ModUtil.AddBuildingToPlanScreen(
                    (HashedString)BaseMenuCategory, spec.KitId, GeysersSubcategory);
                unlockTech?.unlockedItemIDs.Add(spec.KitId);
                GeyserByKit[spec.KitId] = spec.GeyserPrefabTag;
            }

            Debug.Log($"[BuildableGeysers] Registered {GeyserByKit.Count} geyser kits" +
                      $" (unlock tech: {unlockTech?.Id ?? "none - left unlocked"}).");
        }

        private static List<KPrefabID> FindGeyserPrefabs()
        {
            List<KPrefabID> geysers = new List<KPrefabID>();
            foreach (KPrefabID prefab in Assets.Prefabs)
            {
                if (prefab != null &&
                    prefab.GetComponent<Geyser>() != null &&
                    prefab.GetComponent<GeyserConfigurator>() != null)
                {
                    geysers.Add(prefab);
                }
            }
            return geysers;
        }

        private static GeyserKitSpec BuildSpec(KPrefabID geyser)
        {
            KBatchedAnimController anim = geyser.GetComponent<KBatchedAnimController>();
            if (anim == null || anim.AnimFiles == null ||
                anim.AnimFiles.Length == 0 || anim.AnimFiles[0] == null)
            {
                Debug.LogWarning($"[BuildableGeysers] Skipping {geyser.PrefabTag}: no anim on prefab.");
                return null;
            }

            OccupyArea footprint = geyser.GetComponent<OccupyArea>();
            if (footprint == null)
            {
                Debug.LogWarning($"[BuildableGeysers] Skipping {geyser.PrefabTag}: no OccupyArea.");
                return null;
            }
            MeasureFootprint(footprint, out int width, out int height);

            return new GeyserKitSpec(
                KitIdPrefix + geyser.PrefabTag,
                geyser.PrefabTag,
                geyser.GetProperName(),
                anim.AnimFiles[0].name,
                width, height,
                geyser.requiredDlcIds,
                geyser.forbiddenDlcIds);
        }

        private static void MeasureFootprint(OccupyArea footprint, out int width, out int height)
        {
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (CellOffset offset in footprint.OccupiedCellsOffsets)
            {
                minX = Mathf.Min(minX, offset.x);
                maxX = Mathf.Max(maxX, offset.x);
                minY = Mathf.Min(minY, offset.y);
                maxY = Mathf.Max(maxY, offset.y);
            }
            width = maxX - minX + 1;
            height = maxY - minY + 1;
        }

        private static void AddKitStrings(GeyserKitSpec spec)
        {
            string key = spec.KitId.ToUpperInvariant();
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.NAME",
                UI.FormatAsLink(spec.GeyserName + " Kit", spec.KitId));
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.DESC",
                "A flat-packed geyser; some assembly required.");
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{key}.EFFECT",
                "Unpacks into a real " + spec.GeyserName +
                " when construction completes. Its output rolls the same way as a" +
                " naturally generated geyser.");
        }

        private static void AddSubcategoryTitle()
        {
            string key = GeysersSubcategory.ToUpperInvariant();
            Strings.Add($"STRINGS.UI.NEWBUILDCATEGORIES.{key}.NAME", "Geysers");
            Strings.Add($"STRINGS.UI.NEWBUILDCATEGORIES.{key}.BUILDMENUTITLE", "Geysers");
        }

        private static Tech FindUnlockTech()
        {
            Database.Techs techs = Db.Get().Techs;
            Tech tech = techs.TryGetTechForTechItem(UnlockSiblingItem) ??
                        techs.TryGetTechForTechItem(FallbackUnlockSiblingItem);
            if (tech == null)
            {
                Debug.LogWarning("[BuildableGeysers] No unlock tech found (GeoTuner/OilRefinery" +
                                 " missing from the tech tree); kits will be available unresearched.");
            }
            return tech;
        }
    }
}
