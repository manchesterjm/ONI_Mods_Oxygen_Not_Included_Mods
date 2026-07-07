using HarmonyLib;
using UnityEngine;

namespace FishReleaseCapacity
{
    // Fish Release Capacity (ORIGINAL mod, 2026-07-07). The Fish Release building
    // (FishDeliveryPointConfig, in-game "Fish Release") caps its critter-count
    // slider at 20, too small for a real fish farm. The cap is
    // BaggableCritterCapacityTracker.maximumCreatures, assigned in
    // ConfigureBuildingTemplate and surfaced to the side screen as
    // IUserControlledCapacity.MaxCapacity — raise it to 100 after the template
    // is configured. The dupe-chosen limit (creatureLimit) is unchanged; the
    // slider simply reaches further.
    public static class FishReleaseCapacityPatches
    {
        private const int RaisedMaximumCreatures = 100;

        [HarmonyPatch(typeof(FishDeliveryPointConfig),
            nameof(FishDeliveryPointConfig.ConfigureBuildingTemplate))]
        public static class FishDeliveryPointConfig_ConfigureBuildingTemplate_Patch
        {
            public static void Postfix(GameObject go)
            {
                go.GetComponent<BaggableCritterCapacityTracker>().maximumCreatures =
                    RaisedMaximumCreatures;
            }
        }
    }
}
