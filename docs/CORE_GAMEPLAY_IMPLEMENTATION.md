# 五属性与沉浸式对话纵切片实现说明

## 范围与依据

对应 GitHub Issue #10：五属性角色构建、焦点对话、长文本流和一次属性检定。本地分支起点为 `c62f73b`，实现依据为已合并 PR #11（commit `cd4ad6fdc68a3e82d55eed9cbeefd6596ff20a65`）中的 `docs/CORE_GAMEPLAY.md`。设计约束：每项基础值 1、7 点自由分配、初始上限 5；新增的序章开场台词取自作者在 Where Is School 中的现行 `q_wake` 对话并逐字保留，未覆盖的序章文本仍是待审占位内容；检定失败有可继续的结果；主观想象不作为客观事实。

此分支还承接 Issue #3 的视觉校准任务：将 PR #5 的校准场景构建器纳入当前分支，并在共享角色工厂中增加两名原创程序化 3D 学生。人物只借鉴用户 Drive 参考中的低饱和色块、柔和比例与剪纸环境感，不导入/照搬参考模型、贴图、角色姓名或具体造型；校准场景仍需在 Unity Editor 中生成和视觉验收。

## 架构

- `PostSchool.Character.CharacterBuild` 独立拥有五属性、点数预算、基础值和上限规则；UI 不能直接改内部数值。
- `AttributeCheckResolver` 实施可重复的阈值判定（属性值大于等于目标即达成），不掷随机骰。规则明确、可单测；它是原型规则，不宣称为最终数值设计。
- 现有 `DialogueScript` / `DialogueRunner` / `StoryState` 继续分别拥有 JSON 数据、图运行与客观/回忆标记。选择项可声明 `checkAttribute`、`minimumValue` 和 `failureNext`；`next` 是达成分支。旧无检定构造函数及普通对话图保持兼容。
- `CharacterCreationView` 和 `DialoguePresenter` 是独立的 UGUI 呈现组件。对白、选项和叙述以单一可滚动长文本流呈现在屏幕右侧，不使用气泡；字号可调。`PrototypeSceneBootstrap` 负责序章接线、暂停/恢复 `Time.timeScale` 与已有移动/交互组件。
- `Assets/Resources/Dialogue/classroom_opening.json` 收录经作者明确要求复用的 `q_wake` 开场摘录；台词与选项逐字保留，原 Godot 旗标和镜头效果未移植。其余序章新增对白仍待作者编写。
- 开场先让带头部、双臂和双腿枢轴的临时主角灰盒趴在新建课桌上，再以程序化变换动画逐步起身；Q 从教室过道走到桌边，带轻微摆臂、起伏与偏摆，停下后才开始首句对白。这是原型 Transform rig/动作，不是骨骼蒙皮或最终 Humanoid rig。
- `PostSchool_Prototype.unity` 挂载启动组件。当前教室仍由 Bootstrap 生成；玩家保留灰盒，Q 与一名未命名同学由 `ProceduralStudentModelFactory` 生成两套有不同发型、服饰和配件的 3D 原创占位角色。模型用 Unity 基元组合，无新增依赖，也不是最终美术。
- `PostSchool.Persistence.PrototypeSaveStore` 提供离线 JSON 存档 schema v1，仅保存五项属性、客观 flag 与回忆 flag；写入 `Application.persistentDataPath/prototype-save.json`，通过临时文件替换正式文件。场景内 F5 保存、F9 读取；拒绝不兼容版本/非法数据且先验证整个快照。对白节点、对白历史、场景位置和 UI 设置不在本版本存档范围内。

## 已检索与复用

实际搜索/检查命令：

- `git status --short --branch`、`git log --oneline -12`：工作树干净，分支基于 `c62f73b`。
- `gh issue list --state open --limit 100`、`gh pr list --state open --limit 100`：Issue #10 与 #3 开放；Issue #3 已有开放 Draft PR #5；Issue #10 无开放 PR。
- `gh pr view 11 --json ...` 与 `gh api repos/BrainEno/Post-School/contents/docs/CORE_GAMEPLAY.md?ref=main`：PR #11 已合入远端 main，读取了其核心玩法依据；当前 checkout 的旧基线尚未包含这份文档。
- `find Assets -maxdepth 5 -type f | sort`、`rg -n "class (DialogueRunner|DialogueScript|StoryState|PlayerMovement|PlayerInteractor)|Attribute|Stat|Choice|Condition|Effect|InputAction" Assets/Scripts Assets/Tests Assets/Data Assets/Scenes docs`：找到现有 `DialogueRunner`、`DialogueScript`、`StoryState`、`PlayerMovement`、`PlayerInteractor`、`Inspectable`、`FollowCamera` 及其 EditMode 测试；此前未找到属性/UI/存档实现。
- 本次新增存档前执行 `grep -RInE 'persistentDataPath|JsonUtility|File\.Write|File\.Read|TryRestore|CopyValues' Assets/Scripts Assets/Tests docs --exclude='*.meta'`、`find Assets/Tests -maxdepth 4 -type f -print | sort`：命中现有 `JsonUtility` 仅用于对白 JSON、现有核心类型和 EditMode 测试；未命中 `persistentDataPath`、文件存档或恢复 API。
- 阅读 `AGENTS.md`、`docs/AGENT_PROTOCOL.md`、`docs/PROJECT_RELATIONSHIP.md`、`docs/PORTING_MATRIX.md`、`docs/UNITY_TECHNICAL_BASELINE.md`、`docs/PROTOTYPE_ROUND1.md`、产品/叙事/视觉参考，以及现有场景、Input System actions、asmdef 和 CI 检查器。

复用决定：存档复用现有 `CharacterBuild`、`StoryState` 作为状态所有者，借用项目已有 Unity `JsonUtility`、`Application.persistentDataPath` 与 Input System；新增单一 `PrototypeSaveStore` 是因为此前无存档 API。不另建角色/故事状态副本，不增加包依赖，也不将对白执行位置/历史误认为已持久化。

## 验证状态

- 静态 Unity 项目树检查：`python3 tools/check_unity_project.py --check`。
- 检查器自身的 Python 判定测试：`python3 tests/ci/test_check_unity_project.py -q`；涵盖对白节点图的引用与结构检查。
- EditMode 测试已更新以验证 `q_wake` 摘录、原文关键句、三种醒来回应及分支可达性；本轮环境没有 Unity 6000.3.29f1，因此 C# 导入/编译、EditMode、Play Mode 均未运行。
- 需要在锁定版本 Unity Editor 人工确认：主角趴桌/起身动作、Q 走近的路径和步态、对话开始时序、右侧连续滚动文本、中文字体、布局/点击、暂停恢复和原有交互范围。没有提交未经运行的截图或视觉通过声明。

## 已知限制

- 属性上限 5 和确定性阈值比较只是原型；没有随机检定、技能树或属性描述文案。
- 临时课堂通过 scene bootstrap 创建 Unity primitive；不代表完成了 Issue #3 的共享视觉语法、场景美术或静态场景组装验收。
- 存档目前只含五项属性与两类 story flag，不恢复对白节点、对白历史、场景位置或 UI 设置；存档版本不兼容时需由未来迁移实现处理。
- 仍无场景转场、文本速度设置、完整可访问性、本地化字体资产和多轮对话 backlog 持久化。
- 只有本次明确授权的 `q_wake` 开场摘录使用作者原文；未复用部分的新增对白/检视文案仍是占位，不是作者最终文本。
- Unity Editor 未运行，场景导入、脚本编译、真实 Play Mode 和视觉体验都尚未验证。
