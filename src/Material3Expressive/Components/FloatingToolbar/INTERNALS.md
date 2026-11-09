# FloatingToolbar 维护说明

## Token 配置路径

内部必需 spring 的 stiffness 必须大于零。模板首次挂载、重挂或资源暂时撤回时，未就绪配置不构造或提交运行时 spring；有效配置恢复后重新提交当前视觉目标。正在运行的通道继续使用已验证并捕获的规格，不回退 Expressive、Snap 或另一套默认值。

主题将尺寸、运动和 elevation 默认配置送入有效属性；ExpandedShadowElevation 现在是非 nullable double，普通/FAB 默认值在内部条件候选中选择后以主题优先级提供。AnimationSpec 为主题提供的有效有限规格，调用方仍可整值替换。移除重复的公开尺寸/elevation/padding Defaults 常量，行为与展开订阅工厂继续保留。

.vibrant 背景和 FAB 模板/高度只切换内部候选，应用普通 Style、Binding、LocalValue 具有正常优先级。两侧显隐运动在每个新目标读取当前有效规格，运行结果不写回配置。滚动 behavior 的 null spec 参数仍表示使用已挂载 owner 的主题配置，density 相关 decay 工厂不 Token 化。

FAB 分支以 FloatingActionButton 或 FloatingActionButtonTemplate 任一非空为条件，模板-only 同样代表上游非空 FAB 内容 lambda。
pseudoclass、elevation、测量及排列使用同一 presence 规则；收起保留内容分配，移除模板后回到普通三槽分支。
`TemplateOnlyFab_SelectsFabLayoutAndRetainsAllocation` 覆盖两方向及模板移除；双端 `floating-toolbar-template`
补充方向、FAB 位置、展开/收起、vibrant、RTL 和动画反向。Catalog 的 vibrant 横向例使用 template-only 入口。
该 suite 的 44 个双端用例均在既定 raster 容差内，重复捕获与额外被动截图稳定；结构化事实相等。

使用方法见 [README](README.md)。普通入口和 FAB 入口共用公开 ContentControl API，
但各自保持上游布局、占位和动画分支。

## 上游与本地入口

固定版本由 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 指定：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`；Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
两份 Material3 `FloatingToolbar.kt` 在原核对记录中相同。

| 上游文件 / 符号 | 本地对应 |
| --- | --- |
| [FloatingToolbar.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/FloatingToolbar.kt)：`HorizontalFloatingToolbar` / `VerticalFloatingToolbar` | [FloatingToolbar.cs](FloatingToolbar.cs)、[主题](FloatingToolbar.axaml)、[FloatingToolbarLayout](FloatingToolbarLayout.cs) |
| `FloatingToolbarDefaults`、`minimumInteractiveBalancedPadding`、FAB helper | [FloatingToolbarTokenSource](FloatingToolbarTokenSource.cs)、[FloatingToolbarFabContentLayout](FloatingToolbarFabContentLayout.cs)及共享最小交互读数 |
| `FloatingToolbarScrollBehavior` / `FloatingToolbarState` / `VerticalNestedScrollExpansionNode` | [FloatingToolbarScrollBehavior](FloatingToolbarScrollBehavior.cs)、[FloatingToolbarExpansionPolicy](FloatingToolbarExpansionPolicy.cs) |
| animation 的 `EnterExitTransition.kt` / `Transition.kt` | [FloatingToolbarVisibility](FloatingToolbarVisibility.cs)与共享 ComposeAnimation |
| Foundation `Scroll.kt`、UI `Placeable.kt` | 受限滚动测量、物理像素放置与布局对齐 |

当前 ComposeAnimation 随仓库源码构建，外部依赖遵循相应中央包配置；接入方式统一见[公共接入](../../../../docs/GETTING_STARTED.md)。

## 已确认适配与职责

依据[拆分前确认记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/FloatingToolbar/README.md)，
维护者于 2026-10-04 确认共享 ContentControl / 槽 API、主题和 FAB 位置枚举、DIP 滚动状态与显式
fling、宿主提供触摸探索状态及 automation 适配，并另行确认 IDisposable 展开辅助订阅。
原因是原生 ScrollViewer 不能提供 Compose nested-scroll 的剩余 fling 回调；不假造自动速度转交。

普通入口按 Expanded 隐藏两侧槽，FAB 入口则保留全部占位而只显示 FAB。模板不能把两者简化成
同一个 IsVisible。默认 padding 为 8、交叉轴最小尺寸 64 DIP；ContainerBetweenSpace 原始 Token
不等于 Row/Column 默认 Spacing，不额外插入。FAB helper 保留 Button 的交互和显隐能力，
动画中内容中心按 Compose Box 取整到物理像素，自定义固定尺寸仍受自身约束。

IconButton/FAB 最小交互留白通过内部对齐读数聚合；普通两侧槽使用 FastSpatial，
`minimumInteractiveBalancedPadding` 使用 DefaultEffects。48 DIP 分配和补齐两轴留白的确认范围见
[共享决定](../../../../docs/DECISIONS.md#最小交互布局与工具栏对齐读数)。

## 滚动、焦点与生命周期不变量

FloatingToolbarState 保留 Float 运算、Offset 在 limit..0 钳制及独立 ContentOffset；单改
OffsetLimit 不自动重钳制 Offset。默认 spline/snap 先在实际 density 的物理像素内计算再转回 DIP；
自定义动画规格使用公开 DIP。衰减后的 snap、上游有符号 delta 比较及剩余速度返回都需保留。
直接拖动按 ExitDirection 投影，但释放速度不再随退出方向反转第二次。

ScrollSource 对同窗口的单轴物理 ScrollViewer 通过 `FloatingToolbarScrollConnection` 注册到既有
MaterialScrollCoordinator；注册 owner 是栏与来源的最近共同可视 Control 祖先，对应 Compose 中承载正文与浮动栏
的 nestedScroll 宿主，而非任意兄弟栏的深度。post 消费包含正文和内层 post，不包含 pre；本组件返回零位移消费。
compositor 完成每笔事务后，通过有序通知在 UI 以原 Float DIP 算法更新 ContentOffset/Offset。
正文释放的真实剩余速度在相同队列后进入 OnPostFlingAsync，返回值沿用上游 settleFloatingToolbar 将其
remainingVelocity 作为 nested consumption 的现有契约；没有重新解释为 available-minus-returned。
Touch/pen 使用固定 Core 的 18 DIP slop；新输入取消旧 settling，普通 wheel 不制造 post-fling。
源、behavior、共同祖先、模板与触摸探索改变时释放旧注册，晚到通知按 attach 代次丢弃。
非物理/逻辑/双轴来源保留原 ScrollChanged OffsetDelta 兼容观察及显式 OnPostFlingAsync。
behavior 只能有一个可见 owner，不能共享其运行时状态。

2026-10-04 的显式 fling 决定因原依赖不能交付 nested 剩余速度而成立。2026-11-09 本轮维护者明确授权
缺失交互、共享能力及历史决定修订，现仓内 SmoothScroll 已提供 pre→正文惯性→post 异步释放，因此仅扩展
符合上面条件的 ScrollSource。没有删除显式 API；`IsScrollConnected` 让调用方区分两条路径。
物理正文继续采用共享决定 §18 的原有惯性算法；本连接不宣称它已与 Core 的正文 fling 轨迹一致。

展开辅助策略读取应用 Expanded；同步回调后立即读回，其他外部变化在下一次实际滚动前观察。
通知不改 Expanded、不隐藏整个工具栏；阈值/方向固定在订阅期，释放后才可换配置。
触摸探索由宿主提供；automation 的 forceCollapse 独立于应用状态，Expand 只清除此标志。
普通入口 Offset 非零时临时抑制后代焦点，恢复时释放旧属性值而不是强制写默认值。

2026-11-05 阴影高度输出改用共享 `BindAnimatedValue` 的 Float→double 纯投影，补齐已有 Animation
优先级契约；取消逐帧普通赋值及冗余初值写入。目标仍来自 ExpandedShadowElevation /
CollapsedShadowElevation，浮点计算、弹簧、目标提交帧不变；卸载时恢复内部 Surface 的底层值。
`ComponentMotionContributionTests` 覆盖展开→收起→反向和卸载，公开配置及调用方无需迁移。

当前 `toolbar-animation-contribution` 完整 self-test 共 172 例：164 withinTolerance、8 different，
零执行错误，全部重复/额外截图稳定。8 例为双向 balanced-padding / 普通 reverse / FAB start、end expand；
普通反向的 box/size 读数差最多 0.5 DIP，另有 FAB 进入过程 raster 差异。此处如实保留当前失败，
不以历史同名用例的结果替代当前结论，不增加偏移、补帧或舍入豁免。

## 历史问题与验证入口

早期 ComposeAnimation 0.2.2-alpha 的 Transition 空闲唤醒缺口曾令 FAB 在同一 64ms 输入下
出现 68 / 73.25 DIP 与重复捕获不稳定。原文记录后续包含唤醒修正的 0.2.7-alpha 全量自检未再观察到，
包括 balanced-padding 与 FAB reverse；不要据此另建工具栏动画引擎，或把旧版本结果当作当前回归。

[FloatingToolbarTests](../../../../tests/Material3Expressive.Tests/FloatingToolbarTests.cs) 覆盖 Token/状态、
占位、对齐、实际 ScrollViewer 消费、订阅释放、拖动、焦点恢复、automation 与 detach。
[Avalonia 场景](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/FloatingToolbarCases.cs)与
[Compose 场景](../../../../tests/VisualParity/reference/src/main/kotlin/cases/FloatingToolbarCases.kt)
包含方向、普通/FAB、位置、主题、RTL、内容/约束、外观覆盖、展开/反向、滚动、snap/fling。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~FloatingToolbarTests
./tests/VisualParity/parity.ps1 prepare-reference --family floating-toolbar
./tests/VisualParity/parity.ps1 run --family floating-toolbar
./tests/VisualParity/parity.ps1 self-test --family floating-toolbar
```

几何夹具使用受父约束的固有尺寸，对应 Modifier.size，不能用强制 Width 溢出来迁就比较。
旧自检的稳定和 withinTolerance 只表明当时采样范围；真实文字、字体图标和实际桌面显示不在此 suite。
工具栏内容环境不覆盖子控件显式颜色；IconButton 的 ambient 色/描边等未对齐事实仍见
[其维护说明](../IconButton/INTERNALS.md)。实体触屏惯性、宿主辅助技术及字体排版尚未经上述回归证明。
报告比较与稳定性的含义见 [VisualParity](../../../../tests/VisualParity/README.md)。

## 显式容器形状

2026-11-08 本轮授权新增 Shape。固定 AndroidX/Core 的 FloatingToolbar.kt 已逐份核对相同：
四个工具栏重载的 shape 参与 graphicsLayer 裁剪/阴影与背景，Standard/Vibrant FAB helper 各有独立 shape。
普通/FAB 两套工具栏模板把 Shape 传至既有 Surface；专用 FAB 模板传递 FloatingActionButton.Shape。
没有新播放器、布局代理或破坏性迁移，null 继续使用 CornerRadius。
`ToolbarShapeTests` 验证两轴收起尺寸、百分比重算、切换到直接/模板 FAB、独立形状与重挂；
`toolbar-shape` 双端套件覆盖两轴/三个位置、圆角/切角/百分比、RTL、受限模板、展开/收起/反向和滚动。

`toolbar-shape-verified` 完成 60 例，零执行错误、全部重复和额外截图稳定。40 个静态用例均通过
原有 raster 门槛，动态仍保留两轴普通反向的差异；这些不因新增形状被接受。
具体结构化事实与图像记录保留在对应 case 报告，不放宽门槛。

## 公开过渡工厂（2026-11-09）

固定 AndroidX/Core `FloatingToolbarDefaults.horizontalEnterTransition(Alignment.Horizontal)`、
`verticalEnterTransition(Alignment.Vertical)`、`horizontalExitTransition(Alignment.Horizontal)`、
`verticalExitTransition(Alignment.Vertical)` 的实现相同：以 FastSpatial 调用 Expand/Shrink 的对应轴入口。
此前只能从完整工具栏的内部 FloatingToolbarVisibility 消费；本轮依据维护者明确授权新增四个同名
Defaults 工厂，返回既有 Enter/ExitTransition，内部工具栏也改为调用它们。

Compose 的 composable theme/布局环境在本地由显式 `AnimatedVisibility owner`、alignment 和
`Foundation.Motion.SpringSpec animationSpec` 表达。调用方负责在主题输入变更后重新配置结果，
不把一次性 Token 读取伪装成动态主题，也不新增 Visibility 包装控件或动画循环。
原 FloatingToolbarSizeSpec 继续提供物理像素 IntSize 投影、0.01px 阈值及 PostExit→Visible 的
400 stiffness / 1px 回退；其 Vectorize 从 owner 读取当前实际 density 与 segment，避免挂载前
创建工厂时固化 1x。保持 SpringSpec 身份以接续中断速度。
无公开 API 删除或仓内调用方迁移要求；原来手写 Expand/Shrink 的应用可以选择使用新工厂。

`FloatingToolbarTransitionTests` 验证挂载前配置、两轴/方向、进入退出与反向、运行时规格替换、
像素尺寸和卸载清理；新旧 FloatingToolbar 行为测试合计 21 项通过，Catalog 构建通过。
`floating-toolbar-transition` 双端直接调用固定上游真实工厂，覆盖三种对齐、方向、循环、反向、
运行中内容/主题替换与卸载重挂。

首次独立双端运行 `floating-toolbar-transition-public` 34 例为 12 equal / 22 different，
全部稳定、零执行错误。直接 Visible 进入提前一帧（64ms 纵向尺寸 Core 20 DIP、本地 27.75 DIP）。
固定 Core AnimatedVisibility(bool) 使用 updateTransition.animateTo，在 composition 更新目标，
DisposableEffect 的帧循环随后采样；本地默认即时 Visible 则会被同一事件帧的 idle loop 采样。
本轮授权扩展共享 AnimatedVisibility 的显式 `TargetCommitPhase=AfterFrame`，复用已有根提交队列，
保持默认即时模式与 Source 状态所有权；工厂不偷偷写入模式。内部工具栏改用该入口并移除自己的
WaitForNextFrame 延迟，独立用法显式选择 AfterFrame。初始进入的 16ms / 64ms 关键帧现严格相等，
32ms 父容器居中仍可相差 0.25 DIP，不能据此宣称完整轨迹一致。
新共享测试覆盖同批合并、捕获后重入、卸载取消、换源及切回即时模式；原模块回归继续通过。

非对称 marker 进一步发现：RTL 横向 Start 的 64ms 两端外框相等，本地却裁掉物理左侧标记。
固定 Core EnterExitTransition.kt 使用 LayoutDirection.Ltr 与 placeWithLayer；本轮授权把共享
AnimatedVisibility 模板的效果层设为物理 LTR，内容 presenter 仍绑定宿主方向，保留内容 RTL。
8 项真实 TransformToVisual 测试先复现 4 个 RTL 失败，修后完整 ComposeAnimation 1010 项、
FloatingToolbar / SegmentedButton / DatePicker / Expander / SearchBar 实际使用者 163 项全通过。
此次没有扩展到 AnimatedContent 或 AnimateEnterExit 的其他效果层。

最终 `floating-toolbar-transition-physical` 34 例为 12 equal / 22 different，全部重复与额外截图稳定，
零执行错误。RTL Start / End 的 64ms、160ms 裁剪 marker 现与 Core 精确相等。动态剩余差异包括
父级居中的 0.25 DIP、部分返回弹簧样本的 0.25 DIP，以及内容替换时本地重建显隐呈现所产生的跳变
（横向 65ms Core 41×40、本地 110×28 DIP），反向、主题变更与重挂场景也保留不同判定。
这些是未对齐范围，不是接受的近似；没有调整比较容差、删除失败用例或替换基准。
最终 `toolbar-physical-{horizontal,vertical}-{expand,collapse,reverse}` 原组件 6 例回归中，
4 个展开/收起 withinTolerance，2 个已有反向差异仍为 different；3 个 SegmentedButton RTL 选择与
单/多选路径反向用例均 withinTolerance。以上全部稳定、零执行错误。

```powershell
./tests/VisualParity/parity.ps1 self-test --family floating-toolbar-transition
```
