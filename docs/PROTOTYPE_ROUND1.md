# 第一轮原型说明（Post School）

> 状态：原型场景、对话呈现和开场动作代码已接线，**尚未在 Unity 编辑器中编译或运行**。开场对白摘录直接复用作者授权的 Where Is School `q_wake` 原文；校准场景生成与 Play Mode/视觉验收仍待 Editor 验证。

## 依据

- 产品范围与第一切片：`docs/reference/where-is-school/PRODUCT_BRIEF.md`、`PROTOTYPE_BRIEF.md`
- 叙事事实与角色功能：`docs/reference/where-is-school/NARRATIVE_BIBLE.md`
- 开场节拍：主角伏在课桌上醒来 → Q 从过道走近并开启 `q_wake` 对话 → 后续序章节拍仍待接入。

## 本轮已完成

| 领域 | 文件 | 说明 |
|---|---|---|
| 叙事状态 | `Assets/Scripts/Narrative/StoryState.cs` | 客观世界标记与回忆标记分两个命名空间；回忆标记不会改变客观标记 |
| 效果解析 | `Assets/Scripts/Narrative/StoryEffect.cs` | 解析 `objective:<flag>` / `recollection:<flag>`，加载时即可校验 |
| 对话数据 | `Assets/Scripts/Dialogue/DialogueScript.cs` | 可序列化的对话图（JSON），节点类型 line / choice / end |
| 对话运行 | `Assets/Scripts/Dialogue/DialogueRunner.cs` | 不含 UI；加载时校验缺失节点、未知类型、未知效果前缀；记录历史 |
| 开场摘录 | `Assets/Data/Dialogue/classroom_opening.json`、`Assets/Resources/Dialogue/classroom_opening.json` | 所选 `q_wake` 台词与选项逐字来自作者现行文本；未收录部分待后续接入 |
| 移动 | `Assets/Scripts/Core/PlayerMovement.cs` | CharacterController，键盘与手柄左摇杆，可按相机锚点计算方向 |
| 相机 | `Assets/Scripts/Core/FollowCamera.cs` | 固定偏移跟随，帧率无关阻尼；取景待定 |
| 检视 | `Assets/Scripts/Interaction/Inspectable.cs`、`PlayerInteractor.cs` | 最近目标选择，E / 手柄南键触发，检视事件由监听者处理 |
| 测试 | `Assets/Tests/EditMode/*.cs` | 对话分支、主观内容隔离、非法数据报错、状态变更事件等 |

## 本轮未完成（下一步）

1. **Unity 验收**：导入项目并运行 EditMode/Play Mode；检查趴桌、起身、Q 走近和对白启动时序。
2. **视觉与输入**：确认主角和 Q 在画面中的构图、动作节奏、连续文本滚动、中文字体、选项点击和交互范围。
3. **后续序章**：当前仅接入作者 `q_wake` 的一个开场摘录；原来源中的后续对白、书包检查、课堂铃声及代课段落尚未接入。
4. **作者文本边界**：除明确授权复用的摘录外，其余叙述、对白和检视文案继续保持占位，等待作者提供或确认。

## 验证说明

- 已做：Unity 项目静态检查与 Python CI 检查器单测；开场对白图的 19 个节点由工具校验无悬空引用。
- 未做：C# 编译、EditMode/Play Mode 测试运行、编辑器内动作、碰撞、镜头和滚动文本验收。

运行测试的命令（在项目根目录，替换为本机 Unity 6.3 可执行文件）：

```powershell
Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults "Temp\editmode-results.xml" -logFile "Temp\editmode.log"
```
