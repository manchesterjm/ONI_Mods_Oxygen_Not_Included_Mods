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

        // Our reports currently on screen; PopFX.Update wipes its reveal mask
        // open over the first 10% of the lifetime (a 1 s "typing" effect at
        // our 10 s lifetime), so these get the mask forced fully open every
        // frame until the pop recycles.
        private static readonly HashSet<PopFX> LiveReports = new HashSet<PopFX>();

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
            // instead of drawing over each other. track_target keeps the
            // report riding along with the duplicant instead of hanging in
            // the air where it spawned.
            PopFX pop = PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Plus, null, oneLine,
                duplicant.transform, new Vector3(1f, 3.5f),
                SkillStatsProgressRevivedMod.Options.PopupSeconds,
                selfAdjustPositionIfInGroup: true, track_target: true);
            if (pop == null)
            {
                return;
            }
            Style(pop, color);
            PendingStyle[pop] = color;
            LiveReports.Add(pop);
        }

        [HarmonyPatch(typeof(PopFX), "Update")]
        internal static class RevealInstantlyAndSteadyDrift
        {
            private static readonly System.Reflection.FieldInfo OffsetField =
                AccessTools.Field(typeof(PopFX), "offset");

            private static readonly System.Reflection.FieldInfo LifeElapsedField =
                AccessTools.Field(typeof(PopFX), "lifeElapsed");

            private static readonly Vector3 SpawnOffset = new Vector3(1f, 3.5f, 0f);

            // Vanilla drifts a pop up by 2*t^2 tiles - fine for its 1.5 s
            // lifetime, but at our 10 s it accelerates off screen. The offset
            // field is re-added to that drift every frame, so pre-biasing it
            // by (wanted - vanilla) nets out to a steady, configurable climb.
            public static void Prefix(PopFX __instance)
            {
                if (!LiveReports.Contains(__instance))
                {
                    return;
                }
                // Update() is about to advance lifeElapsed by unscaledDeltaTime
                // and position with the NEW value - bias against that same
                // value or the quadratic cancels one frame stale, which shows
                // as frame-time-dependent jitter.
                float t = (float)LifeElapsedField.GetValue(__instance) + Time.unscaledDeltaTime;
                float wantedClimb = SkillStatsProgressRevivedMod.Options.PopupDriftSpeed * t;
                float vanillaClimb = 2f * t * t;
                OffsetField.SetValue(__instance, SpawnOffset + Vector3.up * (wantedClimb - vanillaClimb));
            }

            public static void Postfix(PopFX __instance)
            {
                if (LiveReports.Contains(__instance) && __instance.mask != null)
                {
                    __instance.mask.fillAmount = 1f;
                }
            }
        }

        [HarmonyPatch(typeof(PopFX), nameof(PopFX.Recycle))]
        internal static class ForgetRecycledReports
        {
            public static void Postfix(PopFX __instance)
            {
                LiveReports.Remove(__instance);
                PendingStyle.Remove(__instance);
            }
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
