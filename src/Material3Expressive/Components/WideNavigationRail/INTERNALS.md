# WideNavigationRail 实现说明

本文面向维护者；使用入口见 [README.md](README.md)。固定基线以
[upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 1.5.0-alpha27），
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（v1.13.0-alpha01）。
默认逻辑对照 AndroidX，桌面输入与平台实现对照 Core，不用浮动 HEAD 替代。

## 上游与本地职责

主要入口是固定提交的 [AndroidX WideNavigationRail.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/WideNavigationRail.kt)
及 [Core WideNavigationRail.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/WideNavigationRail.kt)。
下文 Material3 路径均以 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/` 为前缀。

另需固定 `NavigationItem.kt` 的 AnimatedNavigationItem、AnimatedMeasurePolicy、placeAnimatedLabelAndIcon；
原记录核对两个仓库的这两个文件分别相同，桌面输入与动画提交时序另按 Core 固定源码核对。

| 上游符号 / 文件 | 本地职责 |
| --- | --- |
| WideNavigationRail、WideNavigationRailDefaults、Arrangement | [WideNavigationRail.cs](WideNavigationRail.cs)、[WideNavigationRailLayout.cs](WideNavigationRailLayout.cs)、[NavigationRailArrangement.cs](NavigationRailArrangement.cs) |
| WideNavigationRailState / Value | [WideNavigationRailState.cs](WideNavigationRailState.cs)：目标、原始 spring 当前值与多消费者生命周期 |
| AnimatedNavigationItem / AnimatedMeasurePolicy | [WideNavigationRailItem.cs](WideNavigationRailItem.cs)、[WideNavigationRailItemLayout.cs](WideNavigationRailItemLayout.cs)、[WideNavigationRail.axaml](WideNavigationRail.axaml) |
| ModalWideNavigationRail、水平锚点、SnapFlingBehavior | [ModalWideNavigationRail.cs](ModalWideNavigationRail.cs)、[ModalWideNavigationRail.axaml](ModalWideNavigationRail.axaml)：Popup、items 迁移、scrim、拖动与关闭时机 |
| NavigationRail / WideNavigationRail 的相关 Token | [WideNavigationRailTokenSource.cs](WideNavigationRailTokenSource.cs)、[组件 Token](../../Tokens/MaterialTokens.Comp.WideNavigationRail.cs)、[WideNavigationRailPaddingConverter.cs](WideNavigationRailPaddingConverter.cs) |
| Core LSQ2 速度拟合 | 共享 [VelocityTracker](../../Foundation/Input/VelocityTracker.cs)，组件提交真实时间戳样本 |

## 布局、Item 与 Modal 生命周期

Item 模板的布局容器使用透明背景，让整个矩形参与 hover 与点击命中，包括图标与标签之间的空隙、
胶囊外的留白；反馈仍只绘制在胶囊内，对齐 NavigationItem.kt 外层 Box.selectable 与内层 indication。
Wide 和 Modal 共用这一 Item 模板，展开、收起及位置动画期间均沿用实际 Item 边界。

- `WideNavigationRail.axaml` 组合 Surface、物理 inset、内容 padding、header 和 ItemsPresenter。
  `WideNavigationRailLayout` 保留四条 animateDpAsState 轨迹：最小宽、宽度上限、项间距和项最小高度。
  默认宽度收起 96、展开至少 220、至多 360 DIP；header 与项内容决定是否继续增宽。
  横向像素取整、保存展开最大宽、反向时的宽度约束和逐项测量顺序按上游保留。
  物理像素转回 Avalonia DIP 时使用原始 double 布局缩放，避免 float 除法误差被默认 LayoutRounding
  再向上取整成额外 1 像素，影响分数缩放下展开/收起动画的项高度、居中位置和排列。
  外层策略读取每项测量值；ItemsPanel 的总可用尺寸不变时，仍把子项测量失效传回外层，避免动画宽度滞后一帧。
- `NavigationRailArrangement` 提供 Top、Center、Bottom、SpaceBetween、SpaceAround、SpaceEvenly 和 SpacedBy。
  Center 按整个容器居中；有 header 时其他排列使用 header 下方的空间。
  SpacedBy(0, Center) 仍按剩余空间居中，不能替换标准 Center。
- `WideNavigationRailState` 使用 DefaultSpatial Animatable，保留动画中断时的速度。
  TargetValue 是目标；CurrentValue 只有原始值恰好等于 1 时才为 Expanded，空间 spring 的过冲期间也为 Collapsed。
  实际布局和 item 的展开目标跟随 TargetValue。
- `WideNavigationRailItemLayout` 翻译 AnimatedMeasurePolicy / placeAnimatedLabelAndIcon，含纯 icon 分支。
  选择尺寸与透明度共用一条 DefaultSpatial 轨迹，Top→Start 位置使用另一条轨迹，标签颜色使用 DefaultEffects。
  图标颜色按上游直接更新。标签透明度为 `4 * (progress - .5)^2`；padding 插值裁到 0..1，布局保留空间 spring 过冲。
  Top 标签样式在目标 Top 且进度小于 .5 时为 LabelMedium，其余为 LabelLarge。
- Icon presenter 将 WideNavigationRail.IconSize Token 24 DIP 作为 MaterialIcon.DefaultSize 传给无显式尺寸的
  MaterialSymbol；组件不为用户图标施加固定宽高约束。
- Item 复用共享交互控制器和 Surface ripple。目标 Top 时按实际 item 宽、56 DIP 指示器宽和固定 12 DIP 纵向偏移映射按压；
  目标 Start 时不映射。默认焦点呈现使用固定 Compose 的 opacity 状态层。
  Top / Start 目标切换时复用 Surface 的卸载/挂载流程，按上游 indication 节点重建语义清空反馈；Button 的点击入口继续生效。
- Token 保留 System 引用、disabled 0.38 alpha 与默认 padding 的尺寸派生关系；资源变化继续驱动模板和布局。
- `ModalWideNavigationRail` 将同一个 ItemsPresenter 迁入 PopupOverlayLayer 内的模态 Popup，保留 item 对象和外部选中状态。
  非 hidden 模式按 movableContentOf 保留进行中的 Item 动画；hidden 模式在内容重新挂载时释放旧作用域，
  按当前目标初始化位置、选择与颜色，对齐上游首次组合内容的动画初值，不额外播放收起到展开的进入动画。
  hidden 的水平锚点动画归属 rail 状态，在浮层关闭后仍保留并完成；再次打开从收起锚点滑入。
  替换 State 或卸载 rail 时释放该锚点状态，对齐上游 remember(state) 的生命周期。
  常驻模式始终保留收起态占位；隐藏模式不占布局空间，并以两条水平锚点处理滑出与拖动。
  position 与 scrim 使用 DefaultEffects，水平偏移使用 DefaultSpatial；modal 宽度使用 FastSpatial。
  拖动释放保留上游 LSQ2 速度计算、125 DIP/s 阈值、半程目标选择及 SnapFlingBehavior 的增量消费与边界停止。
  程序化偏移动画允许过冲。拖动关闭回调仍属于锚点任务，新目标替换该任务时一并取消其等待的外部状态动画。
  因此上游拖动关闭后可能出现 TargetValue=Collapsed、CurrentValue=Expanded、IsAnimating=false，不强行统一两个状态。
- Modal 阻挡背景指针输入、限制 Tab 和程序化焦点、关闭时恢复先前焦点。Escape 在 KeyUp 关闭；
  ShouldDismissOnBackPress 仅控制 TopLevel.BackRequested，不关闭上游 commonMain 的 Escape KeyUp 分支。
  自动化暴露 pane 和可 Invoke 的 scrim，默认英文名称为 Navigation rail / Close rail，可用 AutomationProperties.Name 覆盖。

## 已确认 API 与宿主边界

以下确认与历史观测依据来自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/WideNavigationRail/README.md)；不以当前代码反推维护者授权。
2026-10-03 维护者确认 Wide/Modal 的状态、内容槽、selectable 和排列 API“采用推荐 API”。
用 ItemsControl 内容/模板、可共享的 hoisted State 及外部 IsSelected 表达上游职责，形状使用 CornerRadius，
最低交互尺寸使用当前桌面默认 48 DIP；不新增通用 Modal/手势框架。
State 可由同一宿主多个消费者使用，最后一个卸载才取消动画；跨 TopLevel 共享未经验证。

同日确认独立 ExpandedHeader：Avalonia Control 无法同时属于两个父节点，收起与展开 Header 需要独立实例，
数据/模板则可创建两份内容。范围是 Wide/Modal 的 Header 与宿主，不改变默认颜色、形状和动画。
最初的普通 OverlayLayer 方案已由 2026-10-04 的
[Dialog / Popup 窗口内宿主决定](../../../../docs/DECISIONS.md#dialog--popup-的窗口内宿主与模态职责)替代。
当前 ModalPresenter 接管客户区覆盖、焦点约束/恢复和清理，Rail 继续拥有 State、同一 ItemsPresenter、
Header、scrim、拖动及关闭时机；没有套用基础 Dialog 的 200/100ms 居中缩放。
Escape KeyUp 与 Dialog KeyDown 的边界见
[Dialog 的平台宽度和 Escape 时机](../../../../docs/DECISIONS.md#dialog-的平台宽度和-escape-时机)。

共享[反馈/激活](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)、
[桌面焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)、
[聚合排版](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)及
[颜色转换](../../../../docs/DECISIONS.md#composeanimation-颜色转换与原始动画终值)按原文范围复用。

## 已接受的纵向动画改进

2026-10-03，维护者确认不再复现固定 Compose 展开/收起过程中的整数布局抖动。
依据为 125% 缩放的真实日志与固定源码中的多层 roundToPx、整数 lerp、居中截断和排列取整；
维护者确认上游也存在抖动，并授权连续化布局改善体验。
内置布局的项最小高度、项间距、纵向 indicator padding、内容高度、图标位置、居中偏移和整组排列
保留小数物理像素；静态尺寸先按原来的规则取整，再由现有动画进度连续连接。
居中与排列的整数端点偏差随进度平滑补偿，完整展开/收起及独立 Top / Start 覆盖保留原来的静态位置。
整组排列使用最近的端点组合，补偿权重在任意动画轴的中点平滑归零，避免端点切换新增跳变。

移动的 item、ItemsPresenter、容器和布局面板关闭 UseLayoutRounding；图标和标签 ContentPresenter
保留原来的内容测量取整，并由外层容器承载小数坐标。自定义模板或 ItemContainerTheme 若重新开启移动容器的
取整，会再次量化位置。C# / XAML 公共 API 与用法没有变化，没有额外的动画轨迹或停稳后的切换路径。
保留 spring 参数、过冲和中断速度、横向取整、标签透明度与 LabelMedium / LabelLarge 切换；
字体样式改变导致的内容测量变化和布局公式本身的平缓折返不属于这次消除的整数抖动。

这项改进同时用于 Wide 与 Modal 的 WideNavigationRailItem；常规 NavigationRail 不在本次修改范围。
展开/收起过程的 geometry 与 raster 可以偏离固定 Compose，因此相应 parity 用例继续保留真实 different，
没有修改参考、夹具或容差来制造通过。该决定不改变 ComposeAnimation；静态端点继续核验。
维护者明确本次不新增专门测试，改后的真实桌面视觉体验由维护者核验。

## 已接受的鼠标速度适配

2026-10-03 维护者确认保留 Avalonia 真实 PointerMoved 历史及固定 Core 的 LSQ2 算法，
范围仅为 Modal 鼠标拖动的释放轨迹，目标选择算法与容差不变。
Core `SyntheticEventSender.updatePointerPosition` 在内容重排后以原坐标/时间戳重发 Move，零位移样本进入速度拟合；
Avalonia 没有同样重发路径，不在组件或夹具中复制样本。GetIntermediatePoints 没有独立时间戳，不为其伪造时间。

旧 `modal-wide-navigation-rail.dynamic.hidden.fling-return` 反向快速拖动中，
最后三个 Move 在 Compose 源历史各出现两次；拟合结果为 Compose 1071.429 DIP/s、Avalonia 749.999 DIP/s。
目标选择相同而释放轨迹不同。这是具体输入历史的观测，不是两个引擎恒定速度比。
确认来源与原始诊断上下文见本文件引用的固定历史记录。

## 未消除的问题与证据边界

Indicator 的 Avalonia SaveLayer alpha 合成与固定 Skiko ModulateAlpha 每次 paint 的 Float 乘法有混色差异。
旧退选 64ms 像素为 Compose `#EEE7FA`、Avalonia `#EEE6F9`。
维护者在 2026-10-03 确认“暂留差异，之后处理”，暂缓新增绘制器；范围仅为 Wide Item 的选择/退选混色，
不等于已对齐，也不是通用绘制能力决定。维护者另行调整过 visual parity 容差并明确无需为该差异追加测试，
旧门限下失败不能当作新门限结论；复核须记录实际容差版本。

纵向连续化的历史验证只重采 Avalonia，复用固定 Compose 参考，没有追加 JVM 重采或完整重复性自检。
静态端点保留原位置，动态 geometry/raster 存在预期 different；与改前 Avalonia 的对比只改变纵向几何，
状态、颜色、点击和横向几何不变。不要把改动前 Wide 全部 equal/withinTolerance 的验收当成当前动态轨迹的保证。

旧 125% 布局输入回放取维护者的 498 次观察，将 Home 收起时 `211 → 212 → 211 → 212 px`
变为 `211.699 → 211.656 → 211.607 → 211.556 px`，终点三项图标仍是 `207 / 292 / 377 px`。
这是实际产品布局的输入回放，不能替代真实桌面观感核验。维护者要求改后桌面视觉由其人工核验，
后续验收需补充相应的真实桌面观察，不能从回放数字推定完成。

PopupOverlayLayer 迁移的旧回归重新采集同矩阵并做重复自检，与冻结的迁移前 Avalonia 在读数和 PNG 上一致，
既有 Compose different 保留；测试宿主补齐 VisualLayerManager / Background，不改布局、时间线和容差。
该历史比对只证明当次宿主迁移保持行为，不消除纵向、混色或速度差异。

尚未实现 Android predictive back；真实文字/字体图标栅格、人工桌面操作、屏幕阅读器、触屏拖动及跨 TopLevel 状态共享未经验证。

## 跟踪诊断入口

[RailLayoutTrace.cs](../../../Material3Expressive.Catalog/RailLayoutTrace.cs)保留布局诊断，
[MainWindow.axaml.cs](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)中的 TraceWideRailLayout 当前为 false，
并非旧记录所述默认启用。需要调查时可临时开启，再运行 Catalog 展开/收起；结束后恢复关闭。
它在动画结束后将 JSONL 写入系统临时目录的 Material3Expressive/rail-layout-traces，并打印 `RailTrace saved:`。

记录横轴为目标改变后的 Stopwatch 时间、序号为布局观察次数，不是屏幕呈现帧号；坐标是客户区物理像素。
包含实际边界、DesiredSize、UseLayoutRounding、图标/标签边界、项间距/高度与端点补偿前后值。
layoutModel 为 `continuous vertical geometry with Compose integer endpoints`，补偿前字段带 BeforeEndpointCorrectionPx。
采集在 LayoutUpdated 后读取，内存缓冲至动画结束，不主动送帧或重新布局。诊断文件是本地输出，不是仓库依赖或可共享证据链接。

## 验证入口

从仓库根目录执行，环境与结果含义见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~WideNavigationRailTests
./tests/VisualParity/parity.ps1 prepare-reference --family wide-navigation-rail
./tests/VisualParity/parity.ps1 run --family wide-navigation-rail
./tests/VisualParity/parity.ps1 self-test --family wide-navigation-rail
```

固定参考源码与配置不变时，无需每次 prepare-reference。
[WideNavigationRailTests.cs](../../../../tests/Material3Expressive.Tests/WideNavigationRailTests.cs)、
[Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/WideNavigationRailCases.cs)和
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/WideNavigationRailCases.kt)是已跟踪的复现入口。
Catalog 入口为 [MainWindow.axaml](../../../Material3Expressive.Catalog/MainWindow.axaml)。

[ModalWideNavigationRailTests](../../../../tests/Material3Expressive.Tests/ModalWideNavigationRailTests.cs)、
[Modal Avalonia 用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ModalWideNavigationRailCases.cs)与
[Modal Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ModalWideNavigationRailCases.kt)另行覆盖浮层：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~ModalWideNavigationRailTests
./tests/VisualParity/parity.ps1 prepare-reference --family modal-wide-navigation-rail
./tests/VisualParity/parity.ps1 run --family modal-wide-navigation-rail
./tests/VisualParity/parity.ps1 self-test --family modal-wide-navigation-rail
```

Wide 矩阵包含两种形态、六种排列、Header、独立位置/标签/选择/禁用、主题/RTL/Insets/尺寸约束、
颜色/位置覆盖、SpacedBy，以及展开/收起/中断、快照、主题/选择/按压/取消/hover/键盘。
Modal 增加 HideOnCollapse、scrim、Escape、隐藏拖动/快速释放；参考端提供真实客户区尺寸、焦点和 density-scaled slop。
位置通过公开 LayoutCoordinates 链读取，避免固定 Core RectManager 缓存与当前绘制暂时不一致。
这些用例调用真实组件、以几何内容隔离字体栅格；仍应分别读取静态端点和动态预期 different。

上述命令与覆盖说明不代表当前修订已执行全量验证。旧测试数量与未跟踪 artifacts 不作为当前验证结果；
详细历史结果见[固定修订的原记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/WideNavigationRail/README.md)，复核以跟踪测试和新生成报告为准。
`run` 执行完成、`self-test` 重复稳定与两端相等是不同结论，必须分别报告。
