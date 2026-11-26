using System;
using UnityEngine;
using UnityEngine.UI;

namespace PostSchool.Ui
{
    /// <summary>
    /// Construction helpers for the character sheet. Deliberately separate from
    /// <see cref="PostSchool.Prototype.PrototypeUiFactory"/>: that factory builds the default dark
    /// prototype chrome, while this one builds printed paper, pencil marks, and editorial type.
    /// </summary>
    internal static class SheetWidgets
    {
        public static Text CreateText(
            Transform parent,
            string name,
            Rect rect,
            string value,
            int fontSize,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft,
            bool serif = false,
            FontStyle style = FontStyle.Normal)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            ApplyRect(textObject.GetComponent<RectTransform>(), rect);

            Text text = textObject.GetComponent<Text>();
            text.font = serif ? UiTypeface.Serif : UiTypeface.Sans;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        /// <summary>
        /// Small-caps label with letter-spacing. Chinese has no case, so tracking is what makes the
        /// label read as printed instead of as a sentence.
        /// </summary>
        public static Text CreateLabel(
            Transform parent,
            string name,
            Rect rect,
            string value,
            int fontSize,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            return CreateText(parent, name, rect, SpaceOut(value), fontSize, color, alignment, serif: false, style: FontStyle.Normal);
        }

        public static SheetGraphic CreateSheet(Transform parent, string name, SheetGraphic.Kind kind, Rect rect)
        {
            SheetGraphic graphic = SheetGraphic.Create(parent, name, kind);
            ApplyRect(graphic.rectTransform, rect);
            return graphic;
        }

        public static Image CreateHitArea(Transform parent, string name, Rect rect, Action onClick)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            ApplyRect(buttonObject.GetComponent<RectTransform>(), rect);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            return image;
        }

        /// <summary>Printed button: paper face, ink rule, small-caps label, pencil hover.</summary>
        public static Button CreateButton(
            Transform parent,
            string name,
            Rect rect,
            string label,
            Action onClick,
            int fontSize,
            Color accent,
            bool filled = false)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            ApplyRect(buttonObject.GetComponent<RectTransform>(), rect);

            Image background = buttonObject.GetComponent<Image>();
            background.color = filled ? accent : CharacterSheetPalette.PaperShade;
            background.raycastTarget = true;

            Color ink = filled ? CharacterSheetPalette.Paper : CharacterSheetPalette.Ink;
            Text text = CreateText(
                buttonObject.transform,
                "Label",
                new Rect(0f, 0f, 1f, 1f),
                SpaceOut(label),
                fontSize,
                ink,
                TextAnchor.MiddleCenter);
            text.raycastTarget = false;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.95f, 0.80f, 1f);
            colors.pressedColor = new Color(0.88f, 0.80f, 0.62f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.32f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            return button;
        }

        public static Image CreateOverlay(Transform parent, string name, Color color, Rect rect)
        {
            GameObject overlayObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            overlayObject.transform.SetParent(parent, false);
            ApplyRect(overlayObject.GetComponent<RectTransform>(), rect);
            Image image = overlayObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Builds the tracked string used by labels and printed buttons.</summary>
        public static string SpaceOut(string value)
        {
            return CharacterSheetRules.SpaceOut(value);
        }

        public static void ApplyRect(RectTransform rect, Rect anchorRect)
        {
            rect.anchorMin = new Vector2(anchorRect.xMin, anchorRect.yMin);
            rect.anchorMax = new Vector2(anchorRect.xMax, anchorRect.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>Rotates a rect in place, used for the taped-on collage strips.</summary>
        public static void Tilt(RectTransform rect, float degrees)
        {
            rect.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }
    }
}
