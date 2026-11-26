using System;
using PostSchool.Character;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PostSchool.Ui
{
    /// <summary>
    /// The five-attribute allocation panel, presented as a pasted paper dossier: a printed sheet with
    /// five stat rows on the left (name, value, level word, a pencil rung ladder, one allocate mark)
    /// and a reading column on the right (quota ticket, then an editorial card for the selected
    /// attribute).
    ///
    /// Point rules stay in <see cref="CharacterBuild"/>; this class only presents them and asks the
    /// model for permission before every change. The layout borrows Disco Elysium's character-sheet
    /// grammar (stat rows, a right-hand reading column, circled values), not its assets or type.
    /// See docs/CHARACTER_SHEET_UI.md.
    /// </summary>
    public sealed class CharacterSheetView : MonoBehaviour
    {
        private static readonly CoreAttribute[] DisplayOrder =
        {
            CoreAttribute.Physique,
            CoreAttribute.Psyche,
            CoreAttribute.Intellect,
            CoreAttribute.Perception,
            CoreAttribute.Imagination,
        };

        // Left column: the five stat rows live inside this horizontal band of the sheet.
        private const float RowBandLeft = 0.050f;
        private const float RowBandRight = 0.600f;

        private sealed class RowView
        {
            public Image Background;
            public Image AccentBar;
            public Text Index;
            public Text Name;
            public Text Value;
            public RectTransform ValueRect;
            public Text Level;
            public SheetGraphic[] Slots;
            public Text[] RungLabels;
            public Button Increase;
            public float Punch;
        }

        private readonly RowView[] _rows = new RowView[CharacterBuild.AttributeCount];
        private CharacterBuild _build;
        private CharacterSheetText _text;
        private Action _onComplete;
        private Image _overlay;
        private Text _cardTitle;
        private Text _cardVoice;
        private Text _cardNarrative;
        private Text _cardSkills;
        private Text _cardNotes;
        private Text _budgetDetail;
        private Text _remainingNumerals;
        private Button _confirmButton;
        private Button _resetButton;
        private int _selected;
        private bool _interactive;

        public bool IsVisible => _overlay != null && _overlay.gameObject.activeSelf;

        public CoreAttribute SelectedAttribute => DisplayOrder[_selected];

        /// <summary>Builds the sheet. <paramref name="build"/> stays the single owner of the point budget.</summary>
        public void Initialize(Transform canvas, CharacterBuild build, CharacterSheetText text, Action onComplete)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            _build = build ?? throw new ArgumentNullException(nameof(build));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));

            if (!_text.Validate(out string textError))
            {
                throw new InvalidOperationException($"Character sheet text cannot be shown: {textError}");
            }

            if (!_build.IsValid(out string buildError))
            {
                throw new InvalidOperationException($"Character build is not in a showable state: {buildError}");
            }

            BuildOverlay(canvas);
            Transform sheet = BuildSheet();
            BuildHeader(sheet);
            BuildQuotaTicket(sheet);
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i] = BuildRow(sheet, i);
            }

            BuildSignatureBlock(sheet);
            BuildDescriptionCard(sheet);
            BuildFooter(sheet);
            _interactive = true;
            Refresh();
            SetVisible(true);
        }

        public void SetVisible(bool visible)
        {
            if (_overlay == null) return;
            _overlay.gameObject.SetActive(visible);
        }

        /// <summary>Locks the sheet while it applies the build so a second Enter cannot double-apply.</summary>
        public void SetInteractive(bool interactive)
        {
            _interactive = interactive;
            if (interactive)
            {
                Refresh();
                return;
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                if (_rows[i]?.Increase != null) _rows[i].Increase.interactable = false;
            }

            if (_confirmButton != null) _confirmButton.interactable = false;
            if (_resetButton != null) _resetButton.interactable = false;
        }

        private void BuildOverlay(Transform canvas)
        {
            GameObject overlayObject = new GameObject("Character Sheet Overlay", typeof(RectTransform), typeof(Image));
            overlayObject.transform.SetParent(canvas, false);
            RectTransform rect = overlayObject.GetComponent<RectTransform>();
            SheetWidgets.ApplyRect(rect, new Rect(0f, 0f, 1f, 1f));
            rect.SetAsLastSibling();

            _overlay = overlayObject.GetComponent<Image>();
            _overlay.color = CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Backdrop, 0.97f);
            _overlay.raycastTarget = true;

            // Two taped fragments behind the sheet: collage depth without any imported texture.
            Image fragment = SheetWidgets.CreateOverlay(_overlay.transform, "Backdrop Fragment A",
                CharacterSheetPalette.WithAlpha(CharacterSheetPalette.PaperShade, 0.10f),
                new Rect(0.03f, 0.62f, 0.16f, 0.10f));
            SheetWidgets.Tilt(fragment.rectTransform, 4.5f);

            Image fragmentB = SheetWidgets.CreateOverlay(_overlay.transform, "Backdrop Fragment B",
                CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Oxblood, 0.16f),
                new Rect(0.86f, 0.08f, 0.11f, 0.06f));
            SheetWidgets.Tilt(fragmentB.rectTransform, -6f);
        }

        private Transform BuildSheet()
        {
            Image underSheet = SheetWidgets.CreateOverlay(_overlay.transform, "Under Sheet",
                CharacterSheetPalette.WithAlpha(CharacterSheetPalette.PaperShade, 0.5f),
                new Rect(0.068f, 0.062f, 0.938f, 0.962f));
            SheetWidgets.Tilt(underSheet.rectTransform, 1.6f);

            SheetGraphic sheet = SheetWidgets.CreateSheet(_overlay.transform, "Character Sheet Paper",
                SheetGraphic.Kind.Paper, new Rect(0.055f, 0.045f, 0.945f, 0.955f));
            sheet.PaperColor = CharacterSheetPalette.Paper;
            sheet.InkColor = CharacterSheetPalette.Ink;
            sheet.Seed = 5;
            SheetWidgets.Tilt(sheet.rectTransform, -1.0f);

            SheetGraphic tapeTop = SheetWidgets.CreateSheet(sheet.transform, "Tape Top",
                SheetGraphic.Kind.Tape, new Rect(0.30f, 0.985f, 0.44f, 1.032f));
            tapeTop.PaperColor = CharacterSheetPalette.Tape;
            tapeTop.InkColor = CharacterSheetPalette.Ink;
            SheetWidgets.Tilt(tapeTop.rectTransform, -2.4f);

            SheetGraphic tapeRight = SheetWidgets.CreateSheet(sheet.transform, "Tape Right",
                SheetGraphic.Kind.Tape, new Rect(0.958f, 0.55f, 0.988f, 0.66f));
            tapeRight.PaperColor = CharacterSheetPalette.Tape;
            tapeRight.InkColor = CharacterSheetPalette.Ink;
            SheetWidgets.Tilt(tapeRight.rectTransform, 5f);

            // Index tabs on the right edge, one per attribute: the dossier's thumb index.
            for (int i = 0; i < CharacterBuild.AttributeCount; i++)
            {
                float top = 0.79f - i * 0.052f;
                SheetGraphic tab = SheetWidgets.CreateSheet(sheet.transform, "Index Tab " + i,
                    SheetGraphic.Kind.IndexTab, new Rect(0.985f, top - 0.044f, 1.024f, top));
                tab.PaperColor = CharacterSheetPalette.WithAlpha(CharacterSheetPalette.PaperShade, 0.9f);
                tab.InkColor = CharacterSheetPalette.Ink;
                tab.AccentColor = AccentFor(DisplayOrder[i]);
            }

            return sheet.transform;
        }

        private void BuildHeader(Transform sheet)
        {
            SheetWidgets.CreateText(sheet, "Sheet Header", new Rect(0.050f, 0.895f, 0.52f, 0.972f),
                _text.header, 42, CharacterSheetPalette.Ink, TextAnchor.MiddleLeft, serif: true);

            SheetWidgets.CreateLabel(sheet, "Sheet Subheader", new Rect(0.052f, 0.855f, 0.60f, 0.897f),
                _text.subheader, 11, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.6f));

            SheetGraphic headerRule = SheetWidgets.CreateSheet(sheet, "Header Rule", SheetGraphic.Kind.Rule,
                new Rect(0.048f, 0.843f, 0.952f, 0.847f));
            headerRule.AccentColor = CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.5f);
            headerRule.StrokeWidth = 2.4f;
            headerRule.Seed = 11;

            Text instruction = SheetWidgets.CreateText(sheet, "Sheet Instruction",
                new Rect(0.050f, 0.775f, 0.595f, 0.838f), _text.instruction, 16,
                CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.8f), TextAnchor.UpperLeft, serif: true);
            instruction.lineSpacing = 1.2f;

            SheetWidgets.CreateLabel(sheet, "Sheet Column Label", new Rect(0.615f, 0.908f, 0.945f, 0.946f),
                "读数区 · 未分配", 11, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.45f), TextAnchor.MiddleRight);
        }

        /// <summary>The quota ticket: the seven unspent points, as a pasted-on tally slip.</summary>
        private void BuildQuotaTicket(Transform sheet)
        {
            Image shadow = SheetWidgets.CreateOverlay(sheet, "Quota Ticket Shadow",
                CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.14f),
                new Rect(0.622f, 0.788f, 0.952f, 0.900f));
            SheetWidgets.Tilt(shadow.rectTransform, 2.2f);

            Image ticket = SheetWidgets.CreateOverlay(sheet, "Quota Ticket",
                CharacterSheetPalette.PaperLift, new Rect(0.618f, 0.794f, 0.948f, 0.904f));
            SheetWidgets.Tilt(ticket.rectTransform, 1.4f);

            SheetGraphic grain = SheetWidgets.CreateSheet(ticket.transform, "Quota Ticket Grain",
                SheetGraphic.Kind.Paper, new Rect(0f, 0f, 1f, 1f));
            grain.PaperColor = CharacterSheetPalette.WithAlpha(CharacterSheetPalette.PaperLift, 0f);
            grain.InkColor = CharacterSheetPalette.Ink;
            grain.Seed = 27;

            SheetGraphic tape = SheetWidgets.CreateSheet(ticket.transform, "Quota Tape",
                SheetGraphic.Kind.Tape, new Rect(0.36f, 0.76f, 0.62f, 1.24f));
            tape.PaperColor = CharacterSheetPalette.Tape;
            tape.InkColor = CharacterSheetPalette.Ink;
            SheetWidgets.Tilt(tape.rectTransform, -3.5f);

            SheetWidgets.CreateLabel(ticket.transform, "Quota Label", new Rect(0.06f, 0.70f, 0.55f, 0.94f),
                _text.budgetLabel, 11, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.7f));

            _remainingNumerals = SheetWidgets.CreateText(ticket.transform, "Quota Value",
                new Rect(0.05f, 0.10f, 0.40f, 0.70f), CharacterBuild.FreePoints.ToString(), 40,
                CharacterSheetPalette.Ink, TextAnchor.MiddleLeft, serif: true);
            _remainingNumerals.fontStyle = FontStyle.Bold;

            SheetWidgets.CreateText(ticket.transform, "Quota Scale", new Rect(0.40f, 0.16f, 0.94f, 0.50f),
                "/ " + CharacterBuild.FreePoints + "　基础 1 · 上限 " + CharacterBuild.AttributeCap, 12,
                CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.6f), TextAnchor.MiddleLeft, serif: true);
        }

        private RowView BuildRow(Transform sheet, int index)
        {
            CoreAttribute attribute = DisplayOrder[index];
            AttributeSheetText entry = _text.For(attribute);
            Color accent = AccentFor(attribute);

            float height = 0.0765f;
            float gap = 0.0075f;
            float top = 0.762f - index * (height + gap);
            Rect rowRect = new Rect(RowBandLeft, top - height, RowBandRight, top);

            RowView row = new RowView();

            row.Background = SheetWidgets.CreateOverlay(sheet, "Row Paper " + attribute,
                index % 2 == 0
                    ? CharacterSheetPalette.WithAlpha(CharacterSheetPalette.PaperShade, 0.32f)
                    : CharacterSheetPalette.WithAlpha(CharacterSheetPalette.PaperLift, 0.30f),
                rowRect);

            // Whole-row hit area for selection, added right after the paper so text stays on top.
            Image hit = SheetWidgets.CreateHitArea(sheet, "Select " + attribute, rowRect, () => Select(index));
            hit.transform.SetSiblingIndex(row.Background.transform.GetSiblingIndex() + 1);

            row.AccentBar = SheetWidgets.CreateOverlay(sheet, "Row Accent " + attribute,
                CharacterSheetPalette.WithAlpha(accent, 0.5f),
                new Rect(rowRect.xMin + 0.0035f, rowRect.yMin + 0.008f, rowRect.xMin + 0.0095f, rowRect.yMax - 0.008f));

            row.Index = SheetWidgets.CreateLabel(sheet, "Row Index " + attribute,
                new Rect(rowRect.xMin + 0.016f, rowRect.yMin, rowRect.xMin + 0.052f, rowRect.yMax),
                (index + 1).ToString("00"), 12, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.42f));

            row.Name = SheetWidgets.CreateText(sheet, "Row Name " + attribute,
                new Rect(rowRect.xMin + 0.056f, rowRect.yMin, rowRect.xMin + 0.150f, rowRect.yMax),
                CoreAttributeNames.DisplayName(attribute), 28, CharacterSheetPalette.Ink, TextAnchor.MiddleLeft, serif: true);

            row.Value = SheetWidgets.CreateText(sheet, "Row Value " + attribute,
                new Rect(rowRect.xMin + 0.152f, rowRect.yMin + 0.003f, rowRect.xMin + 0.196f, rowRect.yMax - 0.003f),
                "1", 38, CharacterSheetPalette.Ink, TextAnchor.MiddleCenter, serif: true);
            row.ValueRect = row.Value.rectTransform;

            row.Level = SheetWidgets.CreateLabel(sheet, "Row Level " + attribute,
                new Rect(rowRect.xMin + 0.198f, rowRect.yMin + 0.020f, rowRect.xMin + 0.252f, rowRect.yMax - 0.020f),
                "基础", 10, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.55f));

            row.Slots = new SheetGraphic[CharacterSheetRules.SlotCount];
            row.RungLabels = new Text[CharacterSheetRules.SlotCount];
            float slotWidth = 0.0265f;
            float step = 0.0405f;
            float slotLeft = rowRect.xMin + 0.262f;
            float slotTop = rowRect.yMax - 0.008f;
            float slotBottom = rowRect.yMin + 0.026f;
            for (int slot = 0; slot < CharacterSheetRules.SlotCount; slot++)
            {
                float left = slotLeft + slot * step;
                SheetGraphic ring = SheetWidgets.CreateSheet(sheet, "Rung " + attribute + " " + slot,
                    SheetGraphic.Kind.PencilRing, new Rect(left, slotBottom, left + slotWidth, slotTop));
                ring.StrokeWidth = 2.2f;
                ring.PaperColor = CharacterSheetPalette.Paper;
                ring.InkColor = CharacterSheetPalette.Ink;
                ring.Seed = index * 17 + slot * 5 + 1;
                row.Slots[slot] = ring;

                row.RungLabels[slot] = SheetWidgets.CreateText(sheet,
                    "Rung Mark " + attribute + " " + slot,
                    new Rect(left - 0.004f, rowRect.yMin, left + slotWidth + 0.004f, slotBottom - 0.001f),
                    slot < entry.rungs.Length ? entry.rungs[slot] : string.Empty,
                    10, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.45f), TextAnchor.MiddleCenter);
            }

            float buttonLeft = rowRect.xMin + 0.470f;
            row.Increase = SheetWidgets.CreateButton(sheet, "Allocate " + attribute,
                new Rect(buttonLeft, rowRect.yMin + 0.018f, buttonLeft + 0.040f, rowRect.yMax - 0.018f),
                "＋", () => Allocate(index), 22, accent);
            row.Increase.transform.SetAsLastSibling();

            // Row rule, drawn last so it sits under the text but above the paper.
            SheetGraphic rule = SheetWidgets.CreateSheet(sheet, "Row Rule " + attribute, SheetGraphic.Kind.Rule,
                new Rect(rowRect.xMin + 0.010f, rowRect.yMin + 0.001f, rowRect.xMax - 0.008f, rowRect.yMin + 0.005f));
            rule.AccentColor = CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.22f);
            rule.StrokeWidth = 1.2f;
            rule.Seed = 30 + index;

            return row;
        }

        private void BuildSignatureBlock(Transform sheet)
        {
            // Lower-left of the sheet stays mostly empty paper, like a form waiting to be signed.
            SheetGraphic thumbprint = SheetWidgets.CreateSheet(sheet, "Thumbprint",
                SheetGraphic.Kind.Thumbprint, new Rect(0.062f, 0.128f, 0.140f, 0.235f));
            thumbprint.InkColor = CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Oxblood, 0.85f);
            thumbprint.StrokeWidth = 1.8f;
            thumbprint.Seed = 4;
            SheetWidgets.Tilt(thumbprint.rectTransform, -4f);

            SheetGraphic signatureRule = SheetWidgets.CreateSheet(sheet, "Signature Rule", SheetGraphic.Kind.Rule,
                new Rect(0.165f, 0.135f, 0.560f, 0.140f));
            signatureRule.AccentColor = CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.55f);
            signatureRule.StrokeWidth = 1.6f;
            signatureRule.Seed = 17;

            SheetWidgets.CreateLabel(sheet, "Signature Label", new Rect(0.165f, 0.100f, 0.560f, 0.133f),
                "签名 · 本人 · 未到校", 11, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.45f));

            SheetWidgets.CreateText(sheet, "Draft Notice", new Rect(0.050f, 0.048f, 0.560f, 0.095f),
                _text.draftNotice, 11, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.45f),
                TextAnchor.UpperLeft);

            // A quartet ornament, echoing the four-part structure of each attribute's ladder.
            SheetGraphic quartet = SheetWidgets.CreateSheet(sheet, "Quartet Ornament",
                SheetGraphic.Kind.QuartetRing, new Rect(0.500f, 0.150f, 0.578f, 0.290f));
            quartet.AccentColor = CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ochre, 0.55f);
            quartet.StrokeWidth = 1.8f;
        }

        private void BuildDescriptionCard(Transform sheet)
        {
            Image shadow = SheetWidgets.CreateOverlay(sheet, "Card Shadow",
                CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.12f),
                new Rect(0.622f, 0.318f, 0.952f, 0.786f));
            SheetWidgets.Tilt(shadow.rectTransform, -1.2f);

            Image card = SheetWidgets.CreateOverlay(sheet, "Description Card", CharacterSheetPalette.PaperLift,
                new Rect(0.618f, 0.322f, 0.948f, 0.790f));
            SheetWidgets.Tilt(card.rectTransform, -0.7f);

            SheetGraphic rules = SheetWidgets.CreateSheet(card.transform, "Card Rules",
                SheetGraphic.Kind.RuledCard, new Rect(0f, 0f, 1f, 1f));
            rules.PaperColor = CharacterSheetPalette.WithAlpha(CharacterSheetPalette.PaperLift, 0f);
            rules.InkColor = CharacterSheetPalette.Ink;
            rules.AccentColor = CharacterSheetPalette.Oxblood;
            rules.RuleSpacing = 15f;
            rules.Seed = 12;

            SheetGraphic tape = SheetWidgets.CreateSheet(card.transform, "Card Tape",
                SheetGraphic.Kind.Tape, new Rect(0.05f, 0.93f, 0.19f, 1.04f));
            tape.PaperColor = CharacterSheetPalette.Tape;
            tape.InkColor = CharacterSheetPalette.Ink;
            SheetWidgets.Tilt(tape.rectTransform, 7f);

            _cardTitle = SheetWidgets.CreateText(card.transform, "Card Title", new Rect(0.090f, 0.845f, 0.55f, 0.955f),
                string.Empty, 26, CharacterSheetPalette.Ink, TextAnchor.MiddleLeft, serif: true);

            _cardVoice = SheetWidgets.CreateText(card.transform, "Card Voice", new Rect(0.090f, 0.755f, 0.95f, 0.845f),
                string.Empty, 15, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.6f),
                TextAnchor.MiddleLeft, serif: true);

            _cardNarrative = SheetWidgets.CreateText(card.transform, "Card Narrative", new Rect(0.090f, 0.40f, 0.95f, 0.74f),
                string.Empty, 17, CharacterSheetPalette.Ink, TextAnchor.UpperLeft, serif: true);
            _cardNarrative.lineSpacing = 1.3f;

            _cardSkills = SheetWidgets.CreateText(card.transform, "Card Skills", new Rect(0.090f, 0.275f, 0.95f, 0.39f),
                string.Empty, 12, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.62f), TextAnchor.UpperLeft);

            _cardNotes = SheetWidgets.CreateText(card.transform, "Card Notes", new Rect(0.090f, 0.035f, 0.95f, 0.255f),
                string.Empty, 11, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.5f), TextAnchor.UpperLeft);
            _cardNotes.lineSpacing = 1.25f;
        }

        private void BuildFooter(Transform sheet)
        {
            _budgetDetail = SheetWidgets.CreateLabel(sheet, "Budget Detail", new Rect(0.618f, 0.268f, 0.948f, 0.310f),
                string.Empty, 11, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.55f));

            _resetButton = SheetWidgets.CreateButton(sheet, "Reset Sheet", new Rect(0.618f, 0.196f, 0.772f, 0.258f),
                _text.resetLabel, ResetBuild, 13, CharacterSheetPalette.Ink);

            _confirmButton = SheetWidgets.CreateButton(sheet, "Confirm Sheet", new Rect(0.782f, 0.196f, 0.948f, 0.258f),
                _text.confirmLabel, Confirm, 16, CharacterSheetPalette.Ochre, filled: true);

            SheetWidgets.CreateLabel(sheet, "Catalogue", new Rect(0.618f, 0.148f, 0.948f, 0.188f),
                _text.catalogueLabel, 11, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.42f));

            SheetWidgets.CreateText(sheet, "Catalogue Note", new Rect(0.618f, 0.100f, 0.948f, 0.150f),
                _text.catalogueNote, 10, CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.4f),
                TextAnchor.UpperLeft);
        }

        private void Allocate(int index)
        {
            if (!_interactive) return;

            CoreAttribute attribute = DisplayOrder[index];
            if (!_build.TryAdjust(attribute, 1))
            {
                // The model refused: report which rule stopped it instead of pretending the point landed.
                string reason = CharacterSheetRules.IsAtCap(_build.GetValue(attribute))
                    ? $"{CoreAttributeNames.DisplayName(attribute)} 已达到暂定上限 {CharacterBuild.AttributeCap}。"
                    : "没有剩余点数可以分配。";
                Debug.LogWarning("Character sheet could not allocate a point: " + reason, this);
                _budgetDetail.text = SheetWidgets.SpaceOut(reason);
                return;
            }

            _rows[index].Punch = 1f;
            _selected = index;
            Refresh();
        }

        private void Select(int index)
        {
            if (index < 0 || index >= _rows.Length) return;
            _selected = index;
            Refresh();
        }

        private void ResetBuild()
        {
            if (!_interactive) return;
            _build.Reset();
            Refresh();
        }

        private void Confirm()
        {
            if (!_interactive) return;

            if (!_build.IsComplete)
            {
                Debug.LogWarning("Character sheet cannot start: allocate all seven free points first.", this);
                return;
            }

            if (!_build.IsValid(out string validationError))
            {
                Debug.LogWarning("Character sheet cannot start: " + validationError, this);
                return;
            }

            SetInteractive(false);
            _onComplete.Invoke();
            SetVisible(false);
        }

        private void Refresh()
        {
            if (_build == null) return;

            for (int i = 0; i < _rows.Length; i++)
            {
                RefreshRow(i);
            }

            RefreshBudget();
            RefreshCard();
        }

        private void RefreshRow(int index)
        {
            RowView row = _rows[index];
            CoreAttribute attribute = DisplayOrder[index];
            int value = _build.GetValue(attribute);
            int remaining = _build.RemainingPoints;
            bool selected = index == _selected;
            Color accent = AccentFor(attribute);

            row.Value.text = value.ToString();
            row.Level.text = SheetWidgets.SpaceOut(CharacterSheetRules.DescribeLevel(value));

            row.Background.color = selected
                ? CharacterSheetPalette.WithAlpha(CharacterSheetPalette.PaperLift, 0.92f)
                : index % 2 == 0
                    ? CharacterSheetPalette.WithAlpha(CharacterSheetPalette.PaperShade, 0.32f)
                    : CharacterSheetPalette.WithAlpha(CharacterSheetPalette.PaperLift, 0.30f);

            row.AccentBar.color = CharacterSheetPalette.WithAlpha(accent, selected ? 0.95f : 0.5f);
            row.Name.color = selected
                ? CharacterSheetPalette.Ink
                : CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.84f);
            row.Index.text = SheetWidgets.SpaceOut((index + 1).ToString("00"));

            for (int slot = 0; slot < row.Slots.Length; slot++)
            {
                bool filled = CharacterSheetRules.IsFilled(slot, value);
                bool preview = CharacterSheetRules.IsPreviewSlot(slot, value, remaining);
                SheetGraphic ring = row.Slots[slot];

                if (filled)
                {
                    ring.AccentColor = accent;
                    ring.StrokeWidth = 2.8f;
                }
                else if (preview)
                {
                    ring.AccentColor = CharacterSheetPalette.WithAlpha(
                        selected ? CharacterSheetPalette.Ochre : CharacterSheetPalette.Ink,
                        selected ? 0.55f : 0.22f);
                    ring.StrokeWidth = 2.0f;
                }
                else
                {
                    ring.AccentColor = CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.16f);
                    ring.StrokeWidth = 1.4f;
                }

                row.RungLabels[slot].color = filled
                    ? CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.8f)
                    : CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ink, 0.38f);
            }

            row.Increase.interactable = _interactive && CharacterSheetRules.CanAllocate(value, remaining);
            row.Increase.GetComponent<Image>().color = CharacterSheetPalette.WithAlpha(accent, 0.85f);
        }

        private void RefreshBudget()
        {
            int remaining = _build.RemainingPoints;
            _remainingNumerals.text = Mathf.Max(0, remaining).ToString();
            _remainingNumerals.color = remaining > 0
                ? CharacterSheetPalette.Ink
                : CharacterSheetPalette.WithAlpha(CharacterSheetPalette.Ochre, 0.92f);

            _budgetDetail.text = SheetWidgets.SpaceOut(
                $"{CharacterSheetRules.DescribeBudget(_build)}　总 {_build.Total} / {CharacterBuild.StartingTotal}");

            _confirmButton.interactable = _interactive && _build.IsComplete && _build.IsValid(out _);
            _resetButton.interactable = _interactive;
        }

        private void RefreshCard()
        {
            CoreAttribute attribute = DisplayOrder[_selected];
            AttributeSheetText entry = _text.For(attribute);
            Color accent = AccentFor(attribute);

            _cardTitle.text = CoreAttributeNames.DisplayName(attribute);
            _cardTitle.color = CharacterSheetPalette.WithAlpha(accent, 0.95f);
            _cardVoice.text = SheetWidgets.SpaceOut("「" + entry.voice + "」");

            _cardNarrative.text = SheetWidgets.SpaceOut(entry.narrative);

            _cardSkills.text = SheetWidgets.SpaceOut(_text.skillLabel + " · " + entry.skills);

            // Deterministic threshold preview instead of implying a dice roll.
            int value = _build.GetValue(attribute);
            string threshold = value >= CharacterBuild.AttributeCap
                ? $"{_text.thresholdLabel} · 已达暂定上限 {CharacterBuild.AttributeCap}"
                : $"{_text.thresholdLabel} · {CharacterSheetRules.DescribeThreshold(attribute, AttributeCheckResolver.Resolve(_build, attribute, value + 1))}";

            string notes = SheetWidgets.SpaceOut(threshold);
            if (!string.IsNullOrWhiteSpace(entry.lowNote) || !string.IsNullOrWhiteSpace(entry.highNote))
            {
                notes += "\n" + SheetWidgets.SpaceOut("低 · " + entry.lowNote + "　高 · " + entry.highNote);
            }

            _cardNotes.text = notes;
        }

        private static Color AccentFor(CoreAttribute attribute)
        {
            switch (attribute)
            {
                case CoreAttribute.Physique: return CharacterSheetPalette.PencilOxblood;
                case CoreAttribute.Psyche: return CharacterSheetPalette.PencilIndigo;
                case CoreAttribute.Intellect: return CharacterSheetPalette.PencilBlack;
                case CoreAttribute.Perception: return CharacterSheetPalette.PencilOlive;
                case CoreAttribute.Imagination: return CharacterSheetPalette.PencilOchre;
                default: throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Unknown core attribute.");
            }
        }

        private void Update()
        {
            if (!IsVisible) return;

            AnimateAllocation();

            if (!_interactive) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (WasPressed(keyboard.digit1Key)) Select(0);
            else if (WasPressed(keyboard.digit2Key)) Select(1);
            else if (WasPressed(keyboard.digit3Key)) Select(2);
            else if (WasPressed(keyboard.digit4Key)) Select(3);
            else if (WasPressed(keyboard.digit5Key)) Select(4);

            if (WasPressed(keyboard.tabKey)) Select((_selected + 1) % _rows.Length);
            if (WasPressed(keyboard.equalsKey) || WasPressed(keyboard.numpadPlusKey)) Allocate(_selected);
            if (WasPressed(keyboard.enterKey) || WasPressed(keyboard.numpadEnterKey)) Confirm();
            if (WasPressed(keyboard.rKey)) ResetBuild();
        }

        private static bool WasPressed(UnityEngine.InputSystem.Controls.KeyControl key)
        {
            return key != null && key.wasPressedThisFrame;
        }

        /// <summary>Quick ink-punch on the value that just received a point.</summary>
        private void AnimateAllocation()
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                RowView row = _rows[i];
                if (row.Punch <= 0f) continue;

                row.Punch = Mathf.Max(0f, row.Punch - Time.unscaledDeltaTime * 4.5f);
                float eased = Mathf.Sin(row.Punch * Mathf.PI);
                float scale = 1f + eased * 0.16f;
                row.ValueRect.localScale = new Vector3(scale, scale, 1f);
                row.Value.color = Color.Lerp(CharacterSheetPalette.Ink, CharacterSheetPalette.Ochre, eased);
            }
        }
    }
}
