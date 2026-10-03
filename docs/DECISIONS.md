# 共享能力的已确认决定

本文件只记录跨组件的共享能力决定。组件行为、适配与确认依据随各组件 README 保存；
通用职责与接入规则见 [ARCHITECTURE.md](ARCHITECTURE.md)，VisualParity 决定见其[测试目录文档](../tests/VisualParity/DECISIONS.md)。

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
组件继承、内容槽和默认桌面分支的确认见 [AlertDialog](../src/Material3Expressive/Components/AlertDialog/README.md)。

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

## Surface 均匀圆角描边

决定：内部 SurfaceOutline 对均匀 Thickness / CornerRadius 直接移植固定 Compose Border.kt 的
物理像素 ceil 与内缩 stroke；描边作为内容上方的视觉层，不消耗内容尺寸。
小圆角使用外圆角减内矩形，大宽度使用填充；非均匀边框保留 Avalonia 原生 Border 路径。
原生零 Thickness 保留无描边语义，公共 API 不变。
原因：Chip 深色静态用例暴露角部差异，已有原生 Border 不符合固定上游的像素取整。
范围：所有 MaterialSurface 调用方；Surface / Button / Chip 的相关渲染和回归需要共同检查。
确认：2026-10-03，维护者明确批准共享 Surface 描边修正及 Surface / Button 回归。

## Core Desktop 原生焦点反馈

决定：共享控制器 Focused 跟随所有原生焦点，包括鼠标点击获得的焦点；控件继续负责激活。
指针按下反馈在 Button 原生类处理之后观察，保留 Focus → Press 的交互顺序。
原因：固定 AndroidX 的 RequestFocusOnClick 返回 false，固定 Core Desktop 返回 true；
仅接受 Tab / Directional 焦点会让 Chip 点击释放后错误地恢复 hover elevation。
范围：共享控制器调用方，不改变公开 API 或已确认的键盘激活职责。
确认：2026-10-04，维护者批准按 Core Desktop 修正共享焦点反馈并回归 Button / IconButton / 导航项。
实现同时按固定 Core `Indication.skiko.kt` 过滤 indication 的焦点源：首次鼠标聚焦不进入
状态层，已经显示的键盘焦点层保留到失焦；输入模式改变不合成 Focus / Unfocus。
内部按窗口保存输入模式，原生 Tab / Directional 导航接入 Keyboard，指针按键接入 Touch，
程序化聚焦沿用当前窗口模式。elevation 的原始交互源仍保留所有焦点。

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
