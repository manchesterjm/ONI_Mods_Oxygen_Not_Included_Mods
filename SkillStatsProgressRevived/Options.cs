using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace SkillStatsProgressRevived
{
    // All settings, edited in-game from the Mods screen (PLib Options button)
    // and stored in config.json next to the DLL. Defaults mirror the old mod
    // except ShowWorkPopups: the old default was off, but the popups are the
    // reason this clone exists, so they start on. RestartRequired because
    // values are read once at mod load.
    [ConfigFile("config.json", true)]
    [RestartRequired]
    public sealed class Options
    {
        [Option("Show work XP popups",
            "Floating reports over a duplicant when they start and finish a task,\nincluding the XP they gained toward the work attribute.",
            "Work popups")]
        [JsonProperty]
        public bool ShowWorkPopups { get; set; } = true;

        [Option("Only for the selected duplicant",
            "Only show popups for the duplicant currently open in the details panel.\nTurn off to see popups over every duplicant (noisy).",
            "Work popups")]
        [JsonProperty]
        public bool OnlySelectedDuplicant { get; set; } = true;

        [Option("Show XP gain popups",
            "A short report of the XP a duplicant just earned toward the work\nattribute, as percent of the next level.",
            "Work popups")]
        [JsonProperty]
        public bool ShowXpGainPopups { get; set; } = true;

        [Option("Show stat level-up popups",
            "A report whenever a duplicant's stat reaches a new level.",
            "Work popups")]
        [JsonProperty]
        public bool ShowStatLevelPopups { get; set; } = true;

        [Option("Show task start reports",
            "The detailed report when work starts (task, efficiency, attribute level).",
            "Work popups")]
        [JsonProperty]
        public bool ShowStartPopup { get; set; } = false;

        [Option("Show task finish reports",
            "The detailed report when work ends (task, time worked, XP gained).\nWhen on, it replaces the short XP gain popup.",
            "Work popups")]
        [JsonProperty]
        public bool ShowTaskFinishPopup { get; set; } = false;

        [Option("Popup font size", "Text size of the floating reports.", "Work popups")]
        [Limit(10, 60)]
        [JsonProperty]
        public int PopupFontSize { get; set; } = 30;

        [Option("Popup duration (seconds)", "How long each floating report stays on screen.", "Work popups")]
        [Limit(2, 30)]
        [JsonProperty]
        public float PopupSeconds { get; set; } = 10f;

        [Option("Popup float-away speed",
            "Tiles per second the report drifts upward.\n0 = stays pinned to the duplicant. The game's own popups accelerate\nquadratically, which flies off screen at long durations.",
            "Work popups")]
        [Limit(0, 5)]
        [JsonProperty]
        public float PopupDriftSpeed { get; set; } = 0.5f;

        [Option("Label successful tasks with Success",
            "Prefix finish reports with the work result even when it succeeded.\nFailures always say Failed regardless of this setting.",
            "Work popups")]
        [JsonProperty]
        public bool ShowSuccessLabel { get; set; } = false;

        [Option("Restyle the game's own popups",
            "Bigger font and solid icons on vanilla popups (level-ups etc.),\nlike the old mod did.",
            "Work popups")]
        [JsonProperty]
        public bool RestyleVanillaPopups { get; set; } = true;

        [Option("Show XP on the attributes panel",
            "Rewrite the duplicant's Attributes panel with XP numbers and percent\ntoward the next level. (Replaces SkillStatsLite - disable that mod.)",
            "Attributes panel")]
        [JsonProperty]
        public bool ShowPanelXp { get; set; } = true;

        [Option("Show XP remaining instead of XP earned",
            "Show how much XP is still needed for the next level\ninstead of how much has been earned so far.",
            "Attributes panel")]
        [JsonProperty]
        public bool ShowRequiredXp { get; set; } = true;

        [Option("Show the XP target next to the number",
            "Append /target to the XP number (e.g. 1234/5000).",
            "Attributes panel")]
        [JsonProperty]
        public bool ShowMaxExp { get; set; } = true;

        [Option("Bold attributes that just gained XP",
            "Highlight an attribute line when its XP changed since the last refresh.",
            "Attributes panel")]
        [JsonProperty]
        public bool HighlightChanges { get; set; } = true;

        [Option("Track XP gained per window",
            "Sample every duplicant's XP on a timer and show how much each stat\ngained inside the rolling window, as (+N) behind the stat line.",
            "XP deltas")]
        [JsonProperty]
        public bool EnableDeltas { get; set; } = false;

        [Option("Delta window (seconds)", "Rolling window the (+N) figure covers.", "XP deltas")]
        [Limit(60, 3600)]
        [JsonProperty]
        public int DeltaWindowSeconds { get; set; } = 600;

        [Option("Sample every (seconds)", "How often duplicant XP is sampled for the deltas.", "XP deltas")]
        [Limit(1, 60)]
        [JsonProperty]
        public int SampleEverySeconds { get; set; } = 5;

        [Option("Show current and average speed",
            "Add a speed row (current tile/s, position) and a rolling average\nto the Attributes panel.",
            "Movement")]
        [JsonProperty]
        public bool ShowSpeedInfo { get; set; } = true;

        [Option("Average speed window (seconds)", "Window for the rolling average speed.", "Movement")]
        [Limit(5, 600)]
        [JsonProperty]
        public float AvgSpeedWindowSeconds { get; set; } = 30f;

        [Option("Show travel distances",
            "Add rows for distance traveled today and in total (this duplicant\nand the whole colony); per-path breakdown in the tooltips.",
            "Movement")]
        [JsonProperty]
        public bool ShowTravelDistances { get; set; } = true;

        public static Options Load()
        {
            return POptions.ReadSettings<Options>() ?? new Options();
        }
    }
}
