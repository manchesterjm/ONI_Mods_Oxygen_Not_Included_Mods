using HarmonyLib;
using KSerialization;
using UnityEngine;

namespace AquatunerCoolingFactor
{
    // Per-building tunable Aquatuner cooling factor. Each Thermo Aquatuner gets a slider
    // side screen (with a type-in number box) setting how many degrees it removes from the
    // liquid per pass; the value is saved per building and applied to the vanilla
    // AirConditioner.temperatureDelta (negative = cooling), which the Effects panel and the
    // heat-output math both read, so displayed heat/cooling figures follow the setting.
    [SerializationConfig(MemberSerialization.OptIn)]
    public class CoolingFactorTuner : KMonoBehaviour, ISingleSliderControl
    {
        public const float VANILLA_FACTOR = 14f;
        public const float MIN_FACTOR = 0f;
        public const float MAX_FACTOR = 100f;

        [Serialize]
        private float coolingFactor = VANILLA_FACTOR;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Apply();
        }

        private void Apply()
        {
            AirConditioner conditioner = GetComponent<AirConditioner>();
            if (conditioner != null)
            {
                conditioner.temperatureDelta = -coolingFactor;
            }
        }

        public string SliderTitleKey => "STRINGS.UI.UISIDESCREENS.AQUATUNERCOOLINGFACTOR.TITLE";

        public string SliderUnits => " °C";

        public int SliderDecimalPlaces(int index)
        {
            return 1;
        }

        public float GetSliderMin(int index)
        {
            return MIN_FACTOR;
        }

        public float GetSliderMax(int index)
        {
            return MAX_FACTOR;
        }

        public float GetSliderValue(int index)
        {
            return coolingFactor;
        }

        public void SetSliderValue(float value, int index)
        {
            coolingFactor = Mathf.Clamp(value, MIN_FACTOR, MAX_FACTOR);
            Apply();
        }

        public string GetSliderTooltipKey(int index)
        {
            return "STRINGS.UI.UISIDESCREENS.AQUATUNERCOOLINGFACTOR.TOOLTIP";
        }

        public string GetSliderTooltip(int index)
        {
            return string.Format(Strings.Get("STRINGS.UI.UISIDESCREENS.AQUATUNERCOOLINGFACTOR.TOOLTIP"), coolingFactor);
        }
    }

    [HarmonyPatch(typeof(LiquidConditionerConfig), nameof(LiquidConditionerConfig.DoPostConfigureComplete))]
    public static class LiquidConditionerConfig_DoPostConfigureComplete_Patch
    {
        public static void Postfix(GameObject go)
        {
            go.AddOrGet<CoolingFactorTuner>();
        }
    }

    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    public static class Db_Initialize_Patch
    {
        public static void Postfix()
        {
            Strings.Add("STRINGS.UI.UISIDESCREENS.AQUATUNERCOOLINGFACTOR.TITLE", "Cooling Factor");
            Strings.Add("STRINGS.UI.UISIDESCREENS.AQUATUNERCOOLINGFACTOR.TOOLTIP",
                "Degrees removed from the liquid each pass (vanilla: 14). Current: {0:0.0} °C. Heat dumped into the room scales with this value.");
        }
    }
}
