using System;
using System.Collections;
using PostSchool.Character;
using PostSchool.Interaction;
using PostSchool.World;
using UnityEngine;
using UnityEngine.UI;

namespace PostSchool.Prototype
{
    /// <summary>
    /// The curtain sequence: walk up to the floor-to-ceiling drop where the front wall should be, choose
    /// whether to look, and find out what is behind it.
    ///
    /// Deliberately not dialogue-driven. Dialogue stops time; this is the one scene in the prototype
    /// where time matters — the step, the drop, the landing all play at real speed, and a menu between
    /// them would make the fall feel like reading. It uses the same interface, the same attribute rules
    /// and the same status line, but it owns its own short coroutine.
    /// </summary>
    public sealed class CurtainScene : MonoBehaviour
    {
        public const string CurtainPropId = "front_curtain";
        public const string DuskWindowPropId = "window_dusk";
        public const string CoveredWindowPropId = "window_curtain";

        private readonly struct Choice
        {
            public Choice(string label, CurtainChoice kind, string note)
            {
                Label = label;
                Kind = kind;
                Note = note;
            }

            public string Label { get; }
            public CurtainChoice Kind { get; }
            public string Note { get; }
        }

        private static readonly Choice[] Choices =
        {
            new Choice("先掀开一角，往下看。", CurtainChoice.Observe, "感知 · 也许能看清落脚点"),
            new Choice("不看，直接走出去。", CurtainChoice.StepThrough, "体魄 · 全靠身体"),
            new Choice("退回去。现在还不到出去的时候。", CurtainChoice.Leave, "不检定"),
        };

        private CharacterBuild _build;
        private HumanoidAnimator _protagonist;
        private HumanoidClip _idleClip;
        private ClassroomLayout _layout;
        private Transform _curtain;
        private Action<bool, Vector3> _onFinished;
        private Action<Vector3?> _onTeleported;

        private Canvas _canvas;
        private GameObject _panel;
        private Text _title;
        private Text _body;
        private Text _status;
        private Button[] _buttons;
        private Button _continueButton;
        private Text _continueLabel;

        private float _curtainClosedY;
        private Coroutine _sequence;
        private bool _awaitingContinue;

        public bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

        /// <summary>Where the front curtain waits when closed, so the scene can restore it.</summary>
        public float ClosedHeight => _curtainClosedY;

        /// <summary>What happened, for the scene's own bookkeeping and for tests.</summary>
        public FallOutcome? LastOutcome { get; private set; }

        /// <summary>Where the fall ended, so the owning scene can put the controller back there.</summary>
        public Vector3 LastLanding { get; private set; }

        public bool LastObserved { get; private set; }

        public void Initialize(
            Canvas canvas,
            CharacterBuild build,
            ClassroomLayout layout,
            Transform curtain,
            HumanoidAnimator protagonist,
            HumanoidClip idleClip,
            Action<Vector3?> onTeleported,
            Action<bool, Vector3> onFinished)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _build = build ?? throw new ArgumentNullException(nameof(build));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _curtain = curtain;
            _protagonist = protagonist;
            _idleClip = idleClip;
            _onTeleported = onTeleported ?? throw new ArgumentNullException(nameof(onTeleported));
            _onFinished = onFinished ?? throw new ArgumentNullException(nameof(onFinished));

            _curtainClosedY = _curtain != null ? _curtain.localPosition.y : 0f;
            BuildInterface(canvas.transform);
            SetPanelVisible(false);
        }

        /// <summary>Opens the choice panel. The scene calls this when the curtain is inspected.</summary>
        public void Begin()
        {
            if (IsOpen) return;

            LastOutcome = null;
            LastObserved = false;
            SetPanelVisible(true);
            ShowChoices();
        }

        // ------------------------------------------------------------- interface

        private void BuildInterface(Transform canvas)
        {
            Image background = PrototypeUiFactory.CreatePanel(
                canvas,
                "Curtain Scene Panel",
                new Vector2(0.03f, 0.16f),
                new Vector2(0.60f, 0.86f),
                new Color(0.055f, 0.05f, 0.06f, 0.96f));
            _panel = background.gameObject;

            _title = PrototypeUiFactory.CreateText(_panel.transform, "Curtain Title",
                new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f), "幕布", 26, new Color(0.95f, 0.9f, 0.82f, 1f));

            _body = PrototypeUiFactory.CreateText(_panel.transform, "Curtain Body",
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.84f), string.Empty, 19, new Color(0.9f, 0.88f, 0.84f, 1f));
            _body.alignment = TextAnchor.UpperLeft;

            _status = PrototypeUiFactory.CreateText(_panel.transform, "Curtain Status",
                new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.51f), string.Empty, 17, new Color(0.98f, 0.8f, 0.45f, 1f));

            _buttons = new Button[Choices.Length];
            for (int i = 0; i < Choices.Length; i++)
            {
                int captured = i;
                float maxY = 0.40f - i * 0.09f;
                float minY = maxY - 0.075f;
                Button button = PrototypeUiFactory.CreateButton(_panel.transform, "Curtain Choice " + i,
                    new Vector2(0.05f, minY), new Vector2(0.95f, maxY), Choices[i].Label, () => Choose(captured), 18);
                _buttons[i] = button;
            }

            _continueButton = PrototypeUiFactory.CreateButton(_panel.transform, "Curtain Continue",
                new Vector2(0.60f, 0.04f), new Vector2(0.95f, 0.12f), "继续", Continue, 18);
            _continueLabel = _continueButton.GetComponentInChildren<Text>();
        }

        private void SetPanelVisible(bool visible)
        {
            if (_panel != null) _panel.SetActive(visible);
        }

        // ---------------------------------------------------------------- choices

        private void ShowChoices()
        {
            _awaitingContinue = false;
            _sequence = null;
            _title.text = "幕布";
            _body.text =
                "你站在原本应该是前墙的地方。没有墙，没有窗，也没有地板——只有一块从天花板垂到地面的幕布，" +
                "布缝里透进黄昏的橙色。\n\n" +
                "左侧那扇没挂东西的窗户外，是别人的晾衣绳。右边那扇窗被同样的布遮着，和这块一样，一直垂到地上。\n\n" +
                "你要怎么走？";

            _status.text = $"体魄 {Value(CoreAttribute.Physique)} · 感知 {Value(CoreAttribute.Perception)}   " +
                           $"（看：感知 ≥ {CurtainFallRules.ObserveTarget}；不看：体魄 ≥ {CurtainFallRules.BlindFallTarget}）";

            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].gameObject.SetActive(true);
                Text label = _buttons[i].GetComponentInChildren<Text>();
                label.text = $"{Choices[i].Label}\n<size=14><color=#A8A093>{Choices[i].Note}</color></size>";
            }

            _continueButton.gameObject.SetActive(false);
        }

        private int Value(CoreAttribute attribute)
        {
            return _build.GetValue(attribute);
        }

        private void Choose(int index)
        {
            if (index < 0 || index >= Choices.Length || _sequence != null) return;
            for (int i = 0; i < _buttons.Length; i++) _buttons[i].gameObject.SetActive(false);
            _sequence = StartCoroutine(RunSequence(Choices[index].Kind));
        }

        private void Continue()
        {
            // The running sequence owns the panel until it is done; Continue only unblocks the wait.
            _awaitingContinue = false;
        }

        // -------------------------------------------------------------- sequence

        private IEnumerator RunSequence(CurtainChoice choice)
        {
            if (_curtain != null) StartCoroutine(LiftCurtain(0.9f));

            if (choice == CurtainChoice.Leave)
            {
                _body.text = "你把幕布重新拉严。布后面传来街道的声音，和教室里吊灯的电流声叠在一起。\n\n" +
                             "……还不能从这里出去。";
                _status.text = string.Empty;
                if (_curtain != null) StartCoroutine(RestoreCurtainAfter(0.9f));
                EndSequence(fell: false);
                yield break;
            }

            // Both remaining paths open the curtain: the choice is whether to look first.
            if (_curtain != null) yield return new WaitForSecondsRealtime(0.55f);

            LastObserved = choice == CurtainChoice.Observe;
            bool observed = LastObserved;

            _title.text = "一切都在下面";
            if (observed)
            {
                AttributeCheckResult observation = CurtainFallRules.Observe(_build);
                _body.text =
                    "你掀开布的一角，往外看。\n\n" +
                    "没有走廊，没有楼梯，没有任何一间教室该有的东西。六米下面是柏油路，路上有一排蓝色的集装箱，" +
                    "最近的那只就在门口正下方。天是黄昏的橙色，街对面晾着别人的衣服，一件衬衫在风里翻过来又翻过去。\n\n" +
                    "你看了多久？久到腿开始发软。";
                _status.text = CurtainFallRules.Describe(observation) +
                               (observation.Passed ? " · 你看清了落点" : " · 你没看清，只能赌");

                yield return new WaitForSecondsRealtime(1.6f);

                // Looking costs: the longer you look at a six-metre drop, the less your legs believe you.
                int faintChance = CurtainFallRules.FaintChanceFor(Value(CoreAttribute.Perception));
                bool fainted = CurtainFallRules.Faints(Value(CoreAttribute.Perception),
                    (min, max) => UnityEngine.Random.Range(min, max));

                _status.text += fainted
                    ? $"\n\n腿一软。（倒下的概率 {faintChance}% —— 这次的骰子落在里面）"
                    : $"\n\n你撑住了。（倒下的概率 {faintChance}% —— 这次没有）";

                yield return new WaitForSecondsRealtime(1.1f);

                AttributeCheckResult fall = CurtainFallRules.ResolveFall(_build, observation.Passed);
                yield return RunFall(fall, observed, fainted);
            }
            else
            {
                AttributeCheckResult fall = CurtainFallRules.ResolveFall(_build, observed: false);
                _body.text = "你没有看。你只是走了出去。\n\n" +
                             "脚落下去的那一瞬间才知道没有地板——风是从下面上来的，幕布在身后合上了。";
                _status.text = CurtainFallRules.Describe(fall) + " · 只能靠身体";
                yield return new WaitForSecondsRealtime(1.5f);
                yield return RunFall(fall, observed: false, fainted: false);
            }
        }

        /// <summary>
        /// Plays the drop itself. The clip travels the fall; this only places the character at the right
        /// height, tells the scene to stop driving it, and reports where it ended up.
        /// </summary>
        private IEnumerator RunFall(AttributeCheckResult fall, bool observed, bool fainted)
        {
            FallOutcome outcome = CurtainFallRules.ResolveFallOutcome(fall, fainted);
            LastOutcome = outcome;

            float drop = CurtainFallRules.DropMetres(_layout);
            Vector3 landing = CurtainFallRules.LandingPosition(_layout);
            LastLanding = landing;
            HumanoidClip clip = outcome == FallOutcome.LandedOnContainer
                ? HumanoidClips.FallFromTheCurtain(drop)
                : HumanoidClips.CrashLanding(drop);

            // The body: pose only. The scene parent owns the transform (and the character controller),
            // so this reports the landing point rather than teleporting — one owner per transform.
            if (_protagonist != null)
            {
                _protagonist.CaptureBaseHeight();
                _protagonist.Play(clip);
            }

            // Order matters: the move happens first, then the character is held, so the fall does not
            // visibly start from the doorway after the pose has already committed.
            _onTeleported?.Invoke(landing);
            yield return null;

            // The fall itself: root position from the doorway down to the lid. The pose supplies the
            // body; this supplies the six metres. Gravity, not an ease — an ease makes it a lift.
            Vector3 from = _protagonist != null ? _protagonist.BasePosition : landing + Vector3.up * drop;
            float flight = Mathf.Max(0.35f, clip.Duration * 0.42f);
            float elapsed = 0f;

            while (elapsed < flight)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / flight);
                if (_protagonist != null)
                {
                    _protagonist.PlaceAt(Vector3.Lerp(from, landing, t * t));
                }

                yield return null;
            }

            if (_protagonist != null) _protagonist.PlaceAt(landing);

            // The clip's own landing key is later than the body arriving; let it play out.
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, clip.Duration - flight));

            switch (outcome)
            {
                case FallOutcome.LandedOnContainer:
                    _title.text = "蓝色的盖子";
                    _body.text =
                        "膝盖先落地，然后手掌拍在钢板上，声音大得像有人在楼下敲了一下门。\n\n" +
                        "集装箱的顶盖是热的。你蹲在那里，抬头看那面本来应该是墙的地方——幕布已经重新垂下来了，" +
                        "从下面看上去，整栋楼只有那一块布在动。";
                    _status.text = "体魄检定达到阈值 · 你站住了";
                    break;
                case FallOutcome.CrashedOnContainer:
                    _title.text = "你没站住";
                    _body.text =
                        "落地的那一下不对。肩膀先撞上去，然后是脸。\n\n" +
                        "你趴在盖子上，一条腿还挂在外面。晾着的衣服在你头顶上翻动，橙色的天压得很低。" +
                        "动一下都疼，但你还醒着。";
                    _status.text = "体魄检定未达到阈值 · 跌落在集装箱上";
                    break;
                default:
                    _title.text = "下面什么都没有";
                    _body.text = "集装箱不在那里。";
                    _status.text = string.Empty;
                    break;
            }

            EndSequence(fell: true);
        }

        private void EndSequence(bool fell)
        {
            for (int i = 0; i < _buttons.Length; i++) _buttons[i].gameObject.SetActive(false);
            _continueButton.gameObject.SetActive(true);
            _continueLabel.text = "继续";
            _awaitingContinue = true;

            // The scene resumes the protagonist's idle only when the card is dismissed, so the last pose
            // of the fall is what the player is looking at while they read what happened.
            StartCoroutine(WaitForContinue(fell));
        }

        private IEnumerator WaitForContinue(bool fell)
        {
            while (_awaitingContinue) yield return null;

            SetPanelVisible(false);
            _sequence = null;

            // The owning scene puts the controller back on the lid and re-bases the figure; this panel
            // must not also play an idle, or the two would fight over the same clip slot.
            _onFinished?.Invoke(fell, LastLanding);
        }

        /// <summary>Raises the curtain off the floor. Sliding it up is the cheapest way to say "no wall".</summary>
        private IEnumerator LiftCurtain(float seconds)
        {
            float from = _curtain.localPosition.y;
            float to = from + _layout.frontWall.curtainTop - 0.35f;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
                Vector3 position = _curtain.localPosition;
                _curtain.localPosition = new Vector3(position.x, Mathf.Lerp(from, to, t), position.z);
                yield return null;
            }
        }

        /// <summary>Puts the curtain back where the builder left it, for a reload or a restart.</summary>
        public void RestoreCurtain()
        {
            if (_curtain == null) return;
            Vector3 position = _curtain.localPosition;
            _curtain.localPosition = new Vector3(position.x, _curtainClosedY, position.z);
        }

        private IEnumerator RestoreCurtainAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            RestoreCurtain();
        }

        private void OnDisable()
        {
            if (_sequence != null)
            {
                StopCoroutine(_sequence);
                _sequence = null;
            }
        }
    }
}
