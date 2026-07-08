using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace StatsUnlimitedLite
{
    // Learning-speed settings, edited in-game from the Mods screen (PLib's
    // Options button) and stored in config.json next to the DLL. Both knobs
    // are plain multipliers on experience gain: 1.0 = vanilla speed, 2.0 =
    // twice as fast, 0.5 = half. RestartRequired because the values are read
    // once at mod load.
    [ConfigFile("config.json", true)]
    [RestartRequired]
    public sealed class Settings
    {
        [Option("Maximum stat level",
            "The level cap for duplicant stats.\nVanilla is 20; the classic Stats Unlimited used 200.")]
        [Limit(20, 9999)]
        [JsonProperty]
        public int MaxStatLevel { get; set; } = 200;

        [Option("Attribute XP multiplier",
            "How fast duplicant stats (Athletics, Digging, Machinery...) level up.\n1 = vanilla speed, 2 = twice as fast.")]
        [Limit(0.1, 100)]
        [JsonProperty]
        public float AttributeXpMultiplier { get; set; } = 1f;

        [Option("Skill XP multiplier",
            "How fast duplicants earn skill points (hats).\n1 = vanilla speed, 2 = twice as fast.")]
        [Limit(0.1, 100)]
        [JsonProperty]
        public float SkillXpMultiplier { get; set; } = 1f;

        public static Settings Load()
        {
            Settings settings = POptions.ReadSettings<Settings>() ?? new Settings();
            if (settings.MaxStatLevel < 20) { settings.MaxStatLevel = 200; }
            if (settings.AttributeXpMultiplier <= 0f) { settings.AttributeXpMultiplier = 1f; }
            if (settings.SkillXpMultiplier <= 0f) { settings.SkillXpMultiplier = 1f; }
            return settings;
        }
    }
}
