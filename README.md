# Post School

**Post School** 是《Where Is School》的 Unity 实验版本：一个以探索、环境观察和文学对白为主的叙事游戏原型。它与 [Godot 版本](https://github.com/BrainEno/where-is-school)并行开发，不会替代或破坏原项目。

> **如果你是第一次使用 Unity：从「第一次打开并运行」开始，按步骤操作即可。** 不需要先写代码，也不需要先生成视觉校准场景。

## 项目现在是什么状态？

- **引擎：** Unity Editor `6000.3.29f1`（revision `96d2fe17529c`），使用 Universal Render Pipeline（URP）。请不要自行升级 Unity 版本。
- **当前可运行入口：** `Assets/Scenes/PostSchool_Prototype.unity`。该场景已列入项目的构建场景设置；本指南先带你在 Editor 里直接运行，不要求先生成独立游戏安装包。
- **目前能体验：** 教室原型、主角从课桌醒来的程序化动作、Q 走近主角、序章对白、五项属性分配、教室内移动与简单互动，以及原型存档/读取。
- **仍属原型：** 人物和教室是灰盒/程序化占位内容；并非最终美术或完整游戏。序章中只有已获作者授权的 `q_wake` 摘录是作者原文，其余未完成对白或物件说明可能是明确标记的占位文案。
- **验证边界：** 自动化静态检查、Python 检查器测试与 Unity Editor 中的实际游玩是不同层级。报告结果时请说明具体跑了哪一种；没有打开 Unity 实测时，不要说 Unity 编译或游戏运行已通过。

## 第一次打开并运行（推荐）

### 1. 安装 Unity Hub 和指定版本

1. 安装并打开 **Unity Hub**，登录 Unity 账户。
2. 在 Hub 的 **Installs（安装）** 页面安装 **Unity `6000.3.29f1`**。如有模块选择，Windows 开发者可按需要安装 Windows Build Support；仅在 Editor 中游玩时不必先构建 Windows 程序。
3. 不要用其他 Unity 版本打开后保存项目，也不要点击升级/迁移项目的确认按钮。如果 Hub 提示项目版本不匹配，请先停止并确认安装的是上述精确版本。

### 2. 用 Unity Hub 添加仓库根目录

1. 获取本仓库到本机（例如用 GitHub Desktop 克隆，或下载后解压）。
2. 在 Unity Hub 的 **Projects（项目）** 页面选择 **Add / Add project from disk**。
3. 选择仓库最外层的 `Post-School` 文件夹，也就是同时能看到 `Assets`、`Packages`、`ProjectSettings` 和 `README.md` 的那一层。**不要只选 `Assets` 文件夹。**
4. 在项目列表中确认它使用 Unity `6000.3.29f1`，然后打开项目。
5. 首次打开会导入资源、编译脚本并解析包；耗时较久或短暂卡顿可能是正常的。等待 Unity 编辑器完成加载，不要在导入期间强制关闭。

### 3. 打开游戏场景并进入 Play Mode

1. 在 Unity 的 **Project** 窗口中打开：`Assets/Scenes/PostSchool_Prototype.unity`。
2. 点击编辑器中间的 **Game** 标签页，确认 Game 视图可见。推荐使用 `16:9` 或 `Free Aspect`。
3. 点击 Unity 窗口上方中央的 **▶ Play** 按钮。
4. 在 Game 视图中等待开场动作播放：主角从课桌醒来，随后 Q 走近；大约数秒后对白界面出现。第一次运行脚本编译或场景加载较慢时请多等一会儿。
5. 结束体验时，再点击一次 **▶ Play** 停止游戏。**停止 Play Mode 会丢弃本次运行中未保存的场景编辑改动**；不要在 Play Mode 中把临时运行状态当作场景编辑结果保存。

> Unity Editor 中的 **Play Mode 就是当前最直接的游戏实测方式**。目前不需要命令行启动，也不需要先创建 `.exe`。若要生成独立构建，请先确认当前构建目标和资源状态；构建流程尚未作为本指南中的已验证步骤。

## 游戏操作：照着走一遍

开始前先用鼠标点一下 **Game** 视图，让它获得键盘焦点。对白和属性面板用鼠标点击；移动和互动用键盘。

| 阶段 | 怎么做 | 预期看到什么 |
|---|---|---|
| 开场 | 点击 Play 后先观察，不需按键 | 主角从桌上醒来，Q 主动走近，然后开启对白 |
| 阅读对白 | 点击右侧的「继续阅读」；到选项时点击其中一项 | 叙述和对白连续显示在屏幕右侧滚动区域，不是对话气泡 |
| 调整字号 | 对话时点击 `A−` / `A+`；可在右侧对白区域滚动查看 | 文字大小变化；历史内容在同一文本流中滚动 |
| 创建角色 | 对话结束后，在左侧属性面板点击各项的 `+` / `−` | 五项属性：体魄、精神、智性、感知、想象。每项起始为 1，共分配 7 点，每项上限为 5；点数用完后点「开始」 |
| 移动 | 在 Game 视图获得焦点后按 `W A S D` 或方向键 | 主角按摄像机方向移动 |
| 互动 | 走近人物或物件；看到左下角提示后按 `E` | 与 Q 或附近占位物件开始对话/查看；当前互动内容仍有占位项 |
| 保存 / 读取 | 正常游戏状态下按 `F5` 保存、`F9` 读取 | 显示短暂反馈；只恢复本版本支持的属性和故事标记，不恢复对白阅读位置/历史或场景位置 |

存档写在 Unity 提供的本机 `Application.persistentDataPath` 中，不保存在仓库目录。首次按 `F9` 前要先有可读取的存档；读取失败时看屏幕提示和 Console。要重新测试整个开场，可停止 Play Mode 后再次点击 Play。**不要预期 F5/F9 能保存当前对白滚动位置。**

## 怎么测试：建议顺序

请区分「自动化检查」和「人工实测」。新手第一次可以只完成 A、B 两组。

### A. 每次改动后的快速静态检查（无需安装 Unity）

在仓库根目录打开终端：

```bash
python3 tools/check_unity_project.py --check
python3 tests/ci/test_check_unity_project.py -q
```

Windows 上如果 `python3` 命令不存在，可尝试：

```powershell
py -3 tools/check_unity_project.py --check
py -3 tests/ci/test_check_unity_project.py -q
```

第一条检查 Unity 项目文件/资源结构，第二条测试这个检查器本身。看到退出状态为成功且测试输出 `OK`，只表示上述检查通过，**不等于** Unity 已经成功编译或运行。

### B. 在 Unity 里人工试玩（验证画面和真实操作）

每次完成一个功能或修 bug 后，至少检查与改动相关的路径。新手可按这一整条路径走一次：

1. 打开指定原型场景，确认 Console 没有红色错误。
2. 点击 Play，确认开场不中断、主角起身、Q 走近，右侧对白出现。
3. 继续对白、选择一个选项，确认按钮可点且文字能滚动。
4. 完成属性分配，确认点数归零后能开始；未分配完时不应能开始。
5. 用 WASD 移动，靠近 Q 或一个占位物件，按 E，确认触发相应反馈。
6. 进入可移动状态后按 F5 保存。要核对读取：停止并重新 Play，等开场对白结束、属性面板出现时按 F9；确认面板/属性反映了已保存的角色数据（完整存档会直接进入可移动状态）。
7. 停止 Play Mode，再重新运行，确认开场可重复。

这是**手工检查清单**，不是本项目已经全部通过的证明。遇到问题就记下失败的步骤，并按「遇到问题时怎样排查」收集信息。

### C. Unity EditMode 自动化测试

这些 C# 测试要由指定版本 Unity 运行：

1. 在 Unity 菜单选择 **Window > General > Test Runner**（部分布局可从 Window 菜单搜索 Test Runner）。
2. 在 Test Runner 选择 **EditMode**。
3. 点击 **Run All**，等待全部测试结束；查看失败测试名称、错误和堆栈信息。

也可在 Windows PowerShell 中从仓库根目录运行（把路径换成你电脑上的 Unity 安装位置）：

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe' `
  -batchmode -nographics -projectPath (Get-Location).Path `
  -runTests -testPlatform EditMode `
  -testResults "$env:TEMP\PostSchool-EditMode-results.xml" `
  -logFile "$env:TEMP\PostSchool-EditMode.log"
```

命令行测试需要 Unity 已安装且许可状态正常。打开 XML 和 log 查看结果；只有进程成功退出且结果文件表明测试通过，才可报告 Unity EditMode 测试通过。

### D. GitHub CI 检查

Pull Request 的检查结果位于 GitHub PR 页面 **Checks** 标签。静态检查与 Unity EditMode 是不同作业；Unity 作业是否执行取决于仓库配置和受信任的运行条件。看到 `NOT RUN`、跳过或没有检查，**不能写成通过**。CI 细节见 [docs/UNITY_CI.md](docs/UNITY_CI.md)。

## 遇到问题时怎样排查（不需要会编程）

### 先做这 5 件事

1. **复现一次并记录操作顺序。** 从打开哪个场景开始，到按了什么键、在哪一步出现问题。
2. **区分画面问题和错误信息。** 打开 Unity 的 **Console**（通常是 Window > General > Console），红色项是 Error，黄色项是 Warning；不要因为黄色提示就假设游戏坏了。
3. **先保存证据再清理 Console。** 截取 Game 视图和 Console；点选第一条红色错误，复制完整文字和堆栈。不要只写「报错了」。
4. **记下环境。** Unity 版本（应为 `6000.3.29f1`）、操作系统、场景路径、Play Mode/编辑模式、发生步骤、是否每次都能复现。
5. **只改动明确相关内容，然后重跑检查。** 静态检查、相关 EditMode 测试，以及对应的 Play Mode 手工步骤。不要为了消除提示而盲目删除文件或升级 Unity。

### 常见情况

- **Play 后看不到游戏：** 确认已打开 `PostSchool_Prototype.unity`，查看的是 Game 而不是 Scene 标签，并再次点击 Play。点 Game 视图使其获得焦点；首次脚本编译/资源导入时等待完成。
- **键盘移动没反应：** 先点 Game 视图，再按 WASD/方向键；对白或角色属性面板开启时，玩家移动会暂停，这是预期行为。
- **按 E 没反应：** 先按屏幕提示走近可互动对象。当前互动距离有限，方向/距离不合适时不会触发；需要贴近人物或物件并再次观察提示。
- **F9 提示读取失败：** 先按 F5 创建存档。存档保存在本机 Unity 数据目录，不是项目中的文件；它不保存对白历史或角色在教室中的位置。
- **Console 有红色编译错误：** 先看第一条错误及文件/行号，不要只看后续连锁错误。确认 Unity 版本正确、Package Manager 完成解析；复制完整错误后再处理。
- **中文缺字或 UI 挤在一起：** 截图并报告分辨率、Game 视图比例和缺字内容。这类显示问题需要作为字体/布局问题处理，不是使用者操作错了。
- **Python 测试无法运行：** 先确认安装 Python 3，并在仓库根目录执行；`pytest` 不是这些快速检查的必要依赖。
- **视觉校准场景不是开场游戏：** `Post School > Visual Calibration > Build Calibration Scene` 会生成/覆盖 `Assets/Scenes/VisualCalibration.unity` 和校准材质；它用于视觉审查，不是当前游玩入口。不了解用途时不要运行此生成菜单。

### 报告 Bug 时复制这个小模板

```text
标题：用一句话说清楚问题
Unity 版本：6000.3.29f1 / 其他（请填写）
系统：Windows / macOS / Linux（请填写）
场景：Assets/Scenes/PostSchool_Prototype.unity / 其他
复现步骤：1. … 2. … 3. …
预期结果：我认为应该发生什么
实际结果：实际发生了什么；是否每次都发生
证据：Game 截图、Console 完整错误/堆栈、测试结果
```

## 项目文档地图

- [docs/CORE_GAMEPLAY_IMPLEMENTATION.md](docs/CORE_GAMEPLAY_IMPLEMENTATION.md) — 当前玩法实现、架构、已验证项和限制。
- [docs/UNITY_TECHNICAL_BASELINE.md](docs/UNITY_TECHNICAL_BASELINE.md) — Unity/URP 技术约定。
- [docs/UNITY_CI.md](docs/UNITY_CI.md) — CI 与自动化测试说明。
- [docs/VISUAL_CALIBRATION.md](docs/VISUAL_CALIBRATION.md) — 单独的视觉校准场景及尚未完成的视觉验收。
- [docs/ASSET_PROVENANCE.md](docs/ASSET_PROVENANCE.md) — 美术、代码和对白来源/授权记录。
- [docs/PROJECT_RELATIONSHIP.md](docs/PROJECT_RELATIONSHIP.md)、[docs/PORTING_MATRIX.md](docs/PORTING_MATRIX.md) — 与 Godot 原项目的关系及哪些内容可复用。
- [AGENTS.md](AGENTS.md) — 项目约束与开发流程；[docs/AGENT_PROTOCOL.md](docs/AGENT_PROTOCOL.md) — 协作和 PR 报告规则。

## 文档同步规则（项目继续推进时必须遵守）

每次合并会改变所有者如何打开、体验、操作或验证项目的改动，都要在同一个变更/PR 中更新对应说明，不能等所有者踩坑后再补：

- 新场景或入口改变：更新本文件的场景路径和启动步骤。
- 按键、UI、对白流程、存档规则改变：更新「游戏操作」及「怎么测试」表格。
- 新增/删除测试或 CI 行为改变：更新快速检查、EditMode 和 CI 段落及 `docs/UNITY_CI.md`。
- 新功能、占位内容、已知问题或验证状态改变：更新「项目现在是什么状态」和相关实现文档；写明**实际跑过**的版本、命令/步骤与结果，并保留未验证事项。
- 资产来源/许可改变：更新 `docs/ASSET_PROVENANCE.md`。
- 完成文档更新后，至少执行 `python3 tools/check_unity_project.py --check`、`git diff --check`；人工步骤若可用还应在指定 Unity 版本中照说明走一遍。若没有运行 Unity，要明确写「未运行」，不要假称通过。

## Copyright and assets

本项目没有一份覆盖所有内容的通用开源许可证。原创素材仍归其所有者保留；第三方依赖和资产遵循各自条款。不要因为素材存在于 Godot 仓库就直接导入；先核对来源、授权、商业使用与再分发权，并在 [docs/ASSET_PROVENANCE.md](docs/ASSET_PROVENANCE.md) 登记。
