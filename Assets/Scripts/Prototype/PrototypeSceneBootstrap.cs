using System;
using System.Collections;
using System.IO;
using PostSchool.Character;
using PostSchool.Core;
using PostSchool.Dialogue;
using PostSchool.Interaction;
using PostSchool.Narrative;
using PostSchool.Persistence;
using PostSchool.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PostSchool.Prototype
{
    /// <summary>
    /// Wires the first playable graybox slice in the prototype scene. Geometry is deliberately
    /// temporary; character rules and dialogue content live in their own model/data files.
    /// </summary>
    public sealed class PrototypeSceneBootstrap : MonoBehaviour
    {
        private const string DialogueResourcePath = "Dialogue/classroom_opening";
        private const string SheetTextResourcePath = "CharacterSheet/character_sheet_text_placeholder";
        private const string SaveFileName = "prototype-save.json";

        private readonly Material[] _materials = new Material[14];
        private CharacterBuild _characterBuild;
        private StoryState _storyState;
        private CharacterSheetView _characterSheet;
        private DialoguePresenter _dialoguePresenter;
        private PlayerMovement _playerMovement;
        private PlayerInteractor _playerInteractor;
        private CharacterController _playerCharacterController;
        private ProtagonistPoseRig _protagonistRig;
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
            BuildClassroomGraybox();
            CreatePlayerAndCamera();
            CreateInspectable("classmate_q", "Q", "【占位说明】靠近并按 E 开始一对一对话。", PrimitiveType.Capsule,
                new Vector3(-3.2f, 0f, 0.7f), Vector3.one, MaterialAt(1));
            CreateOriginalStudent("Unlabeled Student Placeholder", new Vector3(-2.35f, 0f, 4.35f), 150f,
                1.68f, StudentModelStyle.ClayOvershirt);
            CreateInspectable("schoolbag", "书包（占位）", "【占位说明，待作者审定】这里可以检查书包。", PrimitiveType.Cube,
                new Vector3(-2.1f, 0.4f, 1.7f), new Vector3(0.8f, 0.8f, 0.55f), MaterialAt(2));
            CreateInspectable("set_square", "三角尺（占位）", "【占位说明，待作者审定】这里可以检查一件课堂物品。", PrimitiveType.Cube,
                new Vector3(2.1f, 0.48f, 2.2f), new Vector3(0.9f, 0.08f, 0.16f), MaterialAt(3));
            CreateInspectable("classroom_exit", "教室出口（占位）", "【占位】已到达本次纵切片的出口。", PrimitiveType.Cube,
                new Vector3(0f, 1f, -3.55f), new Vector3(1.8f, 2f, 0.12f), MaterialAt(4));

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
            for (int i = 0; i < _materials.Length; i++)
            {
                if (_materials[i] != null) Destroy(_materials[i]);
            }
        }

        private void BuildClassroomGraybox()
        {
            Material floor = MaterialAt(0);
            Material wall = MaterialAt(4);
            CreateBox("Graybox Floor", new Vector3(0f, -0.15f, 3.3f), new Vector3(12f, 0.3f, 14f), floor);
            CreateBox("Left Classroom Wall", new Vector3(-6f, 1.5f, 3.3f), new Vector3(0.25f, 3f, 14f), wall);
            CreateBox("Right Classroom Wall", new Vector3(6f, 1.5f, 3.3f), new Vector3(0.25f, 3f, 14f), wall);
            CreateBox("Back Classroom Wall", new Vector3(0f, 1.5f, 10.25f), new Vector3(12f, 3f, 0.25f), wall);
            CreateBox("Front Classroom Wall Left", new Vector3(-4.25f, 1.5f, -3.65f), new Vector3(3.5f, 3f, 0.25f), wall);
            CreateBox("Front Classroom Wall Right", new Vector3(4.25f, 1.5f, -3.65f), new Vector3(3.5f, 3f, 0.25f), wall);
            CreateBox("Blackboard Placeholder", new Vector3(0f, 2f, 10.05f), new Vector3(3.8f, 1.2f, 0.08f), MaterialAt(1));
            CreateBox("Prologue Wake Desk Top", new Vector3(0f, 0.82f, 0.8f), new Vector3(2.2f, 0.12f, 1.5f), MaterialAt(2));
            CreateBox("Prologue Wake Desk Leg Left Front", new Vector3(-0.95f, 0.4f, 0.2f), new Vector3(0.12f, 0.8f, 0.12f), MaterialAt(3));
            CreateBox("Prologue Wake Desk Leg Right Front", new Vector3(0.95f, 0.4f, 0.2f), new Vector3(0.12f, 0.8f, 0.12f), MaterialAt(3));
            CreateBox("Prologue Wake Desk Leg Left Back", new Vector3(-0.95f, 0.4f, 1.4f), new Vector3(0.12f, 0.8f, 0.12f), MaterialAt(3));
            CreateBox("Prologue Wake Desk Leg Right Back", new Vector3(0.95f, 0.4f, 1.4f), new Vector3(0.12f, 0.8f, 0.12f), MaterialAt(3));

            CreateDesk("Desk A", new Vector3(-3.2f, 0f, 5.6f));
            CreateDesk("Desk B", new Vector3(3.2f, 0f, 5.6f));
            CreateDesk("Desk C", new Vector3(-3.2f, 0f, 7.8f));
            CreateDesk("Desk D", new Vector3(3.2f, 0f, 7.8f));
        }

        private void CreatePlayerAndCamera()
        {
            GameObject player = new GameObject("Player Placeholder");
            player.transform.position = new Vector3(0f, 1f, 0f);
            _playerCharacterController = player.AddComponent<CharacterController>();
            _playerCharacterController.height = 2f;
            _playerCharacterController.radius = 0.42f;
            _playerCharacterController.center = Vector3.zero;
            _protagonistRig = BuildProtagonistPoseRig(player.transform);
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

        private ProtagonistPoseRig BuildProtagonistPoseRig(Transform parent)
        {
            Transform visual = new GameObject("Protagonist Pose Rig").transform;
            visual.SetParent(parent, false);

            CreateVisualPart("Torso", visual, PrimitiveType.Capsule, Vector3.zero,
                new Vector3(0.34f, 0.36f, 0.24f), MaterialAt(3));
            CreateVisualPart("Shirt Panel", visual, PrimitiveType.Cube, new Vector3(0f, 0f, 0.19f),
                new Vector3(0.18f, 0.34f, 0.025f), MaterialAt(7));
            CreateVisualPart("Neck", visual, PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f),
                new Vector3(0.085f, 0.1f, 0.085f), MaterialAt(5));

            Transform head = NewPosePivot("Head Pivot", visual, new Vector3(0f, 0.42f, 0f));
            CreateVisualPart("Face", head, PrimitiveType.Sphere, new Vector3(0f, 0.17f, 0.02f),
                new Vector3(0.19f, 0.22f, 0.17f), MaterialAt(5));
            CreateVisualPart("Hair", head, PrimitiveType.Sphere, new Vector3(0f, 0.33f, -0.015f),
                new Vector3(0.2f, 0.1f, 0.18f), MaterialAt(6));
            CreateVisualPart("Eye Left", head, PrimitiveType.Sphere, new Vector3(-0.06f, 0.18f, 0.177f),
                new Vector3(0.022f, 0.025f, 0.014f), MaterialAt(12));
            CreateVisualPart("Eye Right", head, PrimitiveType.Sphere, new Vector3(0.06f, 0.18f, 0.177f),
                new Vector3(0.022f, 0.025f, 0.014f), MaterialAt(12));

            Transform leftArm = NewPosePivot("Arm Left Pivot", visual, new Vector3(-0.27f, 0.24f, 0f));
            Transform rightArm = NewPosePivot("Arm Right Pivot", visual, new Vector3(0.27f, 0.24f, 0f));
            BuildArm(leftArm, "Left");
            BuildArm(rightArm, "Right");

            Transform leftLeg = NewPosePivot("Leg Left Pivot", visual, new Vector3(-0.12f, -0.34f, 0f));
            Transform rightLeg = NewPosePivot("Leg Right Pivot", visual, new Vector3(0.12f, -0.34f, 0f));
            BuildLeg(leftLeg, "Left");
            BuildLeg(rightLeg, "Right");

            return new ProtagonistPoseRig(head, leftArm, rightArm, leftLeg, rightLeg);
        }

        private void BuildArm(Transform pivot, string side)
        {
            CreateVisualPart("Arm " + side + " Upper", pivot, PrimitiveType.Capsule, new Vector3(0f, -0.15f, 0f),
                new Vector3(0.095f, 0.16f, 0.095f), MaterialAt(3));
            CreateVisualPart("Arm " + side + " Forearm", pivot, PrimitiveType.Capsule, new Vector3(0f, -0.38f, 0.01f),
                new Vector3(0.08f, 0.15f, 0.08f), MaterialAt(3));
            CreateVisualPart("Hand " + side, pivot, PrimitiveType.Sphere, new Vector3(0f, -0.54f, 0.035f),
                new Vector3(0.06f, 0.07f, 0.055f), MaterialAt(5));
        }

        private void BuildLeg(Transform pivot, string side)
        {
            CreateVisualPart("Leg " + side, pivot, PrimitiveType.Capsule, new Vector3(0f, -0.22f, 0f),
                new Vector3(0.12f, 0.2f, 0.12f), MaterialAt(10));
            CreateVisualPart("Shoe " + side, pivot, PrimitiveType.Sphere, new Vector3(0f, -0.54f, 0.05f),
                new Vector3(0.14f, 0.08f, 0.22f), MaterialAt(11));
        }

        private static Transform NewPosePivot(string name, Transform parent, Vector3 localPosition)
        {
            Transform pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            return pivot;
        }

        private static GameObject CreateVisualPart(
            string name,
            Transform parent,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(collider);
                else UnityEngine.Object.DestroyImmediate(collider);
            }

            return part;
        }

        private void CreateDesk(string objectName, Vector3 position)
        {
            CreateBox(objectName + " Top", position + new Vector3(0f, 0.82f, 0f), new Vector3(1.7f, 0.12f, 1.1f), MaterialAt(2));
            CreateBox(objectName + " Leg 1", position + new Vector3(-0.68f, 0.4f, -0.42f), new Vector3(0.12f, 0.8f, 0.12f), MaterialAt(3));
            CreateBox(objectName + " Leg 2", position + new Vector3(0.68f, 0.4f, -0.42f), new Vector3(0.12f, 0.8f, 0.12f), MaterialAt(3));
            CreateBox(objectName + " Leg 3", position + new Vector3(-0.68f, 0.4f, 0.42f), new Vector3(0.12f, 0.8f, 0.12f), MaterialAt(3));
            CreateBox(objectName + " Leg 4", position + new Vector3(0.68f, 0.4f, 0.42f), new Vector3(0.12f, 0.8f, 0.12f), MaterialAt(3));
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
                    CreateStudentMaterials(MaterialAt(8)));
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
            Material outerwear = style == StudentModelStyle.MossCardigan ? MaterialAt(8) : MaterialAt(13);
            ProceduralStudentModelFactory.Create(objectName, transform, position, yaw, height, style, CreateStudentMaterials(outerwear));
        }

        private StudentModelMaterials CreateStudentMaterials(Material outerwear)
        {
            return new StudentModelMaterials(
                MaterialAt(5),
                MaterialAt(6),
                outerwear,
                MaterialAt(7),
                MaterialAt(10),
                MaterialAt(11),
                MaterialAt(9),
                MaterialAt(12));
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

            Vector3 sleepingPosition = new Vector3(0f, 1.02f, 0.8f);
            Vector3 standingPosition = new Vector3(0f, 1f, -0.55f);
            Vector3 qDestination = new Vector3(-1.7f, 0f, -0.15f);
            Vector3 facingQ = qDestination - standingPosition;
            facingQ.y = 0f;
            Quaternion standingRotation = Quaternion.LookRotation(facingQ.normalized, Vector3.up);

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

            _openingSequenceActive = false;
            _dialoguePresenter.Begin();
        }

        private static IEnumerator AnimateWakePose(
            ProtagonistPoseRig rig,
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
            else if (_characterCreation != null)
            {
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

        private GameObject CreateBox(string objectName, Vector3 position, Vector3 scale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = objectName;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private Material MaterialAt(int index)
        {
            if (_materials[index] != null) return _materials[index];
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Neither URP/Lit nor the built-in Standard shader is available in this project.");

            Material material = new Material(shader);
            Color color;
            switch (index)
            {
                case 0: color = new Color32(184, 170, 144, 255); break;
                case 1: color = new Color32(82, 104, 89, 255); break;
                case 2: color = new Color32(196, 183, 151, 255); break;
                case 3: color = new Color32(156, 104, 69, 255); break;
                case 4: color = new Color32(126, 118, 111, 255); break;
                case 5: color = StudentModelColorPalette.Skin; break;
                case 6: color = StudentModelColorPalette.Hair; break;
                case 7: color = StudentModelColorPalette.Shirt; break;
                case 8: color = StudentModelColorPalette.MossCardigan; break;
                case 9: color = StudentModelColorPalette.Accent; break;
                case 10: color = StudentModelColorPalette.Trousers; break;
                case 11: color = StudentModelColorPalette.Shoes; break;
                case 12: color = StudentModelColorPalette.Detail; break;
                case 13: color = StudentModelColorPalette.ClayOvershirt; break;
                default: throw new ArgumentOutOfRangeException(nameof(index), index, "Unknown prototype material index.");
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            material.name = "Prototype Graybox Material " + index;
            _materials[index] = material;
            return material;
        }

        private sealed class ProtagonistPoseRig
        {
            public ProtagonistPoseRig(Transform headPivot, Transform leftArmPivot, Transform rightArmPivot, Transform leftLegPivot, Transform rightLegPivot)
            {
                HeadPivot = headPivot;
                LeftArmPivot = leftArmPivot;
                RightArmPivot = rightArmPivot;
                LeftLegPivot = leftLegPivot;
                RightLegPivot = rightLegPivot;
            }

            public Transform HeadPivot { get; }
            public Transform LeftArmPivot { get; }
            public Transform RightArmPivot { get; }
            public Transform LeftLegPivot { get; }
            public Transform RightLegPivot { get; }
        }
    }
}
