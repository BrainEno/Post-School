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
| 窗户 | **2 扇**，都在西墙（沿墙 11.0 / 7.0，宽 2.3，sill 0.95，高 1.9）；`treatment` 分别为 `dusk`（裸窗）与 `curtain`（被幕布遮住） |
| 门 | **2 扇**：后墙员工门（沿墙 1.7，1.05 × 2.2）+ 前墙出口（沿墙 2.2，1.35 × 2.35） |
| 醒来桌 | (0, 0.5)，2.2 × 1.6 × 0.82，椅背朝后拖开并转过 34° |
| 吊灯 | 4 盏；第 4 盏倾斜 4.5° 且降到 3.05 m |
| 地面粉笔圆 | 圆心 (0, 0.5)，r = 1.45，16 段——与醒来桌同心，是刻意的梦境处理 |
| 前墙 | **不存在**。`frontWall.exists = false`，整面由一幅 10.6 m 宽、3.42 m 高、垂到地面的幕布代替 |
| 门外 | 街道在教室地面下 **4.2 m**；集装箱盖比教室地面低 **5.7 m**，蓝色，12 × 6 m，高 2.0 m |
| 晾衣绳 | `lineY = 1.0`（世界高度），跨 x −9…9，8 件衣服，在集装箱上方 |

两扇门为什么这样放：后墙门若不与黑板错开，验证会报"门开在黑板里"；前墙门若不避开醒来桌，会报"门开在主角桌上"。这类检查在 `ClassroomLayout.Validate`（C#）和 `tools/check_unity_project.py::check_classroom_layout`（CI）里**各写一遍**，因为 CI 里没有 Editor，C# 规则跑不起来。

## 二、后现代／超现实怎么落地

不从"堆装饰"入手，而是从**构图规则**入手。每条规则都能在代码里找到对应实现：

1. **尺度失衡**：16 m 进深对 24 个小桌，前区留出大片空场，主角度假想机位永远能看见"人很小、房间很大"。醒来桌被孤零零放在前区正中，与自己的粉笔圆同心——像平面图上的一处标注，而不是一件家具。
2. **错位的秩序**：24 张桌严格整齐，但第 07 张悬空 0.24 m、第 18 张椅子沉进地面 0.20 m。只破坏两处，秩序感才站得住。
3. **手绘线压过实体**：粉笔圆、黑板上的圆圈/箭头/四道竖线、墙上三行看不清的字、四角的短刻线，全部用薄片几何体画出来，不做贴图。真实感来自"这些是有人画上去的"，而不是材质精度。
4. **悬空的非照明物**：四盏吊灯里有一盏倾斜 4.5° 并下垂到 3.05 m；另有两条吊绳从天花板垂下来，却什么也没挂。
5. **门是墙上的洞，不是贴纸**：每面墙按开口切成若干块实体板（`WallSegment`），门洞是真的空腔，门框、观察窗、把手都是独立构件。窗外与观察窗后面用同一块"纸白"发光面，暗示外面有光但没有具体景象。
6. **低饱和固定色块**：整套颜色收在 `ClassroomArtPalette`，灰玫瑰砖红、雾蓝、鼠尾草绿、米白、深藏青、赭石，全部低饱和，**只有主角身上的袖标是饱和红**——从任何角度看，人都是画面里唯一"不属于这里"的色点。

## 二·五、缺掉的那面墙：幕布、黄昏、六米

这一节记录本轮新增的构图决定，因为它是整个开场里唯一一个**把房间打开**的动作。

**为什么缺的是前墙（−z / 出口侧）。** 需求里"身后没有墙也没有窗户"的"前"有歧义。取"前墙 = 出口侧"这一解释，理由有三：出口门本来就在这面墙上，门洞天然是"人容易从那里掉下去"的位置；两扇窗都在西墙，于是"被帘子遮住的窗"和"被幕布代替的墙"可以共用同一套布料语言，遮住的窗成为缺失墙的伏笔；平面图上，一面虚线加一块布比在实墙上开一个六米高的洞更好读。**若作者不同意这个解释，只需要改 `windows[i].treatment` 两个字符串，墙体本身不用动。**

**构图规则（都能在代码里指到）**

1. **房间是暖的，外面是冷的。** 室内全部沿用既有低饱和色（米白墙、鼠尾草、赭石）；室外只用平涂色块，天空是**六条几何色带**（`DuskSkyHigh` → `DuskLow`），没有渐变 shader、没有贴图——这样离线校样图能读到同一份颜色。
2. **全场景唯一的饱和色是集装箱。** `ContainerBlue #2F6E8C` 只出现在它身上，用来在"一片灰紫的城中村"里一眼指出落脚点。主角袖标的 `#B8442F` 是室内唯一饱和色，两个色点在同一画面里互相指认。
3. **晾衣绳是"有人在住"的唯一证据。** 八件衣物是薄片加轻微扭转（`LaundryLine.garments`），不做布料模拟；读的是剪影，不是褶皱。
4. **幕布用两种颜色做出折痕。** 26 折，交替 `Curtain #B8AE9A` / `CurtainFold #9C9280`，卷帘轨道与下摆各一条，门口留一道黑缝——那道缝是"后面不是墙"的唯一暗示。
5. **被遮的窗用同一块布，同样垂到地。** `window_west_south` 的幕布与前一模一样，读者第二次看到它才会意识到前墙那块是同一件东西。
6. **落差是量出来的，不是写死的。** `fall.roomFloorAboveStreet = 4.2`，`groundY = -4.2`，`DropMetres() = room.height - (groundY + containerHeight) = 5.7 m`。早期版本把 `groundY` 写成 0，落差算出来只有 0.9 m——**这张校样图第一次跑就把它抓出来了**。

**判定规则（`CurtainFallRules`，纯函数，可离线测）**

| 选择 | 检定 | 阈值 |
|---|---|---|
| 先掀开看 | 感知 | ≥ 2 才看清落脚点 |
| 不看就走出去 | 体魄 | ≥ 4 |
| 看过再走出去 | 体魄 | ≥ 3（看得见盖子，目标变低） |
| 观察的代价 | 腿软概率 | `8% + (感知−1)×10%` → 8/18/28/38/48% |

腿软仍会落在盖子上，但是以"摔"的姿态（`FallOutcome.CrashedOnContainer`）；体魄没过同理。**体魄是这个体系里唯一能接住下落的属性**——五个属性里没有敏捷，这一点写进了常量注释。随机只出现在"腿软"一处，且用注入的 `rollPercent` 委托，便于测试。

**坠落是两套系统分开做工。** 姿态（`HumanoidPose.CrashOnContainer` / `LandingBrace`）只管身体，`CurtainScene` 的协程只管位移（`HumanoidAnimator.PlaceAt` 逐帧推），剪片本身**不含世界位移**。第一版让剪片的根偏移同时承担位移，两套系统都在动，落点会双倍——这条写进了 `HumanoidClip.FallFromTheCurtain` 的文档注释。

## 三、代码结构

| 文件 | 职责 |
|---|---|
| `World/CurtainFallRules.cs` | 幕布场景的全部数值与判定（纯函数，无 Unity 依赖除 `Mathf`） |
| `World/DuskCityBuilder.cs` | 窗外世界：天空色带、落日、街、五栋楼、晾衣绳、蓝集装箱 |
| `Prototype/CurtainScene.cs` | 幕布交互：三选项、抬幕布、坠落协程、结果卡 |
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
| `Assets/Resources/Dialogue/classroom_teacher_n.json` / `_revisit.json` | N 的两段对白（第一节课 / 之后的短对话），`status: owner_authored_excerpt`，待作者审定 |
| `tools/teacher_n_proof.py` | 她的证明图：蒙皮复算 + 排版，出 `docs/images/teacher-n-proof.png` |

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

### 幕布场景的离线校样：`tools/curtain_scene_proof.py`

```bash
python3 tools/curtain_scene_proof.py           # → docs/images/curtain-scene-proof.png
# 中文字体：CURTAIN_PROOF_FONT=/path/to/NotoSansSC-Regular.ttf
# （沙箱里用 npm pack @expo-google-fonts/noto-sans-sc，取 400Regular 的整字 TTF）
```

六格：**A 幕布垂着 / B 掀开后 / C 平面 / D 裸窗 / E 被遮的窗 / F 规则清单**。和平面打样一样，它读 `classroom_layout.json`，颜色从 `ClassroomArtPalette` 抄一份（改色时两边一起改）。

它存在的理由只有一个：这个沙箱没有 Unity，而"幕布是不是真的到地、门外是不是真的没墙、落差是不是真的六米"是**可以被数值验伪**的。第一次运行就证明了这一点——落差当时算成 0.9 m，图上的人物落在与教室地面几乎齐平的集装箱上，一眼就不对；`fall.roomFloorAboveStreet` 因此才被加进布局并写进 `Validate` / CI。

**它仍然不是截图。** 图里没有 Unity 的光照、没有字体解析、没有摄像机、不能证明任何一行 C# 能编译。要判断"好不好看"，只能在 Editor 里看。

## 七、N：代课老师

她是第三个角色，也是第一个**成年人**。她进场的整条链路（造型 → 骨骼姿势 → 片段 → 走位 → 交互 → 对白）都是数据驱动的，没有任何一处为她写第二套骨架或第二套动画。

| 项 | 值 | 在哪 |
|---|---|---|
| 身高 | 1.650 m，**7.37 头身**（主角 7.02、Q 6.87） | `HumanoidRig.TeacherN()` |
| 肩宽 | 0.320 m（主角 0.41），头因此显小 | 同上 |
| 头长 / 半径 | 0.224 / 0.079 m | 同上 |
| 胸深 | 0.106 m | 同上 |
| 骨架 | **同一套 19 骨**，同一 LBS 权重场 | `ProceduralSkeleton` |
| 造型 | 单张网格第 4 个 `FigureRole`：齐耳短发（bob）、开衫、衬衫 | `SkinnedFigureFactory` 的 `role == FigureRole.TeacherN` 分支 |
| 姿势 | `TeacherAtPodium()` 原地站着、双手撑讲台 | `HumanoidPose` |
| 片段 | `TeacherAtPodiumIdle()`，3.6 s 循环，三拍 | `HumanoidClip` |
| 走位 | 后墙员工门 → 沿黑板墙 → 讲台后 | `classroom_layout.json` 的 `teacher` |

**为什么她值一个 `FigureRole` 而不是"改一改 Q"。** 三个角色共用骨架与片段，差异全部来自比例表；但"成年人"不能靠缩放得到。她的肩宽 0.320 m 比主角的 0.406 m 窄 0.086 m、头身比 7.37 比主角的 7.02 高出 0.35；手臂却是按成年人给的——前臂 0.265 m、手 0.235 m，而她的身高只有主角的 0.959，所以手臂相对她自己的身体是**偏长**的，这正是她能不弯腰就把手腕搭到 0.44 m 外讲台上的原因。这些数字合起来才是"她比学生更像一个已经定型的人"。造型层再叠三件事：齐耳短发（壳比颅骨宽、从眉线后退、下颌处切平，加两片侧板把耳朵露出来）、开衫下摆落在大腿中段且**不到膝**（`SkinnedCharacterTests.TheTeachersCardiganHangsBelowHerHipsAndStopsAboveHerKnee`）、以及脸上唯一有完整五官的一张脸（眉、下眼睑、低于鼻底的嘴、嘴角向下 2 mm、鼻翼两条软纹）。

**色阶是她的辨识度。** 三个角色是三个明度键：主角深（外套灰值 0.430）、Q 中（0.475）、N 亮而冷（开衫 0.645）。她的头发也刻意不是最深的（0.300，主角 0.160、Q 0.227）——深色 bob 会和两个学生的后脑勺在远处合成同一个剪影。这条规则写成 EditMode 测试（`HumanoidRigTests.TeacherIsTheBrightColdValueKeyOfTheCast`）。

**片段的三拍，以及那一段"没有事发生"。** 0.7 s 点头（颈 18°、头 14°、根下沉 20 mm）、2.0 s 右手离开讲台翻名册（上臂 −62°、前臂 −78°、颈转头 14°）、3.4 s 抬头看教室。**1.0–1.5 s 与 2.5–2.9 s 是回到基准姿势的空拍**：整条循环里她不动的时间比动的时间长，因为"手势加不成掌控"正是这个角色的演法。她的肩膀全程不随呼吸起伏（主角的待机会），这一条也写进了片段注释。

**走位不写在代码里。** `teacher.entryPath` 三个点从员工门沿黑板墙到讲台后，`stand` 是她的标记点，`standYaw = 180`。玩家看见的是 `PrototypeSceneBootstrap.AnimateTeacherEntry()` 按路径长度与 `entrySeconds` 推根节点、由 `WalkCycle` 片段管腿；停下后 0.45 s 转身，再切到 `TeacherAtPodiumIdle()`，并 `CaptureBaseHeight()`（讲台姿势会抬髋 12 mm，基准高度要在切片段之后取，否则她在原地上下跳）。到达后才置 `Objective: teacher_arrived`，`RefreshTeacherPresence()` 据此开关交互体——所以读档回到"她已经到了"的房间，人也站在那里。

**她能被交互到。** 根节点上一个 `isTrigger` 的 `CapsuleCollider`（中心 0.82 m、半径 0.45 m、高 1.64 m）+ `Inspectable`，走上去按 E 就进对话，和 Q 的入口是同一套。**注意 `_conversationTarget` 没有改成她**：Q 的对白入口不变，N 走的是自己的两个 `DialogueRunner`。

**对白路由。** 序章结束前任何交互都还是序章（Q）；序章之后 `ResolveScene()` 看 `Recollection: n_first_class_heard`：没有就给 `classroom_teacher_n`（她的第一节课，一次），有就给 `classroom_teacher_n_revisit`（之后每次都是同一段短对话）。两份 JSON 的 `status` 都是 `owner_authored_excerpt`——**这是我按作者此前的中文语气续写的排演稿，尚未经作者逐句审定**，与序章那份从 Where Is School 逐字导入的摘录不同。她的第一节课有一个五选一的提问节点（B 老师去哪了 / 课本还没发 / 生态系统 / 受精卵 / 什么都不问），每个分支都会回到同一条"自习"收尾，其中"课本还没发"会置 `Objective: textbook_missing`。

**她的证明图：`docs/images/teacher-n-proof.png`**（脚本 `tools/teacher_n_proof.py`）。七格：A 三人同一比例尺（含 1.72 / 1.61 / 1.65 m 与 7.02 / 6.87 / 7.37 头）、B 讲台循环四个关键帧（正面视角看不见的点头与手向前，用同一套 FK 算出的毫米数标在下面）、C 她的脸放大 3 倍叠上标准的脸部比例带（眼线 0.47–0.53、鼻底 0.30–0.36）、D 三套色阶与灰阶条、E 平面走位、F 参数表、G 开场时间线。

```bash
python3 tools/teacher_n_proof.py docs/images/teacher-n-proof.png
# 依赖 matplotlib + numpy（只在生成时安装，不入库）；无中文字体依赖，图内文字全英文
```

**这张图是蒙皮画出来的，不是拼贴。** 脚本为每个角色建一套与 `ProceduralSkeleton` 同构的骨骼表，把静默态网格点按最近两根骨骼做线性混合蒙皮（混合带 0.06 m），再用正交投影画轮廓——所以 B 格里弯腰时的袖子、下摆会跟着弯，而不是几个圆柱互穿。**它有一处诚实的缺陷**：正交投影没有深度排序，只能按材质手动定画序（裤/靴 → 开衫 → 皮肤 → 头发 → 衬衫），并且视角固定在正面 0°。绕 y 轴转 35° 试过，轮廓在袖口与下摆处会撕开，已回退；代价是**点头和手向前在图上根本看不见**（这两种位移主要在 z 轴），所以 B 格改用文字标注实测位移（点头 头 −5.7 mm；翻名册 手 +138 mm 向上、31 mm 向内；抬头 头 +2.6 mm）。这些数字由画图同一个 FK 现算现写，不是手抄的。

## 七·二、造型细化（本轮新增）

### N 的开衫门襟：从"两条竖线"改成真正的 V 领

原来门襟是**两条平行的竖条**（x = ±0.026，各 11 × 115 mm），盖在开衫表面 1 mm 处。平行竖条在视觉上读作**大衣的门襟（placket）**，不是开衫——开衫的定义性特征就是领口敞开、露出里面的衬衫。

改成 V 领的过程里，**画图抓出了两个用代码审阅发现不了的错误**：

1. **倾角符号反了。** 第一版 `leanSign = +1`，结果是两条边在**领口收拢、在腰部张开**——一个倒过来的 V，读作燕尾服下摆。`docs/images/cardigan-v-proof.png` 第一格就是这个错版。
2. **门襟条超出顶点，两条边在腰部以下交叉。** 第二版把条长给成 115 mm、中心给在 20 mm 高度，算出来的内下角在 **−29 mm 处穿过中线**，形成一个明显的 X。第二格是这一版。
3. **衬衫只补了一部分缝隙。** 单块 60 mm 的方格放进从 77 mm 收到 0 的缝里，上面留边、下面戳穿——读作"打了一块补丁"。第三格是修正版：**三段叠起来的衬衫**，每段宽度按**它自己下边缘处的缝宽**取，所以永远窄于它所在的位置。

最终参数是**解出来的**，不是调出来的：门襟半长 86.2 mm、中心在脊椎上方 (28.2, 53.0) mm，使每条门襟的**内下角恰好落在 (0, −29 mm)**（两条边的交点，也是开衫重新合拢的高度），外上角落在领口 135 mm。**因为两条内边都收敛到同一点并在那里结束，它们不可能交叉。** 缝隙宽度：领口 **77 mm**、胸 24 mm、腰 0。

`tests/ci/test_cardigan_opening.py` 把这三条不变式钉死：**向上变宽**、**在腰部汇合且不交叉**、**每段衬衫既不戳穿也不覆盖不足**。第三条在写作时就抓到了我自己的一个 5 mm 溢出。

**必须说明的：** 这张图是从 `SkinnedFigureFactory.cs` **直接读常量**画的（正则读 `const float`，读不到就退出并报错），所以它不会在代码改动后还继续"正确"。但它**仍然不是 Unity 渲染**，开衫的 `squashZ` 压缩、光照、材质都没在图上体现。

## 七·五、序章演出层（本轮新增）

序章的 19 个节点文本**一个字都没动**。新增的是"演出"——谁在看谁、什么时候转开——而不是台词。

**为什么要加。** 序章原本只有对话框在动：房间里三个人都站着，姿势由 `ClassmateIdle` 一个循环撑完全程。最刺眼的是 `q_who_look` 这一行——作者写的舞台提示是"（Q 看向讲台上的三角尺）"，**却没有任何东西真的让 Q 去看**。一句话描写一个动作，不等于把动作做出来。

**接线方式。** `DialoguePresenter` 新增 `NodeShown` 事件，在 `Begin()` / `Advance()` / `Choose()` 之后各报一次当前节点 id（同一节点不重复报）。`PrototypeSceneBootstrap.BeginDialogue()` 订阅它，用一张 `switch` 表把 **节点 id** 映射到一个**一次性片段**：

| 节点 | 节拍 | 片段 |
|---|---|---|
| `q_1` | 他开始搭话，人已经转过来 | `ClassmateTurnBeat` |
| `q_who_look` | 看向讲台上的三角尺 | `ClassmateGlanceBeat(θ)` |
| `q_zero` / `q_blank_1` / `q_absent_1` | 视线离开主角、转到黑板方向（这几句他是在背书，不是在说话） | `ClassmateAwayBeat` |
| `q_exam_end` | 审问结束，收回普通学生的站姿 | `ClassmateIdle` |

**为什么是查表而不是逻辑。** 节点 id 若被改写或删除，对应的节拍**静默消失**——少一个手势，绝不会卡住对话。这是刻意选的失败方式。

**`q_who_look` 的转角是算出来的，不是调出来的。** Q 走到 `(-1.9, 0, -0.1)` 停下，`AnimateQApproach` 结束时让他**面向主角**（世界朝向 108.9°），而三角尺在 `(2.1, 0, 2.9)`（朝向 53.1°），所以需要转 **−55.8°**。这里踩过一个坑：我第一版拿他**创建时**的 128° 当基准，算出来 −74.9°，**差 19°**，视觉上就是"看着那东西旁边"。转角由两个物体坐标算，且把整条骨链（髋→脊椎→胸→颈→头）按 28/24/16/17/15 分摊——只转脖子要么够不着，要么像猫头鹰。

**原型之外没有改的：** N 在走廊入场、主角的头（属于玩家，任何节拍都不夺取控制权）。

**这一层完全没有跑过。** 没有 Unity，`NodeShown` 的触发时机、`ClipFinished` 交还 idle 的衔接、转角的观感全部只在纸面上推过。见第八节。

## 八、验证状态（诚实清单）

已运行并通过：

- `python3 tools/check_unity_project.py --check`：**265 个文件（186 个在 `Assets/`）通过**，含"前墙缺失"十一条规则与 N 的走位检查。
- `python3 tools/check_case_labels.py`：**PASS**，86 个 `case 类型.成员` 标签全部指向已声明类型。**这个检查是本轮补的，因为它逮到了一个真的编译错误**（见下）。
- `python3 tools/cardigan_v_proof.py`：从 `SkinnedFigureFactory.cs` 读常量出图 `docs/images/cardigan-v-proof.png`。**它抓出三个真实缺陷**（倾角反向、门襟交叉、衬衫盖不满），见第七·二节。
- `python3 -m pytest tests/ci -q`：**58 passed**（其中 `test_cardigan_opening.py` 3 项是新增的门襟不变式，`test_check_case_labels.py` 3 项是新增的类型存在性检查）。其中 `MissingFrontWallTests` 11 项（前墙存在、幕布只垂一半、幕布太窄、出口门挪走、集装箱不在门下方、集装箱在室内、街道只有一栋楼、晾衣绳低于盖顶、两扇窗都被遮、未知 `treatment`），`TeacherBlockingTests` 10 项（路径穿桌、标记点在讲台里/座位上/房间外、未知门、走太短/太久、只有一个点、没有 `teacher` 段、线段裁剪不是包围盒）。
- `python3 tools/skinned_figure_preview.py`：把 `HumanoidRig` / `HumanoidPose` / `HumanoidClips` / `SkinnedFigureFactory` 的数学**逐条移植到 Python** 后出图 `docs/images/skinned-figure-preview.png`（九宫格：绑定姿势、起身四个关键帧、走路四个相位、Q、比例对照）。这份图暴露并修掉了三个真实错误：髋骨被放到膝盖高度、趴桌时双腿甩到桌外、走路时脚离地 3 cm。
- 姿势的接地是**解出来的不是调出来的**：每个关键帧的根高度都由 FK 反解到"最低踝 = ankleHeight"，并写成 EditMode 测试（`EveryWakeKeyKeepsTheSolesOnTheFloor` / `EveryWalkKeyKeepsAtLeastOneSoleOnTheFloor`）。
- 本轮的排版校样 `docs/images/curtain-scene-proof.png`（脚本 `tools/curtain_scene_proof.py`，读同一份 JSON）：A 幕布垂着 / B 掀开后 / C 平面 / D 裸窗 / E 被遮的窗。**它第一次跑就抓出了真实缺陷：落差曾被算成 0.9 m。**修完是 5.7 m。
- 平面打样脚本自检：24 座 / 2 窗 / 2 门 / 4 灯，0 problem。
- N 的证明图 `docs/images/teacher-n-proof.png`（`tools/teacher_n_proof.py`）已重渲染多轮并逐格目视复核；它抓出的真实缺陷有三条：发壳纵向半径 0.44 头长时**盖不住颅顶**（顶 0.106 m < 颅顶 0.112 m，画出来是秃的，已改 0.47 并写进注释）、平面投影没有深度排序会把脸画在头发上（改为按材质定画序）、A 格里用横条表示讲台会被读成"第三条手臂"（改为手部圆点 + 高度标注）。

### 本轮必须点名的一件事：`QuizScene` 从来没被定义过

`PrototypeSceneBootstrap.cs` 里有 **14 处** `QuizScene.…`（`_activeScene` 字段、`TitleFor`、`ResolveScene`、`RunnerFor`、`BeginDialogue`、两个 `case` 分支、`q_who_look` 的过滤条件），而**全仓库没有任何地方声明 `enum QuizScene`**（`git grep` 在 HEAD 与全部 ref 上都查不到）。

也就是说：**在我动手之前，这个工程就编不过。** 之前每一轮"CI 全绿"都不代表能编译——`check_unity_project.py` 查的是 GUID/.meta/命名/JSON/布局，`pytest tests/ci` 查的是几何与走位，两者都**不对 C# 的类型存在性发表任何意见**。这个缺陷一路穿过所有检查。

本轮补齐了 `Assets/Scripts/Prototype/QuizScene.cs`（三个成员：`ClassroomOpening` / `TeacherFirstClass` / `TeacherRevisit`，与既有用法一一对应）与它的 `.meta`，并加了 `tools/check_case_labels.py`：所有 `case 类型.成员` 的限定名必须是 `Assets/Scripts/` 里声明过的类型。这个规则零误报（`case` 标签只能写枚举成员），且带负向自测（`tests/ci/test_check_case_labels.py` 用一个只含非法标签的临时目录验证它会 FAIL）。

**但这仍然只是一个静态推断**：三个成员是**按调用点反推**的，没有 Unity 能确认没有第四个成员被别处用到。

**没有**运行：Unity 导入与 C# 编译、EditMode/PlayMode 测试、Play Mode 观感、光照与后处理参数、中文字体解析。新增的 EditMode 断言（骨架完整性、权重归一、LBS 形变方向、片段校验、循环首尾一致、接地、`CurtainFallTests` 11 项，以及 N 的 `TheTeacherIsNotACloneOfTheClassmate` / `TheTeachersCardiganHangsBelowHerHipsAndStopsAboveHerKnee` / `TheTeachersPodiumLoopHasThreeBeatsAndAPause` / `TheTeachersHandsSitAtPodiumHeightNotAtHerSides`；全仓 115 处 `[Test]`/`[TestCase]`）在 Unity 里从未执行过；`skinned-figure-preview.png` 与 `curtain-scene-proof.png` 都只是同一套数据的**离线复算**，不是渲染。本沙箱没有 Unity 6000.3.29f1。

**本轮新增代码一次都没编译过。** 沙箱里没有 `dotnet`、`mcs`、Unity，所以下面这些只能靠人工核对与 CI 的静态检查（`check_unity_project.py` 查 GUID/命名/JSON/布局，不查 C# 语义）：

- `CurtainScene` 与 `PrototypeSceneBootstrap` 的接线（触发体位置、`Initialize` 参数、角色控制器的开关时机）从未运行。
- `HumanoidClips.FallFromTheCurtain(drop)` / `CrashLanding(drop)` 的落地键时刻按落差缩放，**没有在动画上看过**。
- `HumanoidAnimator.PlaceAt` 是新增 API，父空间换算与 `BasePosition` 的语义只在纸面上推过。
- N 的接线同样从未运行：`AnimateTeacherEntry()` 的三段路径插值、0.45 s 转身 Slerp、`CaptureBaseHeight()` 的时机、`Inspectable` 触发体与玩家胶囊的重合。
- **本轮新增的面部几何与演出层同样没编译过。** 具体是：`AddFaceMark` / `TryFaceSurface` / `HeadShells` 三个 helper 与 17 处调用点；`AddCap(..., rimFirst)` 与 `AddBoxFans()` 的绕序修正；`ClassmateTurnsToYou` / `ClassmateTurnsAway` / `ClassmateGlance` 三个姿势与三个一次性片段；`DialoguePresenter.NodeShown` 事件与 `AnnounceNode()`；`PrototypeSceneBootstrap` 的节拍表、`SetSquareYawFromQ()`、`HandleQBeatFinished`。全部只经过人工核对、离线几何复算（`tools/mesh_visibility_check.py`）与静态检查。五官贴面与绕序有**离线数值证据**（见第六节），演出层**没有任何证据**——它只能靠 Editor 里按 Play。
- 新增的 EditMode 断言 `EveryDrawnFaceMarkSitsInFrontOfTheSkinItIsDrawnOn` 与 `PrologueStagingTests`（4 项）也从未执行。前者用 ±1.5 mm / ±4 mm 的取样窗比对同一高度上的皮肤 z；最紧的一处间隙只有 0.46 mm（N 的眼皮），所以这个窗口是按"最小间隙"选的，不是随手写的——但它仍然只是**离线复算过的假设**，不是跑过的结论。
- **`VisualCalibrationSceneBuilder` 没有加 N。** 标定场景里只有主角、Q 和趴桌姿态的主角；N 的造型只在游戏场景里出现。这是有意的（标定场景是为了对照"手工角色 vs 纸片学生"，加第三个人会糊掉这个对照），但如果要审她的造型，需要在 Editor 里手动 `SkinnedCharacterBuilder.Create(..., FigureRole.TeacherN, ...)` 或临时加一行。
- 第一次在 Editor 里按 Play 时最可能出问题的地方，按风险排序：① 幕布触发体的尺寸/位置与玩家胶囊不重合，交互拾取不到；② 坠落协程把角色控制器关掉后，落地时重新启用导致 `CharacterController` 弹开；③ 抬幕布是整体上移 `Curtain` 组，门缝黑片会跟着上去，视觉上像"门也升起来了"。这三处都需要在 Editor 里实际看一眼再定。

## 想象中的生物老师 B（本轮，2026-11-26）

`docs/images/biology-teacher-proof.png` 由 `tools/biology_teacher_proof.py` 生成，**离线绘制，不是 Unity 渲染**。形体参数按作者原文逐条落进 `HumanoidRig.TeacherB()`，一条未改：1.64 m、稍驼背、肩膀高、鹰钩鼻、眼小而集中、带帽檐眼镜、白发盘髻。

量出来的结果（面板 F 逐条打印，与 `SkinnedFigureFactory.cs` 的常量一一对应）：

| 作者说法 | 模型测得 |
|---|---|
| 个子小 | 冠顶 1.640 m，7.4 头身（N 1.65 / 主角 1.72） |
| 稍驼背 | Chest 在 Hips 之上 310 mm，而脊柱前倾 16 mm |
| 肩膀高、脖子前探，像站在电线上的鸟 | 头在肩之前 14 mm；肩宽 276 mm（N 320 / 主角 406） |
| 鹰钩鼻 | 鼻尖之下再垂 11.5 mm，前伸 20 mm |
| 眼小而集中 | 两眼相距 46 mm，单眼宽 24 mm |
| 带帽檐眼镜 | 镜圈 46 mm，帽檐遮光板悬在镜面前 12 mm、前倾 24° |
| 白发盘髻 | 发髻与发针在**颅后**，正面看不见——所以面板 C 不画，只在 F 里说明 |

想象里她可以扭曲：`CreateImaginedTeacherB()` 给她 `localScale (0.94, 1.04, 1.02)`、yaw 偏离主角 18°，并另建一份 0.90×0.97 的**重影**（偏移 −0.48 / −0.30 m、yaw −7°），**不给 animator**。面板 D 画的就是这两层。

### 这一轮证明图抓出的两个真实缺陷

1. **脖子从来没被画过。** `tools/teacher_n_proof.py` 的共用部件表里没有脖子，`SkinnedFigureFactory.BuildHeadSkin` 却是有的。结果此前所有离线图（主角、Q、N）都把头颅画成悬在衣领之上，中间一道空隙。这一轮补上了 `Chest → Neck → Head` 的锥形管。
2. **面板 C 的镜框、镜桥、眉宽三处数值与 C# 不符**：帧位置用了 `+π/8` 偏移（C# 用 `seg * π/4` 无偏移）、镜桥宽度按 `0.03·hr` 画（C# 是 `eyeX − 0.28·hr = 0.02·hr`）、眉半宽 `0.16` 画成 `0.17`。已逐项对齐。教训与上一轮相同：**校验器的数值必须从被检查的代码里读**，手抄一遍就等于在验证自己。

### 仍未验证的部分

沙箱里没有 Unity、没有 dotnet：**本轮全部 C# 未编译、未运行**。想象场景的接线（`SetImaginedTeacherVisible`、序章结束自动进想象、结束发点）、`InsightLedger` 的存档往返、`insight:` 效果在 `DialogueRunner` 里的分支，都只经过人工核对与静态检查。面板 D 里的 4% 纵向拉伸在真机上读起来是"不安"还是"失误"，只能在 Editor 里按 Play 才知道。

## 校园外壳：教学楼、北门、两段连廊（本轮，2026-11-26）

按作者给的区划草图扩建。**只做外壳与屋顶**：没有室内、没有楼梯、没有门厅的实现、没有碰撞与导航，没有任何可以走进去的地方。数据在 `Assets/Resources/World/campus_shell.json`，两张图由 `tools/campus_shell_proof.py` 生成，**离线绘制，不是 Unity 渲染**。

### 尺寸怎么来的

一切都在教室自己的坐标系里：+x 向东，+z 向北，教室地面 y = 0，街道在 −4.2 m。教室是 12.4 × 16.0，直接就是 `classroom_layout.json` 的 `room`，没有改。楼层高 4.15 m（顶棚仍是 3.5 m，结构厚度另算），共三层，教室在中层。教学楼是两条臂合成的 L：北翼沿 z = 11.5–21.0 向东伸 20.6 m，南翼沿 x = −6.2–3.4 向南伸 21 m，教室嵌在南翼的东端。

### 五处故意做错的地方

1. **楼板悬空** —— 一块 32 × 22 m 的楼板停在 9.4 m，和楼下同一层高、同一厚度、同一柱网，下面什么都没有。
2. **翼片比墙长** —— 南立面 34 片、北立面 26 片，都伸出墙体两端；北面是奇数，最后只剩半跨。
3. **连廊通向无物** —— 东侧连廊从南翼出去，停在离地 4.2 m 的中庭上空，另一头没有楼。
4. **没有地面的网格** —— 整片场地只是一层线，没有面，线跑过建筑脚下再跑出去。
5. **窗户对不上层** —— 两翼窗带用了不同间距，转角处永远对不齐。

这五条在 JSON 的 `impossibilities` 里逐条写了理由，`tests/ci/test_campus_shell.py` 里每条都有断言守着——有人把它们"修好"了，CI 会直接失败。

### 这一轮证明图抓出的三个缺陷

1. **近平面裁剪写在了屏幕空间**：先把点投影再裁剪，会让一端在相机背后的线段投出一条贯穿整张图的直线。第一版的网格把画面彻底毁掉了。必须先在相机空间裁剪，再除深度。
2. **画面取景被地面网格主导**：网格一直跑到地平线，让它决定裁剪框，建筑就缩成一个点。取景只看实体。
3. **视点不是构图问题**：一开始凭感觉写的五个视点，数值一算 FOV 全在 105°–176°。改成先用脚本反解"框住这栋楼需要多大 FOV"，再写回 JSON，现在全部落在 32°–62°。

### 还没验证的部分

沙箱里没有 Unity、没有 dotnet：**本轮没有写任何 C#**，所以没有可编译的东西，也没有可运行的东西。这不是完成品，是没有落成几何的体块研究。要变成几何，下一步才是 `CampusShellBuilder.cs` 去读这份 JSON——而且那时第一个要解决的问题是**三层楼的外壳怎么和现有教室的墙接上**，因为教室的南墙是那块不存在的墙（幕布），外壳不能把它补上。
