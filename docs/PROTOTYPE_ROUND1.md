# 第一轮原型说明（Post School）

> 状态：代码基础已落地，**尚未在 Unity 编辑器中编译或运行**。本轮沙箱没有 Unity 6.3 LTS（6000.3.29f1）与 .NET 工具链，所有验证需由作者在本地 Editor 中完成。

## 依据

- 产品范围与第一切片：`docs/reference/where-is-school/PRODUCT_BRIEF.md`、`PROTOTYPE_BRIEF.md`
- 叙事事实与角色功能：`docs/reference/where-is-school/NARRATIVE_BIBLE.md`
- 开场节拍：Q 对话 → 书包检查（生物课本缺失）→ 想象与回忆 → B 缺席、N 代课 → 结束

## 本轮已完成

| 领域 | 文件 | 说明 |
|---|---|---|
| 叙事状态 | `Assets/Scripts/Narrative/StoryState.cs` | 客观世界标记与回忆标记分两个命名空间；回忆标记不会改变客观标记 |
| 效果解析 | `Assets/Scripts/Narrative/StoryEffect.cs` | 解析 `objective:<flag>` / `recollection:<flag>`，加载时即可校验 |
| 对话数据 | `Assets/Scripts/Dialogue/DialogueScript.cs` | 可序列化的对话图（JSON），节点类型 line / choice / end |
| 对话运行 | `Assets/Scripts/Dialogue/DialogueRunner.cs` | 不含 UI；加载时校验缺失节点、未知类型、未知效果前缀；记录历史 |
| 占位剧本 | `Assets/Data/Dialogue/classroom_opening_placeholder.json` | 全部文本为**占位**，待作者审定 |
| 移动 | `Assets/Scripts/Core/PlayerMovement.cs` | CharacterController，键盘与手柄左摇杆，可按相机锚点计算方向 |
| 相机 | `Assets/Scripts/Core/FollowCamera.cs` | 固定偏移跟随，帧率无关阻尼；取景待定 |
| 检视 | `Assets/Scripts/Interaction/Inspectable.cs`、`PlayerInteractor.cs` | 最近目标选择，E / 手柄南键触发，检视事件由监听者处理 |
| 测试 | `Assets/Tests/EditMode/*.cs` | 对话分支、主观内容隔离、非法数据报错、状态变更事件等 |

## 本轮未完成（下一步）

1. **场景组装**：`PostSchool_Prototype.unity` 尚未改动，仍是模板灰盒；需要在编辑器中放置教室灰盒、碰撞、玩家、相机与可检视道具。
2. **对话 UI**：说话人、选项、记录（backlog）与文字速度/字号设置尚无呈现层。
3. **运行时接线**：把 `DialogueRunner` 与场景中的 `Inspectable` 连接，需要一个 owner 明确的控制器，避免多个状态来源。
4. **转场与存档**：教室出口与状态持久化未实现。
5. **作者文本**：JSON 中的台词与描述均为占位，不得当作定稿。

## 验证说明

- 已做：JSON 结构与节点引用的静态检查（10 个节点，无悬空引用）；asmdef JSON 格式检查；`.meta` GUID 生成；LF 行尾检查。
- 未做：C# 编译、EditMode 测试运行、PlayMode 测试、编辑器内的碰撞与相机手感检查。

运行测试的命令（在项目根目录，替换为本机 Unity 6.3 可执行文件）：

```powershell
Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults "Temp\editmode-results.xml" -logFile "Temp\editmode.log"
```
