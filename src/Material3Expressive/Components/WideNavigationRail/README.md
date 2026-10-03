# WideNavigationRail / ModalWideNavigationRail / WideNavigationRailItem

Wide rail 支持展开、收起、排列和外部选中状态；独立 item 可以覆盖 Top / Start 图标位置。
Modal rail 支持常驻收起态与收起后隐藏两种模式，在当前窗口客户区显示浮层与 scrim。

## 源码与实现

固定基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
两端 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`
中的 `WideNavigationRail.kt` 和 `NavigationItem.kt` 分别相同；桌面交互与动画提交顺序按固定 Core 核对。

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
- Item 复用共享交互控制器和 Surface ripple。目标 Top 时按实际 item 宽、56 DIP 指示器宽和固定 12 DIP 纵向偏移映射按压；
  目标 Start 时不映射。默认焦点呈现使用固定 Compose 的 opacity 状态层。
  Top / Start 目标切换时复用 Surface 的卸载/挂载流程，按上游 indication 节点重建语义清空反馈；Button 的点击入口继续生效。
- Token 保留 System 引用、disabled 0.38 alpha 与默认 padding 的尺寸派生关系；资源变化继续驱动模板和布局。
- `ModalWideNavigationRail` 将同一个 ItemsPresenter 迁入 OverlayLayer，保留 item、选中状态与进行中的动画。
  常驻模式始终保留收起态占位；隐藏模式不占布局空间，并以两条水平锚点处理滑出与拖动。
  position 与 scrim 使用 DefaultEffects，水平偏移使用 DefaultSpatial；modal 宽度使用 FastSpatial。
  拖动释放保留上游 LSQ2 速度计算、125 DIP/s 阈值、半程目标选择及 SnapFlingBehavior 的增量消费与边界停止。
  程序化偏移动画允许过冲。拖动关闭回调仍属于锚点任务，新目标替换该任务时一并取消其等待的外部状态动画。
  因此上游拖动关闭后可能出现 TargetValue=Collapsed、CurrentValue=Expanded、IsAnimating=false，不强行统一两个状态。
- Modal 阻挡背景指针输入、限制 Tab 和程序化焦点、关闭时恢复先前焦点。Escape 在 KeyUp 关闭；
  ShouldDismissOnBackPress 仅控制 TopLevel.BackRequested，不关闭上游 commonMain 的 Escape KeyUp 分支。
  自动化暴露 pane 和可 Invoke 的 scrim，默认英文名称为 Navigation rail / Close rail，可用 AutomationProperties.Name 覆盖。

## 使用

加载 MaterialTheme，并按项目 [README](../../../../README.md) 初始化 TopLevel 的 VisualAnimationScope。
构造状态或未挂载时的 SnapToAsync 可以设置初值；动画方法应在控件挂载后调用。

```csharp
public WideNavigationRailState RailState { get; } = new(WideNavigationRailValue.Collapsed);

// 挂载后，在 UI 线程调用；再次操作或卸载会取消之前的任务。
await RailState.ToggleAsync();
```

```xml
<m3:WideNavigationRail xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
                       State="{Binding RailState}">
  <m3:WideNavigationRail.Header>
    <m3:IconButton Symbol="Menu" Command="{Binding ToggleRail}" />
  </m3:WideNavigationRail.Header>
  <m3:WideNavigationRailItem Label="主页" IsSelected="{Binding IsHomeSelected}"
                            Command="{Binding NavigateHome}">
    <m3:WideNavigationRailItem.Icon>
      <m3:MaterialSymbol Symbol="HomeAndGarden" Width="24" Height="24" />
    </m3:WideNavigationRailItem.Icon>
  </m3:WideNavigationRailItem>
</m3:WideNavigationRail>
```

```csharp
var rail = new WideNavigationRail
{
    State = new(WideNavigationRailValue.Expanded),
    Arrangement = NavigationRailArrangement.SpacedBy(8, VerticalAlignment.Center)
};
var home = new WideNavigationRailItem { Icon = homeIcon, Label = "主页", IsSelected = true };
rail.Items.Add(home);
home.Click += (_, _) => NavigateHome();
```

Rail 使用 ItemsControl 的 Items / ItemsSource / ItemTemplate；数据模板生成 WideNavigationRailItem。
Header、Icon、Label 接受内容与 DataTemplate，字符串 label 由普通 TextBlock 消费聚合默认排版。
点击、Command 和自动化 Select 只请求导航；应用维护 IsSelected 与各项互斥。
Space / Enter 在松开时调用。自动化暴露 TabItem、选中状态和所属 rail；有 label 时抑制 icon 的重复描述。

Modal 复用同一 State 和 item API。宿主须有 OverlayLayer（标准 Window 模板的 VisualLayerManager 可提供；
自定义宿主使用 `VisualLayerManager EnableOverlayLayer="True"`）。浮层覆盖整个 TopLevel 客户区，不受 rail 的布局位置限制。
`Header` 用于收起态，`ExpandedHeader` 用于展开态；直接传 Control 时必须是两个实例。
使用 Header 数据和 HeaderTemplate 时，展开槽默认复用数据与模板以生成第二份内容。

```xml
<m3:ModalWideNavigationRail State="{Binding RailState}" HideOnCollapse="True"
                          Header="{Binding MenuData}" HeaderTemplate="{StaticResource MenuTemplate}">
  <m3:WideNavigationRailItem Icon="{Binding HomeIcon}" Label="主页"
                            IsSelected="{Binding IsHomeSelected}" Command="{Binding NavigateHome}" />
</m3:ModalWideNavigationRail>
```

```csharp
var modal = new ModalWideNavigationRail
{
    State = new(WideNavigationRailValue.Collapsed),
    HideOnCollapse = false,
    Header = collapsedMenuButton,
    ExpandedHeader = expandedMenuButton
};
modal.Items.Add(new WideNavigationRailItem { Icon = homeIcon, Label = "主页", IsSelected = true });
// 挂载后：
await modal.State.ExpandAsync();
```

| 配置 | 语义 |
| --- | --- |
| State | 可绑定的展开状态；同一宿主中多个 rail 可共享，最后一个消费者卸载时取消动画，重挂载从保存目标恢复 |
| Background / Foreground / CornerRadius | 容器和内容环境；默认内容色复用 contentColorFor 角色匹配，未知容器色继承环境 |
| WindowInsets | 物理 left/top/right/bottom，桌面默认零；RTL 不交换物理边，没有 Compose 的嵌套 inset 消费环境 |
| ContentPadding | 逻辑 start/top/end/bottom，默认 top 44 DIP |
| RailExpanded / IconPosition | 默认继承 rail 展开目标；IconPosition 可独立指定 Top 或 Start |
| IndicatorPadding | 非空时固定 padding；否则按位置进度插值 CollapsedIndicatorPadding / ExpandedIndicatorPadding |
| SelectedIconColor / IndicatorColor | 选中图标和指示器色 |
| SelectedTextColorTopIconPosition / SelectedTextColorStartIconPosition | 两种目标位置的选中标签色 |
| UnselectedIconColor / UnselectedTextColor / DisabledIconColor / DisabledTextColor | 未选中与禁用内容色 |
| HideOnCollapse | false 保留收起态占位；true 隐藏占位并启用水平拖动关闭 |
| ExpandedHeader / ExpandedHeaderTemplate / ExpandedHeaderTopPadding | 独立展开内容及额外 top padding，默认 0 |
| ModalBackground / ModalForeground / ScrimBrush | 浮层内容环境及 scrim 色；默认 scrim 为主题 Scrim 的 0.32 alpha |
| CollapsedCornerRadius / ExpandedCornerRadius | Modal 的收起和展开形状，默认 0 / 16 DIP |
| ShouldDismissOnBackPress | 是否响应原生返回请求，默认 true；不影响 Escape KeyUp |

内容槽、状态生命周期及 selectable 适配的确认依据见 [DECISIONS](../../../../docs/DECISIONS.md)。
形状使用现有 CornerRadius，最低交互尺寸使用当前 desktop 默认 48 DIP；没有新增通用 Modal/手势基础框架。

## 已接受的纵向动画改进

2026-10-03，维护者确认不再复现固定 Compose 展开/收起过程中的整数布局抖动。
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
没有修改参考、夹具或容差来制造通过。决定见 [DECISIONS](../../../../docs/DECISIONS.md)。

## 验证

Catalog 临时启用了 [RailLayoutTrace](../../../Material3Expressive.Catalog/RailLayoutTrace.cs)，用于调查展开/收起时的坐标抖动。
运行 Catalog，每次改变 rail 的目标状态后等待动画结束，会自动在
`%TEMP%/Material3Expressive/rail-layout-traces/` 保存独立 JSONL，并在 Debug 输出中打印 `RailTrace saved:` 路径。
Debug 输出也包含带 `RailTrace` 前缀的完整记录；Release 仍保存文件。
记录以目标改变后的 Stopwatch 时间为横轴、布局观察次数为序号，并非屏幕呈现帧号；
坐标为窗口客户区物理像素，保留小数。每项含实际边界、DesiredSize、UseLayoutRounding、图标与标签内容边界，
以及原始项间距/最小高度、padding、内容高度、端点补偿前位置与补偿前后居中偏移。
新记录的 layoutModel 为 continuous vertical geometry with Compose integer endpoints；
补偿前读数以 BeforeEndpointCorrectionPx 命名，旧的整数布局日志保留原有字段。
采集在 LayoutUpdated 之后读取，内存缓冲到相关布局动画结束再输出；不会主动送帧或重新布局。
将 MainWindow 的 `TraceWideRailLayout` 改为 false 即可关闭此临时诊断。

独立双端入口为 [WideNavigationRailCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/WideNavigationRailCases.cs)
和 [WideNavigationRailCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/WideNavigationRailCases.kt)。
两端调用真实组件，以确定尺寸几何内容验证布局、颜色与动画，文字栅格按 AUTHORING 不纳入 parity。
矩阵包括两种形态 × 六种标准排列 × header 有无，独立 item 的位置/label/selection/enabled，
暗色、RTL、物理 inset、内容尺寸与约束、颜色与位置覆盖、SpacedBy，以及展开/收起/中断/快照、
主题变化、选择、按压、取消、禁用、hover 和键盘激活。
完整比较结果与重复性记录见 [移植证据](../../../../artifacts/wide-navigation-rail-port/README.md)。
原始移植验收（2026-10-03，纵向连续化之前）：控件库 264/264、parity 框架 105/105、Catalog Release 构建通过。
Wide 最终 113 配置全部 equal/withinTolerance；Modal 92 配置双端重复性稳定，91 配置 equal/withinTolerance，
唯一 different 为下方已接受的鼠标速度适配。equal 与 withinTolerance 分开计数，不宣称严格像素相等。

纵向连续化验证（2026-10-03）：控件库 264/264、Catalog 构建通过，已有断言改读相对 item 的实际图标坐标，
预期位置不变；未新增专门测试。使用当前 run 流程重新采集 Avalonia，复用 Compose 参考
`20261003-061524-1838ca71`，没有重采 JVM 参考，也没有追加重复性自检。
Wide 静态 81 equal / 4 withinTolerance，动态 10 equal / 7 withinTolerance / 11 different；
Modal 静态 24 equal / 48 withinTolerance，动态 5 withinTolerance / 15 different。
205 配置均无执行错误；相对改动前的 Avalonia 捕获，615 个样本的既有结构化读数只改变纵向几何，
状态、颜色、点击次数及横向几何保持不变。Modal 原有鼠标速度适配仍保留。

将维护者提供的 498 次原生布局观察中的动画输入与标签测量高度，回放到实际产品布局（125% 缩放）：
Home 收起时原来的 `211 → 212 → 211 → 212 px` 变为 `211.699 → 211.656 → 211.607 → 211.556 px`，
最终三项图标仍为 `207 / 292 / 377 px`。这是布局输入回放，不能替代本次真实桌面观感核验。
三种 Catalog 展示均完成展开/收起的 Headless smoke，并正常生成六份诊断日志。
报告、坐标曲线与回放入口见 [本次验证产物](../../../../artifacts/rail-jitter-diagnostics/README.md)；
修改后的桌面视觉体验按维护者要求由其人工核验。

Modal 独立入口为 [ModalWideNavigationRailCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ModalWideNavigationRailCases.cs)
和 [ModalWideNavigationRailCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ModalWideNavigationRailCases.kt)。
92 个配置覆盖 HideOnCollapse × 两种形态 × 六种排列 × header 有无，以及 RTL、颜色、inset、约束、
展开、反向、scrim、Escape、主题变化和拖动/快速释放。参考端提供实际桌面环境的客户区像素尺寸、窗口焦点与密度缩放 touch slop。
真实坐标通过公开 LayoutCoordinates 链读取，避免固定 Core RectManager 缓存与当前绘制位置暂时不一致。

保留差异：Indicator 使用 Avalonia 的 SaveLayer alpha 合成，与固定 Skiko 的 ModulateAlpha 每次 paint float 乘法有混色差异。
退选 64ms 的像素为 Compose `#EEE7FA`、Avalonia `#EEE6F9`；维护者已决定暂缓新增绘制器。
维护者另行调整 visual parity 容差，并明确无需为该差异追加测试。旧门限的失败记录不能作为新门限的结论。
独立复现及使用的容差版本见 [移植证据](../../../../artifacts/wide-navigation-rail-port/README.md)。

未经验证：真实文字和字体图标栅格一致性、人工桌面操作、实际屏幕阅读器体验、触屏拖动，以及跨 TopLevel 共享状态。
平台范围为桌面，未提供 Android predictive back。

已接受的鼠标速度适配（2026-10-03）：保留 Avalonia 的真实 PointerMoved 历史和固定 Compose Desktop 的 LSQ2 算法。
固定 Core 的 `SyntheticEventSender.updatePointerPosition` 在内容重新布局后，以原坐标和时间戳重发鼠标 Move；
这些零位移样本也进入速度拟合。Avalonia 没有对应重发路径。本次不在组件或夹具中复制样本。
速度历史使用带时间戳的 PointerMoved；Avalonia 的 GetIntermediatePoints 不提供独立时间戳，不为其构造时间。
`modal-wide-navigation-rail.dynamic.hidden.fling-return` 的反向快速拖动，在最后三个 Move 的源历史中每个样本出现两次，
Compose 为 1071.429 DIP/s，Avalonia 为 749.999 DIP/s；目标选择相同，但释放后的轨迹存在差异。
读数来自真实输入，诊断历史和最终采样见 [移植证据](../../../../artifacts/wide-navigation-rail-port/README.md)。
