using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SkillStatsProgressRevived
{
    // Styled floating text over a duplicant, built on the game's PopFX system
    // the way the old mod did it: wider text box, custom color and font size,
    // icon hidden, lifted spawn offset. The per-popup drift speed the old mod
    // tweaked is a const in current builds, so drift speed is vanilla now.
    internal static class FloatingText
    {
        // Pops in a stacking group are laid out (PopFX.Run) a frame AFTER
        // SpawnFX returns, and Run resets the icon to full opacity - so we
        // style immediately AND re-style from a Run postfix, keyed by
        // instance. Entries are removed once applied, so recycled pops reused
        // by vanilla popups are untouched.
        private static readonly Dictionary<PopFX, Color> PendingStyle = new Dictionary<PopFX, Color>();

        public static void Show(string text, GameObject duplicant, Color color)
        {
            if (!CanShowOver(duplicant) || PopFXManager.Instance == null)
            {
                return;
            }
            // One line per report: multi-line text centers itself inside the
            // pop's fixed-height text rect and clips top and bottom, so the
            // report reads far better as a single wide line.
            string oneLine = text.TrimEnd('\n').Replace(":\n", ": ").Replace("\n", "  |  ");
            // The 9-arg overload gives us the spawn offset directly plus
            // selfAdjustPositionIfInGroup, so simultaneous reports stack
            // instead of drawing over each other.
            PopFX pop = PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Plus, null, oneLine,
                duplicant.transform, new Vector3(1f, 3.5f),
                SkillStatsProgressRevivedMod.Options.PopupSeconds);
            if (pop == null)
            {
                return;
            }
            Style(pop, color);
            PendingStyle[pop] = color;
        }

        // Size BOTH rects - the pop's root box AND the text component's own
        // rect (the actual clipping element) - to the measured single-line
        // text, so any font size fits.
        private static void Style(PopFX pop, Color color)
        {
            LocText text = pop.TextDisplay;
            text.color = color;
            text.fontSize = SkillStatsProgressRevivedMod.Options.PopupFontSize;
            text.enableWordWrapping = false;
            text.overflowMode = TMPro.TextOverflowModes.Overflow;
            text.ForceMeshUpdate();
            float width = text.preferredWidth + 24f;
            float height = text.preferredHeight + 12f;
            SetSize(text.rectTransform, width, height);
            SetSize(pop.GetComponent<RectTransform>(), width, height);
            HideIcons(pop);
        }

        private static void SetSize(RectTransform rect, float width, float height)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        [HarmonyPatch(typeof(PopFX), nameof(PopFX.Run), typeof(Vector3), typeof(Vector3))]
        internal static class RestyleAfterRun
        {
            public static void Postfix(PopFX __instance)
            {
                if (PendingStyle.TryGetValue(__instance, out Color color))
                {
                    PendingStyle.Remove(__instance);
                    Style(__instance, color);
                }
            }
        }

        public static string Shrink(string s, int maxLength)
        {
            if (s.Length <= maxLength)
            {
                return s;
            }
            return s.Substring(0, maxLength);
        }

        private static bool CanShowOver(GameObject duplicant)
        {
            Options options = SkillStatsProgressRevivedMod.Options;
            if (!options.ShowWorkPopups)
            {
                return false;
            }
            if (!options.OnlySelectedDuplicant)
            {
                return true;
            }
            DetailsScreen details = DetailsScreen.Instance;
            return details != null && details.IsActive() && details.target == duplicant;
        }

        // PopFX has two icon images (MainIconDisplay carries the sprite we
        // pass in; IconDisplay is the secondary) - hide both so the report is
        // text only. The old mod predates the split and hid just IconDisplay,
        // which is why the port initially leaked a "+" per popup.
        private static void HideIcons(PopFX pop)
        {
            SetAlpha(pop.MainIconDisplay, 0f);
            SetAlpha(pop.IconDisplay, 0f);
        }

        private static void SetAlpha(UnityEngine.UI.Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    // The old mod's "RestorePopFx": vanilla popups (attribute level-ups etc.)
    // get a bigger font and a solid icon so they are actually readable.
    [HarmonyPatch(typeof(PopFXManager), nameof(PopFXManager.SpawnFX), new Type[]
    {
        typeof(Sprite), typeof(string), typeof(Transform), typeof(Vector3),
        typeof(float), typeof(bool), typeof(bool)
    })]
    internal static class RestyleVanillaPopups
    {
        public static void Postfix(PopFX __result)
        {
            if (!SkillStatsProgressRevivedMod.Options.RestyleVanillaPopups || __result == null)
            {
                return;
            }
            __result.TextDisplay.fontSize = 24f;
            Color iconColor = __result.MainIconDisplay.color;
            iconColor.a = 1f;
            __result.MainIconDisplay.color = iconColor;
        }
    }
}
