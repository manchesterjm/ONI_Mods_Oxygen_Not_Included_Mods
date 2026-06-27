using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace BuildableNaturalTile
{
    // Mod settings, loaded from config.json next to the DLL. Three knobs the
    // original CoolAzura mod exposed: the resource mass to BUILD the tile, the
    // mass of the natural block it leaves behind, and the build speed. Missing or
    // zeroed values fall back to the documented defaults (50 / 50 / 3).
    public class Settings
    {
        private static readonly string ConfigPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");

        public float BuildMass { get; set; } = 50f;
        public float BlockMass { get; set; } = 50f;
        public float BuildSpeed { get; set; } = 3f;

        public static Settings Load()
        {
            Settings settings = new Settings();
            if (File.Exists(ConfigPath))
            {
                Settings loaded = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(ConfigPath));
                if (loaded != null)
                {
                    settings = loaded;
                }
            }
            bool rewrite = false;
            if (settings.BuildMass == 0f) { settings.BuildMass = 50f; rewrite = true; }
            if (settings.BlockMass == 0f) { settings.BlockMass = 50f; rewrite = true; }
            if (settings.BuildSpeed == 0f) { settings.BuildSpeed = 3f; rewrite = true; }
            if (rewrite || !File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            return settings;
        }
    }
}
