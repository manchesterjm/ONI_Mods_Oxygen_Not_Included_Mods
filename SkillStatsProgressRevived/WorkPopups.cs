using System.Collections.Generic;
using HarmonyLib;
using Klei.AI;
using UnityEngine;

namespace SkillStatsProgressRevived
{
    // The floating work reports - the heart of the old mod. On StartWork we
    // remember the duplicant's XP in the task's work attribute; when the work
    // session ends (StandardWorker.Work returns Success or Failed, not
    // InProgress) we report how long it took and how much XP it earned as a
    // percentage of the next level ("Lvl UP" when the level ticked over).
    internal sealed class WorkSession
    {
        private static readonly Dictionary<MinionResume, WorkSession> Active =
            new Dictionary<MinionResume, WorkSession>();

        public float StartTime;
        public float StartExperience;
        public string WorkableTypeName;
        public string AttributeId;

        // Carrying (Pickupable) trains Athletics; everything else trains the
        // workable's own attribute, when it has one.
        public static string WorkAttributeId(Workable workable)
        {
            if (workable.GetType() == typeof(Pickupable))
            {
                return Db.Get().Attributes.Athletics.Id;
            }
            return workable.GetWorkAttribute()?.Id;
        }

        public static void Begin(MinionResume dupe, Workable workable, AttributeLevels levels, string attributeId)
        {
            AttributeLevel level = (attributeId != null) ? levels?.GetAttributeLevel(attributeId) : null;
            Active[dupe] = new WorkSession
            {
                StartTime = GameClock.Instance.GetTime(),
                StartExperience = level?.experience ?? 0f,
                WorkableTypeName = workable.GetType().Name,
                AttributeId = attributeId,
            };
        }

        public static WorkSession End(MinionResume dupe)
        {
            if (Active.TryGetValue(dupe, out WorkSession session))
            {
                Active.Remove(dupe);
                return session;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(Workable), nameof(Workable.StartWork))]
    internal static class WorkStartReport
    {
        private static readonly Color ReportColor = Color.green;

        public static void Postfix(Workable __instance)
        {
            WorkerBase worker = __instance.worker;
            if (worker == null)
            {
                return;
            }
            MinionResume dupe = worker.GetComponent<MinionResume>();
            if (dupe == null)
            {
                return; // robots and machines earn no XP
            }
            AttributeLevels levels = worker.GetComponent<AttributeLevels>();
            string attributeId = WorkSession.WorkAttributeId(__instance);
            WorkSession.Begin(dupe, __instance, levels, attributeId);
            if (!SkillStatsProgressRevivedMod.Options.ShowStartPopup)
            {
                return;
            }
            float efficiencyPct = (__instance.GetEfficiencyMultiplier(worker) - 1f) * 100f;
            bool litBonus = Traverse.Create(__instance).Field("lightEfficiencyBonus").GetValue<bool>();
            string text = FloatingText.Shrink(__instance.GetType().Name, 12) + ":\nEff:"
                + ((efficiencyPct < 0f) ? "" : "+") + efficiencyPct.ToString("F0") + "% "
                + Flag(__instance.currentlyLit) + "/" + Flag(litBonus) + "\n";
            if (attributeId != null && levels != null)
            {
                AttributeLevel level = levels.GetAttributeLevel(attributeId);
                AttributeInstance instance = worker.gameObject.GetAttributes().AttributeTable
                    .Find((AttributeInstance a) => a.Id == attributeId);
                text += Db.Get().Attributes.Get(attributeId).Name + ":"
                    + (level?.level.ToString() ?? "??") + "/" + (instance?.GetTotalValue().ToString() ?? "?");
            }
            FloatingText.Show(text, worker.gameObject, ReportColor);
        }

        private static string Flag(bool on)
        {
            return on ? "*" : "-";
        }
    }

    [HarmonyPatch(typeof(StandardWorker), nameof(StandardWorker.Work), typeof(float))]
    internal static class WorkEndReport
    {
        private static readonly Color ReportColor = Color.cyan;

        public static void Postfix(StandardWorker __instance, WorkerBase.WorkResult __result)
        {
            if (__result == WorkerBase.WorkResult.InProgress)
            {
                return; // still working - report only when the session ends
            }
            MinionResume dupe = __instance.GetComponent<MinionResume>();
            if (dupe == null)
            {
                return;
            }
            WorkSession session = WorkSession.End(dupe);
            if (session == null)
            {
                return;
            }
            string text = __result + ":\n" + FloatingText.Shrink(session.WorkableTypeName, 12)
                + "\nTime:" + (GameClock.Instance.GetTime() - session.StartTime).ToString("F2") + "s\n";
            if (session.AttributeId != null)
            {
                AttributeLevel level = __instance.GetComponent<AttributeLevels>()?.GetAttributeLevel(session.AttributeId);
                if (level != null)
                {
                    text += FloatingText.Shrink(Db.Get().Attributes.Get(session.AttributeId).ProfessionName, 3) + ":";
                    if (level.experience >= session.StartExperience)
                    {
                        float gained = level.experience - session.StartExperience;
                        text += (gained / level.GetExperienceForNextLevel() * 100f).ToString("F3") + "%";
                    }
                    else
                    {
                        text += "Lvl UP"; // XP reset to 0 by the level-up
                    }
                }
            }
            FloatingText.Show(text, __instance.gameObject, ReportColor);
        }
    }
}
