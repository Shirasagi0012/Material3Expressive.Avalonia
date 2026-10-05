# 共享能力的已确认决定

本文件只记录跨组件的共享能力决定。组件行为、适配与确认依据随各组件维护文档保存（已拆分时为 INTERNALS.md，否则为 README）；
通用职责与接入规则见 [ARCHITECTURE.md](ARCHITECTURE.md)，VisualParity 决定见其[测试目录文档](../tests/VisualParity/DECISIONS.md)。

## Padding 消费向嵌套 Scaffold 传播

决定：`MaterialWindowInsets.ConsumePadding` 为 Control 提供逻辑 start/top/end/bottom 的已应用 Padding 输入，
按固定 Compose `consumeWindowInsets(PaddingValues)` 逐层累加，转换为物理边并向后代传播。
输入不自动应用 Padding；Scaffold 按物理边扣除祖先消费量并钳制到零。
原因：现有物理 Thickness 转换不包含祖先消费环境，不能独自保留 Scaffold 的嵌套 Insets 语义。
做法：独立输入与内部继承的物理总量；提供者跟踪父环境、FlowDirection 与重挂载，非提供者直接继承。
组件仍负责选择 Insets 和布局，宿主仍负责提供实际系统 Insets。
范围：本次 Scaffold 及 PaddingValues 消费的共享入口，不扩展为 WindowInsets union、自动系统栏/IME
或完整 Insets 框架，不改变既有 rail/bar 的行为。Scaffold 槽 API 与应用 Padding 的示例见
[组件说明](../src/Material3Expressive/Components/Scaffold/README.md)。
确认：2026-10-03，维护者对具体 API 和 C#/XAML 示例选择“采用推荐 API 和共享 ConsumePadding”。

## 普通协程延时与动画帧时钟

决定：ComposeAnimation 的根 VisualAnimationScope 接入 .NET TimeProvider，子作用域和 Popup 继承；
DelayAsync 使用同一时间源，并连接调用方及作用域取消。保留旧 Initialize 重载默认使用系统时间。
原因：LoadingIndicator.kt 并发运行 spring 与 kotlinx.coroutines.delay(650)，后者不是 animation-core
求解器，也不受动画帧或 DurationScale 控制。用 Tween 或下一帧计时替代会改变循环及稀疏帧行为。
接入：本地 NuGet Runtime/Avalonia/TestSupport 统一使用 0.2.7-alpha。测试使用 ManualTimeProvider，
AvaloniaHost 可显式接入，在送帧前推进延时；未接入的旧用例保持原有时间环境，Draw/Snapshot 仍被动。
范围：普通协程等待与确定的测试时间源；不改写 animation-core 算法，不引入 Compose Runtime。
确认：2026-10-04 维护者在说明协程与 animation-core 职责后明确授权修改 ComposeAnimation。

## 最小交互布局与工具栏对齐读数

决定：IconButton 和 Button 共用已有 ButtonLayout，尺寸 Token 提供可见容器的默认尺寸，
外层默认分配至少 48 DIP；显式 Width/Height、MinWidth/MinHeight 保留调用方约束。
XS class 对应上游显式 size(32dp)，继续以 32 DIP 约束外层；Small 默认容器 40 DIP、外层 48 DIP。
ButtonLayout / FabLayout 提供内部最小交互 top/left 对齐读数，Foundation 合并后代读数，
FloatingToolbar 按固定上游 minimumInteractiveBalancedPadding 动画补齐两轴留白差。
原因：原有 Small IconButton 的 40 DIP 外层尺寸缺少上游的 48 DIP 布局分配，
工具栏也无法读取可见容器和交互分配的差值。这里只预留布局空间，不实现 Android 的额外触屏目标扩展。
范围：Button / IconButton / FAB 的内部读数与 IconButton 布局；其他颜色、描边及选择语义不随之改变。
确认：2026-10-04，维护者接受 FloatingToolbar API 方案第 3 项，并要求完成相关回归。

## Dialog / Popup 的窗口内宿主与模态职责

决定：组件各自持有 Avalonia Popup，显式使用 `ShouldUseOverlayLayer = true`，由原生 Popup 接入
当前 TopLevel 的 PopupOverlayLayer，不创建独立 Window，不引入中央管理器。
内部 ModalPresenter 复用客户区覆盖、焦点约束/还原、Escape/Back 和清理；组件保留自己的状态与动画。
最上层模态按实际宿主顺序判定，子 overlay Popup 通过 PlacementTarget 链判定焦点归属。
基础 Dialog 区分 IsOpen 目标与 IsPresented 实际呈现，DismissRequested 交给调用方决定是否关闭，
退出动画期间保留模态层。普通 Popup 继续直接组合原生能力。
原因：复用 Avalonia 弹层定位与承载，同时支持嵌套菜单和模态组件共享输入语义；普通 OverlayLayer
继续供 ConnectedAnimation 等视觉覆盖能力消费。
范围：基础 Dialog 与 ModalWideNavigationRail 迁移；不替代 Rail 的拖动、遮罩和运动，不包含 TextField。
此项取代 Rail 2026-10-03 决定中的普通 OverlayLayer 宿主选择，其余 Header/State 等决定保持原范围。
确认：2026-10-04，维护者明确选择 PopupOverlayLayer、拒绝独立窗口、认可组件独立持有 Popup，
并授权基础 Dialog 和 Rail 迁移。接口、桌面动画来源及未实现/未经验证项见
[Overlays 说明](../src/Material3Expressive/Foundation/Overlays/README.md)。

## Dialog 的平台宽度和 Escape 时机

决定：Foundation Dialog 新增默认启用的 UsePlatformDefaultWidth，按固定 Core 的客户区短边选择
320 / 440 / 580 DIP 平台上限；显式关闭该属性可保留此前的宽内容行为。
Dialog 系列对未处理的 Escape KeyDown 发送关闭请求，内部控件处理该按键时不发送；
ModalWideNavigationRail 保留现有 KeyDown/KeyUp 配对策略。
原因：固定 Core `ComposeSceneLayerMeasurePolicy.kt` 提供平台宽度限制，Desktop 的
`ComposeSceneMediator` / `BackNavigationEventInput` 在 KeyDown 处理未消费的 Escape。
范围：Dialog、BasicAlertDialog、AlertDialog；不改变 Rail 的动画和关闭时机。
确认：2026-10-04 维护者分别批准默认平台宽度属性及 Dialog 对齐固定 Core 的 KeyDown。
组件继承、内容槽和默认桌面分支的确认见 [AlertDialog](../src/Material3Expressive/Components/AlertDialog/INTERNALS.md)。

## Dialog 的首帧与退出中重开

决定：Dialog 按固定 Core 的 composition / LaunchedEffect 帧顺序启动动画；
退出保留冻结内容和独立动画作用域，重开创建新入场，旧冻结层继续退出。
入场中关闭时保留关闭重组帧的最终入场进度，退出时长使用该 progress × 100ms。
原因：AlertDialog 双端用例确认，提前启动、取消旧退出层及提前取消入场会改变时序与叠层画面。
范围：Foundation Dialog、BasicAlertDialog、AlertDialog；Rail 继续使用自己的动画。
适配：Avalonia 使用实际 RenderScaling 的 RenderTargetBitmap 冻结内容，源矩形使用像素、布局与变换使用 DIP；
局部启用整层透明度合成，保持 Core GraphicsLayer 的背景与内容一起淡入淡出语义。
确认：2026-10-04 维护者批准首帧对齐与保留旧退出层，并要求检查 Foundation 退出缩放异常。

## 聚合 TextStyle 与 Token 层文字单位换算

决定：通过不可变 TextStyle、MaterialText.Style/DefaultStyle、Typography 角色绑定及派生 XAML 扩展，
让普通 TextBlock 消费聚合排版；不要求公开 MaterialTextBlock。DefaultStyle 合并环境，Style 显式选择。
角色保留标量来源并增加聚合 Style Token；Button/Rail 的标签接入同一默认值机制。
sp/em 在 Token 消费位置换算为 DIP，保持资源覆盖与字体缩放的动态性，TextStyle 本身只保存 DIP。
原因：保持 Compose 的样式选择、合并与提供默认值语义，同时通过 Avalonia 属性优先级保留原生覆盖。
范围：聚合 API 与当前可投影字段；未授权用近似行为补齐 LineHeightStyle 等底层排版缺口。
具体使用方式、原生样式就近规则、旧 Button Component 标量槽迁移及未实现项见
[Typography 说明](../src/Material3Expressive/Foundation/Typography/README.md)。
确认：2026-10-03，维护者认可 API 方案并要求落地，同时提出在 Token 层完成 sp/em → DIP 换算。

## ComposeAnimation 颜色转换与原始动画终值

决定：在相邻 ComposeAnimation 修正现有 ColorConverter，保留公开 API 与 double 向量接口，
按固定 Compose 的 sRGB / XYZ / Oklab 路径保留矩阵、Float 边界、两次 Half / 10 位 alpha 存储及舍入。
TargetBasedAnimation 保留原始起点和目标，结束时返回原目标，避免有损颜色往返改变终值。
原因：固定上游 Color.VectorConverter 调用 ui-graphics Color.convert，其中间存储会改变动画颜色；
Rail 反向 65ms 的 R=46/47 已由独立数值复现定位。范围为现有 byte sRGB 颜色 API，未决定扩展 ColorSpace API，
也未授权全局改变 core 的 double 精度或亚毫秒处理；若仍有相关差异，另行提出具体方案。
接入：依照现有本地 NuGet 流程打包 Runtime / Avalonia / TestSupport，版本统一为 `0.2.1-alpha`。
确认：2026-10-03，维护者批准修复方案，并指定“打包版本用0.2.1-alpha”。

## 共享交互反馈与键盘激活职责

决定：`MaterialInteractionController` 的键盘输入处理只维护交互反馈，激活由控件自身负责。
共享控制器不拦截 Enter，不通过 automation Invoke 或其他路径代为点击，也不维护组件类型排除名单。
原因：反馈接入不应改变应用命令的执行时机；接管 Enter 会与原生 Space 和导航项的键盘处理形成多套激活路径。
做法：移除专用 Enter 激活状态和 KeyUp 点击分支，保留 Space / Enter 的按压反馈，
并在释放、失焦或禁用时清理反馈。真实 Compose 参考、采样与比较规则保留，激活时机差异如实报告。
影响与范围：共享控制器的键盘反馈接入遵循上述职责；普通 Button / IconButton 保留 Avalonia 原生激活，
Enter 按下激活与固定 Compose 的松开激活不同，已作为框架适配接受，Space 遵循原生 ClickMode。
导航项已有的释放激活保留；该决定不统一各组件的键盘行为。具体行为见
[Button](../src/Material3Expressive/Components/Button/README.md#默认值与接入)与
[IconButton](../src/Material3Expressive/Components/IconButton/README.md#形状与接入)说明。
确认：2026-10-03，维护者要求“不需要Enter拦截了。然后将决策记录在案”，
并进一步要求移除共享控制器中非必要的 Enter 特殊处理；此决定替代此前在 KeyUp 经 automation Invoke 激活的决定。

## Ripple 首次绘制几何与尺寸动画衔接

决定：RippleAnimation 按固定 Core material3-ripple 在首次绘制（包含零 alpha）锁定起始半径、
无界原点和目标中心；目标半径仍在添加 press 时解析。通常从 Surface 的实际绘制尺寸初始化。
共享控制器增加内部 `InitializeRippleDrawGeometry(Size)`，允许 Switch 在按压测量中提前锁定
这一几何，波纹动画、绘制和取消仍归共享层。
原因：Switch ThumbNode 先测量当前 sizeAnim 值，再启动 pressed SnapSpec；固定 Compose 的
首个 draw 使用该次测量尺寸。Avalonia 可能在首次 render 前已完成多轮布局，用扩张后的尺寸
初始化会使动画后按压的波纹中心偏移约 2 DIP。
范围：只处理有上游首绘测量依据的布局与绘制衔接，不提供新的公开交互 API，不建立组件内 ripple。
其他组件仍在首次 render 初始化，不能未经核验推广 Switch 的提前初始化时机。
确认：2026-10-03，维护者选择“增加内部衔接方法，修复该波纹差异（推荐）”。

2026-10-04 补充，替代上述决定中 Switch 锁定绘制中心的部分：Switch 的无界按压波纹
每次绘制都跟随 Surface 当前中心，与放大、收缩、拖动中的 thumb 及 hover/focus 圈保持同心。
原因：首绘锁定的小尺寸中心会在 thumb 放大后向左上错位，收缩时又向右下错位。
做法：共享控制器增加内部 `RippleFollowsPresenterCenter`，默认 false，仅 Switch 启用；
RippleAnimation 的内部 Draw 接收这一选择，只调整无界波纹的实际绘制中心。
首绘测量仍决定起始半径，目标半径、扩散、透明度、取消与生命周期保持原实现。
其他调用方保留固定 Core 的中心锁定和有界中心漂移，不增加公开 C#/XAML API。
确认：维护者在先前提出的同心优化方案后要求“继续修复波纹的问题”，并提供新的错位截图。

## Surface 圆角描边

决定：内部 SurfaceOutline 对均匀 Thickness 直接移植固定 Compose Border.kt 的物理像素 ceil
与内缩 stroke；描边作为内容上方的视觉层，不消耗内容尺寸。
均匀圆角保留中心线内缩、小圆角减内矩形和大宽度填充；非对称圆角以外轮廓减去完整线宽内缩的
轮廓，各角独立缩减并限制为非负。非均匀 Thickness 保留 Avalonia 原生 Border 路径。
普通 Surface 的零 Thickness 保留无描边语义，Toggle/Split 描边动画的零宽按 Compose 绘制 hairline。
动画层只提供连续线宽、颜色与原始外圆角；物理取整、线宽限制及圆角内缩统一在绘制层执行一次。
公共 API 不变。
原因：Chip 深色静态用例暴露角部差异，已有原生 Border 不符合固定上游的像素取整。
范围：所有 MaterialSurface 调用方；Surface / Button / Chip / ToggleButton / SplitButton 的相关渲染
和回归需要共同检查。
确认：2026-10-03，维护者明确批准共享 Surface 描边修正及 Surface / Button 回归。
2026-10-04 合并 Toggle/Split 时，维护者批准保留共享 SurfaceOutline、接入新描边动画并补齐
非对称圆角算法，避免分支的原生 Border 半线宽处理与共享绘制内缩叠加。

## Core Desktop 原生焦点反馈

决定：共享控制器 Focused 跟随所有原生焦点，包括鼠标和程序化焦点；控件继续负责激活。
指针按下反馈在 Button 原生类处理之后观察，保留 Focus → Press 的交互顺序。
原因：固定 AndroidX 的 RequestFocusOnClick 返回 false，固定 Core Desktop 返回 true；
仅接受 Tab / Directional 焦点会让 Chip 点击释放后错误地恢复 hover elevation。
范围：共享控制器调用方，不改变公开 API 或已确认的键盘激活职责。
确认：2026-10-04，维护者批准按 Core Desktop 修正共享焦点反馈并回归 Button / IconButton / 导航项。
实现同时按固定 Core `Indication.skiko.kt` 过滤 indication 的焦点源：首次鼠标聚焦不进入
状态层，已经显示的键盘焦点层保留到失焦；输入模式改变不合成 Focus / Unfocus。
内部 MaterialInputMode 按 TopLevel 保存输入模式，初始为 Keyboard；Tab 按键及原生 Tab / Directional
导航接入 Keyboard，指针按下/释放或 Pointer 焦点导航接入 Touch，程序化聚焦沿用当前窗口模式。
`button.Focus()` 驱动实际焦点与 elevation，其视觉焦点反馈取决于当时的输入模式；
`button.Focus(NavigationMethod.Tab)` 表达 Avalonia 的键盘焦点导航。elevation 的原始交互源仍保留所有焦点。
子控件冒泡的焦点事件不视为父控件获得焦点；对外 API 不变，没有增加 FocusRing 或 InteractionSource API。
核验 Core 桌面过滤分支后，维护者进一步选择“按 Core 桌面过滤 indication，保留真实焦点/elevation（推荐）”。

## AppBar 与原生滚动接入

决定：标准 Scaffold 模板内置 AppBarScrollScope，自动引用实际呈现栏的 ScrollBehavior；公开 Scope 暂时保留用于显式组合。
公开 AppBar 状态以物理像素表达，真实正文滚动使用下述 SmoothScroll 合成接入。
Scope 只连接其 Scaffold 正文槽内唯一的主 ScrollViewer；内层独立，不建立多个正文连接。
移出、detach、行为或模板替换及新输入取消旧惯性/吸附并释放连接。接入按实际 SmoothScroll presenter
及物理滚动/布局能力判断；Scope 不改写 Template/ControlTheme/Style，不订阅内层 viewer 的模板变化。

正文 touch/pen、捕获接力和惯性由 SmoothScroll 接入；直接拖动栏及其 settle 保留 ComposeAnimation。
原 Rail 的 Lsq2 VelocityTracker 原样移入内部 Foundation/Input，由 Rail 与 AppBar 共用。
没有新增通用输入框架或完整系统 insets 框架；标题和 Bottom 自定义内容的槽规则留在组件 README。
确认：2026-10-04，维护者采用修订后的 AppBarScrollScope 及槽规则，要求保留自定义模板、嵌套和生命周期，
并分别验证组件 parity 与真实桌面输入集成。

2026-10-06 维护者确认 XAML behavior 与 Scaffold 内部 Scope：两个 sealed StyledElement 类型支持对象元素和 markup extension，
栏负责逻辑父关系，Scope 只持引用；按最近 Scope 判断正文所有权。Scaffold 读取实际 ContentPresenter 的根 AppBar，
支持直接栏及数据模板栏。布局合约和 SmoothScroll 消费算法保持原有范围，任意外部布局连接仍未实现。

## 普通物理滚动与 AppBar 的合成接入

决定：普通 ScrollViewer 使用 Material 自有主题与 SmoothScroll presenter，保留 Avalonia 的公开控件和
Offset。标准 Scaffold 的真实 AppBar 与物理正文共享 compositor 上的滚动事务、动态范围和视觉输出；
Material 消费规则及布局知识保留在本库，依赖中只增加通用参与者、数值回读与合成属性支持。
参与者协议位于 Interaction 命名空间，数值输出及合成适配工具位于 Composition 命名空间，仍属于同一个程序集。

此路径使用 SmoothScroll 的惯性，不要求与 Compose 物理曲线一致。顶栏只返回实际消费的位移，边界剩余量
在同一帧交给正文；底栏观察正文消费，正文结束后独立 spring 吸附。直接拖动栏保留已有 UI 输入/settle。
公开 AppBar 状态仍以物理像素表示，内部滚动事务用 DIP；spring 规格在物理像素中求值。

范围：连接物理主视口、内置 behavior、标准栏模板及 Scaffold.ContentPadding 布局。
外层 Scope 不重复接管标准 Scaffold 的正文；逻辑主正文、主正文原生 snap points 和其他布局暂不自动联动 AppBar。
不回退旧正文引擎；使用 SmoothScroll presenter 的自定义模板可在满足相同布局条件时接入。
绑定配置在 UI 线程捕获，compositor 消费快照。公开 Offset 与状态是 UI 回读值，不是跨线程实时接口。
确认：维护者明确选择基于 SmoothScroll、接受小型通用扩展，并授权实现普通物理滚动与真实 AppBar、验证布局衔接。
本阶段未承诺完整通用嵌套滚动引擎、逻辑虚拟化抗阻塞、统一手势框架或新的公开滚动控制器 API。

补充决定（2026-10-04）：保留 SmoothScroll 的原生边界冒泡和触摸捕获接力。主正文内的滚动器独立滚动，
不驱动外层 AppBar；内层到边界后下一笔输入交给主正文，才驱动正文与栏。不增加部分余量或惯性跨层分配。
维护者明确选择了该行为，并审阅同意 ConfigureVerticalScroll 的可选
Func<double, bool>? canConsumeAtBoundary 参数：仅在 UI 线程根据快照查询额外参与者能否消费方向位移，
正 DIP 增加 Offset，查询不改变状态，null/false 沿用原生判界；清除参与者时清除回调。
该参数不传递滚动量、不跨线程读取控件，也不建立共享栏状态或多 tracker 惯性会话。

2026-10-04 补充：维护者批准将已验证的参与者消费接入 SmoothScroll 主路径。
普通指针、直接滚轮和惯性帧共用增量事务，状态不再按参与者存在与否选择两套移动方法；
tracker 统一计算有效范围并提交横纵向位置。普通弹性回弹、参与者纵向夹取与现有 zoom/snap
限制保留；绝对定位及定位动画不进入 pre/post。UI 冒泡查询仍使用只读快照。
2026-10-05 维护者批准完成公开协议和辅助入口的命名迁移：去除实验前缀，使用 AttachVerticalScroll、
ConfigureVerticalScroll、RunOnScrollParticipant、DispatchToUI、ApplyVerticalScrollDelta、
CompleteVerticalScroll 和 CancelScroll。挂载生命周期移入 ScrollPresenter 主文件，删除无调用的
额外正文位移入口及其表达式分支；原型公开惯性启动入口已删除，固定初速度检查由子模块测试调用内部
StartInertia。验证阶段的桌面探针通过路由输入启动惯性，并通过 ProjectReference 消费当前源码；
临时探针及其专用采样钩子现已移除，保留正式回归测试、VisualParity 与 Catalog 使用示例。

本次替代此前暂留实验命名和原型入口的规则；消费顺序、单位、边界冒泡、取消语义及现有 zoom/snap 限制不变。

## 本次保留 Tween / Bezier 精度差异

决定：Chip 移植继续消费 ComposeAnimation 0.2.1-alpha，不修改其 FloatTweenSpec /
CubicBezierEasing 的 double 运算。固定 Compose 的 Float 运算和不同求根路径可造成约
0.0000006 DIP 的 elevation 差异。原始 motion 事实继续精确比较并报告不同。
范围：本次 Chip 及共享回归所观察的数值差异，不接受时序、布局或可见效果的其他偏离。
确认：2026-10-04，维护者在依赖修正与保留差异的具体方案中选择“本次保留并记录该精度差异”。

## 本次保留阴影坐标接入差异

决定：本次不改变 MaterialSkiaShadow 的 DIP 路径及 density CTM，保留其与 Compose Desktop
物理像素路径 / 相应 CTM 的 raster 差异。公共 DIP elevation、光照和布局不变。
原因：透明 AssistChip 的 6 DIP elevation 暴露内部阴影差异；独立 Skiko 原生探针使用同一
轮廓、光照及标志，分别以 DIP 路径 + 4× CTM 和物理像素路径 + 1× CTM 绘制，复现两端结果。
原生 tessellator 对路径坐标的敏感性使这两种坐标接入不等价。
范围：本次观察到的阴影 raster，不接受其他布局、颜色或时序偏离；保留原始 PNG 和比较失败。
确认：2026-10-04，维护者在坐标适配方案与保留差异中选择“本次保留并记录阴影差异”。
