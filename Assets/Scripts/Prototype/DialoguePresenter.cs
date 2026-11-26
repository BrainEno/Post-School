using System;
using System.Collections.Generic;
using System.Text;
using PostSchool.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace PostSchool.Prototype
{
    /// <summary>Displays a continuous, scrollable dialogue stream and choice controls.</summary>
    public sealed class DialoguePresenter : MonoBehaviour
    {
        private const int MinimumFontSize = 20;
        private const int MaximumFontSize = 34;

        private readonly List<Button> _choiceButtons = new List<Button>();
        private DialogueRunner _runner;
        private Action _onStarted;
        private Action _onEnded;
        private Image _panel;
        private Text _stream;
        private Text _status;
        private ScrollRect _scroll;
        private Button _advanceButton;
        private Text _advanceLabel;
        private int _fontSize = 25;

        public bool IsActive => _panel != null && _panel.gameObject.activeSelf;

        public void Initialize(Transform canvas, DialogueRunner runner, Action onStarted, Action onEnded)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _onStarted = onStarted ?? throw new ArgumentNullException(nameof(onStarted));
            _onEnded = onEnded ?? throw new ArgumentNullException(nameof(onEnded));

            _panel = PrototypeUiFactory.CreatePanel(
                canvas,
                "Focused Dialogue Column",
                new Vector2(0.515f, 0.035f),
                new Vector2(0.975f, 0.965f),
                new Color(0.075f, 0.078f, 0.085f, 0.97f));
            Transform root = _panel.transform;

            PrototypeUiFactory.CreateText(root, "Dialogue Title", new Vector2(0.045f, 0.91f), new Vector2(0.65f, 0.99f), "序章 · 教室", 24, Color.white);
            PrototypeUiFactory.CreateButton(root, "Smaller Text", new Vector2(0.70f, 0.92f), new Vector2(0.80f, 0.985f), "A−", () => ChangeFontSize(-2), 18);
            PrototypeUiFactory.CreateButton(root, "Larger Text", new Vector2(0.82f, 0.92f), new Vector2(0.92f, 0.985f), "A+", () => ChangeFontSize(2), 18);
            _status = PrototypeUiFactory.CreateText(root, "Attribute Check Status", new Vector2(0.045f, 0.85f), new Vector2(0.95f, 0.92f), string.Empty, 18, new Color(1f, 0.82f, 0.46f, 1f));

            _scroll = CreateScrollView(root);
            _stream = _scroll.content.GetComponent<Text>();
            _stream.fontSize = _fontSize;

            for (int i = 0; i < 4; i++)
            {
                int choiceIndex = i;
                float maxY = 0.245f - i * 0.061f;
                float minY = maxY - 0.052f;
                Button choiceButton = PrototypeUiFactory.CreateButton(
                    root,
                    "Dialogue Choice " + i,
                    new Vector2(0.045f, minY),
                    new Vector2(0.955f, maxY),
                    string.Empty,
                    () => Choose(choiceIndex),
                    18);
                _choiceButtons.Add(choiceButton);
            }

            _advanceButton = PrototypeUiFactory.CreateButton(root, "Dialogue Continue", new Vector2(0.60f, 0.055f), new Vector2(0.955f, 0.12f), "继续", AdvanceOrClose, 20);
            _advanceLabel = _advanceButton.GetComponentInChildren<Text>();
            SetVisible(false);
        }

        public void Begin()
        {
            if (IsActive) return;
            _panel.gameObject.SetActive(true);
            _runner.Begin();
            _onStarted.Invoke();
            Render();
        }

        public void SetVisible(bool visible)
        {
            if (_panel != null) _panel.gameObject.SetActive(visible);
        }

        private static ScrollRect CreateScrollView(Transform parent)
        {
            GameObject viewportObject = new GameObject("Dialogue Scroll View", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            viewportObject.transform.SetParent(parent, false);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0.045f, 0.315f);
            viewportRect.anchorMax = new Vector2(0.955f, 0.845f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.025f);
            Mask mask = viewportObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new GameObject("Dialogue Stream", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(8f, 0f);
            contentRect.offsetMax = new Vector2(-8f, 0f);
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Text stream = contentObject.GetComponent<Text>();
            stream.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            stream.fontSize = 25;
            stream.color = new Color(0.94f, 0.92f, 0.87f, 1f);
            stream.alignment = TextAnchor.UpperLeft;
            stream.horizontalOverflow = HorizontalWrapMode.Wrap;
            stream.verticalOverflow = VerticalWrapMode.Overflow;
            stream.supportRichText = true;
            stream.raycastTarget = false;

            ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.inertia = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        private void Choose(int index)
        {
            _runner.Choose(index);
            Render();
        }

        private void AdvanceOrClose()
        {
            if (_runner.IsFinished)
            {
                SetVisible(false);
                _onEnded.Invoke();
                return;
            }

            _runner.Advance();
            Render();
        }

        private void Render()
        {
            StringBuilder text = new StringBuilder();
            IReadOnlyList<DialogueHistoryEntry> history = _runner.History;
            for (int i = 0; i < history.Count; i++)
            {
                DialogueHistoryEntry entry = history[i];
                AppendEntry(text, entry);
            }

            _stream.text = text.ToString();
            _status.text = _runner.HasLastAttributeCheck
                ? string.Format(
                    "最近检定：{0} {1} / {2} · {3}",
                    PostSchool.Character.CoreAttributeNames.DisplayName(_runner.LastAttributeCheck.Attribute),
                    _runner.LastAttributeCheck.Value,
                    _runner.LastAttributeCheck.MinimumValue,
                    _runner.LastAttributeCheck.Passed ? "达到阈值" : "未达到阈值，仍可继续")
                : string.Empty;

            bool isChoice = _runner.Current.kind == DialogueKinds.Choice;
            DialogueChoice[] choices = isChoice ? _runner.Current.choices : Array.Empty<DialogueChoice>();
            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                bool visible = isChoice && i < choices.Length;
                _choiceButtons[i].gameObject.SetActive(visible);
                if (visible)
                {
                    _choiceButtons[i].GetComponentInChildren<Text>().text = choices[i].text;
                }
            }

            _advanceButton.gameObject.SetActive(!isChoice);
            if (!isChoice)
            {
                _advanceLabel.text = _runner.IsFinished ? "结束对话" : "继续阅读";
            }

            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
        }

        private void AppendEntry(StringBuilder output, DialogueHistoryEntry entry)
        {
            string safeText = Escape(entry.Text);
            if (entry.Presentation == DialoguePresentations.Speech)
            {
                string speaker = string.IsNullOrWhiteSpace(entry.Speaker) ? "说话者" : Escape(DisplaySpeaker(entry.Speaker));
                output.Append("<b>").Append(speaker).Append("</b>\n").Append(safeText).Append("\n\n");
            }
            else if (entry.Presentation == DialoguePresentations.PlayerChoice)
            {
                output.Append("<color=#D6C69E>› ").Append(safeText).Append("</color>\n\n");
            }
            else if (entry.Presentation == DialoguePresentations.Recollection)
            {
                output.Append("<color=#C9BBAA>【主观回忆】").Append(safeText).Append("</color>\n\n");
            }
            else if (entry.Presentation == DialoguePresentations.Imagination)
            {
                output.Append("<color=#D9B079>【主观想象】").Append(safeText).Append("</color>\n\n");
            }
            else if (entry.Presentation == DialoguePresentations.AttributeCheck)
            {
                output.Append("<color=#E7C976>【属性检定】").Append(safeText).Append("</color>\n\n");
            }
            else
            {
                output.Append(safeText).Append("\n\n");
            }
        }

        private static string DisplaySpeaker(string speaker)
        {
            switch (speaker)
            {
                case "q": return "Q";
                case "protagonist": return "主角";
                case "n": return "N";
                case "b": return "B";
                case "student": return "同学";
                default: return speaker;
            }
        }

        private void ChangeFontSize(int delta)
        {
            _fontSize = Mathf.Clamp(_fontSize + delta, MinimumFontSize, MaximumFontSize);
            _stream.fontSize = _fontSize;
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
