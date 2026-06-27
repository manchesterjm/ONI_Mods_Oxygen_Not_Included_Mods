// Self-sealing Airlocks Revived — local fork rebuilt for ONI build 737790 (Aquatic / U59).
//
// Upstream: LoneMarauder/ONI-Mods (GPL-3.0), itself a revival of the original
// Self-sealing Airlocks mod. Makes doors (other than the pneumatic/Internal door)
// gas- AND liquid-tight while set to Auto.
//
// 737790 port notes:
//   * Dropped the original Door.OnPrefabInit postfix. It assigned
//     `overrideAnims = new KAnimFile[]{ Assets.GetAnim("anim_use_remote_kanim") }`
//     to prevent a crash. The base game now does this itself in Door.OnPrefabInit
//     (`overrideAnims = OVERRIDE_ANIMS`, the same anim), and the field changed from
//     a public KAnimFile[] to a protected Dictionary<HashedString, AnimLookupData>,
//     so the old assignment no longer compiles and is no longer needed. Verified
//     against Assembly-CSharp.dll (build 737790).
//   * The remaining two patches recompile unchanged: the SimMessages.* signatures
//     gained an optional `callbackIdx = -1` parameter, which the fresh compile binds
//     automatically (this is why the pre-built Steam DLL warned/misbehaved).
//   * config.json (AirlocksBlockLiquids) from upstream was never read by the code —
//     dropped. Liquid + gas blocking is unconditional, as it always was.

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SelfSealingAirlocks
{
    // Ensure cell properties are cleared on clean up
    [HarmonyPatch(typeof(Door), "OnCleanUp")]
    internal class SelfSealingAirlocks_Door_OnCleanUp
    {
        private static void Postfix(Door __instance)
        {
            foreach (int cell in __instance.building.PlacementCells)
            {
                SimMessages.ClearCellProperties(cell, 3);
            }
        }
    }

    // Update sim state setter to make airlock doors gas/liquid impermeable
    [HarmonyPatch(typeof(Door), "SetSimState")]
    internal class SelfSealingAirlocks_Door_SetSimState
    {
        private static bool Prefix(Door __instance, bool is_door_open, IList<int> cells)
        {
            // If the attached gameobject doesn't exist, exit here
            if (__instance.gameObject == null)
            { return true; }

            // Get the door control state
            Door.ControlState controlState = Traverse.Create(__instance).Field("controlState").GetValue<Door.ControlState>();

            // Get the door type
            Door.DoorType doorType = __instance.doorType;

            // Exit here if the door type is 'Internal', or the door state is set to 'Opened'
            if (doorType == Door.DoorType.Internal)
            {
                return true;
            }

            // Get the mass of the door (per cell)
            PrimaryElement element = __instance.GetComponent<PrimaryElement>();
            float mass_per_cell = element.Mass / cells.Count;

            // Loop over each cell making up the door
            for (int i = 0; i < cells.Count; i++)
            {
                int cell = cells[i];

                SimMessages.SetCellProperties(cell, 1);
                // On opening
                if (is_door_open)
                {
                    MethodInfo method_opened = AccessTools.Method(typeof(Door), "OnSimDoorOpened", null, null);
                    System.Action cb_opened = (System.Action)Delegate.CreateDelegate(typeof(System.Action), __instance, method_opened);
                    HandleVector<Game.CallbackInfo>.Handle handle = Game.Instance.callbackManager.Add(new Game.CallbackInfo(cb_opened, false));

                    if (controlState != Door.ControlState.Opened)
                    {
                        SimMessages.ReplaceAndDisplaceElement(cell, element.ElementID, CellEventLogger.Instance.DoorOpen, mass_per_cell, element.Temperature, byte.MaxValue, 0, handle.index);
                    }
                    else
                    {
                        SimMessages.ClearCellProperties(cell, 1);
                        SimMessages.ReplaceAndDisplaceElement(cell, SimHashes.Vacuum, CellEventLogger.Instance.DoorOpen, 0.0f, callbackIdx: handle.index);
                    }
                }
                // On closing
                else
                {
                    MethodInfo method_closed = AccessTools.Method(typeof(Door), "OnSimDoorClosed", null, null);
                    System.Action cb_closed = (System.Action)Delegate.CreateDelegate(typeof(System.Action), __instance, method_closed);
                    HandleVector<Game.CallbackInfo>.Handle handle = Game.Instance.callbackManager.Add(new Game.CallbackInfo(cb_closed, false));
                    SimMessages.ReplaceAndDisplaceElement(cell, element.ElementID, CellEventLogger.Instance.DoorClose, mass_per_cell, element.Temperature, byte.MaxValue, 0, handle.index);
                }
            }

            // Exit, do not run the orginal method
            return false;
        }
    }
}
