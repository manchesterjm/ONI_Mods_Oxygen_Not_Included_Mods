using System;
using HarmonyLib;
using KSerialization;
using STRINGS;
using TUNING;
using UnityEngine;

namespace TemperatureFilters
{
    // Liquid/Gas Temperature Filter: a vanilla-Filter clone that routes by packet TEMPERATURE
    // instead of element. The side screen is the thermo sensor's threshold panel (IThresholdSwitch:
    // Above/Below buttons + typed input + nonlinear slider, native °C/°F/K) — in Above mode packets
    // warmer than the threshold leave through the filtered port, in Below mode cooler ones do;
    // everything else continues out the normal output. Flow handling mirrors the decompiled
    // vanilla ElementFilter at 740622 minus the solid-conveyor branch and the element picker.
    [SerializationConfig(MemberSerialization.OptIn)]
    public class TemperatureFilter : KMonoBehaviour, ISecondaryOutput, IThresholdSwitch
    {
        private const float DEFAULT_THRESHOLD_K = 348.15f; // 75 °C
        private const float RANGE_MIN_K = 0f;
        private const float RANGE_MAX_K = 9999f;

        [SerializeField]
        public ConduitPortInfo portInfo;

        [Serialize]
        private float thresholdKelvin = DEFAULT_THRESHOLD_K;

        [Serialize]
        private bool activateAboveThreshold = true;

        private float lastSeenTempKelvin = DEFAULT_THRESHOLD_K;

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
                        string key = filter.activateAboveThreshold
                            ? "STRINGS.BUILDINGS.PREFABS.TEMPERATUREFILTER.STATUS_ITEM_ABOVE"
                            : "STRINGS.BUILDINGS.PREFABS.TEMPERATUREFILTER.STATUS_ITEM_BELOW";
                        return string.Format(Strings.Get(key),
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
                if (contents.mass > 0f)
                {
                    lastSeenTempKelvin = contents.temperature;
                }
                bool toFiltered = activateAboveThreshold
                    ? contents.temperature > thresholdKelvin
                    : contents.temperature < thresholdKelvin;
                int destination = toFiltered ? filteredCell : outputCell;
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

        public float Threshold
        {
            get => thresholdKelvin;
            set => thresholdKelvin = Mathf.Clamp(value, RANGE_MIN_K, RANGE_MAX_K);
        }

        public bool ActivateAboveThreshold
        {
            get => activateAboveThreshold;
            set => activateAboveThreshold = value;
        }

        public float CurrentValue => lastSeenTempKelvin;

        public float RangeMin => RANGE_MIN_K;

        public float RangeMax => RANGE_MAX_K;

        public LocString Title => UI.UISIDESCREENS.TEMPERATURESWITCHSIDESCREEN.TITLE;

        public LocString ThresholdValueName => UI.UISIDESCREENS.THRESHOLD_SWITCH_SIDESCREEN.TEMPERATURE;

        public string AboveToolTip =>
            Strings.Get("STRINGS.UI.UISIDESCREENS.TEMPERATUREFILTER.TOOLTIP_ABOVE");

        public string BelowToolTip =>
            Strings.Get("STRINGS.UI.UISIDESCREENS.TEMPERATUREFILTER.TOOLTIP_BELOW");

        public ThresholdScreenLayoutType LayoutType => ThresholdScreenLayoutType.SliderBar;

        public int IncrementScale => 1;

        public NonLinearSlider.Range[] GetRanges => new NonLinearSlider.Range[4]
        {
            new NonLinearSlider.Range(25f, 260f),
            new NonLinearSlider.Range(50f, 400f),
            new NonLinearSlider.Range(12f, 1500f),
            new NonLinearSlider.Range(13f, 10000f),
        };

        public float GetRangeMinInputField()
        {
            return GameUtil.GetConvertedTemperature(RangeMin);
        }

        public float GetRangeMaxInputField()
        {
            return GameUtil.GetConvertedTemperature(RangeMax);
        }

        public LocString ThresholdValueUnits()
        {
            return GameUtil.temperatureUnit switch
            {
                GameUtil.TemperatureUnit.Celsius => UI.UNITSUFFIXES.TEMPERATURE.CELSIUS,
                GameUtil.TemperatureUnit.Fahrenheit => UI.UNITSUFFIXES.TEMPERATURE.FAHRENHEIT,
                _ => UI.UNITSUFFIXES.TEMPERATURE.KELVIN,
            };
        }

        public string Format(float value, bool units)
        {
            return GameUtil.GetFormattedTemperature(value, GameUtil.TimeSlice.None,
                GameUtil.TemperatureInterpretation.Absolute, units, roundInDestinationFormat: true);
        }

        public float ProcessedSliderValue(float input)
        {
            return Mathf.Round(input);
        }

        public float ProcessedInputValue(float input)
        {
            return GameUtil.GetTemperatureConvertedToKelvin(input);
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
            Strings.Add("STRINGS.BUILDINGS.PREFABS.TEMPERATUREFILTER.STATUS_ITEM_ABOVE",
                "Filtered output: above {0}");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.TEMPERATUREFILTER.STATUS_ITEM_BELOW",
                "Filtered output: below {0}");
            Strings.Add("STRINGS.UI.UISIDESCREENS.TEMPERATUREFILTER.TOOLTIP_ABOVE",
                "Contents warmer than the threshold will leave through the filtered output");
            Strings.Add("STRINGS.UI.UISIDESCREENS.TEMPERATUREFILTER.TOOLTIP_BELOW",
                "Contents cooler than the threshold will leave through the filtered output");
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
