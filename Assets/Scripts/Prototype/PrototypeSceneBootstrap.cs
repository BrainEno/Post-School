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
    /// ProtagonistModelFactory. Character rules and dialogue content live in their own model/data files.
    /// </summary>
    public sealed class PrototypeSceneBootstrap : MonoBehaviour
    {
        private const string DialogueResourcePath = "Dialogue/classroom_opening";
        private const string SheetTextResourcePath = "CharacterSheet/character_sheet_text_placeholder";
        private const string SaveFileName = "prototype-save.json";
        private const string ClassroomLayoutResourcePath = "World/classroom_layout";

        private WorldSurfaceLibrary _surfaces;
        private ClassroomLayout _layout;
        private CharacterBuild _characterBuild;
        private StoryState _storyState;
        private CharacterSheetView _characterSheet;
        private DialoguePresenter _dialoguePresenter;
        private PlayerMovement _playerMovement;
        private PlayerInteractor _playerInteractor;
        private CharacterController _playerCharacterController;
        private ProtagonistRig _protagonistRig;
        private ProtagonistIdleMotion _protagonistIdle;
        private Transform _qStudentRoot;
        private Text _interactionPrompt;
        private Inspectable _conversationTarget;
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
            CreateInspectable("classmate_q", "Q", "【占位说明】靠近并按 E 开始一对一对话。", PrimitiveType.Capsule,
                new Vector3(-2.9f, 0f, 1.1f), Vector3.one, null);
            CreateOriginalStudent("Unlabeled Student Placeholder", new Vector3(3.5f, 0f, 1.7f), 205f,
                1.66f, StudentModelStyle.ClayOvershirt);
            CreateInspectable("schoolbag", "书包（占位）", "【占位说明，待作者审定】这里可以检查书包。", PrimitiveType.Cube,
                new Vector3(-1.62f, 0.24f, 0.5f), new Vector3(0.5f, 0.42f, 0.34f), _surfaces.Surface(SurfaceRole.Ink));
            CreateInspectable("set_square", "三角尺（占位）", "【占位说明，待作者审定】这里可以检查一件课堂物品。", PrimitiveType.Cube,
                new Vector3(2.1f, 0.79f, 2.9f), new Vector3(0.62f, 0.05f, 0.14f), _surfaces.Surface(SurfaceRole.Paper));
            CreateInspectable("classroom_exit", "教室出口（占位）", "【占位】已到达本次纵切片的出口。", PrimitiveType.Cube,
                new Vector3(-4.0f, 1.1f, -4.4f), new Vector3(1.3f, 2.2f, 0.22f), _surfaces.Surface(SurfaceRole.Ink));

            TextAsset dialogueAsset = Resources.Load<TextAsset>(DialogueResourcePath);
            if (dialogueAsset == null)
            {
                throw new InvalidOperationException($"Missing dialogue resource '{DialogueResourcePath}'. Check Assets/Resources/{DialogueResourcePath}.json and its import status.");
            }

            DialogueScript script = DialogueScript.FromJson(dialogueAsset.text);
            _storyState = new StoryState();
            _characterBuild = new CharacterBuild();
            CharacterSheetText sheetText = LoadSheetText();

            Canvas canvas = PrototypeUiFactory.CreateCanvas();
            CreateInteractionPrompt(canvas.transform);
            _characterSheet = canvas.gameObject.AddComponent<CharacterSheetView>();
            _characterSheet.Initialize(canvas.transform, _characterBuild, sheetText, CompleteCharacterBuild);
            _dialoguePresenter = canvas.gameObject.AddComponent<DialoguePresenter>();
            DialogueRunner runner = new DialogueRunner(script, _storyState, _characterBuild);
            _dialoguePresenter.Initialize(canvas.transform, runner, PauseWorldForDialogue, ResumeWorldAfterDialogue);

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
            if (_surfaces != null)
            {
                _surfaces.Dispose();
                _surfaces = null;
            }
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
            ClassroomBuilder.Build(_layout, _surfaces, transform);

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

        private void CreatePlayerAndCamera()
        {
            GameObject player = new GameObject("Player Placeholder");
            player.transform.position = new Vector3(0f, 1f, 0f);
            _playerCharacterController = player.AddComponent<CharacterController>();
            _playerCharacterController.height = 2f;
            _playerCharacterController.radius = 0.42f;
            _playerCharacterController.center = Vector3.zero;
            // The controller is a 2 m capsule centred on its transform, so the feet sit one metre below.
            _protagonistRig = ProtagonistModelFactory.Build(
                player.transform, _surfaces.CreateProtagonistMaterials(), new Vector3(0f, -1f, 0f));
            _protagonistIdle = player.AddComponent<ProtagonistIdleMotion>();
            _protagonistIdle.Initialize(_protagonistRig);
            _protagonistIdle.Suspend();
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
            GameObject prop;
            if (id == "classmate_q")
            {
                prop = ProceduralStudentModelFactory.Create(
                    displayName,
                    transform,
                    position,
                    165f,
                    1.60f,
                    StudentModelStyle.MossCardigan,
                    CreateStudentMaterials(StudentModelStyle.MossCardigan));
                CapsuleCollider characterTrigger = prop.AddComponent<CapsuleCollider>();
                characterTrigger.center = new Vector3(0f, 0.84f, 0f);
                characterTrigger.height = 1.68f;
                characterTrigger.radius = 0.42f;
                characterTrigger.isTrigger = true;
            }
            else
            {
                prop = GameObject.CreatePrimitive(primitiveType);
                prop.transform.position = position;
                prop.transform.localScale = scale;
                prop.GetComponent<Renderer>().sharedMaterial = material;
                Collider collider = prop.GetComponent<Collider>();
                collider.isTrigger = true;
            }

            prop.SetActive(false);
            prop.name = displayName;
            Inspectable inspectable = prop.AddComponent<Inspectable>();
            inspectable.Configure(id, displayName, description);
            inspectable.Inspected += OnInspectableUsed;
            prop.SetActive(true);

            if (id == "classmate_q")
            {
                _conversationTarget = inspectable;
                _qStudentRoot = prop.transform;
                AddNameMarker(prop.transform, "Q");
            }
        }

        private void CreateOriginalStudent(string objectName, Vector3 position, float yaw, float height, StudentModelStyle style)
        {
            ProceduralStudentModelFactory.Create(objectName, transform, position, yaw, height, style, CreateStudentMaterials(style));
        }

        private StudentModelMaterials CreateStudentMaterials(StudentModelStyle style)
        {
            return _surfaces.CreateStudentMaterials(style);
        }

        private IEnumerator PlayClassroomOpening()
        {
            if (_qStudentRoot == null || _playerCharacterController == null)
            {
                throw new InvalidOperationException("The classroom opening sequence requires both the player and Q.");
            }

            Transform player = _playerCharacterController.transform;
            if (_protagonistRig == null)
            {
                throw new InvalidOperationException("The classroom opening sequence requires the protagonist pose rig.");
            }

            // Lying face-down on the wake desk: the model's origin is at its feet, so the desk-side
            // position is offset along +z and the body spans the desk top.
            Vector3 sleepingPosition = new Vector3(0f, 1.02f, 1.4f);
            Vector3 standingPosition = new Vector3(0f, 1f, -0.55f);
            Vector3 qDestination = new Vector3(-1.7f, 0f, -0.15f);
            Vector3 facingQ = qDestination - standingPosition;
            facingQ.y = 0f;
            Quaternion standingRotation = Quaternion.LookRotation(facingQ.normalized, Vector3.up);

            _protagonistIdle.Suspend();
            player.SetPositionAndRotation(sleepingPosition, Quaternion.Euler(90f, 0f, 0f));
            _protagonistRig.HeadPivot.localRotation = Quaternion.Euler(18f, 0f, 0f);
            _protagonistRig.LeftArmPivot.localRotation = Quaternion.Euler(30f, 0f, -24f);
            _protagonistRig.RightArmPivot.localRotation = Quaternion.Euler(30f, 0f, 24f);
            _protagonistRig.LeftLegPivot.localRotation = Quaternion.Euler(-8f, 0f, -3f);
            _protagonistRig.RightLegPivot.localRotation = Quaternion.Euler(8f, 0f, 3f);
            yield return new WaitForSecondsRealtime(0.45f);
            yield return AnimateWakePose(_protagonistRig, player, sleepingPosition, standingPosition, standingRotation, 1.35f);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return AnimateQApproach(_qStudentRoot, qDestination, player.position, 1.8f);

            // Idle motion takes over the pose once the protagonist is on their feet; the wake pose has
            // already set every pivot to its resting value.
            _protagonistIdle.Resume();
            _openingSequenceActive = false;
            _dialoguePresenter.Begin();
        }

        private static IEnumerator AnimateWakePose(
            ProtagonistRig rig,
            Transform player,
            Vector3 startPosition,
            Vector3 endPosition,
            Quaternion upright,
            float duration)
        {
            Quaternion lyingDown = Quaternion.Euler(90f, 0f, 0f);
            Quaternion liftingHead = Quaternion.Euler(67f, 0f, 0f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                float poseProgress = Mathf.SmoothStep(0f, 1f, progress);
                rig.HeadPivot.localRotation = Quaternion.Slerp(Quaternion.Euler(18f, 0f, 0f), Quaternion.identity, poseProgress);
                rig.LeftArmPivot.localRotation = Quaternion.Slerp(
                    Quaternion.Euler(30f, 0f, -24f), Quaternion.Euler(0f, 0f, -8f), poseProgress);
                rig.RightArmPivot.localRotation = Quaternion.Slerp(
                    Quaternion.Euler(30f, 0f, 24f), Quaternion.Euler(0f, 0f, 8f), poseProgress);
                rig.LeftLegPivot.localRotation = Quaternion.Slerp(Quaternion.Euler(-8f, 0f, -3f), Quaternion.identity, poseProgress);
                rig.RightLegPivot.localRotation = Quaternion.Slerp(Quaternion.Euler(8f, 0f, 3f), Quaternion.identity, poseProgress);

                if (progress < 0.28f)
                {
                    float liftProgress = Mathf.SmoothStep(0f, 1f, progress / 0.28f);
                    player.rotation = Quaternion.Slerp(lyingDown, liftingHead, liftProgress);
                    player.position = startPosition;
                }
                else
                {
                    float standProgress = Mathf.SmoothStep(0f, 1f, (progress - 0.28f) / 0.72f);
                    player.rotation = Quaternion.Slerp(liftingHead, upright, standProgress);
                    player.position = Vector3.Lerp(startPosition, endPosition, standProgress);
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            player.SetPositionAndRotation(endPosition, upright);
            rig.HeadPivot.localRotation = Quaternion.identity;
            rig.LeftArmPivot.localRotation = Quaternion.Euler(0f, 0f, -8f);
            rig.RightArmPivot.localRotation = Quaternion.Euler(0f, 0f, 8f);
            rig.LeftLegPivot.localRotation = Quaternion.identity;
            rig.RightLegPivot.localRotation = Quaternion.identity;
        }

        private static IEnumerator AnimateQApproach(Transform q, Vector3 destination, Vector3 playerPosition, float duration)
        {
            Vector3 startPosition = q.position;
            Vector3 endPosition = q.parent != null ? q.parent.TransformPoint(destination) : destination;
            Vector3 facing = playerPosition - endPosition;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;

            Quaternion startRotation = q.rotation;
            Quaternion endRotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
            Transform leftArm = q.Find("Arm_Left_Upper");
            Transform rightArm = q.Find("Arm_Right_Upper");
            Quaternion leftArmRest = leftArm != null ? leftArm.localRotation : Quaternion.identity;
            Quaternion rightArmRest = rightArm != null ? rightArm.localRotation : Quaternion.identity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                float stride = Mathf.Sin(progress * Mathf.PI * 8f);
                q.position = Vector3.Lerp(startPosition, endPosition, eased) + Vector3.up * (Mathf.Abs(stride) * 0.035f);
                q.rotation = Quaternion.Slerp(startRotation, endRotation, eased) * Quaternion.Euler(0f, 0f, stride * 1.5f);
                if (leftArm != null) leftArm.localRotation = leftArmRest * Quaternion.Euler(stride * 12f, 0f, 0f);
                if (rightArm != null) rightArm.localRotation = rightArmRest * Quaternion.Euler(-stride * 12f, 0f, 0f);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            q.SetPositionAndRotation(endPosition, endRotation);
            if (leftArm != null) leftArm.localRotation = leftArmRest;
            if (rightArm != null) rightArm.localRotation = rightArmRest;
        }

        private void OnInspectableUsed(Inspectable inspectable)
        {
            if (inspectable == _conversationTarget)
            {
                _dialoguePresenter.Begin();
                return;
            }

            if (inspectable.PropId == "classroom_exit")
            {
                _storyState.Set(StoryNamespace.Objective, "prototype_slice_exit_reached");
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
            if (_worldPaused) return;
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
