using System.Collections.Generic;
using HarmonyLib;
using Klei.AI;
using UnityEngine;

namespace SkillStatsProgressRevived
{
    // The dupe's Attributes panel, rewritten the way the old mod drew it:
    // a skill-XP header, then one row per attribute with level, total value,
    // XP (earned or remaining, per options), the XP target, and the percent
    // toward the next level - bolded when the stat just gained XP. Speed and
    // travel rows follow. The resume panel gets a skill-point XP row on top.
    //
    // Both vanilla methods are tiny SetLabel loops (decompiled 740622), so a
    // prefix-replace that redraws everything is safe and matches the old
    // mod's approach. NOTE: supersedes SkillStatsLite - disable that mod, the
    // two would fight over the same panel.
    internal static class PanelXp
    {
        private static readonly Dictionary<(int dupe, string attribute), float> XpAtLastRefresh =
            new Dictionary<(int, string), float>();

        [HarmonyPatch(typeof(MinionPersonalityPanel), "RefreshAttributesPanel")]
        internal static class AttributesPanelPatch
        {
            public static bool Prefix(CollapsibleDetailContentPanel targetPanel, GameObject targetEntity)
            {
                if (!SkillStatsProgressRevivedMod.Options.ShowPanelXp)
                {
                    return true; // vanilla panel
                }
                if (!targetEntity.GetComponent<MinionIdentity>())
                {
                    targetPanel.SetActive(active: false);
                    return false;
                }
                MinionIdentity dupe = targetEntity.GetComponent<MinionIdentity>();
                DrawSkillXpHeader(targetPanel, targetEntity, dupe);
                DrawAttributeRows(targetPanel, targetEntity, dupe);
                DrawMovementRows(targetPanel, dupe);
                targetPanel.Commit();
                return false;
            }
        }

        [HarmonyPatch(typeof(MinionPersonalityPanel), "RefreshResumePanel")]
        internal static class ResumePanelPatch
        {
            // Adds the skill-point XP row, then lets vanilla draw the resume.
            public static void Prefix(CollapsibleDetailContentPanel targetPanel, GameObject targetEntity)
            {
                if (!SkillStatsProgressRevivedMod.Options.ShowPanelXp)
                {
                    return;
                }
                MinionResume resume = targetEntity.GetComponent<MinionResume>();
                if (resume == null)
                {
                    return;
                }
                SkillXpNumbers(resume, out int earned, out int target, out float percent);
                targetPanel.SetLabel("sspr_skillpoints",
                    $"Skill points: <b>{resume.AvailableSkillpoints}</b> available / {resume.TotalSkillPointsGained} total  Exp: <b>{percent:F1}%</b>",
                    $"Experience toward the next skill point: {earned:N0}/{target:N0}\nStill needed: {target - earned:N0}");
            }
        }

        private static void DrawSkillXpHeader(CollapsibleDetailContentPanel targetPanel,
            GameObject targetEntity, MinionIdentity dupe)
        {
            MinionResume resume = targetEntity.GetComponent<MinionResume>();
            if (resume == null)
            {
                return;
            }
            Options options = SkillStatsProgressRevivedMod.Options;
            SkillXpNumbers(resume, out int earned, out int target, out float percent);
            string delta = options.EnableDeltas
                ? $"  (+{XpDeltas.GainedInWindow(dupe, XpDeltas.SkillKey):F0})" : "";
            targetPanel.SetLabel("sspr_skillxp",
                $"Exp: <b>{earned}</b>{(options.ShowMaxExp ? "/" + target : "")} <b>{percent:F2}%</b>  SP:{resume.AvailableSkillpoints}/{resume.TotalSkillPointsGained}{delta}",
                $"Skill-point experience and available/total skill points.\nExp needed: {target - earned:N0}");
        }

        private static void DrawAttributeRows(CollapsibleDetailContentPanel targetPanel,
            GameObject targetEntity, MinionIdentity dupe)
        {
            Options options = SkillStatsProgressRevivedMod.Options;
            AttributeLevels levels = targetEntity.GetComponent<AttributeLevels>();
            List<AttributeInstance> attributes = new List<AttributeInstance>(targetEntity.GetAttributes().AttributeTable)
                .FindAll((AttributeInstance a) => a.Attribute.ShowInUI == Klei.AI.Attribute.Display.Skill);
            foreach (AttributeInstance attribute in attributes)
            {
                AttributeLevel level = levels?.GetAttributeLevel(attribute.Id);
                if (level == null)
                {
                    targetPanel.SetLabel(attribute.Id, $"{attribute.Name}: {attribute.GetFormattedValue()}",
                        attribute.GetAttributeValueTooltip());
                    continue;
                }
                float needed = level.GetExperienceForNextLevel() - level.experience;
                float xpShown = options.ShowRequiredXp ? needed : level.experience;
                string target = options.ShowMaxExp ? $"/{level.GetExperienceForNextLevel():F0}" : "";
                string delta = options.EnableDeltas
                    ? $"  (+{XpDeltas.GainedInWindow(dupe, attribute.Id):F0})" : "";
                string label = $"{attribute.Name} {level.GetLevel()} /{attribute.GetFormattedValue()} "
                    + $"<b>{xpShown:F0}</b>{target} <b>{level.GetPercentComplete() * 100f:F2}%</b>{delta}";
                if (JustGainedXp(dupe, attribute.Id, level.experience) && options.HighlightChanges)
                {
                    label = "=><b>" + label + "</b>";
                }
                targetPanel.SetLabel(attribute.Id, label,
                    attribute.GetAttributeValueTooltip() + $"\nExp needed: <b>{needed:F2}</b>");
            }
        }

        private static void DrawMovementRows(CollapsibleDetailContentPanel targetPanel, MinionIdentity dupe)
        {
            Options options = SkillStatsProgressRevivedMod.Options;
            Navigator navigator = dupe.GetComponent<Navigator>();
            if (navigator == null)
            {
                return;
            }
            if (options.ShowSpeedInfo)
            {
                targetPanel.SetLabel("sspr_speed", MoveStats.CurrentSpeedLabel(navigator), "Current movement speed.");
                targetPanel.SetLabel("sspr_avgspeed", MoveStats.AverageSpeedLabel(navigator),
                    $"Average speed over the last {options.AvgSpeedWindowSeconds:F0} s.");
            }
            if (options.ShowTravelDistances)
            {
                MoveStats.DistanceSummary(navigator, dupe,
                    out string todayLabel, out string todayTooltip, out string totalLabel, out string totalTooltip);
                targetPanel.SetLabel("sspr_travel_today", todayLabel, todayTooltip);
                targetPanel.SetLabel("sspr_travel_total", totalLabel, totalTooltip);
            }
        }

        private static void SkillXpNumbers(MinionResume resume, out int earned, out int target, out float percent)
        {
            int previousBar = (int)MinionResume.CalculatePreviousExperienceBar(resume.TotalSkillPointsGained);
            int nextBar = (int)MinionResume.CalculateNextExperienceBar(resume.TotalSkillPointsGained);
            earned = (int)resume.TotalExperienceGained - previousBar;
            target = nextBar - previousBar;
            percent = (target > 0) ? (100f * earned / target) : 100f;
        }

        // A stat is highlighted while its XP differs from the last refresh we
        // recorded; the record is frozen while paused so the bold survives a
        // pause, exactly like the old mod.
        private static bool JustGainedXp(MinionIdentity dupe, string attributeId, float experience)
        {
            (int, string) key = (dupe.GetInstanceID(), attributeId);
            if (!XpAtLastRefresh.TryGetValue(key, out float previous))
            {
                XpAtLastRefresh[key] = experience;
                return false;
            }
            if (previous == experience)
            {
                return false;
            }
            if (SpeedControlScreen.Instance == null || !SpeedControlScreen.Instance.IsPaused)
            {
                XpAtLastRefresh[key] = experience;
            }
            return true;
        }
    }
}
