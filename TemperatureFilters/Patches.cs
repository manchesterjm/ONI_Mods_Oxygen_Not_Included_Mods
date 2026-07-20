using System;
using HarmonyLib;
using KSerialization;
using STRINGS;
using TUNING;
using UnityEngine;

namespace TemperatureFilters
{
    // Liquid/Gas Temperature Filter: a vanilla-Filter clone that routes by packet TEMPERATURE
    // instead of element. Packets at or above the threshold leave through the filtered (orange)
    // port; cooler packets continue out the normal output. Threshold is a per-building slider
    // side screen with a type-in box (same ISingleSliderControl pattern as Aquatuner Cooling
    // Factor). Flow handling mirrors the decompiled vanilla ElementFilter at 740622 minus the
    // solid-conveyor branch and the Filterable element picker.
    [SerializationConfig(MemberSerialization.OptIn)]
    public class TemperatureFilter : KMonoBehaviour, ISecondaryOutput, ISingleSliderControl
    {
        public const float DEFAULT_THRESHOLD_C = 75f;
        public const float MIN_C = -200f;
        public const float MAX_C = 2500f;
        private const float KELVIN_OFFSET = 273.15f;

        [SerializeField]
        public ConduitPortInfo portInfo;

        [Serialize]
        private float thresholdKelvin = DEFAULT_THRESHOLD_C + KELVIN_OFFSET;

#pragma warning disable CS0649 // populated by KMonoBehaviour's MyCmp attribute wiring
        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Building building;

        [MyCmpReq]
        private KSelectable selectable;

        [MyCmpGet]
        private KBatchedAnimController controller;
#pragma warning restore CS0649

        private Guid needsConduitStatusItemGuid;
        private Guid conduitBlockedStatusItemGuid;
        private int inputCell = -1;
        private int outputCell = -1;
        private int filteredCell = -1;
        private FlowUtilityNetwork.NetworkItem networkItem;
        private HandleVector<int>.Handle partitionerEntry;
        private SimHashes lastElementMoved = SimHashes.Vacuum;

        private static StatusItem thresholdStatusItem;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            if (thresholdStatusItem == null)
            {
                thresholdStatusItem = new StatusItem("TemperatureFilterThreshold", "BUILDING", "",
                    StatusItem.IconType.Info, NotificationType.Neutral, allow_multiples: false,
                    OverlayModes.LiquidConduits.ID)
                {
                    resolveStringCallback = delegate (string _, object data)
                    {
                        TemperatureFilter filter = (TemperatureFilter)data;
                        return string.Format(Strings.Get("STRINGS.BUILDINGS.PREFABS.TEMPERATUREFILTER.STATUS_ITEM"),
                            GameUtil.GetFormattedTemperature(filter.thresholdKelvin));
                    },
                };
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            inputCell = building.GetUtilityInputCell();
            outputCell = building.GetUtilityOutputCell();
            int baseCell = Grid.PosToCell(transform.GetPosition());
            filteredCell = Grid.OffsetCell(baseCell, building.GetRotatedOffset(portInfo.offset));
            networkItem = new FlowUtilityNetwork.NetworkItem(portInfo.conduitType, Endpoint.Source,
                filteredCell, gameObject);
            Conduit.GetNetworkManager(portInfo.conduitType)
                .AddToNetworks(filteredCell, networkItem, is_endpoint: true);
            ConduitConsumer consumer = GetComponent<ConduitConsumer>();
            if (consumer != null)
            {
                consumer.isConsuming = false;
            }
            Conduit.GetFlowManager(portInfo.conduitType).AddConduitUpdater(OnConduitTick);
            selectable.SetStatusItem(Db.Get().StatusItemCategories.Main, thresholdStatusItem, this);
            UpdateConduitExistsStatus();
            UpdateConduitBlockedStatus();
            ScenePartitionerLayer layer = portInfo.conduitType == ConduitType.Gas
                ? GameScenePartitioner.Instance.gasConduitsLayer
                : GameScenePartitioner.Instance.liquidConduitsLayer;
            partitionerEntry = GameScenePartitioner.Instance.Add("TemperatureFilterConduitExists",
                gameObject, filteredCell, layer, delegate { UpdateConduitExistsStatus(); });
        }

        protected override void OnCleanUp()
        {
            Conduit.GetNetworkManager(portInfo.conduitType)
                .RemoveFromNetworks(filteredCell, networkItem, is_endpoint: true);
            Conduit.GetFlowManager(portInfo.conduitType).RemoveConduitUpdater(OnConduitTick);
            if (partitionerEntry.IsValid() && GameScenePartitioner.Instance != null)
            {
                GameScenePartitioner.Instance.Free(ref partitionerEntry);
            }
            base.OnCleanUp();
        }

        private void OnConduitTick(float dt)
        {
            bool moved = false;
            UpdateConduitBlockedStatus();
            if (operational.IsOperational)
            {
                ConduitFlow flowManager = Conduit.GetFlowManager(portInfo.conduitType);
                ConduitFlow.ConduitContents contents = flowManager.GetContents(inputCell);
                int destination = contents.temperature >= thresholdKelvin ? filteredCell : outputCell;
                if (contents.mass > 0f && flowManager.GetContents(destination).mass <= 0f)
                {
                    moved = true;
                    float added = flowManager.AddElement(destination, contents.element, contents.mass,
                        contents.temperature, contents.diseaseIdx, contents.diseaseCount);
                    if (added > 0f)
                    {
                        if (lastElementMoved != contents.element && contents.element != SimHashes.Vacuum
                            && portInfo.conduitType == ConduitType.Liquid)
                        {
                            Element element = ElementLoader.FindElementByHash(contents.element);
                            if (element != null)
                            {
                                GameUtil.TintLiquidSymbolOnBuilding("liquid", controller, element);
                            }
                        }
                        lastElementMoved = contents.element;
                        flowManager.RemoveElement(inputCell, added);
                    }
                }
            }
            operational.SetActive(moved);
        }

        private void UpdateConduitExistsStatus()
        {
            bool connected = RequireOutputs.IsConnected(filteredCell, portInfo.conduitType);
            StatusItem statusItem = portInfo.conduitType == ConduitType.Gas
                ? Db.Get().BuildingStatusItems.NeedGasOut
                : Db.Get().BuildingStatusItems.NeedLiquidOut;
            if (connected == (needsConduitStatusItemGuid != Guid.Empty))
            {
                needsConduitStatusItemGuid = selectable.ToggleStatusItem(statusItem,
                    needsConduitStatusItemGuid, !connected);
            }
        }

        private void UpdateConduitBlockedStatus()
        {
            bool empty = Conduit.GetFlowManager(portInfo.conduitType).IsConduitEmpty(filteredCell);
            if (empty == (conduitBlockedStatusItemGuid != Guid.Empty))
            {
                conduitBlockedStatusItemGuid = selectable.ToggleStatusItem(
                    Db.Get().BuildingStatusItems.ConduitBlockedMultiples,
                    conduitBlockedStatusItemGuid, !empty);
            }
        }

        public bool HasSecondaryConduitType(ConduitType type)
        {
            return portInfo.conduitType == type;
        }

        public CellOffset GetSecondaryConduitOffset(ConduitType type)
        {
            return portInfo.offset;
        }

        public string SliderTitleKey => "STRINGS.UI.UISIDESCREENS.TEMPERATUREFILTER.TITLE";

        public string SliderUnits => " °C";

        public int SliderDecimalPlaces(int index)
        {
            return 1;
        }

        public float GetSliderMin(int index)
        {
            return MIN_C;
        }

        public float GetSliderMax(int index)
        {
            return MAX_C;
        }

        public float GetSliderValue(int index)
        {
            return thresholdKelvin - KELVIN_OFFSET;
        }

        public void SetSliderValue(float value, int index)
        {
            thresholdKelvin = Mathf.Clamp(value, MIN_C, MAX_C) + KELVIN_OFFSET;
        }

        public string GetSliderTooltipKey(int index)
        {
            return "STRINGS.UI.UISIDESCREENS.TEMPERATUREFILTER.TOOLTIP";
        }

        public string GetSliderTooltip(int index)
        {
            return string.Format(Strings.Get("STRINGS.UI.UISIDESCREENS.TEMPERATUREFILTER.TOOLTIP"),
                GameUtil.GetFormattedTemperature(thresholdKelvin));
        }
    }

    public class LiquidTemperatureFilterConfig : IBuildingConfig
    {
        public const string ID = "LiquidTemperatureFilter";

        private readonly ConduitPortInfo secondaryPort =
            new ConduitPortInfo(ConduitType.Liquid, new CellOffset(0, 0));

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(ID, 3, 1, "filter_liquid_kanim", 30, 10f,
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER3, MATERIALS.RAW_METALS, 1600f,
                BuildLocationRule.Anywhere, noise: NOISE_POLLUTION.NOISY.TIER1,
                decor: TUNING.BUILDINGS.DECOR.PENALTY.TIER0);
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 120f;
            def.SelfHeatKilowattsWhenActive = 4f;
            def.ExhaustKilowattsWhenActive = 0f;
            def.InputConduitType = ConduitType.Liquid;
            def.OutputConduitType = ConduitType.Liquid;
            def.Floodable = false;
            def.ViewMode = OverlayModes.LiquidConduits.ID;
            def.AudioCategory = "Metal";
            def.UtilityInputOffset = new CellOffset(-1, 0);
            def.UtilityOutputOffset = new CellOffset(1, 0);
            def.PermittedRotations = PermittedRotations.R360;
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.LiquidVentIDs, ID);
            def.AddSearchTerms(SEARCH_TERMS.FILTER);
            return def;
        }

        private void AttachPort(GameObject go)
        {
            go.AddComponent<ConduitSecondaryOutput>().portInfo = secondaryPort;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
            AttachPort(go);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            AttachPort(go);
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            go.AddOrGet<TemperatureFilter>().portInfo = secondaryPort;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGetDef<PoweredActiveController.Def>().showWorkingStatus = true;
            go.GetComponent<KPrefabID>().AddTag(GameTags.OverlayInFrontOfConduits);
        }
    }

    public class GasTemperatureFilterConfig : IBuildingConfig
    {
        public const string ID = "GasTemperatureFilter";

        private readonly ConduitPortInfo secondaryPort =
            new ConduitPortInfo(ConduitType.Gas, new CellOffset(0, 0));

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(ID, 3, 1, "filter_gas_kanim", 30, 10f,
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER1, MATERIALS.RAW_METALS, 1600f,
                BuildLocationRule.Anywhere, noise: NOISE_POLLUTION.NOISY.TIER1,
                decor: TUNING.BUILDINGS.DECOR.PENALTY.TIER0);
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 120f;
            def.SelfHeatKilowattsWhenActive = 0f;
            def.ExhaustKilowattsWhenActive = 0f;
            def.InputConduitType = ConduitType.Gas;
            def.OutputConduitType = ConduitType.Gas;
            def.Floodable = false;
            def.ViewMode = OverlayModes.GasConduits.ID;
            def.AudioCategory = "Metal";
            def.UtilityInputOffset = new CellOffset(-1, 0);
            def.UtilityOutputOffset = new CellOffset(1, 0);
            def.PermittedRotations = PermittedRotations.R360;
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.GasVentIDs, ID);
            def.AddSearchTerms(SEARCH_TERMS.FILTER);
            return def;
        }

        private void AttachPort(GameObject go)
        {
            go.AddComponent<ConduitSecondaryOutput>().portInfo = secondaryPort;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
            AttachPort(go);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            AttachPort(go);
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            go.AddOrGet<TemperatureFilter>().portInfo = secondaryPort;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGetDef<PoweredActiveController.Def>().showWorkingStatus = true;
            go.GetComponent<KPrefabID>().AddTag(GameTags.OverlayInFrontOfConduits);
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
    {
        public static void Prefix()
        {
            AddStrings(LiquidTemperatureFilterConfig.ID, "Liquid Temperature Filter",
                "Sorts Liquid by temperature instead of type.",
                "Liquid at or above the temperature threshold is redirected to the filtered output; "
                + "cooler liquid continues through the normal output.");
            AddStrings(GasTemperatureFilterConfig.ID, "Gas Temperature Filter",
                "Sorts Gas by temperature instead of type.",
                "Gas at or above the temperature threshold is redirected to the filtered output; "
                + "cooler gas continues through the normal output.");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.TEMPERATUREFILTER.STATUS_ITEM",
                "Filtered output: at or above {0}");
            Strings.Add("STRINGS.UI.UISIDESCREENS.TEMPERATUREFILTER.TITLE", "Temperature Threshold");
            Strings.Add("STRINGS.UI.UISIDESCREENS.TEMPERATUREFILTER.TOOLTIP",
                "Contents at or above {0} leave through the filtered output; cooler contents "
                + "continue through the normal output.");
            AddToPlanScreenBeside("LiquidFilter", LiquidTemperatureFilterConfig.ID);
            AddToPlanScreenBeside("GasFilter", GasTemperatureFilterConfig.ID);
        }

        private static void AddStrings(string id, string name, string description, string effect)
        {
            string key = "STRINGS.BUILDINGS.PREFABS." + id.ToUpperInvariant();
            Strings.Add(key + ".NAME", UI.FormatAsLink(name, id));
            Strings.Add(key + ".DESC", description);
            Strings.Add(key + ".EFFECT", effect);
        }

        // Reuse the vanilla filter's plan-screen category + subcategory (runtime lookup, immune
        // to menu reshuffles) and slot the new building directly after it.
        private static void AddToPlanScreenBeside(string vanillaId, string newId)
        {
            foreach (PlanScreen.PlanInfo planInfo in TUNING.BUILDINGS.PLANORDER)
            {
                foreach (System.Collections.Generic.KeyValuePair<string, string> entry
                         in planInfo.buildingAndSubcategoryData)
                {
                    if (entry.Key == vanillaId)
                    {
                        ModUtil.AddBuildingToPlanScreen(planInfo.category, newId, entry.Value, vanillaId);
                        return;
                    }
                }
            }
            ModUtil.AddBuildingToPlanScreen("Plumbing", newId, "uncategorized");
        }
    }

    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    public static class Db_Initialize_Patch
    {
        // Unlock each filter in whatever tech already unlocks its vanilla counterpart.
        public static void Postfix()
        {
            AddToTechBeside("LiquidFilter", LiquidTemperatureFilterConfig.ID);
            AddToTechBeside("GasFilter", GasTemperatureFilterConfig.ID);
        }

        private static void AddToTechBeside(string vanillaId, string newId)
        {
            foreach (Tech tech in Db.Get().Techs.resources)
            {
                if (tech.unlockedItemIDs.Contains(vanillaId))
                {
                    tech.unlockedItemIDs.Add(newId);
                    return;
                }
            }
            Debug.LogWarning("[TemperatureFilters] No tech found containing " + vanillaId
                + "; " + newId + " will be unlocked from the start.");
        }
    }
}
