# Pull to refresh 内部机制与维护说明

## 1. 上游基线与职责映射

AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 与 Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/pulltorefresh/PullToRefresh.kt` 内容相同。

| 上游符号 / 职责 | 本地类型 / 成员 | 对应说明 |
| --- | --- | --- |
| `PullToRefreshStateImpl` | `PullToRefreshState` | Float Animatable、默认弹簧、距离与运行状态 |
| `PullToRefreshSampleCustomState` | 派生 `PullToRefreshState`、受保护 `AnimateToAsync` | 自定义操作复用相同的 Animatable 和挂载生命周期 |
| `PullToRefreshDefaults.IndicatorBox` | `PullToRefreshIndicatorBox` / 内部 Layout | 软尺寸、仅顶部裁剪、图层平移、条件阴影 |
| `Indicator` / `CircularArrowProgressIndicator` | `PullToRefreshIndicator` / `PullToRefreshArrow` | 箭头与现有 CircularProgressIndicator Crossfade |
| `LoadingIndicator` | `PullToRefreshLoadingIndicator` | 现有 ContainedLoadingIndicator 的确定/不确定重载 Crossfade |
| `ArrowValues` / `drawArrow` | 同名内部计算和 Skia 绘制 | Float 运算、超阈值张力和物理像素箭头 |
| `PullToRefreshBox` | 同名 ContentControl | 正文与默认/自定义 Indicator 槽，自动连接唯一纵向物理视口 |
| `PullToRefreshModifierNode` | `PullToRefreshScrollBehavior` / `PullToRefreshScrollParticipant` | 共享 Float 距离算法，显式与合成 pre/post，UI pre-fling、刷新请求及回弹 |

## 2. 关键机制与架构契约

State 保存原始 Float，不额外钳制到 0–1。两个动画方法使用 Animatable 的默认弹簧，保持速度和取消语义；
它们不是 Material MotionScheme 动画。显式 Snap 可在未挂载时执行。消费方共享窗口根下的子作用域，最后一个消费者离开时取消运行，保留值，重挂后可继续操作。

`PullToRefreshState` 可派生，三个操作均为 virtual；`AnimateToAsync` 接受有限动画规格，默认 null 仍走原默认弹簧。
此非破坏性组件扩展在剩余移植任务授权内实现，不改变现有 `State` 属性类型，也不引入公共共享状态框架。
上游自定义状态样例的 HighBouncy 阈值 spring 可直接用派生类表达；默认隐藏、Snap、速度、通知及共享消费者取消保持原实现。
历史上此选择仅覆盖复用距离 Animatable 的自定义运动，为避免新增公共状态框架而未提供任意状态接口。
2026-11-09 按本轮维护者对新增 API/破坏性迁移的明确授权修订：新增组件专属 `IPullToRefreshState`，
直接表达固定上游接口的两个只读值和三个 suspend 操作；以 INotifyPropertyChanged 对应快照状态观察，
Task/CancellationToken 对应调用方协程。无需引入通用状态框架，因此原先延后任意外部状态的理由不再阻碍实施。
`State` 类型拓宽为接口，内置类型仍实现它并保留原生命周期；普通状态的资源由应用拥有。
指示器只订阅/退订，滚动策略传递自己作用域的取消令牌。内容的 Animation 优先级绑定接受任意通知源。
空属性名通知代表全部属性变化。迁移和示例见 README。

图层位移为 `fraction * round(maxDistance * density) - measuredHeightPx`，再转换到 DIP。布局尺寸保持不变；
裁剪只限制 y≥0，不能使用 ClipToBounds 把下方内容和侧边阴影裁掉。背景不应用 Surface 的 tonal elevation。
Compose 的 LTRB 极值转换成 Avalonia 的 xywh 时需为宽度和变换留下浮点余量，避免 Skia 溢出后整层消失。
尺寸、默认圆角、颜色、高度和 Effects spring 从主题提供，不在属性元数据复制默认值；Expressive 尺寸和颜色引用 LoadingIndicator Token。

刷新 Crossfade 与箭头阈值透明度复用 ComposeAnimation。透明度目标在 fraction≥1 时为 1，否则为 .3；同一侧连续改变距离不能重启动画。
箭头复用 `AnimatedState` 的 AfterFrame + Restart：按 `animateFloatAsState` 的 SideEffect 提交顺序，
在旧动画当帧采样后取最新目标；帧间多次跨越阈值只提交最终目标。中断先取消旧任务，再从当前显示值
以零速度和新首帧启动，不沿用裸 Animatable 的旧速度/时间基准；距离状态自己的可中断弹簧语义不受影响。
旧内容保留到 Crossfade 结束才卸载，取消其持续动画。动画对 Avalonia 属性以 Animation 优先级绑定，释放恢复底层属性源。
Expressive 确定模式在 fraction>1 时整体额外旋转 `-(fraction-1)*180°`，模式切换创建各自的 LoadingIndicator 状态。
其内部画布保持 LoadingIndicator Token 的 requiredSize，在小父约束内居中溢出，由外层形状裁剪。
Crossfade 启用 Avalonia 完整图层透明度合成；内容仅在透明度小于 1 时按图层尺寸裁剪，复现 Compose Auto 离屏层。
卸载先隐藏并断开拥有的 Crossfade，再移除子树，避免子动画在已释放的作用域上继续启动。

滚动 behavior 在实际密度中累计 Float 像素，threshold 取整后计算 .5 拖动倍率和张力，公开入参/返回值为 DIP 与 DIP/s。
pre 仅消费向上的 UserInput，post 消费剩余输入并发布比例；side effect、禁用或 State 动画中不消费。
释放不检查 enabled，严格 `adjustedDistance > threshold` 才发出请求；向上速度或未拉动时消费为零。
刷新状态变化按重组提交边界合并，旧帧先采样，随后执行阈值/隐藏动画；finally 保留上游清零和阈值赋值顺序。
显式 behavior 不注册 ScrollViewer；自动 Box 的数值参与器在组合线程消费位移，通过有序快照在 UI
发布相同状态。首次越阈值的 touch/pen 输入仅扣除固定 Core 的 18 DIP slop。Double 协议出口夹取
Float 舍入产生的微小超额，内部距离运算不变。共享参与与异步释放见[决定 §39](../../../../docs/DECISIONS.md#39-物理滚动共享参与与异步释放)。
Box 默认拉伸内容以给 Avalonia ScrollViewer 有限视口，这是相对于 Compose 默认 TopStart 的布局适配，
不改滚动算法；外部可覆盖内容对齐。自定义 Indicator 拥有自己的状态绑定与 MaxDistance。
State 替换保留累计距离、不 Snap 新状态；取消旧本地 scope 并用代次隔离迟到任务属于框架生命周期适配，
不声称固定上游 update 本身执行取消。刷新事件中同步移除或换源后，旧回调不再动画新状态。
已开始的拖动取消遵循固定 Core `DragGestureNode.processDragCancel` 的零速度 onDragStopped，
故拉动未越阈值会回弹、超阈值仍请求刷新。仅有实际异步释放消费者时启用这一捕获丢失路径；
卸载或显式 CancelScroll 仍废弃事务，不从已卸载 owner 触发请求。

箭头保留上游不寻常的两个运算：外层 rotate 的参数是未乘 360 的 rotation；箭头局部角度为 endAngle 减物理像素 strokeWidth。
不能因视觉直觉将它们改为其他角度。Skia 图元在实际密度的物理像素中构造，并消费合成层 CurrentOpacity。

## 3. 已确认决定与已知差异

- **授权范围**：本轮维护者明确授权缺失组件、API、必要共享能力及历史决定修订；未新增公共共享状态框架。
- **尚未实现**：自动保存/恢复适配；自动连接逻辑、双轴或多个并列视口。自定义回弹可通过派生或外部状态表达。
- **显式容器形状**：本轮剩余移植授权下新增 IndicatorBox.Shape，直接映射固定上游该函数的 shape 参数，经共享 Surface 统一背景、阴影和内容裁剪。内部 Layout 的顶部 clip、位移和条件阴影不变。内置两种 Indicator 通过继承获得此本地便利入口；其 Compose 快捷函数没有 shape 参数，双端参考使用 IndicatorBox 自定义内容组合。
- **未经验证**：实体触屏/触控笔、屏幕阅读器客户端和非 Windows 平台。原生 raw-touch 现已覆盖，不能推广为实体设备验收。
- **已接受差异**：没有为本组件新增像素或时序容差；共享适配仅在已有决定的适用范围内复用。

**未解决观测**：回弹途中在 209ms 卸载，Compose 在该帧采样后重组移除，保留 fraction `0.44828412`；
Avalonia 立即卸载，保留最后 208ms 的 `0.45967421`。两端任务均取消、停止动画、无剩余图片或捕获错误。
`pull-refresh-scroll.detach` 保留该精确差异，未删除卸载帧、延迟所有事件或修改比较规则；这不是已接受差异。

**自动接入的原限制与修订**：原来 SmoothScroll 12.0.0.1（固定 nuspec commit
`429c3c685a1e630337024c2500a9814b96fcc10c`）只有同步 pre/post 和惯性后的 OnIdle，且单个参与槽
已由 AppBar 占用，故只能提供显式 behavior。这一原理由保留。2026-11-09 本轮明确授权后，实际导入
该固定源码，补齐异步释放事务和内部统一参与者所有权，完成自动 Box，未用 OnIdle/ScrollChanged 冒充释放。
回调前排空位移快照，等待 UI 状态操作后返回剩余速度；新输入、换源和卸载取消旧事务。祖先 pre、正文、
反向 post 的次序适用于标准 AppBar 共存；逻辑/双轴/歧义视口明确不连接。普通 Core 滚轮默认不发 pre-fling，
沿用这一边界，不将滚轮 idle 解释成刷新。卸载末帧差异仍保留，不延长已卸载控件的生命期来补采样。

## 4. 验证入口

2026-11-08 `pull-shape-final` 完整重跑 75 例：4 equal、68 withinTolerance、3 different，全部重复稳定。
新增 16 个 `cut.*` 全部在既有容差内、重复/额外截图稳定，
没有执行错误。覆盖逻辑切角与 RTL、四个拉动距离、动画状态切换、刷新及中途反向；已检查实际刷新截图。
三个真实模板行为测试另验证 inherited Shape、路径属性变更、清除、距离平移和卸载清理。
原有自定义状态精度、卸载保留值及切换图像差异仍保留，不因新增用例通过而改写为整个族已一致。

- `tests/Material3Expressive.Tests/PullToRefreshTests.cs`：距离、动画中断、共享消费者、换源、透明度、Crossfade、资源更新、尺寸约束及卸载。
- `PullToRefreshCases.cs` / `.kt`：传统/Expressive、距离阶段、明暗、RTL、受限空间、颜色与距离覆盖、自定义内容及刷新/反向/卸载；含半透明绘制和帧内目标合并。
- `./tests/VisualParity/parity.ps1 self-test --family pull-to-refresh`；实际结果以生成报告为准。
- Catalog：`PullToRefreshDemo`，示范真实自动正文、显式控制距离、外部状态与刷新状态。
- 自定义运动：`pull-to-refresh.custom-motion.*` 覆盖传统/Expressive 的高回弹、反向/Snap 和卸载重挂；`pull-refresh-scroll.custom-motion.*` 验证滚动策略调用派生操作。行为测试另检查显式取消、共享消费者及任务释放。

自定义运动验证的当前结果：指示器 59 例、滚动 17 例均无捕获错误且重复/额外截图稳定；原有指示器 53 例仍在既有门禁内，
滚动原有 14 例中 13 例在门禁内，保留上文卸载差异。新增九例仍标记 different：自定义 Tween 的距离尾差约 10⁻⁷、
非帧时刻卸载的保留值差异，以及圆箭头在高回弹跨越阈值时的透明度提交差异。Expressive 的 bounce 时间线全部截图在容差内，
圆箭头 96ms 的距离精确相同而图像超差，不能归入数值尾差。行为测试 29 项、整仓 Release 构建通过。

**帧末提交能力已实现**：当前 `pull-frame-before` 再次复现帧内 DistanceFraction 更新晚提交一帧。
本轮依据用户扩大授权重新决定，使用共享 `AnimationScope.WaitForFrameEndAsync` 区分当前批次内与帧间修改，
随后读取最新透明度目标；不按 IsAnimating 猜测、不补减固定时长，也不重写弹簧或目标合并规则。
固定 AndroidX/Core 的 PullToRefresh.kt 内容相同，AnimateAsState.kt 的 SideEffect 是提交依据。
独立作用域测试和真实 BouncyState 越阈值测试通过；API、取消与职责范围见 [共享决定](../../../../docs/DECISIONS.md#帧末目标提交边界)。
`pull-frame-after` 的 96ms 图像从超差变为既有容差内，降采样最大通道差 27 → 2；不是严格像素相等。
原自定义 Tween 数值尾差与卸载末帧问题没有由此消除，仍分别报告。
当前 `pull-frame-current` 的 59 例无执行错误、全部重复/额外截图稳定：4 equal、49 withinTolerance、
6 different（自定义状态的数值/卸载及个别切换图像差异）；圆箭头 bounce 的 15 张图全部在既有门禁内。
`pull-scroll-frame-current` 的 17 例同样稳定且无错误：2 equal、11 withinTolerance、4 different，
保留自定义回调/程序化/释放用例和既有 detach 差异。两族均未改变用例时间线、规格、容差或参考基线。

Windows Skia 双端 `20261104-070918-1b23c57b`：指示器 53/53 用例在既有容差内，结构读数相等，重复捕获稳定、错误为零。
显式滚动 `20261104-070928-773f13b1`：13/14 用例在容差内；剩下的卸载保留值差异见上文，14 个均稳定且无捕获错误。
Windows Catalog 已实际操作阈值处释放、越阈值刷新、完成隐藏、明暗切换和小窗口滚动；按钮模拟 nested-scroll 输入，不代表真实触摸验证。
Release 整仓构建通过；控件库 1958/1958（其中本组件 26 项）、ComposeAnimation 935/935、VisualParity 基础设施 124/124 通过。
Crossfade 绘制顺序修正另检查既有消费者：SegmentedButton 的反向切换通过；TimePicker `selection-reverse.ltr` 在修正前后均存在差异，未将其记为通过。
未通过改变参考提交、依赖锁或像素容差获取这些结果。

## 外部状态验证（2026-11-09）

新增 `external-pull-state-delivery` 18 例，三个容器都覆盖独立可观察状态、跨阈值、内容切换/反向、
帧间合并、换源、卸载期间更新及重挂。18 例零执行错误、重复稳定；16 例在既有图像门禁内。
圆箭头换源用例在 160ms 图片 different，其余几何和状态读数一致。

已检查两端截图并定位固定 `CircularArrowProgressIndicator`：`remember { derivedStateOf { progress() ... } }`
不以 progress 为 key，换 state 后透明度仍观察首次函数捕获的旧状态；绘制与位移则读取新状态。
本轮维护者对语义决定的授权下，保留本地原有“全部呈现跟随当前 State”的行为，不让已替换的 ViewModel
继续参与透明度或保持订阅。此处是明确选择的上游偏离，失败用例保留，不更改参考或容差。
另一个失配为 Expressive 在 145ms 的反向 crossfade 图片，尚未接受；其他时刻通过。
16 例通过不能推导所有自定义动画、旧有卸载末帧差异已解决。

行为验证含内置与外部状态、三种模板、换源退订、全属性通知、运行状态的消费门槛、异步取消；
与日期 formatter 合计 42 项通过。该轮未包含自动 nested-scroll；当前自动容器验证见下文。

本轮接口迁移后的原有 `pull-state-interface-regression` 75 例：4 equal、68 withinTolerance、3 different，
均重复稳定且执行错误为零。different 为 circular.custom-motion.state 的 257ms 箭头截图，以及
circular/loading.custom-motion.state-detach 的卸载回弹状态/图像；后两项的历史边界仍见上文。
此处保留真实差异，不把接口行为测试通过推广为全部动画轨迹对齐。

**自动容器与释放验证**：`pull-box-final` 使用真实两端触摸、相同颜色和默认开启的正文惯性，
共 15 例、225 个采样，零捕获错误；2 equal、5 withinTolerance、8 different。
圆形/Expressive 的释放、取消与 RTL 拉动均通过现有门禁；阈值、请求次数和可见距离一致。
新的原生触摸测试覆盖 1×/2× slop、正文先消费/向上先回收、禁用、换源、视口替换、
Scaffold AppBar 共存、捕获取消与卸载；生命周期测试另覆盖忽略取消的外部状态、同步重入及 Float/Double 边界。

未解决的 8 例分别为两种指示器的正文/反向惯性、卸载与刷新 Crossfade。正文/反向的本地 Offset
受原 SmoothScroll 合成计时影响，重复/额外截图不稳定，不能宣称该物理惯性轨迹已验证一致。
卸载仍是 Core 在 208ms 采样后移除、本地立即移除（保留 fraction .5046552 / .7345495）；两端均停止动画。
刷新状态与距离相等，但 176–800ms 的 Crossfade/不确定进度图像仍超差；已查看实际图像，未接受为视觉偏离。
其他 11 例重复稳定。原始 `pull-box-first`、`pull-box-slop`、`pull-box-delivery` 和 `pull-box-matched-inputs`
保留输入阈值、执行屏障、配色及惯性配置的证据，不以改时间或容差取得新结论。

箭头本身的剩余中断已由既有 AfterFrame + Restart 解决：旧 `pull-arrow-restart-state` 的 257ms
最大 1× 通道差从 22 降到 6；同组 bounce/conflation/progress 三条回归也全部在既有门禁内且稳定。
新自动释放的 208ms 同样通过。先前 WaitForFrameEnd 修的是提交批次，本次复用 Restart 补齐取消重启，
不改变共享算法、动画参数或距离状态的速度接续。
