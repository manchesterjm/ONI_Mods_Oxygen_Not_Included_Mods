using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Database;
using HarmonyLib;
using Klei;
using KMod;
using UnityEngine;
using static CustomizeYourPaints.CustomizeYourPaints;

namespace CustomizeYourPaints
{
    public class CustomizeYourPaints : UserMod2
    {
        public const string CUSTOM_PAINT_ID = "CustomizeYourPaints";

        public enum CanvasSize
        {
            Normal,
            Tall,
            Wide
        }

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
        }

        public static Components.Cmps<ArtOverrideRestorer> artRestorers = new Components.Cmps<ArtOverrideRestorer>();

        public static List<string> myOverrides = new List<string>();
    }

    // Inlined from Pholib - Logging utility
    public static class Logs
    {
        private static bool initiated = false;
        private static string modName = "";

        public static void InitIfNot()
        {
            if (initiated) return;
            modName = Assembly.GetExecutingAssembly().GetName().Name;
            Debug.Log($"[{modName}] CustomizeYourPaints mod loaded");
            initiated = true;
        }

        public static void Error(string informations)
        {
            InitIfNot();
            Debug.Log($"[{modName}][ERROR] " + informations);
        }

        public static void Log(string informations)
        {
            InitIfNot();
            Debug.Log($"[{modName}] " + informations);
        }
    }

    // Inlined from Pholib - Image utilities
    public static class ImageUtil
    {
        public static Texture2D LoadPNG(string filePath)
        {
            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
            }
            else
            {
                Logs.Error($"{filePath} not found!");
            }
            return tex;
        }

        public static Texture2D ScaleTexture(this Texture2D source, int targetWidth, int targetHeight)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);

            for (int i = 0; i < result.height; ++i)
            {
                for (int j = 0; j < result.width; ++j)
                {
                    Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                    result.SetPixel(j, i, newColor);
                }
            }
            result.Apply();
            return result;
        }

        public static Texture2D MergeImage(Texture2D background, Texture2D watermark, int startPositionX, int startPositionY)
        {
            startPositionY = -startPositionY - watermark.height;
            for (int x = startPositionX; x < background.width; x++)
            {
                for (int y = startPositionY; y < background.height; y++)
                {
                    if (x - startPositionX < watermark.width && y - startPositionY < watermark.height)
                    {
                        var bgColor = background.GetPixel(x, y);
                        var wmColor = watermark.GetPixel(x - startPositionX, y - startPositionY);
                        var finalColor = Color.Lerp(bgColor, wmColor, wmColor.a / 1.0f);
                        background.SetPixel(x, y, finalColor);
                    }
                }
            }
            background.Apply();
            return background;
        }
    }

    // Inlined from Pholib - Utilities
    public static class Utilities
    {
        public static string ModPath()
        {
            return Directory.GetParent(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar.ToString();
        }
    }

    [HarmonyPatch(typeof(Manager), nameof(Manager.Load))]
    public class ModManager_Load_AnimPatch
    {
        private static readonly string ORIGINALS_PATH = Path.Combine("src", "originals");

        public static void Prefix(Content content)
        {
            if (content != Content.Animation) return;

            byte[] normal_painting_anim = File.ReadAllBytes(Path.Combine(Utilities.ModPath(), ORIGINALS_PATH, "painting_art_b_anim.bytes"));
            byte[] normal_painting_build = File.ReadAllBytes(Path.Combine(Utilities.ModPath(), ORIGINALS_PATH, "painting_art_b_build.bytes"));

            byte[] tall_painting_anim = File.ReadAllBytes(Path.Combine(Utilities.ModPath(), ORIGINALS_PATH, "painting_tall_art_a_anim.bytes"));
            byte[] tall_painting_build = File.ReadAllBytes(Path.Combine(Utilities.ModPath(), ORIGINALS_PATH, "painting_tall_art_a_build.bytes"));

            byte[] wide_painting_anim = File.ReadAllBytes(Path.Combine(Utilities.ModPath(), ORIGINALS_PATH, "painting_wide_art_a_anim.bytes"));
            byte[] wide_painting_build = File.ReadAllBytes(Path.Combine(Utilities.ModPath(), ORIGINALS_PATH, "painting_wide_art_a_build.bytes"));

            ArtableStages_Constructor_Patch.IdsToAdds = new List<Tuple<string, CanvasSize>>();
            string customPaintsPath = FileSystem.Normalize(Path.Combine(Path.Combine(Manager.GetDirectory(), "config"), "CustomizeYourPaints"));

            if (!Directory.Exists(customPaintsPath))
            {
                Directory.CreateDirectory(customPaintsPath);
                Logs.Log($"Created config folder: {customPaintsPath}");
            }

            int counter = 0;
            foreach (string filePath in Directory.EnumerateFiles(customPaintsPath))
            {
                if (!filePath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) &&
                    !filePath.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                counter++;
                string[] splitedFile = Path.GetFileNameWithoutExtension(filePath).Split('_', '-');

                if (splitedFile.Length < 2) continue;

                string prefix = splitedFile[0].ToLower();
                // Use full filename (without extension) for deterministic IDs across sessions
                string suffix = Path.GetFileNameWithoutExtension(filePath);
                CanvasSize canvasSize = CanvasSize.Normal;
                if (prefix.Contains("wide")) canvasSize = CanvasSize.Wide;
                if (prefix.Contains("tall")) canvasSize = CanvasSize.Tall;

                var anim = new KAnimFile.Mod
                {
                    textures = new List<Texture2D>()
                };

                string artToReplace = "painting_art_b_0.png";
                Vector2Int bigImageToReplace = new Vector2Int(120, 136);
                Vector2Int littleImageToReplace = new Vector2Int(75, 86);
                Vector2Int bigImagePosition = new Vector2Int(547, 555);
                Vector2Int littleImagePosition = new Vector2Int(556, 713);
                switch (canvasSize)
                {
                    case CanvasSize.Normal:
                        anim.anim = normal_painting_anim;
                        anim.build = normal_painting_build;
                        break;
                    case CanvasSize.Tall:
                        anim.anim = tall_painting_anim;
                        anim.build = tall_painting_build;
                        artToReplace = "painting_tall_art_a_0.png";
                        bigImageToReplace = new Vector2Int(130, 215);
                        littleImageToReplace = new Vector2Int(55, 95);
                        bigImagePosition = new Vector2Int(588, 11);
                        littleImagePosition = new Vector2Int(430, 296);
                        break;
                    case CanvasSize.Wide:
                        anim.anim = wide_painting_anim;
                        anim.build = wide_painting_build;
                        artToReplace = "painting_wide_art_a_0.png";
                        bigImageToReplace = new Vector2Int(213, 128);
                        littleImageToReplace = new Vector2Int(112, 68);
                        bigImagePosition = new Vector2Int(812, 1);
                        littleImagePosition = new Vector2Int(830, 154);
                        break;
                }
                Texture2D textureToReplace = ImageUtil.LoadPNG(filePath);
                Texture2D normalPainting = ImageUtil.LoadPNG(Path.Combine(Utilities.ModPath(), ORIGINALS_PATH, artToReplace));
                Texture2D normalPainting2 = ImageUtil.MergeImage(normalPainting, textureToReplace.ScaleTexture(bigImageToReplace.x, bigImageToReplace.y), bigImagePosition.x, bigImagePosition.y);
                normalPainting2 = ImageUtil.MergeImage(normalPainting2, textureToReplace.ScaleTexture(littleImageToReplace.x, littleImageToReplace.y), littleImagePosition.x, littleImagePosition.y);

                anim.textures.Add(normalPainting2);

                string kanimId = $"{CustomizeYourPaints.CUSTOM_PAINT_ID}_{suffix}_kanim";
                Logs.Log("Adding custom painting: " + kanimId);
                ModUtil.AddKAnimMod(kanimId, anim);
                ArtableStages_Constructor_Patch.IdsToAdds.Add(new Tuple<string, CanvasSize>(kanimId, canvasSize));
            }

            Logs.Log($"Loaded {counter} custom painting(s)");
        }
    }

    [HarmonyPatch(typeof(InventoryOrganization), "GenerateSubcategories")]
    public class InventoryOrganization_GenerateSubcategories_Patch
    {
        public static void Postfix()
        {
            foreach (Tuple<string, CanvasSize> tuple in ArtableStages_Constructor_Patch.IdsToAdds)
            {
                switch (tuple.second)
                {
                    case CanvasSize.Normal:
                        InventoryOrganization.subcategoryIdToPermitIdsMap[InventoryOrganization.PermitSubcategories.BUILDING_CANVAS_STANDARD].Add(tuple.first);
                        break;
                    case CanvasSize.Tall:
                        InventoryOrganization.subcategoryIdToPermitIdsMap[InventoryOrganization.PermitSubcategories.BUILDING_CANVAS_PORTRAIT].Add(tuple.first);
                        break;
                    case CanvasSize.Wide:
                        InventoryOrganization.subcategoryIdToPermitIdsMap[InventoryOrganization.PermitSubcategories.BUILDING_CANVAS_LANDSCAPE].Add(tuple.first);
                        break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ArtableStages), MethodType.Constructor, typeof(ResourceSet))]
    public class ArtableStages_Constructor_Patch
    {
        public static List<Tuple<string, CanvasSize>> IdsToAdds;

        public static void Postfix(ArtableStages __instance)
        {
            foreach (Tuple<string, CanvasSize> tuple in IdsToAdds)
            {
                Logs.Log("Registering artwork: " + tuple.first);
                // Extract display name: remove prefix (wide_, tall_, normal_) and underscores
                string rawName = tuple.first.Replace("_kanim", "").Replace($"{CustomizeYourPaints.CUSTOM_PAINT_ID}_", "");
                // Remove the canvas type prefix (wide_, tall_, normal_)
                string displayName = rawName;
                if (rawName.StartsWith("wide_")) displayName = rawName.Substring(5);
                else if (rawName.StartsWith("tall_")) displayName = rawName.Substring(5);
                else if (rawName.StartsWith("normal_")) displayName = rawName.Substring(7);
                displayName = displayName.Replace("_", " ").Replace("-", " ");

                AddCustomPaint(__instance,
                    displayName,
                    "A custom painting added using the CustomizeYourPaints mod.",
                    tuple.first, tuple.first, tuple.second);
            }
        }

        private static void AddCustomPaint(ArtableStages __instance, string name, string description, string id, string kanim, CanvasSize canvasSize)
        {
            string targetPrefabId = CanvasConfig.ID;
            string animSymbol = "art_b"; // Normal canvas uses art_b
            int decor = 15;
            switch (canvasSize)
            {
                case CanvasSize.Tall:
                    targetPrefabId = CanvasTallConfig.ID;
                    animSymbol = "art_a"; // Tall canvas uses art_a
                    break;
                case CanvasSize.Wide:
                    targetPrefabId = CanvasWideConfig.ID;
                    animSymbol = "art_a"; // Wide canvas uses art_a
                    break;
            }
            if (__instance.Exists(id))
            {
                Logs.Error($"Painting with id {id} is already loaded. Check your image names.");
                return;
            }
            CustomizeYourPaints.myOverrides.Add(id);
            __instance.Add(
                id,
                name,
                description,
                PermitRarity.Universal,
                kanim,
                animSymbol,
                decor,
                true,
                ArtableStatuses.ArtableStatusType.LookingGreat.ToString(),
                targetPrefabId,
                "",
                new string[] { },
                new string[] { });
        }
    }
}
