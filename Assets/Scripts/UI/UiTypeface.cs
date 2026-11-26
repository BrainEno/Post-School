using System;
using UnityEngine;

namespace PostSchool.Ui
{
    /// <summary>
    /// Font resolution for the printed/editorial UI vocabulary. The project still uses legacy UGUI
    /// Text and ships no font asset, so the sheet asks the operating system for a serif (headings)
    /// and a sans (body) face that covers Simplified Chinese, then falls back to Unity's built-in
    /// Arial. No font file is copied into the repository; see docs/CHARACTER_SHEET_UI.md.
    /// </summary>
    public static class UiTypeface
    {
        private const int ProbeSize = 40;

        private static readonly string[] SerifCandidates =
        {
            "Source Han Serif SC",
            "Noto Serif CJK SC",
            "Songti SC",
            "STSong",
            "SimSun",
            "Source Han Serif CN",
            "Times New Roman",
        };

        private static readonly string[] SansCandidates =
        {
            "Source Han Sans SC",
            "Noto Sans CJK SC",
            "Microsoft YaHei",
            "PingFang SC",
            "Hiragino Sans GB",
            "SimHei",
            "WenQuanYi Micro Hei",
            "Arial",
        };

        private static Font _serif;
        private static Font _sans;

        /// <summary>Heading face: a Chinese serif when the OS provides one.</summary>
        public static Font Serif
        {
            get
            {
                if (_serif == null)
                {
                    _serif = Load(SerifCandidates, "serif");
                }

                return _serif;
            }
        }

        /// <summary>Body face: a Chinese sans when the OS provides one; also used by other prototype UI.</summary>
        public static Font Sans
        {
            get
            {
                if (_sans == null)
                {
                    _sans = Load(SansCandidates, "sans");
                }

                return _sans;
            }
        }

        private static Font Load(string[] candidates, string role)
        {
            Font font = null;
            try
            {
                font = Font.CreateDynamicFontFromOSFont(candidates, ProbeSize);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"Could not resolve an OS font for the {role} role: {error.Message}");
            }

            if (font != null)
            {
                return font;
            }

            Debug.LogWarning(
                $"No OS font matched the {role} role; falling back to the built-in Arial. " +
                "Simplified Chinese glyphs may be missing on some platforms. See docs/CHARACTER_SHEET_UI.md.");
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
