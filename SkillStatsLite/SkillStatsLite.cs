using HarmonyLib;
using KMod;
using Klei.AI;
using System.Collections.Generic;
using UnityEngine;

namespace SkillStatsLite
{
    public class SkillStatsLiteMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("SkillStatsLite v1.0 loaded - Shows skill and stat XP progress");
        }
    }

    /// <summary>
    /// Patches MinionPersonalityPanel.RefreshAttributesPanel to show XP progress for each attribute
    /// </summary>
    [HarmonyPatch(typeof(MinionPersonalityPanel), "RefreshAttributesPanel")]
    public static class MinionPersonalityPanel_RefreshAttributesPanel_Patch
    {
        public static bool Prefix(CollapsibleDetailContentPanel targetPanel, GameObject targetEntity)
        {
            if (!targetEntity.GetComponent<MinionIdentity>())
            {
                targetPanel.SetActive(false);
                return false;
            }

            // Get attribute levels component for XP info
            AttributeLevels attrLevels = targetEntity.GetComponent<AttributeLevels>();

            // Get skill-display attributes
            List<AttributeInstance> list = new List<AttributeInstance>(targetEntity.GetAttributes().AttributeTable)
                .FindAll((AttributeInstance a) => a.Attribute.ShowInUI == Klei.AI.Attribute.Display.Skill);

            if (list.Count > 0)
            {
                foreach (AttributeInstance item in list)
                {
                    string label;
                    string tooltip = item.GetAttributeValueTooltip();

                    // Try to get XP progress for this attribute
                    if (attrLevels != null)
                    {
                        AttributeLevel level = attrLevels.GetAttributeLevel(item.Id);
                        if (level != null)
                        {
                            float currentExp = level.experience;
                            float expForNext = level.GetExperienceForNextLevel();
                            float percentComplete = level.GetPercentComplete() * 100f;

                            // Format: "Athletics: 5  (125/500 XP - 25.0%)"
                            label = string.Format("{0}: {1}  ({2:F0}/{3:F0} XP - {4:F1}%)",
                                item.Name,
                                item.GetFormattedValue(),
                                currentExp,
                                expForNext,
                                percentComplete);

                            // Enhanced tooltip with XP info
                            tooltip += string.Format("\n\n<b>Experience Progress:</b>\nCurrent: {0:F0} XP\nNeeded: {1:F0} XP\nProgress: {2:F1}%",
                                currentExp,
                                expForNext - currentExp,
                                percentComplete);
                        }
                        else
                        {
                            label = string.Format("{0}: {1}", item.Name, item.GetFormattedValue());
                        }
                    }
                    else
                    {
                        label = string.Format("{0}: {1}", item.Name, item.GetFormattedValue());
                    }

                    targetPanel.SetLabel(item.Id, label, tooltip);
                }
            }

            targetPanel.Commit();
            return false; // Skip original method
        }
    }

    /// <summary>
    /// Patches MinionPersonalityPanel.RefreshResumePanel to show skill point XP progress
    /// </summary>
    [HarmonyPatch(typeof(MinionPersonalityPanel), "RefreshResumePanel")]
    public static class MinionPersonalityPanel_RefreshResumePanel_Patch
    {
        public static bool Prefix(CollapsibleDetailContentPanel targetPanel, GameObject targetEntity)
        {
            MinionResume component = targetEntity.GetComponent<MinionResume>();
            if (component == null)
            {
                return true; // Run original if no resume
            }

            targetPanel.SetTitle(string.Format(STRINGS.UI.DETAILTABS.PERSONALITY.GROUPNAME_RESUME, targetEntity.name.ToUpper()));

            // Add skill point XP progress at the top
            int prevExpBar = (int)MinionResume.CalculatePreviousExperienceBar(component.TotalSkillPointsGained);
            int nextExpBar = (int)MinionResume.CalculateNextExperienceBar(component.TotalSkillPointsGained);
            int currentExp = (int)component.TotalExperienceGained - prevExpBar;
            int expNeeded = nextExpBar - prevExpBar;
            float percent = (expNeeded > 0) ? (100f * currentExp / expNeeded) : 100f;

            string skillXpLabel = string.Format("<b>Skill Points:</b> {0} available / {1} total",
                component.AvailableSkillpoints,
                component.TotalSkillPointsGained);

            string skillXpTooltip = string.Format(
                "Experience toward next skill point:\n" +
                "  Current: {0:N0} XP\n" +
                "  Needed: {1:N0} XP\n" +
                "  Progress: {2:F1}%\n\n" +
                "Total experience earned: {3:N0} XP",
                currentExp, expNeeded, percent, (int)component.TotalExperienceGained);

            targetPanel.SetLabel("skill_xp_progress", skillXpLabel, skillXpTooltip);

            // XP bar as text
            string progressBar = string.Format("    [{0:N0} / {1:N0} XP] ({2:F1}%)", currentExp, expNeeded, percent);
            targetPanel.SetLabel("skill_xp_bar", progressBar, skillXpTooltip);

            // Now show mastered skills (from original method)
            List<Database.Skill> masteredSkills = new List<Database.Skill>();
            foreach (KeyValuePair<string, bool> item2 in component.MasteryBySkillID)
            {
                if (item2.Value)
                {
                    Database.Skill skill = Db.Get().Skills.Get(item2.Key);
                    masteredSkills.Add(skill);
                }
            }

            targetPanel.SetLabel("mastered_skills_header", STRINGS.UI.DETAILTABS.PERSONALITY.RESUME.MASTERED_SKILLS, STRINGS.UI.DETAILTABS.PERSONALITY.RESUME.MASTERED_SKILLS_TOOLTIP);

            if (masteredSkills.Count == 0)
            {
                targetPanel.SetLabel("no_skills", "    • " + STRINGS.UI.DETAILTABS.PERSONALITY.RESUME.NO_MASTERED_SKILLS.NAME,
                    string.Format(STRINGS.UI.DETAILTABS.PERSONALITY.RESUME.NO_MASTERED_SKILLS.TOOLTIP, targetEntity.name));
            }
            else
            {
                foreach (Database.Skill skill in masteredSkills)
                {
                    if (!Game.IsCorrectDlcActiveForCurrentSave(skill))
                    {
                        continue;
                    }
                    string perksText = "";
                    foreach (Database.SkillPerk perk in skill.perks)
                    {
                        if (Game.IsCorrectDlcActiveForCurrentSave(perk))
                        {
                            perksText = perksText + "    • " + perk.Name + "\n";
                        }
                    }
                    targetPanel.SetLabel(skill.Id, "    • " + skill.Name, skill.description + "\n" + perksText);
                }
            }

            targetPanel.Commit();
            return false; // Skip original method
        }
    }
}
