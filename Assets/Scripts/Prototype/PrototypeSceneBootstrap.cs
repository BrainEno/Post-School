using System;
using System.IO;
using PostSchool.Character;
using PostSchool.Core;
using PostSchool.Dialogue;
using PostSchool.Interaction;
using PostSchool.Narrative;
using PostSchool.Persistence;
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
        private const string DialogueResourcePath = "Dialogue/character_build_placeholder";
        private const string SaveFileName = "prototype-save.json";

        private readonly Material[] _materials = new Material[5];
        private CharacterBuild _characterBuild;
        private StoryState _storyState;
        private CharacterCreationView _characterCreation;
        private DialoguePresenter _dialoguePresenter;
        private PlayerMovement _playerMovement;
        private PlayerInteractor _playerInteractor;
        private Text _interactionPrompt;
        private Inspectable _conversationTarget;
        private float _temporaryPromptUntil;
        private float _previousTimeScale = 1f;
        private bool _worldPaused;
        private bool _buildComplete;
        private bool _saveLoadFeedbackActive;

        private void Start()
        {
            BuildClassroomGraybox();
            CreatePlayerAndCamera();
            CreateInspectable("classmate_q", "Q", "【占位说明】靠近并按 E 开始一对一对话。", PrimitiveType.Capsule,
                new Vector3(0f, 1f, 3.7f), new Vector3(0.8f, 1f, 0.8f), MaterialAt(1));
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

            Canvas canvas = PrototypeUiFactory.CreateCanvas();
            CreateInteractionPrompt(canvas.transform);
            _characterCreation = canvas.gameObject.AddComponent<CharacterCreationView>();
            _characterCreation.Initialize(canvas.transform, _characterBuild, CompleteCharacterBuild);
            _dialoguePresenter = canvas.gameObject.AddComponent<DialoguePresenter>();
            DialogueRunner runner = new DialogueRunner(script, _storyState, _characterBuild);
            _dialoguePresenter.Initialize(canvas.transform, runner, PauseWorldForDialogue, ResumeWorldAfterDialogue);
        }

        private void Update()
        {
            if (_interactionPrompt == null || _dialoguePresenter == null || _dialoguePresenter.IsActive) return;
            HandleSaveLoadShortcuts();
            if (_saveLoadFeedbackActive && Time.unscaledTime >= _temporaryPromptUntil)
            {
                _characterCreation.SetFeedback(string.Empty);
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

            CreateDesk("Desk A", new Vector3(-3.2f, 0f, 5.6f));
            CreateDesk("Desk B", new Vector3(3.2f, 0f, 5.6f));
            CreateDesk("Desk C", new Vector3(-3.2f, 0f, 7.8f));
            CreateDesk("Desk D", new Vector3(3.2f, 0f, 7.8f));
        }

        private void CreatePlayerAndCamera()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player Placeholder";
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.GetComponent<Renderer>().sharedMaterial = MaterialAt(3);
            Collider primitiveCollider = player.GetComponent<Collider>();
            primitiveCollider.enabled = false;

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.42f;
            controller.center = Vector3.zero;
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
            GameObject prop = GameObject.CreatePrimitive(primitiveType);
            prop.SetActive(false);
            prop.name = displayName;
            prop.transform.position = position;
            prop.transform.localScale = scale;
            prop.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = prop.GetComponent<Collider>();
            collider.isTrigger = true;

            Inspectable inspectable = prop.AddComponent<Inspectable>();
            inspectable.Configure(id, displayName, description);
            inspectable.Inspected += OnInspectableUsed;
            prop.SetActive(true);

            if (id == "classmate_q")
            {
                _conversationTarget = inspectable;
                AddNameMarker(prop.transform, "Q");
            }
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

                _characterCreation.RefreshFromModel();
                _buildComplete = _characterBuild.IsComplete;
                _characterCreation.SetVisible(!_buildComplete);
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
            _characterCreation.SetFeedback(message);
            _saveLoadFeedbackActive = true;
            _temporaryPromptUntil = Time.unscaledTime + 3.5f;
        }

        private void CompleteCharacterBuild()
        {
            _buildComplete = true;
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
            if (_buildComplete && _playerMovement != null) _playerMovement.InputEnabled = true;
            if (_buildComplete && _playerInteractor != null) _playerInteractor.enabled = true;
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
            markerObject.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            TextMesh text = markerObject.GetComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
                default: color = new Color32(126, 118, 111, 255); break;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            material.name = "Prototype Graybox Material " + index;
            _materials[index] = material;
            return material;
        }
    }
}
