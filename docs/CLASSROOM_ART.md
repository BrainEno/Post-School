# 开场教室与主角：美术与技术说明

本文件记录本次「开场教室场景 + 主角建模」的实现依据、构图规则和尚未验证的部分。中文为主，规格数字与代码/JSON 中的值一致，改代码时请同步改这里。

范围：分支 `arena/17fb699e-post-school`。作者授权本项目在视觉上比 Where Is School **更大胆**，两个项目为互不相干的平行项目，因此只借鉴"低饱和色块、硬边低多边形、尺度失衡的超现实感"这一**视觉气质**，不共用世界观、不导入任何参考模型/贴图/文案，也不复用其角色造型。

## 一、唯一真源：`Assets/Resources/World/classroom_layout.json`

教室的每一个位置都在这个 JSON 里，运行时构建器（C#）和离线平面打样（JS）读同一份数据。手改坐标后两边会一起变，不会出现"代码里一套、图上另一套"。

坐标约定（写进 JSON 的 `note`）：

- `+z` 指向黑板墙，`-z` 指向出口墙；`+x` 为东，`y` 向上；单位米。
- 房间 `12.4 × 16.0 × 3.5`，`centerZ 3.5`（所以 z 从 −4.5 到 11.5），墙厚 0.26。
- 门窗用**沿墙距离** `centerAlong` 表示：前/后墙从 `minX` 起往 `+x` 量；西/东墙从 `minZ` 起往 `+z` 量。这条约定只写在 `ClassroomLayout.WallAlongOrigin` 一处。

硬性清单（CI 会逐条查）：

| 项目 | 值 |
|---|---|
| 座位 | 6 列 × 4 排 = **24**，列 `[-4.9,-3.5,-2.1,2.1,3.5,4.9]`，排 `[2.9,4.5,6.1,7.7]` |
| 黑板 | 5.6 × 1.45，sill 1.0，粉笔槽 0.16 |
| 讲桌 | (0, 9.9)，1.7 × 0.8 × 1.02，配一把转过去的教师椅 |
| 窗户 | **2 扇**，都在西墙（沿墙 11.0 / 7.0，宽 2.3，sill 0.95，高 1.9） |
| 门 | **2 扇**：后墙员工门（沿墙 1.7，1.05 × 2.2）+ 前墙出口（沿墙 2.2，1.35 × 2.35） |
| 醒来桌 | (0, 0.5)，2.2 × 1.6 × 0.82，椅背朝后拖开并转过 34° |
| 吊灯 | 4 盏；第 4 盏倾斜 4.5° 且降到 3.05 m |
| 地面粉笔圆 | 圆心 (0, 0.5)，r = 1.45，16 段——与醒来桌同心，是刻意的梦境处理 |

两扇门为什么这样放：后墙门若不与黑板错开，验证会报"门开在黑板里"；前墙门若不避开醒来桌，会报"门开在主角桌上"。这类检查在 `ClassroomLayout.Validate`（C#）和 `tools/check_unity_project.py::check_classroom_layout`（CI）里**各写一遍**，因为 CI 里没有 Editor，C# 规则跑不起来。

## 二、后现代／超现实怎么落地

不从"堆装饰"入手，而是从**构图规则**入手。每条规则都能在代码里找到对应实现：

1. **尺度失衡**：16 m 进深对 24 个小桌，前区留出大片空场，主角度假想机位永远能看见"人很小、房间很大"。醒来桌被孤零零放在前区正中，与自己的粉笔圆同心——像平面图上的一处标注，而不是一件家具。
2. **错位的秩序**：24 张桌严格整齐，但第 07 张悬空 0.24 m、第 18 张椅子沉进地面 0.20 m。只破坏两处，秩序感才站得住。
3. **手绘线压过实体**：粉笔圆、黑板上的圆圈/箭头/四道竖线、墙上三行看不清的字、四角的短刻线，全部用薄片几何体画出来，不做贴图。真实感来自"这些是有人画上去的"，而不是材质精度。
4. **悬空的非照明物**：四盏吊灯里有一盏倾斜 4.5° 并下垂到 3.05 m；另有两条吊绳从天花板垂下来，却什么也没挂。
5. **门是墙上的洞，不是贴纸**：每面墙按开口切成若干块实体板（`WallSegment`），门洞是真的空腔，门框、观察窗、把手都是独立构件。窗外与观察窗后面用同一块"纸白"发光面，暗示外面有光但没有具体景象。
6. **低饱和固定色块**：整套颜色收在 `ClassroomArtPalette`，灰玫瑰砖红、雾蓝、鼠尾草绿、米白、深藏青、赭石，全部低饱和，**只有主角身上的袖标是饱和红**——从任何角度看，人都是画面里唯一"不属于这里"的色点。

## 三、代码结构

| 文件 | 职责 |
|---|---|
| `Assets/Scripts/World/ClassroomLayout.cs` | JSON → 强类型模型 + `Validate()`：24 座、门窗不穿墙角、门不撞黑板/主角桌、主角桌前留 0.70 m 站位 |
| `Assets/Scripts/World/ClassroomBuilder.cs` | 纯几何装配：墙体与开口、门/窗构件、黑板与粉笔痕、讲桌、24 套桌椅、醒来桌、吊灯、地面与墙面标记、碰撞盒 |
| `Assets/Scripts/World/ClassroomArtPalette.cs` | 唯一调色板（结构色 + 三处点缀色 + 主角专属色） |
| `Assets/Scripts/World/WorldSurfaceLibrary.cs` | 唯一材质所有者，实现 `IClassroomSurfaceProvider`；负责释放 |
| `Assets/Scripts/Character/HumanoidRig.cs` | 19 骨骼骨架 + 两套比例表（主角 / Q），带 `Validate()` |
| `Assets/Scripts/Character/ProceduralSkeleton.cs` | 骨架数据、绑定姿势、线性混合蒙皮（LBS）与权重校验 |
| `Assets/Scripts/Character/SkinnedMeshBuilder.cs` | 环形拓扑管体 / 椭球 / 四边形 / 盒体，按骨架权重写入顶点 |
| `Assets/Scripts/Character/SkinnedFigureFactory.cs` | 造型本体：把比例表变成一张带 4 个材质子网格的蒙皮网格 |
| `Assets/Scripts/Character/HumanoidPose.cs` | 姿势表（骨骼 → 局部旋转）+ 根高度偏移；全部关键姿势在这里 |
| `Assets/Scripts/Character/HumanoidClip.cs` | 手写关键帧片段：`WakeFromDesk` / `StandingIdle` / `ClassmateIdle` / `ClassmateWalk` |
| `Assets/Scripts/Character/HumanoidAnimator.cs` | 播放器：采样、crossfade、`Time.unscaledDeltaTime`（对话暂停时也能演） |
| `Assets/Scripts/Character/CharacterSkin.cs` | 五槽材质（body/hair/coat/shirt/accent）+ 两角色色板 |
| `Assets/Scripts/Character/ProceduralStudentModelFactory.cs` | **LEGACY**，只给 `VisualCalibrationSceneBuilder` 当对照，游戏内不再调用 |

`PrototypeSceneBootstrap` 的改动：删掉旧的硬编码灰盒教室（4 张桌 A–D、占位黑板）、旧 14 槽 `MaterialAt` 调色板、旧 `BuildProtagonistPoseRig`；改为读布局 → 建教室 → `SkinnedCharacterBuilder.Create` 生成主角与 Q → `HumanoidAnimator.Play(WakeFromDesk)` → 起身结束接 `StandingIdle`。序章不再手写"转五个枢轴"的协程，改为等片段的**自己的时钟**（`while (_protagonistAnimator.IsPlaying ...)`），所以改关键帧时间不会和剧情脱节。Q 的走近动画只驱动根节点位移，腿部动作交给 `WalkCycle` 片段——这是蒙皮角色的正确分工（片段管姿势，代码管位移）。未命名同学、书包、三角尺、出口四个占位交互物**重摆了位置**，全部挪到前区空场，不再和 24 套桌椅或讲桌重叠。

碰撞：地板、天花板、四面墙各一组盒体，**两个门洞都留空**，玩家可以走进任何一个门；门楣上方有盒体挡住越顶。**24 套桌椅现在都有实体碰撞**（每张桌一个盒体、每把椅子一个盒体，覆盖座面到椅背），醒来桌与讲桌同样是障碍。碰撞体挂在摆放组上而不是每个零件上，物体数量不涨。代价是玩家会被椅子挡住，这是作者要的"真的能撞到东西"。

角色碰撞：主角用 `CharacterController`（2 m 胶囊，中心在原点，脚在 −1 m）；Q 的交互触发器是一个 `CapsuleCollider`（`isTrigger`），挂在 Q 根节点下，不参与物理推挤。

## 四、角色：真骨骼 + 手写关键帧（本轮重写）

旧的"部件堆叠"模型（约 40 个胶囊/盒子挂在 5 个枢轴上）已被删除。现在两个角色共用一套 **19 骨骼蒙皮骨架**，造型是程序化生成的**单张网格**：

| 项 | 值 |
|---|---|
| 骨骼 | `Hips, Spine, Chest, Neck, Head, Shoulder_L/R, UpperArm_L/R, Forearm_L/R, Hand_L/R, Thigh_L/R, Shin_L/R, Foot_L/R` |
| 每顶点影响 | 最多 4 个，关节处按距离混合 |
| 网格 | 一个 `SkinnedMeshRenderer`，4 个子网格 = 皮肤 / 头发 / 外套 / 衬衫（脸部画线与领口在最后一格） |
| 片段 | `WakeFromDesk` 3.40 s · `StandingIdle` 6.00 s 循环 · `ClassmateIdle` 7.05 s 循环 · `WalkCycle` 1.10 s 循环 |
| 时间源 | `Time.unscaledDeltaTime`，对话把 `timeScale` 压到 0 时动画照常 |

为什么这么做：参考项目里的人只是整体剪切平移，没有骨骼；真蒙皮让肘、膝、腰在弯折时是**曲面**而不是两个圆柱互穿，也让"同一个片段在两个人身上跑"成为可能——差异全部来自比例表，不来自第二套动画。

## 五、主角：年龄"卡在中间"怎么做出来

设定是"成年之后因为说不清的原因回到学校，并迷失其中"。所以造型上**不能**给出任何一个能让人一眼定年龄的特征。做法是让两套比例互相打架：

| 部位 | 选择 | 为什么 |
|---|---|---|
| 总高 | 1.72 m，**7 头身** | 儿童约 6 头身，成人约 7.5；7 正好落在两边之间 |
| 肩 | 肩点间距 0.426 m，窄 | 少年感 |
| 胸廓 | 深 0.235 m，成年体积 | 与窄肩冲突 |
| 前臂与手 | 相对更长、指节有画线 | 成年人的骨架 |
| 服装 | 长外套（成人行为）配学校衬衫领（学生制度） | 一件衣服里放两个人 |
| 脸 | 长椭圆脸 + 成年下颌 + 少年颧骨；**无**胡茬、**无**婴儿肥、**无**妆容 | 抹掉所有年龄线索 |
| 面部裂缝 | 从头顶裂到脸颊的一条铅笔线，旁边还有一条**对不上的**第二条线，左眼是一条画的缝、右眼是一个空洞 | 两张不同年龄的脸共用一个头；这是"迷失"的视觉锚点 |
| 头发 | 两侧短、头顶长且往错的方向翘，硬边几何片 | 不是校规发型，也不是职场发型 |
| 袖标 | 右臂一道饱和红袖标，没有编号 | 像学校发的，但哪个学校都没发过 |
| 待机 | 一次性 6 s 循环：呼吸、左右看、重心偏移永不落定；起身后先"没站稳"再进入待机 | 人是"被放在这里"的，不是"站在这里"的 |
| 骨架 | 与 Q 完全相同（19 骨），差异只来自比例表 | 一套片段两个人用，人物差异是**身体**的而不是动画的 |

性别与族裔刻意不编码：没有可读的第二性征，没有发型/肤色/服饰的文化标记。玩家看到的是一个"学生/成年人"的模糊体。

### Q：同桌

Q 和主角共用骨架与片段，但比例表换成 `HumanoidProportions.Classmate()`：更矮（1.615 m）、头骨更小（6.87 头身）、肩更窄、颈更长。造型层再补三点：

- **脸几乎是空的**：两条水平的眼线 + 一条完全水平的嘴，没有裂缝、没有瞳孔。
- **头发太整齐**：平贴的一片，没有翘起的方向——Q 身上唯一"符合规矩"的地方。
- **待机不对**：`ClassmateIdle` 里没有呼吸，头是**阶梯式**转动（0.15 s 内几乎瞬移 22°），身体其余部分不动。主角的待机每帧都在飘，Q 的待机在大部分时间里完全静止，然后突然换一个方向。

骨骼标尺（米，地面为 0；`HumanoidProportions` 里的值，`Validate()` 会核对脚踝与头顶）：

```
主角 Proportions.Protagonist()
  踝 0.070   膝 0.460   髋关节 0.870   骨盆(hips) 0.940
  肩 1.370   颈 1.420   头骨 1.480     头顶 1.725（申报 1.72）
  1.72 m / 7.02 头身

Q Proportions.Classmate()
  踝 0.065   膝 0.425   髋关节 0.800   骨盆(hips) 0.860
  肩 1.260   头顶 1.615
  1.615 m / 6.87 头身
```

每个骨骼是"自己那一段的起点关节"：`Thigh` 是髋关节（不是髋的高度），`Shin` 是膝盖，`Foot` 是脚踝，脚底在踝下 `ankleHeight`。这条约定让"转髋"和"抬腿"是两件事——早期版本把 `Thigh` 放在膝盖高度，抬腿会从膝盖开始折，`Validate()` 现在会拦住这种错误（关节顺序 ankle < knee < hip）。

## 六、离线验收：`docs/images/classroom-plan-proof.js`

```bash
cd /tmp && npm i @napi-rs/canvas      # 依赖不入库
NODE_PATH=/tmp/node_modules node docs/images/classroom-plan-proof.js \
  Assets/Resources/World/classroom_layout.json docs/images
```

产出 `docs/images/classroom-plan-proof.png`：俯视平面（+z 朝上、不镜像）+ 正立面（朝向黑板，含 0.5 m 高度刻度与 1.72 m 主角剪影）。脚本同时重算所有布局约束，有问题会写进图里并以退出码 1 结束。

**这不是 Unity 渲染。** 它只证明坐标、开口、间距、遮挡关系自洽；材质、光照、相机实际观感、中文字体都不在其中。图中圆点代表主角醒来的位置，红色是"被破坏/不可能"的标注（悬空桌、下沉椅、倾斜吊灯）。

## 七、验证状态（诚实清单）

已运行并通过：

- `python3 tools/check_unity_project.py --check`：236 个文件通过，含新增的教室布局规则。
- `python3 tools/skinned_figure_preview.py`：把 `HumanoidRig` / `HumanoidPose` / `HumanoidClips` / `SkinnedFigureFactory` 的数学**逐条移植到 Python** 后出图 `docs/images/skinned-figure-preview.png`（九宫格：绑定姿势、起身四个关键帧、走路四个相位、Q、比例对照）。这份图暴露并修掉了三个真实错误：髋骨被放到膝盖高度、趴桌时双腿甩到桌外、走路时脚离地 3 cm。
- 姿势的接地是**解出来的不是调出来的**：每个关键帧的根高度都由 FK 反解到"最低踝 = ankleHeight"，并写成 EditMode 测试（`EveryWakeKeyKeepsTheSolesOnTheFloor` / `EveryWalkKeyKeepsAtLeastOneSoleOnTheFloor`）。
- `python3 -m unittest discover -s tests/ci -p 'test_*.py'`：29 项通过，其中 9 项是新增的布局检查用例（少一座、少一扇门、开口穿角、门开在黑板/主角桌、主角桌前站位不足、缺 status）。
- 平面打样脚本自检：24 座 / 2 窗 / 2 门 / 4 灯，0 problem。

**没有**运行：Unity 导入与 C# 编译、EditMode/PlayMode 测试、Play Mode 观感、光照与后处理参数、中文字体解析。新增的 25 个 EditMode 断言（骨架完整性、权重归一、LBS 形变方向、片段校验、循环首尾一致、接地）在 Unity 里从未执行过；`skinned-figure-preview.png` 只是同一套数学的**离线复算**，不是渲染。本沙箱没有 Unity 6000.3.29f1。所有"好看"的判断都需要在 Editor 里复核；第一次打开场景时按 `Start()` 的顺序会先建教室再建玩家，任何布局校验失败都会抛异常并指出具体字段。
