using System;
using System.Collections.Generic;
using System.Text;
using PostSchool.Character;

namespace PostSchool.Ui
{
    /// <summary>
    /// Presentation rules for the character sheet that are worth testing without a scene: the value
    /// ladder, the threshold preview, and how a point is described before it is spent. No point
    /// budget logic lives here; CharacterBuild remains the only owner of the seven free points.
    /// </summary>
    public static class CharacterSheetRules
    {
        /// <summary>
        /// Ladder slots always span the full provisional scale so the row geometry never jumps
        /// while the player allocates. Unfilled slots are the points still available.
        /// </summary>
        public static int SlotCount => CharacterBuild.AttributeCap - CharacterBuild.BaseValue + 1;

        /// <summary>
        /// Slots stand for attribute values 1..5, so a slot is paid for while its value is at or below
        /// the current value. A fresh character therefore shows exactly one filled rung.
        /// </summary>
        public static bool IsFilled(int slotIndex, int attributeValue)
        {
            return slotIndex < attributeValue;
        }

        /// <summary>
        /// True when the next free point would land on this slot. Only the first empty slot of a row
        /// can be previewed; later empty slots stay unreachable until the earlier ones are paid for.
        /// </summary>
        public static bool IsPreviewSlot(int slotIndex, int attributeValue, int remainingPoints)
        {
            return remainingPoints > 0 && slotIndex == attributeValue;
        }

        /// <summary>True when the row is at the provisional cap and can take no further points.</summary>
        public static bool IsAtCap(int attributeValue)
        {
            return attributeValue >= CharacterBuild.AttributeCap;
        }

        /// <summary>
        /// Describes one press of the allocate button. Returns false when the build would refuse the
        /// point, so the view can disable the control instead of guessing.
        /// </summary>
        public static bool CanAllocate(int attributeValue, int remainingPoints)
        {
            return remainingPoints > 0 && attributeValue < CharacterBuild.AttributeCap;
        }

        /// <summary>
        /// Word for a value: "基础" at the floor every character starts from, then 低/中/高/极高.
        /// Values of 1 or below are named rather than scored, and anything past the cap is flagged.
        /// </summary>
        public static string DescribeLevel(int attributeValue)
        {
            if (attributeValue <= CharacterBuild.BaseValue)
            {
                return "基础";
            }

            switch (attributeValue)
            {
                case 2: return "低";
                case 3: return "中";
                case 4: return "高";
                case 5: return "极高";
                default: return "超出刻度";
            }
        }

        /// <summary>
        /// Threshold preview: "<c>体魄 2 / 3 · 未达到</c>". Checks are deterministic threshold
        /// comparisons (AttributeCheckResolver), so the sheet states them plainly rather than
        /// implying a dice roll.
        /// </summary>
        public static string DescribeThreshold(CoreAttribute attribute, AttributeCheckResult check)
        {
            string name = CoreAttributeNames.DisplayName(attribute);
            string verdict = check.Passed ? "达到" : "未达到";
            return $"{name} {check.Value} / {check.MinimumValue} · {verdict}";
        }

        /// <summary>True when any of the seven free points are still unspent.</summary>
        public static bool HasUnspentPoints(CharacterBuild build)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            return build.RemainingPoints > 0;
        }

        /// <summary>
        /// Footer status line. Kept as one sentence so the sheet reads as a note rather than a HUD.
        /// </summary>
        public static string DescribeBudget(CharacterBuild build)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));

            int remaining = build.RemainingPoints;
            if (remaining > 0)
            {
                return $"还有 {remaining} 点未被写下。点选任意一项，把它分配出去。";
            }

            return remaining == 0
                ? "七点已全部分配。可以开始。"
                : $"点数超出预算 {Math.Abs(remaining)} 点；请撤销多余的点。";
        }

        /// <summary>
        /// Readability measurement used by the sheet: how many full-width character cells a line
        /// occupies. Chinese punctuation counts as a full cell, ASCII as roughly half.
        /// </summary>
        public static int DisplayWidth(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            float width = 0f;
            foreach (char character in text)
            {
                width += IsNarrow(character) ? 0.55f : 1f;
            }

            return (int)Math.Ceiling(width);
        }

        /// <summary>Wraps text at a full-width column budget so the description card stays readable.</summary>
        public static string WrapForDisplay(string text, int columnBudget)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            if (columnBudget <= 0) throw new ArgumentOutOfRangeException(nameof(columnBudget), columnBudget, "Column budget must be positive.");

            List<string> lines = new List<string>();
            StringBuilder current = new StringBuilder();
            float width = 0f;
            foreach (char character in text)
            {
                if (character == '\n')
                {
                    lines.Add(current.ToString());
                    current.Clear();
                    width = 0f;
                    continue;
                }

                float cell = IsNarrow(character) ? 0.55f : 1f;
                if (width + cell > columnBudget)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                    width = 0f;
                }

                current.Append(character);
                width += cell;
            }

            lines.Add(current.ToString());
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Adds a hair of tracking between adjacent Chinese characters so printed labels read as
        /// labels. ASCII runs are left alone; the character has no case, so spacing is the only
        /// small-caps cue available.
        /// </summary>
        public static string SpaceOut(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length * 2);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                builder.Append(character);
                bool isCjk = character >= 0x2E80 && character <= 0x9FFF;
                bool joinsNext = i + 1 < value.Length && value[i + 1] >= 0x2E80;
                if (isCjk && joinsNext)
                {
                    builder.Append('\u2009');
                }
            }

            return builder.ToString();
        }

        private static bool IsNarrow(char character)
        {
            return character < 0x2E80;
        }
    }
}
