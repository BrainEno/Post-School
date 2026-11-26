using System;
using PostSchool.Character;
using UnityEngine;
using UnityEngine.UI;

namespace PostSchool.Prototype
{
    /// <summary>Small allocation UI; all point rules remain in CharacterBuild.</summary>
    public sealed class CharacterCreationView : MonoBehaviour
    {
        private static readonly CoreAttribute[] DisplayOrder =
        {
            CoreAttribute.Physique,
            CoreAttribute.Psyche,
            CoreAttribute.Intellect,
            CoreAttribute.Perception,
            CoreAttribute.Imagination,
        };

        private readonly Text[] _valueLabels = new Text[CharacterBuild.AttributeCount];
        private readonly Button[] _decreaseButtons = new Button[CharacterBuild.AttributeCount];
        private readonly Button[] _increaseButtons = new Button[CharacterBuild.AttributeCount];

        private CharacterBuild _build;
        private Action _onComplete;
        private Text _totalLabel;
        private Text _remainingLabel;
        private Text _feedbackLabel;
        private Button _confirmButton;
        private Image _root;

        public void Initialize(Transform canvas, CharacterBuild build, Action onComplete)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            _build = build ?? throw new ArgumentNullException(nameof(build));
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));

            _root = PrototypeUiFactory.CreatePanel(
                canvas,
                "Character Creation Panel",
                new Vector2(0.035f, 0.09f),
                new Vector2(0.49f, 0.92f),
                new Color(0.10f, 0.105f, 0.12f, 0.94f));

            Transform root = _root.transform;
            PrototypeUiFactory.CreateText(root, "Title", new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.98f), "角色倾向", 38, Color.white, TextAnchor.MiddleLeft);
            PrototypeUiFactory.CreateText(root, "Instructions", new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.84f), "每项基础值 1 · 暂定上限 5 · 自由分配 7 点\nF5 保存 · F9 读取", 20, new Color(0.84f, 0.85f, 0.86f, 1f));

            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                int index = i;
                float top = 0.70f - i * 0.105f;
                float bottom = top - 0.085f;
                PrototypeUiFactory.CreateText(root, "Attribute " + DisplayOrder[i], new Vector2(0.07f, bottom), new Vector2(0.39f, top), CoreAttributeNames.DisplayName(DisplayOrder[i]), 25, Color.white);
                _valueLabels[i] = PrototypeUiFactory.CreateText(root, "Value " + DisplayOrder[i], new Vector2(0.42f, bottom), new Vector2(0.56f, top), string.Empty, 26, Color.white, TextAnchor.MiddleCenter);
                _decreaseButtons[i] = PrototypeUiFactory.CreateButton(root, "Decrease " + DisplayOrder[i], new Vector2(0.62f, bottom + 0.005f), new Vector2(0.75f, top - 0.005f), "−", () => Adjust(index, -1), 24);
                _increaseButtons[i] = PrototypeUiFactory.CreateButton(root, "Increase " + DisplayOrder[i], new Vector2(0.78f, bottom + 0.005f), new Vector2(0.91f, top - 0.005f), "+", () => Adjust(index, 1), 24);
            }

            _totalLabel = PrototypeUiFactory.CreateText(root, "Total", new Vector2(0.07f, 0.13f), new Vector2(0.38f, 0.21f), string.Empty, 20, new Color(0.89f, 0.88f, 0.83f, 1f));
            _remainingLabel = PrototypeUiFactory.CreateText(root, "Remaining Points", new Vector2(0.07f, 0.06f), new Vector2(0.55f, 0.14f), string.Empty, 22, new Color(1f, 0.82f, 0.46f, 1f));
            PrototypeUiFactory.CreateButton(root, "Reset Build", new Vector2(0.57f, 0.055f), new Vector2(0.75f, 0.14f), "重置", Reset, 20);
            _confirmButton = PrototypeUiFactory.CreateButton(root, "Confirm Build", new Vector2(0.77f, 0.055f), new Vector2(0.94f, 0.14f), "开始", Confirm, 20);
            _feedbackLabel = PrototypeUiFactory.CreateText(root, "Save Load Feedback", new Vector2(0.06f, 0.005f), new Vector2(0.94f, 0.055f), string.Empty, 15, new Color(1f, 0.82f, 0.46f, 1f));
            Refresh();
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.gameObject.SetActive(visible);
        }

        public void RefreshFromModel()
        {
            if (_build != null && _root != null) Refresh();
        }

        public void SetFeedback(string message)
        {
            if (_feedbackLabel != null) _feedbackLabel.text = message ?? string.Empty;
        }

        private void Adjust(int index, int delta)
        {
            if (_build.TryAdjust(DisplayOrder[index], delta)) Refresh();
        }

        private void Reset()
        {
            _build.Reset();
            Refresh();
        }

        private void Confirm()
        {
            if (!_build.IsComplete)
            {
                Debug.LogWarning("Character build cannot start: allocate all seven free points first.", this);
                return;
            }

            if (!_build.IsValid(out string validationError))
            {
                Debug.LogWarning("Character build cannot start: " + validationError, this);
                return;
            }

            SetVisible(false);
            _onComplete.Invoke();
        }

        private void Refresh()
        {
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                int value = _build.GetValue(DisplayOrder[i]);
                _valueLabels[i].text = value.ToString();
                _decreaseButtons[i].interactable = value > CharacterBuild.BaseValue;
                _increaseButtons[i].interactable = value < CharacterBuild.AttributeCap && _build.RemainingPoints > 0;
            }

            _totalLabel.text = $"当前总值：{_build.Total} / {CharacterBuild.StartingTotal}";
            _remainingLabel.text = $"剩余点数：{_build.RemainingPoints}";
            _confirmButton.interactable = _build.IsComplete && _build.IsValid(out _);
        }
    }
}
