using HarmonyLib;
using static STRINGS.UI;
using Database;
using System.Collections.Generic;

namespace InsulatedPlatesMod
{
    [HarmonyPatch(typeof(Db))]
    [HarmonyPatch("Initialize")]
    public static class Db_Initialize_Patch
    {
        public static void Prefix()
        {}

        public static void Postfix()
        {
            ModUtil.AddBuildingToPlanScreen("Power", InsulatedWireBridgeHighWattageConfig.ID);
            ModUtil.AddBuildingToPlanScreen("Power", InsulatedWireRefinedBridgeHighWattageConfig.ID);
            ModUtil.AddBuildingToPlanScreen("Power", LongInsulatedWireBridgeHighWattageConfig.ID);
            ModUtil.AddBuildingToPlanScreen("Power", LongInsulatedRefinedWireBridgeHighWattageConfig.ID);

            // Both prefer postfix() for adding tech tree entries
            bridgeHelpers.bridgeTechTree(InsulatedWireBridgeHighWattageConfig.ID, InsulatedWireBridgeHighWattageConfig.tech);
            bridgeHelpers.bridgeTechTree(InsulatedWireRefinedBridgeHighWattageConfig.ID, InsulatedWireRefinedBridgeHighWattageConfig.tech);
            bridgeHelpers.bridgeTechTree(LongInsulatedWireBridgeHighWattageConfig.ID, LongInsulatedWireBridgeHighWattageConfig.tech);
            bridgeHelpers.bridgeTechTree(LongInsulatedRefinedWireBridgeHighWattageConfig.ID, LongInsulatedRefinedWireBridgeHighWattageConfig.tech);
        }
    }

    [HarmonyPatch(typeof(BuildingComplete), "OnSpawn")]
    public class InsulatedBridge_BuildingComplete_OnSpawn
    {
        public static void Postfix(ref BuildingComplete __instance)
        {

            if (string.Compare(__instance.name, "LongInsulatedRefinedWireBridgeHighWattageComplete") == 0)
            {
                __instance.gameObject.AddOrGet<InsulatingPlate>();
            }
            if (string.Compare(__instance.name, "LongInsulatedWireBridgeHighWattageComplete") == 0)
            {
                __instance.gameObject.AddOrGet<InsulatingPlate>();
            }
            InsulatingPlate insulatingPlate = __instance.gameObject.GetComponent<InsulatingPlate>();

            if (insulatingPlate != null)
            {
                insulatingPlate.SetInsulation(__instance.gameObject, insulatingPlate.building.Def.ThermalConductivity);
            }
        }
    }

    public class bridgeHelpers
    {
        public static void bridgeTechTree(string id, string researchGroup)
        {
            if (researchGroup == "none") return;
            
            Tech tech = Db.Get().Techs.TryGet(researchGroup);
            tech?.AddUnlockedItemIDs(id);
        }
    }


    public class STRINGS
    {
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class INSULATEDWIREBRIDGEHIGHWATTAGE
                {
                    public static LocString NAME = FormatAsLink("Insulated Heavi-Watt Joint Plate", InsulatedWireBridgeHighWattageConfig.ID);
                    public static LocString DESC = "Joint plates can run Heavi-Watt wires through walls without leaking gas or liquid.";
                    public static LocString EFFECT = "Insulated version. Allows " + FormatAsLink("Heavi-Watt Wire", "HIGHWATTAGEWIRE") + " to be run through wall and floor tile.\n\nFunctions as regular tile.";
                }
                public class INSULATEDWIREREFINEDBRIDGEHIGHWATTAGE
                {
                    public static LocString NAME = FormatAsLink("Insulated Heavi-Watt Conductive Joint Plate", InsulatedWireRefinedBridgeHighWattageConfig.ID);
                    public static LocString DESC = "Insulated Version. Joint plates can run Heavi-Watt wires through walls without leaking gas or liquid.";
                    public static LocString EFFECT = "Insulated Version. Carries more than a regular Insulate Heavi-Watt Joint Plate without overloading.";
                }
                public class LONGINSULATEDWIREBRIDGEHIGHWATTAGE
                {
                    public static LocString NAME = FormatAsLink("Long Insulated Heavi-Watt Joint Plate", LongInsulatedWireBridgeHighWattageConfig.ID);
                    public static LocString DESC = "Joint plates can run Heavi-Watt wires through walls without leaking gas or liquid.";
                    public static LocString EFFECT = "Insulated Long version. Allows " + FormatAsLink("Heavi-Watt Wire", "HIGHWATTAGEWIRE") + " to be run through wall and floor tile.\n\nFunctions as regular tile.";
                }
                public class LONGINSULATEDREFINEDWIREBRIDGEHIGHWATTAGE
                {
                    public static LocString NAME = FormatAsLink("Long Insulated Conductive Joint Plate", LongInsulatedRefinedWireBridgeHighWattageConfig.ID);
                    public static LocString DESC = "Insulated Long Version. Joint plates can run Heavi-Watt wires through walls without leaking gas or liquid.";
                    public static LocString EFFECT = "Insulated Long Version. Carries more than a regular Insulate Heavi-Watt Joint Plate without overloading.";
                }
            }
        }
    }
}
