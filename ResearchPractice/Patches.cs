using HarmonyLib;

namespace ResearchPractice
{
    // Research Practice (ORIGINAL mod, 2026-07-08). Once the tech tree is
    // finished, vanilla hard-disables every research station: the flag update
    // gates ResearchSelectedFlag on an active tech that still needs points and
    // explicitly checks IsAllResearchComplete - so Science becomes untrainable
    // for the rest of the game. This keeps the stations workable as practice:
    // with all research complete the flag is forced on, dupes take the normal
    // research chore, and they earn Science XP at the normal rate. The station
    // still consumes its usual material (the thematic cost) and the pointless
    // research points bank harmlessly. Applies to every building carrying the
    // ResearchCenter component (Research Station, Super Computer, ...); while
    // anything is left to research, vanilla behavior is untouched.
    public static class ResearchPracticePatches
    {
        [HarmonyPatch(typeof(ResearchCenter), "UpdateWorkingState")]
        public static class ResearchCenter_UpdateWorkingState_Patch
        {
            public static bool Prefix(ResearchCenter __instance)
            {
                if (!AllResearchComplete())
                {
                    return true; // vanilla behavior while the tree is unfinished
                }
                KSelectable selectable = __instance.GetComponent<KSelectable>();
                selectable.RemoveStatusItem(Db.Get().BuildingStatusItems.NoResearchSelected);
                selectable.RemoveStatusItem(Db.Get().BuildingStatusItems.NoApplicableResearchSelected);
                Operational operational = Traverse.Create(__instance).Field("operational").GetValue<Operational>();
                operational.SetFlag(ResearchCenter.ResearchSelectedFlag, true);
                return false;
            }

            private static bool AllResearchComplete()
            {
                foreach (Tech tech in Db.Get().Techs.resources)
                {
                    if (!tech.IsComplete())
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
