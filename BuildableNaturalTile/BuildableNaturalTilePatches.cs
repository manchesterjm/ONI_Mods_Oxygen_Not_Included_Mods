using HarmonyLib;
using UnityEngine;

namespace BuildableNaturalTile
{
    // The three patches that wire the Natural Tile building into the game and give
    // it its behaviour. Ported from CoolAzura's mod (Steam 1840755803); the source
    // host (code.ecool.dev) is gone, so this was rebuilt from the shipped DLL for
    // build 737790. CaiLib/CoolLib helpers were dropped (inlined / replaced with
    // current-API calls); the delete uses Util.KDestroyGameObject.
    public static class BuildableNaturalTilePatches
    {
        public static readonly Settings Settings = Settings.Load();

        // Register the building's strings and add it to the Base build menu.
        [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
        public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
        {
            public static void Prefix()
            {
                ModUtils.AddBuildingStrings(
                    NaturalTileConfig.Id, NaturalTileConfig.DisplayName,
                    NaturalTileConfig.Description, NaturalTileConfig.Effect);
                ModUtils.AddBuildingToBaseMenu(NaturalTileConfig.Id);
            }
        }

        // Unlock the building with the Basic Farming tech (same as the Ration Box).
        [HarmonyPatch(typeof(Db), "Initialize")]
        public static class Db_Initialize_Patch
        {
            public static void Postfix()
            {
                ModUtils.UnlockBuildingWithBasicFarming(NaturalTileConfig.Id);
            }
        }

        // When a Natural Tile finishes building, replace its cell with a real
        // natural block of the build element, shove any pickupable in the cell into
        // an adjacent open cell, then delete the now-redundant building object.
        [HarmonyPatch(typeof(BuildingComplete), "OnSpawn")]
        public static class BuildingComplete_OnSpawn_Patch
        {
            private static readonly CellOffset[] DisplacementOffsets =
            {
                new CellOffset(0, 1), new CellOffset(0, -1),
                new CellOffset(1, 0), new CellOffset(-1, 0),
                new CellOffset(1, 1), new CellOffset(1, -1),
                new CellOffset(-1, 1), new CellOffset(-1, -1)
            };

            public static void Postfix(BuildingComplete __instance)
            {
                if (__instance.name != "NaturalTileComplete")
                {
                    return;
                }

                GameObject go = __instance.gameObject;
                PrimaryElement element = go.GetComponent<PrimaryElement>();
                int cell = Grid.PosToCell(go.transform.position);

                SimMessages.ReplaceAndDisplaceElement(
                    cell, element.ElementID, null, Settings.BlockMass, element.Temperature);

                DisplacePickupablesOutOfCell(cell);
                Util.KDestroyGameObject(go);
            }

            // Move anything standing in the filled cell to the nearest open neighbour
            // so it isn't trapped inside the new solid block.
            private static void DisplacePickupablesOutOfCell(int cell)
            {
                foreach (Pickupable pickupable in Components.Pickupables)
                {
                    if (Grid.PosToCell(pickupable) != cell)
                    {
                        continue;
                    }

                    foreach (CellOffset offset in DisplacementOffsets)
                    {
                        int target = Grid.OffsetCell(cell, offset);
                        if (!Grid.IsValidCell(target) || Grid.Solid[target])
                        {
                            continue;
                        }

                        MovePickupableToCell(pickupable, target);
                        break;
                    }
                }
            }

            private static void MovePickupableToCell(Pickupable pickupable, int target)
            {
                Vector3 destination = Grid.CellToPosCBC(target, (Grid.SceneLayer)25);
                KCollider2D collider = pickupable.GetComponent<KCollider2D>();
                if (collider != null)
                {
                    float feet = TransformExtensions.GetPosition(pickupable.transform).y;
                    destination.y += feet - collider.bounds.min.y;
                }

                TransformExtensions.SetPosition(pickupable.transform, destination);
                Traverse faller = Traverse.Create(pickupable);
                faller.Method("RemoveFaller").GetValue();
                faller.Method("AddFaller", new[] { typeof(Vector2) }).GetValue(Vector2.zero);
            }
        }
    }
}
