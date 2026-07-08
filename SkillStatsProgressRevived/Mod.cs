using HarmonyLib;
using KMod;
using UnityEngine;

namespace SkillStatsProgressRevived
{
    // Skill and Stats Progress (Revived), 2026-07-08. Full clone of the dead
    // Steam Workshop mod "Skill and Stats Progress" (1710666223, internally
    // "Stats Info" / OniStatsPlus), rebuilt for build 740622 from the
    // decompiled source preserved in ONI_Mods_Fork/SkillStatsProgress/.
    //
    // Subsystems (each in its own file):
    //   WorkPopups   - floating reports over a duplicant when they START work
    //                  (workable, efficiency, attribute level) and FINISH work
    //                  (time worked, XP gained as % of next level / "Lvl UP").
    //                  This is the popup Josh missed from the old mod.
    //   PanelXp      - the dupe's Attributes panel rewritten with XP numbers,
    //                  %, and needed-XP tooltips; skill-point XP on the resume
    //                  panel. (Replaces SkillStatsLite - disable that mod.)
    //   XpDeltas     - optional rolling-window "XP gained recently" per stat.
    //   MoveStats    - current/average speed and travel-distance rows.
    //
    // API drift from the 2021 original, mapped 2026-07-08:
    //   MinionStatsPanel (gone)      -> MinionPersonalityPanel SetLabel/Commit
    //   Worker                       -> WorkerBase / StandardWorker.Work
    //   PopFX.Speed instance field   -> const (per-popup drift speed dropped)
    //   XML StatsConfig.TxT          -> PLib Options (Mods screen gear button)
    public class SkillStatsProgressRevivedMod : UserMod2
    {
        public static Options Options { get; private set; } = new Options();

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PeterHan.PLib.Core.PUtil.InitLibrary(false);
            new PeterHan.PLib.Options.POptions().RegisterOptions(this, typeof(Options));
            Options = Options.Load();
            Debug.Log("SkillStatsProgressRevived loaded! Work popups: "
                + (Options.ShowWorkPopups ? "on" : "off")
                + (Options.OnlySelectedDuplicant ? " (selected duplicant only)" : " (all duplicants)"));
        }
    }
}
