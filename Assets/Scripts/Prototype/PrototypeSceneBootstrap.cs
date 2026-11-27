using System;
using System.Collections;
using System.Linq;
using System.IO;
using PostSchool.Character;
using PostSchool.Core;
using PostSchool.Dialogue;
using PostSchool.Interaction;
using PostSchool.Narrative;
using PostSchool.Persistence;
using PostSchool.Ui;
using PostSchool.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PostSchool.Prototype
{
    /// <summary>
    /// Wires the first playable slice in the prototype scene. The classroom itself is composed from
    /// Assets/Resources/World/classroom_layout.json by ClassroomBuilder; the protagonist is built by
    /// SkinnedCharacterBuilder (see Assets/Scripts/Character). Character rules and dialogue content live
    /// in their own model/data files.
    /// </summary>
    public sealed class PrototypeSceneBootstrap : MonoBehaviour
    {
        private const string DialogueResourcePath = "Dialogue/classroom_opening";
        private const string TeacherDialogueResourcePath = "Dialogue/classroom_teacher_n";
        private const string ImaginationResourcePath = "Dialogue/imagination_biology_teacher";
        private const string TeacherRevisitResourcePath = "Dialogue/classroom_teacher_n_revisit";
        private const string SheetTextResourcePath = "CharacterSheet/character_sheet_text_placeholder";
        private const string SaveFileName = "prototype-save.json";
        private const string ClassroomLayoutResourcePath = "World/classroom_layout";

        /// <summary>The campus shell around the room. Massing only; see CampusShellBuilder.</summary>
        private const string CampusShellResourcePath = "World/campus_shell";

        private WorldSurfaceLibrary _surfaces;

        /// <summary>The campus around the room, or null when its data is missing.</summary>
        private GameObject _campusRoot;
        private ClassroomLayout _layout;
        private CharacterBuild _characterBuild;
        private StoryState _storyState;
        private CharacterSheetView _characterSheet;
        private DialoguePresenter _dialoguePresenter;
        private PlayerMovement _playerMovement;
        private PlayerInteractor _playerInteractor;
        private CharacterController _playerCharacterController;
        private SkinnedFigure _protagonist;
        private HumanoidAnimator _protagonistAnimator;
        private SkinnedFigure _qFigure;
        private HumanoidAnimator _qAnimator;
        private SkinnedFigure _teacherFigure;
        private HumanoidAnimator _teacherAnimator;
        private HumanoidClip _wakeClip;
        private HumanoidClip _idleClip;
        private HumanoidClip _qIdleClip;
        private HumanoidClip _qTurnClip;
        private HumanoidClip _qAwayClip;
        private HumanoidClip _qGlanceClip;

        /// <summary>Where Q comes to a stop beside the protagonist's desk. One number, so the walk and
        /// the beat that turns him to the set square cannot drift apart.</summary>
        private static readonly Vector3 QDestination = new Vector3(-1.9f, 0f, -0.1f);

        /// <summary>The set square on the podium, which one authored line tells Q to look at.</summary>
        private static readonly Vector3 SetSquarePosition = new Vector3(2.1f, 0f, 2.9f);

        /// <summary>The yaw Q is created with, before he walks to the desk.</summary>
        private const float ClassmateQFacing = 128f;

        /// <summary>
        /// Where the protagonist stands to wake up. Q's approach ends by turning him to face this point,
        /// so by the time any dialogue beat plays his facing is this, not <see cref="ClassmateQFacing"/>.
        /// </summary>
        private static readonly Vector3 ProtagonistStandingPosition = new Vector3(0f, 1f, -0.75f);
        private HumanoidClip _teacherIdleClip;
        private Transform _qStudentRoot;
        private Transform _teacherRoot;
        private Inspectable _teacherTarget;
        private DialogueRunner _openingRunner;
        private DialogueRunner _teacherRunner;
        private DialogueRunner _imaginationRunner;
        private InsightLedger _insight;
        private SkinnedFigure _imaginedTeacherB;
        private SkinnedFigure _imaginedTeacherGhost;
        private Transform _imaginedTeacherRoot;
        private DialogueRunner _teacherRevisitRunner;
        private QuizScene _activeScene;
        private bool _openingFinished;
        private Vector3 _teacherMark;
        private float _teacherYaw;
        private Transform _curtain;
        private CurtainScene _curtainScene;
        private GameObject _classroomRoot;
        private Text _interactionPrompt;
        private Inspectable _qTarget;
        private float _temporaryPromptUntil;
        private float _previousTimeScale = 1f;
        private bool _worldPaused;
        private bool _buildComplete;
        private bool _saveLoadFeedbackActive;
        private bool _openingSequenceActive;

        private void Start()
        {
            _surfaces = new WorldSurfaceLibrary();
            BuildClassroom();
            CreatePlayerAndCamera();

            // The cast stands in the empty front band, never inside the seating grid.
            CreateClassmateQ(new Vector3(-2.9f, 0f, 1.1f), ClassmateQFacing);
            CreateTeacherN();
            CreateImaginedTeacherB();
            CreateInspectable("schoolbag", "书包（占位）", "【占位说明，待作者审定】这里可以检查书包。", PrimitiveType.Cube,
                new Vector3(-1.62f, 0.24f, 0.5f), new Vector3(0.5f, 0.42f, 0.34f), _surfaces.Surface(SurfaceRole.Ink));
            CreateInspectable("set_square", "三角尺（占位）", "【占位说明，待作者审定】这里可以检查一件课堂物品。", PrimitiveType.Cube,
                new Vector3(SetSquarePosition.x, 0.79f, SetSquarePosition.z), new Vector3(0.62f, 0.05f, 0.14f), _surfaces.Surface(SurfaceRole.Paper));
            // No "classroom_exit" placeholder: the curtain now occupies that doorway, and two triggers
            // in the same gap would fight over the interaction raycast.

            TextAsset dialogueAsset = Resources.Load<TextAsset>(DialogueResourcePath);
            if (dialogueAsset == null)
            {
                throw new InvalidOperationException($"Missing dialogue resource '{DialogueResourcePath}'. Check Assets/Resources/{DialogueResourcePath}.json and its import status.");
            }

            DialogueScript script = DialogueScript.FromJson(dialogueAsset.text);

            // N's scene is a separate script because it starts where the opening ends: the prologue
            // belongs to Q, and the substitute's first class is its own file with its own start node.
            // Two runners over one StoryState is what keeps the prologue's text untouched.
            TextAsset teacherAsset = Resources.Load<TextAsset>(TeacherDialogueResourcePath);
            if (teacherAsset == null)
            {
                throw new InvalidOperationException($"Missing dialogue resource '{TeacherDialogueResourcePath}'. Check Assets/Resources/{TeacherDialogueResourcePath}.json and its import status.");
            }

            DialogueScript teacherScript = DialogueScript.FromJson(teacherAsset.text);

            TextAsset revisitAsset = Resources.Load<TextAsset>(TeacherRevisitResourcePath);
            if (revisitAsset == null)
            {
                throw new InvalidOperationException($"Missing dialogue resource '{TeacherRevisitResourcePath}'. Check Assets/Resources/{TeacherRevisitResourcePath}.json and its import status.");
            }

            DialogueScript revisitScript = DialogueScript.FromJson(revisitAsset.text);
            _storyState = new StoryState();
            _storyState.FlagChanged += OnStoryFlagChanged;
            _characterBuild = new CharacterBuild();
            CharacterSheetText sheetText = LoadSheetText();

            Canvas canvas = PrototypeUiFactory.CreateCanvas();
            CreateInteractionPrompt(canvas.transform);
            _characterSheet = canvas.gameObject.AddComponent<CharacterSheetView>();
            _characterSheet.Initialize(canvas.transform, _characterBuild, sheetText, CompleteCharacterBuild);
            _dialoguePresenter = canvas.gameObject.AddComponent<DialoguePresenter>();
            _openingRunner = new DialogueRunner(script, _storyState, _characterBuild);
            _teacherRunner = new DialogueRunner(teacherScript, _storyState, _characterBuild);

            // The imagined lesson is the only scene that pays out: it awards insight, so it needs the
            // ledger, and it is built with the same character build the attribute checks resolve against.
            TextAsset imaginationAsset = Resources.Load<TextAsset>(ImaginationResourcePath);
            if (imaginationAsset == null)
            {
                throw new InvalidOperationException($"Missing dialogue resource '{ImaginationResourcePath}'.");
            }

            _insight = new InsightLedger();
            _imaginationRunner = new DialogueRunner(
                DialogueScript.FromJson(imaginationAsset.text), _storyState, _characterBuild, _insight);
            _teacherRevisitRunner = new DialogueRunner(revisitScript, _storyState, _characterBuild);
            _dialoguePresenter.Initialize(canvas.transform, _openingRunner, PauseWorldForDialogue, HandleDialogueEnded);

            // After the sheet exists, so the scene has the same build object the sheet spends points on.
            CreateCurtainInteraction(canvas.transform);

            _playerMovement.InputEnabled = false;
            _playerMovement.enabled = false;
            _playerInteractor.enabled = false;
            _playerCharacterController.enabled = false;
            _characterSheet.SetVisible(false);
            _interactionPrompt.text = string.Empty;
            _openingSequenceActive = true;
            StartCoroutine(PlayClassroomOpening());
        }

        private void Update()
        {
            if (_interactionPrompt == null || _dialoguePresenter == null) return;
            if (_openingSequenceActive)
            {
                _interactionPrompt.text = string.Empty;
                return;
            }

            // The curtain scene owns the screen while it is open.
            if (_curtainScene != null && _curtainScene.IsOpen) return;

            if (_dialoguePresenter.IsActive) return;

            // The paper sheet owns the screen while it is open: no prompt churn, no save shortcuts.
            if (_characterSheet != null && _characterSheet.IsVisible) return;

            HandleSaveLoadShortcuts();
            if (_saveLoadFeedbackActive && Time.unscaledTime >= _temporaryPromptUntil)
            {
                if (_characterSheet != null) _characterSheet.SetFeedback(string.Empty);
                _saveLoadFeedbackActive = false;
            }

            if (Time.unscaledTime < _temporaryPromptUntil)
            {
                return;
            }

            if (!_buildComplete)
            {
                _interactionPrompt.text = "先完成五项属性的点数分配。\nF5 保存 · F9 读取";
                return;
            }

            Inspectable target = _playerInteractor != null ? _playerInteractor.Target : null;
            _interactionPrompt.text = target != null
                ? $"按 E 互动：{target.DisplayName}\nF5 保存 · F9 读取"
                : "WASD / 方向键移动 · 靠近人物或物件后按 E 互动\nF5 保存 · F9 读取";
        }

        private void OnDisable()
        {
            RestoreWorldState();
        }

        private void OnDestroy()
        {
            RestoreWorldState();

            // Meshes are runtime objects: Unity does not collect them with the GameObjects.
            if (_protagonist != null) _protagonist.DisposeMesh();
            if (_qFigure != null) _qFigure.DisposeMesh();

            if (_surfaces != null)
            {
                _surfaces.Dispose();
                _surfaces = null;
            }
        }

        /// <summary>
        /// The building the classroom sits inside, and the courtyard below it. Built after the room and
        /// never touching it: the shell skips the column the room occupies on every floor.
        ///
        /// A missing shell is not fatal. The prototype's room still works without a building around it,
        /// and a scene that refuses to start because a scenery file is absent is worse than a scene with
        /// an honest hole in the sky.
        /// </summary>
        private void BuildCampusShell()
        {
            TextAsset shellAsset = Resources.Load<TextAsset>(CampusShellResourcePath);
            if (shellAsset == null)
            {
                Debug.LogWarning(
                    $"Missing campus shell '{CampusShellResourcePath}'. Check Assets/Resources/{CampusShellResourcePath}.json. " +
                    "The classroom will stand on its own.");
                return;
            }

            CampusShellLayout shell = CampusShellLayout.FromJson(shellAsset.text, CampusShellResourcePath);
            _campusRoot = CampusShellBuilder.Build(shell, _surfaces, transform);
        }

        private void BuildClassroom()
        {
            TextAsset layoutAsset = Resources.Load<TextAsset>(ClassroomLayoutResourcePath);
            if (layoutAsset == null)
            {
                throw new InvalidOperationException(
                    $"Missing classroom layout '{ClassroomLayoutResourcePath}'. Check Assets/Resources/{ClassroomLayoutResourcePath}.json.");
            }

            _layout = ClassroomLayout.FromJson(layoutAsset.text, ClassroomLayoutResourcePath);
            _classroomRoot = ClassroomBuilder.Build(_layout, _surfaces, transform);
            BuildCampusShell();

            // The curtain that stands where the front wall should be. Its id is part of the scene's
            // contract (CurtainScene), so a rename fails here instead of failing silently at dusk.
            Transform curtain = _classroomRoot.transform.Find(ClassroomBuilder.FrontCurtainName);
            if (curtain == null)
            {
                throw new InvalidOperationException($"The classroom has no '{ClassroomBuilder.FrontCurtainName}'.");
            }

            _curtain = curtain;

            // The exit trigger must sit in a doorway the layout actually cut, or the prologue has no end.
            if (Array.Find(_layout.doors, door => door.id == ClassroomBuilder.ExitDoorId) == null)
            {
                throw new InvalidOperationException("The classroom layout has no front exit door; the prologue cannot end.");
            }

            if (ClassroomBuilder.SeatPositions(_layout).Count() != 24)
            {
                throw new InvalidOperationException("The classroom layout must place exactly 24 seats.");
            }
        }

        /// <summary>
        /// Q, built the same way the protagonist is: proportions -> skeleton -> one skinned mesh. This
        /// method was called but never defined at HEAD, which meant the whole assembly did not compile;
        /// it is written here rather than worked around, because Q is the figure the opening camera is
        /// pointed at.
        /// </summary>
        private void CreateClassmateQ(Vector3 position, float yawDegrees)
        {
            _qFigure = SkinnedCharacterBuilder.Create(
                "Q",
                transform,
                position,
                yawDegrees,
                HumanoidProportions.Classmate(),
                FigureRole.ClassmateQ,
                _surfaces.CreateCharacterSkin(FigureRole.ClassmateQ));

            _qStudentRoot = _qFigure.Root;
            _qAnimator = _qStudentRoot.gameObject.AddComponent<HumanoidAnimator>();
            _qAnimator.Initialize(_qFigure);
            _qAnimator.ClipFinished -= HandleQBeatFinished;
            _qAnimator.ClipFinished += HandleQBeatFinished;

            // Q keeps the trigger the old placeholder capsule had, so "press E on Q" still starts the
            // prologue. The name marker is only for the greybox camera, and it is the same one.
            CapsuleCollider reach = _qStudentRoot.gameObject.AddComponent<CapsuleCollider>();
            reach.isTrigger = true;
            reach.center = new Vector3(0f, 0.8f, 0f);
            reach.radius = 0.42f;
            reach.height = 1.58f;

            _qTarget = _qStudentRoot.gameObject.AddComponent<Inspectable>();
            _qTarget.Configure("classmate_q", "Q", "【占位说明】靠近并按 E 开始一对一对话。");
            _qTarget.Inspected += OnInspectableUsed;
            AddNameMarker(_qStudentRoot, "Q");
        }

        /// <summary>
        /// N, the substitute teacher. She enters through the staff door in the blackboard wall and
        /// stands behind the podium, so the opening camera can find her the moment the player stands up:
        /// Q is in front, N is at the back, and the room is populated at two depths.
        ///
        /// Her start is the first point of the layout's <c>teacher.entryPath</c>, so the blocking is
        /// data. If the layout has no path, this throws instead of placing her somewhere arbitrary.
        /// </summary>
        private void CreateTeacherN()
        {
            if (_layout == null || _layout.teacher == null || _layout.teacher.entryPath.Length == 0)
            {
                throw new InvalidOperationException("The classroom layout has no teacher entry path.");
            }

            TeacherPlacement blocking = _layout.teacher;
            Vector3 start = new Vector3(blocking.entryPath[0].x, 0f, blocking.entryPath[0].y);
            _teacherMark = new Vector3(blocking.stand.x, 0f, blocking.stand.y);
            _teacherYaw = blocking.standYaw;

            // Face along the path, not at the mark: she walks on screen before she turns.
            Vector3 facing = _teacherMark - start;
            facing.y = 0f;
            float startYaw = facing.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(facing.normalized, Vector3.up).eulerAngles.y
                : _teacherYaw;

            _teacherFigure = SkinnedCharacterBuilder.Create(
                "N",
                transform,
                start,
                startYaw,
                HumanoidProportions.TeacherN(),
                FigureRole.TeacherN,
                _surfaces.CreateCharacterSkin(FigureRole.TeacherN));

            _teacherRoot = _teacherFigure.Root;
            _teacherAnimator = _teacherRoot.gameObject.AddComponent<HumanoidAnimator>();
            _teacherAnimator.Initialize(_teacherFigure);
            _teacherIdleClip = HumanoidClips.TeacherAtPodiumIdle();
            if (!_teacherIdleClip.Validate(_teacherFigure.Skeleton, out string teacherClipError))
            {
                throw new InvalidOperationException("N's podium clip does not fit her skeleton: " + teacherClipError);
            }

            CreateTeacherInteraction();
        }

        /// <summary>
        /// B, standing at the front of the room for the whole game, present only while the imagining is
        /// open.
        ///
        /// She is built at startup and hidden, rather than built when the scene begins, because building a
        /// skinned mesh mid-conversation would stall the frame the player is reading. Hiding is a
        /// renderer toggle; the bones keep their pose.
        ///
        /// The distortion is deliberate and is the point of her: she is not a person in the room, she is
        /// what the protagonist's imagination made out of Q's description, and it made her wrong. Three
        /// things carry that, all of them cheap to undo if the owner wants her sober:
        ///
        ///   1. a second copy of her, half a metre to the left and slightly smaller, rendered as a
        ///      ghost. A remembered figure is never exactly where you put it.
        ///   2. her scale is not quite uniform: 4% taller than the table says, 6% narrower, so she is
        ///      stretched. Nothing about her should pass a measurement.
        ///   3. she faces the room but is turned 18 degrees off the protagonist, because the imagination
        ///      is not sure which seat he is in.
        ///
        /// She is given no animator: the imagining does not move her. A figure that never shifts its
        /// weight is the most dreamlike thing in the scene, and it costs nothing.
        /// </summary>
        private void CreateImaginedTeacherB()
        {
            // In front of the podium, facing the class. Off the layout's teacher path on purpose: this is
            // not the woman who walks in, it is a figure standing where a teacher stands.
            Vector3 mark = new Vector3(_layout != null && _layout.podium != null ? _layout.podium.positionX : 0f, 0f, 2.4f);
            const float yaw = 180f;
            const float skewYaw = 18f;

            _imaginedTeacherB = SkinnedCharacterBuilder.Create(
                "B (imagined)",
                transform,
                mark,
                yaw + skewYaw,
                HumanoidProportions.TeacherB(),
                FigureRole.TeacherB,
                _surfaces.CreateCharacterSkin(FigureRole.TeacherB));

            _imaginedTeacherRoot = _imaginedTeacherB.Root;

            // The stretch: taller, narrower, and a little deeper than she should be. Applied to the
            // figure's own transform so the mesh and the bones agree and the skinning stays valid.
            Transform figure = _imaginedTeacherRoot.Find("Figure");
            if (figure != null) figure.localScale = new Vector3(0.94f, 1.04f, 1.02f);

            // The ghost: the same geometry, a second renderer, offset and smaller. A copy rather than a
            // second build, so there is one mesh and one skeleton and no chance of the two drifting.
            _imaginedTeacherGhost = SkinnedCharacterBuilder.Create(
                "B (afterimage)",
                transform,
                mark + new Vector3(-0.48f, 0f, -0.30f),
                yaw + skewYaw - 7f,
                HumanoidProportions.TeacherB(),
                FigureRole.TeacherB,
                _surfaces.CreateCharacterSkin(FigureRole.TeacherB));
            Transform ghostFigure = _imaginedTeacherGhost.Root.Find("Figure");
            if (ghostFigure != null) ghostFigure.localScale = new Vector3(0.90f, 0.97f, 0.90f);

            SetImaginedTeacherVisible(false);
        }

        /// <summary>
        /// Shows or hides B. The afterimage fades with her, because it is the same thought.
        /// </summary>
        private void SetImaginedTeacherVisible(bool visible)
        {
            if (_imaginedTeacherRoot != null) _imaginedTeacherRoot.gameObject.SetActive(visible);
            if (_imaginedTeacherGhost != null) _imaginedTeacherGhost.Root.gameObject.SetActive(visible);
        }

        /// <summary>
        /// The three scripts the panel can play, in the order the slice reaches them. The title is part
        /// of the scene, not of the script, because the panel is one panel.
        /// </summary>
        private static string TitleFor(QuizScene scene)
        {
            switch (scene)
            {
                case QuizScene.ImaginedBiologyTeacher: return "想象 · 生物老师";
                case QuizScene.TeacherFirstClass: return "序章 · 代课老师";
                case QuizScene.TeacherRevisit: return "N 老师";
                default: return "序章 · 教室";
            }
        }

        /// <summary>
        /// Which conversation is due. The prologue plays once and belongs to Q; after it, N's first
        /// class plays once; after that she only has the short revisit. All three share the one
        /// StoryState, so the flags survive a save.
        /// </summary>
        private QuizScene ResolveScene()
        {
            if (!_openingFinished) return QuizScene.ClassroomOpening;
            if (!_storyState.IsSet(StoryNamespace.Recollection, "n_first_class_heard")) return QuizScene.TeacherFirstClass;
            return QuizScene.TeacherRevisit;
        }

        private DialogueRunner RunnerFor(QuizScene scene)
        {
            switch (scene)
            {
                case QuizScene.ImaginedBiologyTeacher: return _imaginationRunner;
                case QuizScene.TeacherFirstClass: return _teacherRunner;
                case QuizScene.TeacherRevisit: return _teacherRevisitRunner;
                default: return _openingRunner;
            }
        }

        /// <summary>
        /// Points the panel at one script and opens it. Every way into a conversation goes through
        /// here, so the title, the runner and the end-of-scene bookkeeping cannot drift apart.
        /// </summary>
        private void BeginDialogue(DialoguePresenter presenter, QuizScene scene)
        {
            _activeScene = scene;
            presenter.SetRunner(RunnerFor(scene));
            presenter.SetTitle(TitleFor(scene));

            // Staging is subscribed once, not per scene: the table below decides which beats belong to
            // which node, so a node in another script with the same id would simply not match.
            presenter.NodeShown -= OnDialogueNodeShown;
            presenter.NodeShown += OnDialogueNodeShown;

            // She exists only while the imagining is open. Before the first node is shown and after the
            // last, the front of the room has nobody at it.
            SetImaginedTeacherVisible(scene == QuizScene.ImaginedBiologyTeacher);

            presenter.Begin();
        }

        /// <summary>
        /// Stages one beat per authored node, without touching a word of the text.
        ///
        /// The prologue is one conversation in a room that is otherwise idle, so before this the only
        /// thing moving during it was the dialogue panel. Each entry answers "who is being talked to,
        /// and where are they looking" for one node: Q turns to face the protagonist when addressed, and
        /// turns away to the board for the lines where he is reciting the story of the zero rather than
        /// speaking to a person. The protagonist's own head belongs to the player and is never taken
        /// over, and N is on a fixed path in from the corridor, so she has no beats here.
        ///
        /// This is a table and not logic on purpose. If a node id is reworded or dropped, its beat
        /// silently stops happening --- a missing gesture, never a broken conversation.
        /// </summary>
        private void OnDialogueNodeShown(string nodeId)
        {
            if (_activeScene != QuizScene.ClassroomOpening) return;
            if (_qAnimator == null || _qIdleClip == null) return;

            HumanoidClip beat;
            switch (nodeId)
            {
                // He opens the conversation, so he is already turning to the protagonist.
                case "q_1":
                    beat = _qTurnClip;
                    break;

                // The authored line says Q looks at the set square. Nothing performed it before.
                case "q_who_look":
                    beat = _qGlanceClip;
                    break;

                // The recitation nodes: eyes leave the protagonist and go to the board.
                case "q_zero":
                case "q_blank_1":
                case "q_absent_1":
                    beat = _qAwayClip;
                    break;

                // The questions are over; he goes back to being an ordinary student facing front.
                case "q_exam_end":
                    beat = _qIdleClip;
                    break;

                default:
                    return;
            }

            if (beat != null) _qAnimator.Play(beat);
        }

        /// <summary>
        /// How far Q has to turn, in degrees, to face the set square from where he comes to a stop.
        /// Negative is to his right. Derived from the two objects rather than typed, so moving either
        /// the prop or his mark cannot leave him staring at the wrong piece of the room.
        /// </summary>
        private float SetSquareYawFromQ()
        {
            // His facing at the moment the line is spoken is the one AnimateQApproach leaves him with:
            // he turns to face the protagonist as he arrives. Using his creation yaw here instead is a
            // 20-degree error that reads as him looking past the thing the line names.
            Vector3 toPlayer = ProtagonistStandingPosition - QDestination;
            toPlayer.y = 0f;
            float approachYaw = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;

            Vector3 toTarget = SetSquarePosition - QDestination;
            float targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

            return Mathf.DeltaAngle(approachYaw, targetYaw);
        }

        /// <summary>
        /// Hands Q back to the idle loop after a beat finishes, so the room never freezes on a pose.
        /// Wired once at construction: the beats are shorter than the nodes, so the return always
        /// happens while the player is still reading.
        /// </summary>
        private void HandleQBeatFinished(HumanoidClip clip)
        {
            if (clip == null || _qAnimator == null || _qIdleClip == null) return;
            if (clip == _qIdleClip) return;
            _qAnimator.Play(_qIdleClip);
        }

        private void HandleDialogueEnded()
        {
            // ResumeWorldAfterDialogue also re-enables movement, which puts the player back in control;
            // without it the panel closing would leave the room frozen (world time is still zero).
            ResumeWorldAfterDialogue();

            // The imagining is over: the front of the room empties.
            if (_activeScene == QuizScene.ImaginedBiologyTeacher) SetImaginedTeacherVisible(false);

            switch (_activeScene)
            {
                case QuizScene.ClassroomOpening:
                    _openingFinished = true;
                    _storyState.Set(StoryNamespace.Objective, "opening_finished");

                    // Straight into what he imagined. It is not a separate place or a cut: Q is
                    // mid-sentence when the prologue ends, and the description of B is what the player
                    // has just been handed, so the imagining has to follow immediately or the beat is
                    // lost. No input is needed from the player to get there.
                    BeginDialogue(_dialoguePresenter, QuizScene.ImaginedBiologyTeacher);
                    break;

                case QuizScene.ImaginedBiologyTeacher:
                    // The ledger has been holding insight throughout the scene; this is where it
                    // becomes attribute points. Granted once, at the end, so the sheet does not change
                    // under the player mid-conversation.
                    int gained = _insight != null ? _insight.GrantEarnedPoints(_characterBuild) : 0;
                    if (gained > 0)
                    {
                        _storyState.Set(StoryNamespace.Recollection, "b_insight_paid_out");
                    }

                    break;

                default:
                    _storyState.Set(StoryNamespace.Recollection, "n_first_class_heard");
                    break;
            }
        }

        /// <summary>
        /// She is only there once she has walked in. Before that the figure is standing in the doorway
        /// with its trigger off, which is what stops the player from interacting with a person who has
        /// not arrived. Driven by the objective flag the walk-in sets, not by a boolean, so a save that
        /// recorded "teacher_arrived" restores a room with a teacher standing in it.
        /// </summary>
        private void RefreshTeacherPresence()
        {
            if (_teacherTarget == null) return;

            bool arrived = _storyState != null && _storyState.IsSet(StoryNamespace.Objective, "teacher_arrived");
            if (arrived && !_teacherTarget.enabled) _teacherTarget.enabled = true;
            if (!arrived && _teacherTarget.enabled) _teacherTarget.enabled = false;
        }

        /// <summary>
        /// The teacher is a person, not a prop: a trigger on her root so that walking up to her and
        /// pressing E starts her scene, the same way Q's conversation is started.
        /// </summary>
        private void CreateTeacherInteraction()
        {
            CapsuleCollider reach = _teacherRoot.gameObject.AddComponent<CapsuleCollider>();
            reach.isTrigger = true;
            reach.center = new Vector3(0f, 0.82f, 0f);
            reach.radius = 0.45f;
            reach.height = 1.64f;

            _teacherTarget = _teacherRoot.gameObject.AddComponent<Inspectable>();
            _teacherTarget.Configure(
                "teacher_n",
                "N 老师",
                "【占位】代课的生物老师。她站在讲台后面，手上是还没改完的卷子。");
            _teacherTarget.Inspected += OnInspectableUsed;
            RefreshTeacherPresence();
        }

        private void CreatePlayerAndCamera()
        {
            GameObject player = new GameObject("Player Placeholder");
            player.transform.position = new Vector3(0f, 1f, 0f);
            _playerCharacterController = player.AddComponent<CharacterController>();
            _playerCharacterController.height = 2f;
            _playerCharacterController.radius = 0.42f;
            _playerCharacterController.center = Vector3.zero;
            // The controller is a 2 m capsule centred on its transform, so the figure hangs one metre
            // below the pivot; its own origin is at the feet.
            _protagonist = SkinnedCharacterBuilder.Create(
                "Protagonist",
                player.transform,
                new Vector3(0f, -1f, 0f),
                0f,
                HumanoidProportions.Protagonist(),
                FigureRole.Protagonist,
                _surfaces.CreateCharacterSkin(FigureRole.Protagonist));
            _protagonistAnimator = player.AddComponent<HumanoidAnimator>();
            _protagonistAnimator.Initialize(_protagonist);

            _wakeClip = HumanoidClips.WakeFromDesk();
            _idleClip = HumanoidClips.StandingIdle();
            _qIdleClip = HumanoidClips.ClassmateIdle();
            _qTurnClip = HumanoidClips.ClassmateTurnBeat();
            _qAwayClip = HumanoidClips.ClassmateAwayBeat();
            _qGlanceClip = HumanoidClips.ClassmateGlanceBeat(SetSquareYawFromQ());
            if (!_wakeClip.Validate(_protagonist.Skeleton, out string wakeError)
                || !_idleClip.Validate(_protagonist.Skeleton, out string idleError)
                || !_qIdleClip.Validate(_protagonist.Skeleton, out string qError)
                || !_qTurnClip.Validate(_protagonist.Skeleton, out string qTurnError)
                || !_qAwayClip.Validate(_protagonist.Skeleton, out string qAwayError)
                || !_qGlanceClip.Validate(_protagonist.Skeleton, out string qGlanceError))
            {
                throw new InvalidOperationException("Animation clip does not fit the skeleton: "
                    + (wakeError ?? idleError ?? qError ?? qTurnError ?? qAwayError ?? qGlanceError));
            }

            // The fall clips are only ever built inside the curtain scene, which means a wrong bone name
            // would not show up until the player had already stepped through the wall. Build and check
            // them here instead, at startup, with the drop the layout actually has.
            float drop = CurtainFallRules.DropMetres(_layout);
            HumanoidClip fallClip = HumanoidClips.FallFromTheCurtain(drop);
            HumanoidClip crashClip = HumanoidClips.CrashLanding(drop);
            if (!fallClip.Validate(_protagonist.Skeleton, out string fallError)
                || !crashClip.Validate(_protagonist.Skeleton, out string crashError))
            {
                throw new InvalidOperationException("Curtain fall clip does not fit the skeleton: " + (fallError ?? crashError));
            }
            _playerMovement = player.AddComponent<PlayerMovement>();
            _playerMovement.InputEnabled = false;
            _playerInteractor = player.AddComponent<PlayerInteractor>();
            _playerInteractor.enabled = false;

            Camera prototypeCamera = Camera.main;
            if (prototypeCamera == null)
            {
                throw new InvalidOperationException("The prototype scene must contain a camera tagged MainCamera.");
            }

            FollowCamera followCamera = prototypeCamera.GetComponent<FollowCamera>();
            if (followCamera == null) followCamera = prototypeCamera.gameObject.AddComponent<FollowCamera>();
            followCamera.SetTarget(player.transform);
        }

        private void CreateInspectable(string id, string displayName, string description, PrimitiveType primitiveType, Vector3 position, Vector3 scale, Material material)
        {
            GameObject prop = GameObject.CreatePrimitive(primitiveType);
            prop.transform.position = position;
            prop.transform.localScale = scale;
            prop.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = prop.GetComponent<Collider>();
            collider.isTrigger = true;

            prop.SetActive(false);
            prop.name = displayName;
            Inspectable inspectable = prop.AddComponent<Inspectable>();
            inspectable.Configure(id, displayName, description);
            inspectable.Inspected += OnInspectableUsed;
            prop.SetActive(true);
        }

        private IEnumerator PlayClassroomOpening()
        {
            if (_qStudentRoot == null || _playerCharacterController == null)
            {
                throw new InvalidOperationException("The classroom opening sequence requires both the player and Q.");
            }

            if (_protagonist == null || _protagonistAnimator == null || _wakeClip == null)
            {
                throw new InvalidOperationException("The classroom opening sequence requires the skinned protagonist and its wake clip.");
            }

            Transform player = _playerCharacterController.transform;

            // The figure's origin is at its feet, so the player transform is placed where the standing
            // figure should end up; the clip's own root offset does the crouching and the lie-down. The
            // wake desk is at z = 0.5, so the figure stands just in front of it and reaches back.
            Vector3 standingPosition = ProtagonistStandingPosition;
            Vector3 qDestination = QDestination;
            Vector3 facingQ = qDestination - standingPosition;
            facingQ.y = 0f;
            Quaternion standingRotation = Quaternion.LookRotation(facingQ.normalized, Vector3.up);

            // While the clip plays, the character controller is off and the figure is driven by the
            // clip; the crouch comes from the pose's root offset, not from moving the collider.
            player.SetPositionAndRotation(standingPosition, standingRotation);
            _protagonistAnimator.Play(_wakeClip);

            if (_qAnimator != null && _qIdleClip != null) _qAnimator.Play(_qIdleClip);

            // The substitute comes in while the protagonist is still face-down on the desk. Nobody
            // announces her, which is the point: by the time the player can look up, the room already
            // has an adult in it.
            Coroutine teacherEntry = StartCoroutine(AnimateTeacherEntry());

            // Q walks over while the protagonist is still getting up: the two beats overlap, which is
            // what makes the room feel occupied instead of staged.
            const float qDelay = 1.15f;
            yield return new WaitForSecondsRealtime(qDelay);
            yield return AnimateQApproach(_qStudentRoot, new Vector3(qDestination.x, 0f, qDestination.z), player.position, 1.8f);

            // Wait out the rest of the clip by the clip's own clock, so changing a key time cannot
            // desynchronise the sequence from the animation.
            while (_protagonistAnimator.IsPlaying && _protagonistAnimator.CurrentClip == _wakeClip)
            {
                yield return null;
            }

            _protagonistAnimator.Play(_idleClip);

            // The sheet is shown by the end of the opening, so the walk-in has to be finished or the
            // player would look up to a teacher frozen mid-stride behind the UI.
            yield return teacherEntry;

            _openingSequenceActive = false;
            BeginDialogue(_dialoguePresenter, QuizScene.ClassroomOpening);
        }

        /// <summary>
        /// Walks N along the layout's entry path and turns her to face the room. One transform owns the
        /// path and the walk clip owns the legs; the head is tilted down a few degrees at each corner so
        /// the walk reads as someone finding her way rather than a slide along a spline.
        /// </summary>
        private IEnumerator AnimateTeacherEntry()
        {
            if (_teacherRoot == null || _layout == null || _layout.teacher == null) yield break;

            TeacherPlacement blocking = _layout.teacher;
            HumanoidClip walk = HumanoidClips.ClassmateWalk();
            if (_teacherAnimator != null) _teacherAnimator.Play(walk, 0f);

            Vector2[] path = blocking.entryPath;
            float seconds = Mathf.Max(0.5f, blocking.entrySeconds);

            for (int i = 1; i < path.Length; i++)
            {
                Vector3 from = new Vector3(path[i - 1].x, 0f, path[i - 1].y);
                Vector3 to = new Vector3(path[i].x, 0f, path[i].y);
                float legLength = Vector3.Distance(from, to);
                float legSeconds = seconds * legLength / Mathf.Max(0.001f, PathLength(path));

                Vector3 direction = to - from;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    // Turn into the corner before walking it: a teacher who uses the room's geometry.
                    Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    float turn = 0f;
                    const float turnSeconds = 0.35f;
                    Quaternion startRotation = _teacherRoot.rotation;
                    while (turn < turnSeconds)
                    {
                        turn += Time.unscaledDeltaTime;
                        _teacherRoot.rotation = Quaternion.Slerp(startRotation, target, Mathf.SmoothStep(0f, 1f, turn / turnSeconds));
                        yield return null;
                    }

                    _teacherRoot.rotation = target;
                }

                float legElapsed = 0f;
                while (legElapsed < legSeconds)
                {
                    float t = Mathf.Clamp01(legElapsed / legSeconds);
                    _teacherRoot.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                    legElapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                _teacherRoot.position = to;
            }

            // She stops, turns to the room, and only then lets go of the walk clip.
            Quaternion facing = Quaternion.Euler(0f, _teacherYaw, 0f);
            Quaternion walkRotation = _teacherRoot.rotation;
            float settle = 0f;
            const float settleSeconds = 0.45f;
            while (settle < settleSeconds)
            {
                settle += Time.unscaledDeltaTime;
                _teacherRoot.rotation = Quaternion.Slerp(walkRotation, facing, Mathf.SmoothStep(0f, 1f, settle / settleSeconds));
                yield return null;
            }

            _teacherRoot.SetPositionAndRotation(_teacherMark, facing);
            if (_teacherAnimator != null)
            {
                _teacherAnimator.Play(_teacherIdleClip);
                // The podium pose lifts her hips 12 mm; capture the mark as her base so the offset is
                // measured from the floor, not from the middle of the walk.
                _teacherAnimator.CaptureBaseHeight();
            }

            _storyState.Set(StoryNamespace.Objective, "teacher_arrived");
        }

        private static float PathLength(Vector2[] path)
        {
            float total = 0f;
            for (int i = 1; i < path.Length; i++) total += Vector2.Distance(path[i - 1], path[i]);
            return total;
        }

        /// <summary>
        /// Walks Q to their mark before the conversation. The legs are driven by the walk clip on Q's
        /// own animator; this coroutine only owns the root path, which is exactly the split a skinned
        /// character wants (clip = pose, code = locomotion).
        /// </summary>
        private IEnumerator AnimateQApproach(Transform q, Vector3 destination, Vector3 playerPosition, float duration)
        {
            if (q == null) yield break;

            Vector3 startPosition = q.position;
            Vector3 endPosition = q.parent != null ? q.parent.TransformPoint(destination) : destination;
            Vector3 facing = playerPosition - endPosition;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;

            Quaternion startRotation = q.rotation;
            Quaternion endRotation = Quaternion.LookRotation(facing.normalized, Vector3.up);

            if (_qAnimator != null) _qAnimator.Play(HumanoidClips.ClassmateWalk(), 0.15f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                q.position = Vector3.Lerp(startPosition, endPosition, eased);
                q.rotation = Quaternion.Slerp(startRotation, endRotation, eased);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            q.SetPositionAndRotation(endPosition, endRotation);
            if (_qAnimator != null) _qAnimator.Play(_qIdleClip);
        }

        /// <summary>
        /// The curtain is the slice's only branching scene, so it gets a trigger across the whole opening
        /// and its own component. The doorway is behind the curtain; the trigger is wide enough that the
        /// player finds it by walking at the missing wall, not by pixel-hunting a door handle.
        /// </summary>
        private void CreateCurtainInteraction(Transform canvas)
        {
            // Called once the character sheet has resolved the build: CurtainScene resolves attribute
            // checks against it and throws on null, by design.
            if (_characterBuild == null) throw new InvalidOperationException("The curtain scene needs a character build.");
            WallOpening exit = Array.Find(_layout.doors, door => door.id == ClassroomBuilder.ExitDoorId);
            float exitX = _layout.room.MinX + exit.centerAlong;
            float z = _layout.room.MinZ - _layout.room.wallThickness * 0.5f;

            // Parented to the room, not the curtain: the curtain is lifted during the sequence, and a
            // trigger riding up with it would stop being where the player is standing.
            GameObject trigger = new GameObject("Curtain Trigger");
            trigger.transform.SetParent(_classroomRoot.transform, true);
            trigger.transform.position = new Vector3(exitX, 1.1f, z + 0.45f);

            BoxCollider collider = trigger.AddComponent<BoxCollider>();
            collider.size = new Vector3(exit.width + 1.4f, 2.2f, 1.1f);
            collider.isTrigger = true;

            Inspectable inspectable = trigger.AddComponent<Inspectable>();
            inspectable.Configure(CurtainScene.CurtainPropId, "幕布（前墙）", "【占位】一块从天花板垂到地面的幕布，后面没有墙。");
            inspectable.Inspected += OnInspectableUsed;

            _curtainScene = trigger.AddComponent<CurtainScene>();
            _curtainScene.Initialize(
                canvas,
                _characterBuild,
                _layout,
                _curtain,
                _protagonistAnimator,
                _idleClip,
                HandleCurtainTeleport,
                HandleCurtainFinished);
        }

        /// <summary>
        /// Hands the body to the fall. The transform is not moved here: the fall clip travels it, and the
        /// controller is switched off for the duration so gravity does not race the animation to the lid.
        /// </summary>
        private void HandleCurtainTeleport(Vector3? landing)
        {
            _playerMovement.InputEnabled = false;
            _playerInteractor.enabled = false;
            if (_playerCharacterController != null) _playerCharacterController.enabled = false;
        }

        /// <summary>
        /// Called when the fall card is dismissed. The controller is put back on the lid (the figure was
        /// moved by the fall, the controller was not), and the player only gets control back if they
        /// landed well: a crash landing is not something this slice lets you walk out of.
        /// </summary>
        private void HandleCurtainFinished(bool fell, Vector3 landing)
        {
            if (!fell) return;

            bool landedWell = _curtainScene != null && _curtainScene.LastOutcome == FallOutcome.LandedOnContainer;
            _storyState.Set(StoryNamespace.Objective, landedWell ? "fell_landed_on_container" : "fell_crashed_on_container");

            if (_playerCharacterController != null)
            {
                // Capsule is centred, so its pivot sits one metre above the lid; the figure rebase below
                // then keeps the feet on the same spot.
                Vector3 pivot = landing + Vector3.up * 1f;
                _playerCharacterController.transform.position = pivot;
                _playerCharacterController.enabled = true;
                if (_protagonistAnimator != null)
                {
                    _protagonistAnimator.CaptureBaseHeight();
                    _protagonistAnimator.PlaceAt(landing);
                }
            }

            if (landedWell)
            {
                _playerMovement.enabled = true;
                _playerMovement.InputEnabled = true;
                _playerInteractor.enabled = true;
            }
        }

        private void OnInspectableUsed(Inspectable inspectable)
        {
            if (_curtainScene != null && inspectable.PropId == CurtainScene.CurtainPropId)
            {
                _curtainScene.Begin();
                return;
            }

            if (_qTarget != null && inspectable == _qTarget)
            {
                // Q's questions are the prologue, and the prologue plays once. After that Q has
                // nothing new to say in this slice, so the prompt shows the placeholder instead of
                // starting a conversation that has already happened.
                if (!_openingFinished) BeginDialogue(_dialoguePresenter, ResolveScene());
                else ShowTemporaryPrompt("Q 已经在背知识点了。（此阶段没有新的对话。）");
                return;
            }

            if (_teacherTarget != null && inspectable == _teacherTarget)
            {
                BeginDialogue(_dialoguePresenter, ResolveScene());
                return;
            }

            _interactionPrompt.text = inspectable.DisplayName + "\n" + inspectable.Description;
            _temporaryPromptUntil = Time.unscaledTime + 3.5f;
        }

        private static CharacterSheetText LoadSheetText()
        {
            TextAsset sheetAsset = Resources.Load<TextAsset>(SheetTextResourcePath);
            if (sheetAsset == null)
            {
                throw new InvalidOperationException(
                    $"Missing character sheet text '{SheetTextResourcePath}'. Check Assets/Resources/{SheetTextResourcePath}.json and its import status.");
            }

            return CharacterSheetText.FromJson(sheetAsset.text, SheetTextResourcePath);
        }

        private void HandleSaveLoadShortcuts()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                string path = Path.Combine(Application.persistentDataPath, SaveFileName);
                bool saved = PrototypeSaveStore.TrySave(path, _characterBuild, _storyState, out string error);
                ShowTemporaryPrompt(saved ? "原型存档已保存。" : "保存失败：" + error);
                return;
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                string path = Path.Combine(Application.persistentDataPath, SaveFileName);
                if (!PrototypeSaveStore.TryLoad(path, _characterBuild, _storyState, out string error))
                {
                    ShowTemporaryPrompt("读取失败：" + error);
                    return;
                }

                if (_characterSheet != null) _characterSheet.RefreshFromModel();
                _buildComplete = _characterBuild.IsComplete;
                _characterSheet.SetVisible(!_buildComplete);
                if (_buildComplete)
                {
                    CompleteCharacterBuild();
                }
                else
                {
                    _playerMovement.InputEnabled = false;
                    _playerInteractor.enabled = false;
                }

                ShowTemporaryPrompt("原型存档已读取；不恢复对白位置或历史。");
            }
        }

        private void ShowTemporaryPrompt(string message)
        {
            _interactionPrompt.text = message;
            if (_characterSheet != null) _characterSheet.SetFeedback(message);
            _saveLoadFeedbackActive = true;
            _temporaryPromptUntil = Time.unscaledTime + 3.5f;
        }

        /// <summary>True while the curtain scene is running; the scene's pause must respect it.</summary>
        private bool CurtainSceneActive => _curtainScene != null && _curtainScene.IsOpen;

        private void CompleteCharacterBuild()
        {
            _buildComplete = true;
            _playerCharacterController.enabled = true;
            _playerMovement.enabled = true;
            _playerMovement.InputEnabled = true;
            _playerInteractor.enabled = true;
        }

        private void PauseWorldForDialogue()
        {
            if (_worldPaused || CurtainSceneActive) return;
            _worldPaused = true;
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _playerMovement.InputEnabled = false;
            _playerInteractor.enabled = false;
        }

        private void ResumeWorldAfterDialogue()
        {
            RestoreWorldState();
            if (_buildComplete)
            {
                if (_playerMovement != null)
                {
                    _playerMovement.enabled = true;
                    _playerMovement.InputEnabled = true;
                }

                if (_playerInteractor != null) _playerInteractor.enabled = true;
                if (_playerCharacterController != null) _playerCharacterController.enabled = true;
            }
            else if (_characterSheet != null)
            {
                // Character build is still unfinished, so the paper sheet comes back with the world.
                _characterSheet.SetVisible(true);
            }
        }

        private void RestoreWorldState()
        {
            if (!_worldPaused) return;
            Time.timeScale = _previousTimeScale;
            _worldPaused = false;
        }

        private void CreateInteractionPrompt(Transform canvas)
        {
            PrototypeUiFactory.CreatePanel(canvas, "Interaction Prompt Background", new Vector2(0.025f, 0.025f), new Vector2(0.49f, 0.13f), new Color(0.06f, 0.065f, 0.075f, 0.88f));
            _interactionPrompt = PrototypeUiFactory.CreateText(canvas, "Interaction Prompt", new Vector2(0.04f, 0.04f), new Vector2(0.475f, 0.115f), "先完成五项属性的点数分配。", 18, Color.white, TextAnchor.MiddleLeft);
        }

        private void AddNameMarker(Transform parent, string marker)
        {
            GameObject markerObject = new GameObject("Temporary Name Marker", typeof(TextMesh));
            markerObject.transform.SetParent(parent, false);
            markerObject.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            TextMesh text = markerObject.GetComponent<TextMesh>();
            text.font = UiTypeface.Sans;
            text.text = marker;
            text.fontSize = 48;
            text.characterSize = 0.08f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(0.12f, 0.1f, 0.08f, 1f);
        }
    }
}
