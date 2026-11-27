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
        private SkinnedFigure _protagonist;
        private HumanoidAnimator _protagonistAnimator;
        private SkinnedFigure _qFigure;
        private HumanoidAnimator _qAnimator;
        private HumanoidClip _wakeClip;
        private HumanoidClip _idleClip;
        private HumanoidClip _qIdleClip;
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
            CreateClassmateQ(new Vector3(-2.9f, 0f, 1.1f), 128f);
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

            // Meshes are runtime objects: Unity does not collect them with the GameObjects.
            if (_protagonist != null) _protagonist.DisposeMesh();
            if (_qFigure != null) _qFigure.DisposeMesh();

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
            if (!_wakeClip.Validate(_protagonist.Skeleton, out string wakeError)
                || !_idleClip.Validate(_protagonist.Skeleton, out string idleError)
                || !_qIdleClip.Validate(_protagonist.Skeleton, out string qError))
            {
                throw new InvalidOperationException("Animation clip does not fit the skeleton: " + (wakeError ?? idleError ?? qError));
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
            Vector3 standingPosition = new Vector3(0f, 1f, -0.75f);
            Vector3 qDestination = new Vector3(-1.9f, 0f, -0.1f);
            Vector3 facingQ = qDestination - standingPosition;
            facingQ.y = 0f;
            Quaternion standingRotation = Quaternion.LookRotation(facingQ.normalized, Vector3.up);

            // While the clip plays, the character controller is off and the figure is driven by the
            // clip; the crouch comes from the pose's root offset, not from moving the collider.
            player.SetPositionAndRotation(standingPosition, standingRotation);
            _protagonistAnimator.Play(_wakeClip);

            if (_qAnimator != null && _qIdleClip != null) _qAnimator.Play(_qIdleClip);

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
            _openingSequenceActive = false;
            _dialoguePresenter.Begin();
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
