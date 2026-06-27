using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using KMod;

namespace CrackReacher2
{
    // Crack Reacher (Steam 1854704869). Revived from the shipped DLL and rebuilt for
    // build 737790 (the Steam version is from 2020/2021, source host gone). Adds four
    // straight-down reach offsets so duplicants can reach 4 cells below themselves
    // (e.g. down a 1-wide crack) by extending the inverted standard offset tables.
    public class Patches : UserMod2
    {
        // The extra reach: 4 cells straight down.
        private static readonly CellOffset[] TableExpansion =
        {
            new CellOffset(0, -4), new CellOffset(0, -3),
            new CellOffset(0, -2), new CellOffset(0, -1)
        };

        private static bool _offsetsExpanded;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
        }

        [HarmonyPatch(typeof(Game), "OnPrefabInit")]
        public static class Game_OnPrefabInit_Patch
        {
            public static void Postfix()
            {
                if (_offsetsExpanded)
                {
                    return;
                }
                ExpandTable(ref OffsetGroups.InvertedStandardTable);
                ExpandTable(ref OffsetGroups.InvertedStandardTableWithCorners);
                _offsetsExpanded = true;
            }

            private static void ExpandTable(ref CellOffset[][] table)
            {
                if (table.Any(row => RowsEqual(row, TableExpansion)))
                {
                    return;
                }
                List<CellOffset[]> rows = table.ToList();
                rows.Add(TableExpansion);
                table = OffsetTable.Mirror(rows.ToArray());
            }

            private static bool RowsEqual(CellOffset[] a, CellOffset[] b)
            {
                if (a == null || b == null || a.Length != b.Length || a.Length == 0)
                {
                    return false;
                }
                for (int i = 0; i < a.Length; i++)
                {
                    if (!a[i].Equals(b[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
