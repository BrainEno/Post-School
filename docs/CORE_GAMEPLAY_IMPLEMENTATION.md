# 五属性与沉浸式对话纵切片实现说明

## 范围与依据

对应 GitHub Issue #10：五属性角色构建、焦点对话、长文本流和一次属性检定。本地分支起点为 `c62f73b`，该基线不含核心玩法文档；实现依据为已合并 PR #11（commit `cd4ad6fdc68a3e82d55eed9cbeefd6596ff20a65`）中的 `docs/CORE_GAMEPLAY.md`。设计约束：每项基础值 1、7 点自由分配、初始上限 5；作者对白和叙述只用占位文字；检定失败有可继续的结果；主观想象不作为客观事实。

Issue #3 已有开放 Draft PR #5，且其视觉校准场景代码与本切片不重叠；本次按无代码 PR 重叠的 Issue #10 实现。

## 架构

- `PostSchool.Character.CharacterBuild` 独立拥有五属性、点数预算、基础值和上限规则；UI 不能直接改内部数值。
- `AttributeCheckResolver` 实施可重复的阈值判定（属性值大于等于目标即达成），不掷随机骰。规则明确、可单测；它是原型规则，不宣称为最终数值设计。
- 现有 `DialogueScript` / `DialogueRunner` / `StoryState` 继续分别拥有 JSON 数据、图运行与客观/回忆标记。选择项可声明 `checkAttribute`、`minimumValue` 和 `failureNext`；`next` 是达成分支。旧无检定构造函数及普通对话图保持兼容。
- `CharacterCreationView` 和 `DialoguePresenter` 是独立的 UGUI 呈现组件。对话列固定在右侧，保留滚动历史并提供字号调整；`PrototypeSceneBootstrap` 负责场景交互接线、暂停/恢复 `Time.timeScale` 与已有移动/交互组件。
- `Assets/Resources/Dialogue/character_build_placeholder.json` 是唯一新增示例对白数据；所有叙述/选项均标为待作者审定的占位文本。
- `PostSchool_Prototype.unity` 挂载启动组件。当前教室、玩家、Q 和少量检视物使用临时 primitive graybox 便于验证回路，不是最终美术或生产场景资产。未引入包或外部资源。

## 已检索与复用

实际搜索/检查命令：

- `git status --short --branch`、`git log --oneline -12`：工作树干净，分支基于 `c62f73b`。
- `gh issue list --state open --limit 100`、`gh pr list --state open --limit 100`：Issue #10 与 #3 开放；Issue #3 已有开放 Draft PR #5；Issue #10 无开放 PR。
- `gh pr view 11 --json ...` 与 `gh api repos/BrainEno/Post-School/contents/docs/CORE_GAMEPLAY.md?ref=main`：PR #11 已合入远端 main，读取了其核心玩法依据；当前 checkout 的旧基线尚未包含这份文档。
- `find Assets -maxdepth 5 -type f | sort`、`rg -n "class (DialogueRunner|DialogueScript|StoryState|PlayerMovement|PlayerInteractor)|Attribute|Stat|Choice|Condition|Effect|InputAction" Assets/Scripts Assets/Tests Assets/Data Assets/Scenes docs`：找到现有 `DialogueRunner`、`DialogueScript`、`StoryState`、`PlayerMovement`、`PlayerInteractor`、`Inspectable`、`FollowCamera` 及其 EditMode 测试；未找到角色属性或 UI 实现。
- 阅读 `AGENTS.md`、`docs/AGENT_PROTOCOL.md`、`docs/PROJECT_RELATIONSHIP.md`、`docs/PORTING_MATRIX.md`、`docs/UNITY_TECHNICAL_BASELINE.md`、`docs/PROTOTYPE_ROUND1.md`、产品/叙事/视觉参考，以及现有场景、Input System actions、asmdef 和 CI 检查器。

复用决定：不另建移动、相机、交互、对话或叙事状态系统；扩展现有对话选择数据/runner、`Inspectable` 配置接口与 `FollowCamera` 目标设置；使用已有 Input System、URP、UGUI，不加依赖。

## 验证状态

- 静态 Unity 项目树检查：`python3 tools/check_unity_project.py --check`。
- 检查器自身的 Python 判定测试：`python3 -m unittest discover -s tests/ci -p 'test_*.py' -v`，新增覆盖检定属性名、目标范围和失败分支引用。
- 新增 Unity EditMode 测试覆盖初始值、七点预算、上限/下限、重置、确定性检定、成功/失败分支及错误配置。沙箱未安装 Unity 6000.3.29f1，因此这些 Unity 测试、C# 导入/编译和 Play Mode 尚未运行。
- 需要在锁定版本 Unity Editor 手动确认：UGUI 中文字体回退、布局与滚动、输入模块点击、物理交互范围、对话期间暂停/恢复和检定两分支。没有提交虚构的截图、录屏或运行通过声明。

## 已知限制

- 属性上限 5 和确定性阈值比较只是原型；没有随机检定、技能树或属性描述文案。
- 临时课堂通过 scene bootstrap 创建 Unity primitive；不代表完成了 Issue #3 的共享视觉语法、场景美术或静态场景组装验收。
- 无存档、场景转场、文本速度设置、完整可访问性、本地化字体资产和多轮对话 backlog 持久化。
- 所有新增对白/检视文案均为占位，不是作者最终文本。
- Unity Editor 未运行，场景导入、脚本编译、真实 Play Mode 和视觉体验都尚未验证。
