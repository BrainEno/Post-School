using UnityEngine;
using UnityEngine.UI;

namespace PostSchool.Ui
{
    /// <summary>
    /// Procedural marks drawn as UI geometry: paper, ruled cards, tape, pencil circles, quarter rings
    /// and a thumbprint. Keeping them as meshes (instead of imported textures) means the sheet adds
    /// no external art and every mark stays crisp at any resolution. See docs/CHARACTER_SHEET_UI.md.
    /// </summary>
    public sealed class SheetGraphic : Graphic
    {
        public enum Kind
        {
            /// <summary>Opaque paper fill with faint fibres.</summary>
            Paper,

            /// <summary>Torn strip used to tape things down.</summary>
            Tape,

            /// <summary>Card with printed baseline rules.</summary>
            RuledCard,

            /// <summary>Hand-drawn pencil ring, used to circle the chosen value.</summary>
            PencilRing,

            /// <summary>Four quarter arcs, the attribute's "quartet" ornament.</summary>
            QuartetRing,

            /// <summary>Fingerprint mark beside the signature line.</summary>
            Thumbprint,

            /// <summary>Index tab that sticks out of the sheet edge.</summary>
            IndexTab,

            /// <summary>Single hand-drawn rule.</summary>
            Rule,
        }

        [SerializeField] private Kind _kind = Kind.Paper;
        [SerializeField] private Color _paper = Color.white;
        [SerializeField] private Color _ink = Color.black;
        [SerializeField] private Color _accent = Color.black;
        [SerializeField] private float _strokeWidth = 2f;
        [SerializeField] private float _ruleSpacing = 16f;
        [SerializeField] private int _seed = 1;

        public Kind GraphicKind
        {
            get => _kind;
            set
            {
                _kind = value;
                SetVerticesDirty();
            }
        }

        public Color PaperColor
        {
            get => _paper;
            set
            {
                _paper = value;
                SetVerticesDirty();
            }
        }

        public Color InkColor
        {
            get => _ink;
            set
            {
                _ink = value;
                SetVerticesDirty();
            }
        }

        public Color AccentColor
        {
            get => _accent;
            set
            {
                _accent = value;
                SetVerticesDirty();
            }
        }

        public float StrokeWidth
        {
            get => _strokeWidth;
            set
            {
                _strokeWidth = Mathf.Max(0.5f, value);
                SetVerticesDirty();
            }
        }

        public float RuleSpacing
        {
            get => _ruleSpacing;
            set
            {
                _ruleSpacing = Mathf.Max(4f, value);
                SetVerticesDirty();
            }
        }

        public int Seed
        {
            get => _seed;
            set
            {
                _seed = value;
                SetVerticesDirty();
            }
        }

        public static SheetGraphic Create(Transform parent, string name, Kind kind)
        {
            GameObject sheetObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(SheetGraphic));
            sheetObject.transform.SetParent(parent, false);
            RectTransform rect = sheetObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            SheetGraphic graphic = sheetObject.GetComponent<SheetGraphic>();
            graphic.GraphicKind = kind;
            graphic.raycastTarget = false;
            return graphic;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            switch (_kind)
            {
                case Kind.Paper:
                    DrawPaper(vh, rect);
                    break;
                case Kind.Tape:
                    DrawTape(vh, rect);
                    break;
                case Kind.RuledCard:
                    DrawRuledCard(vh, rect);
                    break;
                case Kind.PencilRing:
                    DrawPencilRing(vh, rect);
                    break;
                case Kind.QuartetRing:
                    DrawQuartetRing(vh, rect);
                    break;
                case Kind.Thumbprint:
                    DrawThumbprint(vh, rect);
                    break;
                case Kind.IndexTab:
                    DrawIndexTab(vh, rect);
                    break;
                case Kind.Rule:
                    DrawRule(vh, rect);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        private void DrawPaper(VertexHelper vh, Rect rect)
        {
            AddQuad(vh, rect.min, rect.max, _paper);

            // Faint fibres so the surface reads as paper rather than a flat panel.
            System.Random random = new System.Random(_seed * 977 + 13);
            Color fibre = new Color(_ink.r, _ink.g, _ink.b, 0.045f);
            for (int i = 0; i < 46; i++)
            {
                float x = Mathf.Lerp(rect.xMin, rect.xMax, (float)random.NextDouble());
                float y = Mathf.Lerp(rect.yMin, rect.yMax, (float)random.NextDouble());
                float length = Mathf.Lerp(2f, 9f, (float)random.NextDouble());
                float tilt = Mathf.Lerp(-0.6f, 0.6f, (float)random.NextDouble());
                AddLine(vh, new Vector2(x, y), new Vector2(x + length, y + length * tilt), 1f, fibre);
            }
        }

        private void DrawTape(VertexHelper vh, Rect rect)
        {
            // Slight parallelogram, so the tape looks torn and pressed down at an angle.
            float skew = rect.height * 0.12f;
            Color tape = _paper;
            tape.a *= 0.72f;
            AddQuad(
                vh,
                new Vector2(rect.xMin + skew, rect.yMin),
                new Vector2(rect.xMax + skew, rect.yMin),
                new Vector2(rect.xMax - skew, rect.yMax),
                new Vector2(rect.xMin - skew, rect.yMax),
                tape);

            Color edge = new Color(_ink.r, _ink.g, _ink.b, 0.08f);
            AddLine(vh, new Vector2(rect.xMin + skew, rect.yMin), new Vector2(rect.xMax + skew, rect.yMin), 1f, edge);
            AddLine(vh, new Vector2(rect.xMin - skew, rect.yMax), new Vector2(rect.xMax - skew, rect.yMax), 1f, edge);
        }

        private void DrawRuledCard(VertexHelper vh, Rect rect)
        {
            AddQuad(vh, rect.min, rect.max, _paper);

            Color rule = new Color(_ink.r, _ink.g, _ink.b, 0.11f);
            for (float y = rect.yMin + _ruleSpacing; y < rect.yMax; y += _ruleSpacing)
            {
                AddLine(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), 1f, rule);
            }

            Color margin = new Color(_accent.r, _accent.g, _accent.b, 0.5f);
            float marginX = rect.xMin + 34f;
            if (marginX < rect.xMax)
            {
                AddLine(vh, new Vector2(marginX, rect.yMin), new Vector2(marginX, rect.yMax), 1.5f, margin);
            }
        }

        private void DrawPencilRing(VertexHelper vh, Rect rect)
        {
            Vector2 center = rect.center;
            float radiusX = rect.width * 0.46f;
            float radiusY = rect.height * 0.44f;

            // Two passes with different jitter stand in for a pencil going round twice.
            AddJitteredEllipse(vh, center, radiusX, radiusY, _strokeWidth, _accent, 72, _seed, 1f, 0f);
            Color second = _accent;
            second.a *= 0.55f;
            AddJitteredEllipse(vh, center, radiusX * 0.965f, radiusY * 0.955f, _strokeWidth * 0.8f, second, 64, _seed + 7, 0.7f, 8f);
        }

        private void DrawQuartetRing(VertexHelper vh, Rect rect)
        {
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.46f;
            float gap = 9f;
            for (int quadrant = 0; quadrant < 4; quadrant++)
            {
                float start = quadrant * 90f + gap;
                float end = (quadrant + 1) * 90f - gap;
                AddArc(vh, center, radius, _strokeWidth, _accent, start, end, 12, 0f, 0f);
            }
        }

        private void DrawThumbprint(VertexHelper vh, Rect rect)
        {
            Vector2 center = new Vector2(rect.center.x, rect.center.y - rect.height * 0.05f);
            float radiusX = rect.width * 0.42f;
            float radiusY = rect.height * 0.40f;
            Color ink = _ink;
            for (int ring = 0; ring < 6; ring++)
            {
                float factor = 1f - ring * 0.15f;
                Color ringColor = ink;
                ringColor.a *= 0.75f - ring * 0.09f;
                AddJitteredEllipse(
                    vh,
                    center,
                    radiusX * factor,
                    radiusY * factor,
                    Mathf.Max(1.2f, _strokeWidth * 0.8f),
                    ringColor,
                    48,
                    _seed + ring * 13,
                    0.5f,
                    ring * 21f);
            }

            Color core = ink;
            core.a *= 0.36f;
            AddEllipseFill(vh, center, radiusX * 0.16f, radiusY * 0.2f, core, 20);
        }

        private void DrawIndexTab(VertexHelper vh, Rect rect)
        {
            float slant = rect.height * 0.35f;
            AddQuad(
                vh,
                new Vector2(rect.xMin + slant, rect.yMin),
                new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMin, rect.yMax),
                _paper);
        }

        private void DrawRule(VertexHelper vh, Rect rect)
        {
            float y = rect.center.y;
            float wobble = rect.width * 0.004f;
            int segments = Mathf.Max(2, Mathf.RoundToInt(rect.width / 40f));
            Vector2 previous = new Vector2(rect.xMin, y);
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float x = Mathf.Lerp(rect.xMin, rect.xMax, t);
                float noise = Mathf.Sin((t * 7f + _seed) * Mathf.PI) * wobble;
                Vector2 current = new Vector2(x, y + noise);
                AddLine(vh, previous, current, _strokeWidth, _accent);
                previous = current;
            }
        }

        private static void AddJitteredEllipse(
            VertexHelper vh,
            Vector2 center,
            float radiusX,
            float radiusY,
            float stroke,
            Color color,
            int segments,
            int seed,
            float jitter,
            float phase)
        {
            Vector2 previous = Vector2.zero;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float wobble = 1f + Mathf.Sin(angle * 3f + seed + phase) * 0.012f * jitter
                                   + Mathf.Sin(angle * 7f + seed * 0.5f) * 0.008f * jitter;
                Vector2 point = new Vector2(
                    center.x + Mathf.Cos(angle) * radiusX * wobble,
                    center.y + Mathf.Sin(angle) * radiusY * wobble);
                if (i > 0)
                {
                    AddLine(vh, previous, point, stroke, color);
                }

                previous = point;
            }
        }

        private static void AddEllipseFill(VertexHelper vh, Vector2 center, float radiusX, float radiusY, Color color, int segments)
        {
            int start = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector2 point = new Vector2(center.x + Mathf.Cos(angle) * radiusX, center.y + Mathf.Sin(angle) * radiusY);
                vh.AddVert(point, color, Vector2.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                vh.AddTriangle(start, start + 1 + i, start + 2 + i);
            }
        }

        private static void AddArc(
            VertexHelper vh,
            Vector2 center,
            float radius,
            float stroke,
            Color color,
            float startDegrees,
            float endDegrees,
            int segments,
            float jitter,
            float phase)
        {
            Vector2 previous = Vector2.zero;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float degrees = Mathf.Lerp(startDegrees, endDegrees, t);
                float radians = degrees * Mathf.Deg2Rad;
                float wobble = 1f + Mathf.Sin(radians * 5f + phase) * jitter;
                Vector2 point = new Vector2(
                    center.x + Mathf.Cos(radians) * radius * wobble,
                    center.y + Mathf.Sin(radians) * radius * wobble);
                if (i > 0)
                {
                    AddLine(vh, previous, point, stroke, color);
                }

                previous = point;
            }
        }

        private static void AddLine(VertexHelper vh, Vector2 from, Vector2 to, float width, Color color)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude < 0.000001f)
            {
                return;
            }

            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (width * 0.5f);
            AddQuad(vh, from - normal, to - normal, to + normal, from + normal, color);
        }

        private static void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
        {
            AddQuad(vh, min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y), color);
        }

        private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddVert(d, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
