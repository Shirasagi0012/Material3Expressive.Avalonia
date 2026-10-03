# FloatingToolbar

`HorizontalFloatingToolbar` / `VerticalFloatingToolbar` 移植固定
[AndroidX / Core 基线](../../../../docs/upstream-baseline.json)的 `FloatingToolbar.kt`。
两份 Material3 源文件在该基线相同；动画还对照固定 `EnterExitTransition.kt` 和
`Transition.kt`，滚动测量对照 Foundation `Scroll.kt` / UI `Placeable.kt`。

## 入口与使用

两种方向共用 `FloatingToolbar : ContentControl`。普通入口用 `Content`、
`LeadingContent` / `TrailingContent` 及各自的模板；`Expanded` 只隐藏两侧槽。
提供 `FloatingActionButton` / `FloatingActionButtonTemplate` 后选择 FAB 入口，两侧槽不参与该布局。
该入口收起时仅显示 FAB，仍保留完整布局占位；FAB 从 56 DIP 动画到 80 DIP。
横向 FAB 位置为 `Start` / `End`，纵向为 `Top` / `Bottom`，默认 End / Bottom。

```xml
<m3:HorizontalFloatingToolbar Expanded="{Binding ToolbarExpanded}">
  <m3:HorizontalFloatingToolbar.LeadingContent>
    <m3:IconButton Symbol="ArrowBack" />
  </m3:HorizontalFloatingToolbar.LeadingContent>
  <StackPanel Orientation="Horizontal">
    <m3:IconButton Symbol="Edit" />
    <m3:IconButton Symbol="Share" />
  </StackPanel>
</m3:HorizontalFloatingToolbar>

<m3:VerticalFloatingToolbar Classes="vibrant" FloatingActionButtonPosition="Top"
                           Expanded="{Binding ToolbarExpanded}">
  <m3:VerticalFloatingToolbar.FloatingActionButton>
    <m3:FloatingActionButton Theme="{StaticResource MaterialVibrantFloatingToolbarFabTheme}">
      <m3:MaterialSymbol Symbol="Add" Width="24" Height="24" />
    </m3:FloatingActionButton>
  </m3:VerticalFloatingToolbar.FloatingActionButton>
  <StackPanel>
    <m3:IconButton Symbol="Edit" />
    <m3:IconButton Symbol="Share" />
  </StackPanel>
</m3:VerticalFloatingToolbar>
```

默认 Standard 使用 SurfaceContainer，`vibrant` class 使用 PrimaryContainer；
`Foreground` 通过 ContentColorFor 得到默认内容环境色。覆盖 Background、Foreground、Padding、
CornerRadius、ExpandedShadowElevation、CollapsedShadowElevation 可改变对应参数。
ExpandedShadowElevation 为 null 时选择普通入口 0 / FAB 入口 1 DIP；收起默认 0 DIP。
FAB 的 `AnimationSpec` 接受 `IFiniteAnimationSpec<float>`，默认 FastSpatial。
普通入口的槽使用上游 FastSpatial，交互留白使用 DefaultEffects。
默认 Padding 为 8 DIP，cross axis 最小尺寸为 64 DIP；ScreenOffset=16 由调用方的定位/Margin 应用。
上游原始 ContainerBetweenSpace Token 不等于 Row/Column 的默认 Spacing，本地不插入额外间距。

`MaterialStandardFloatingToolbarFabTheme` / `MaterialVibrantFloatingToolbarFabTheme` 对应上游两个
FAB helper：背景分别为 PrimaryContainer / TertiaryContainer，默认、按下和聚焦 elevation=3，hover=6。
模板保留原有 FAB 的交互和可见性能力，按 Compose Box 的规则将动画中的内容中心取整到物理像素。
自定义 FAB 内容需 Stretch 到槽的尺寸，显式固定 Width/Height 会限制其自身内容，和上游自定义
FAB 应使用 fillMaxSize 的要求对应。

## 滚动和无障碍

```csharp
var behavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(
    FloatingToolbarExitDirection.Bottom);
toolbar.ScrollBehavior = behavior;
toolbar.ScrollSource = scrollViewer;

// 自定义宿主传入实际已消费距离及剩余速度，向上为负。
behavior.OnPostScroll(consumedY: -24f);
float remainingVelocity = await behavior.OnPostFlingAsync(availableY: -400f);

using var subscription = FloatingToolbarDefaults.FloatingToolbarVerticalNestedScroll(
    scrollViewer,
    expanded: () => viewModel.ToolbarExpanded,
    onExpand: () => viewModel.ToolbarExpanded = true,
    onCollapse: () => viewModel.ToolbarExpanded = false,
    reverseLayout: false,
    expandScrollDistanceThreshold: 40,
    collapseScrollDistanceThreshold: 40);
```

`FloatingToolbarState` 保存 OffsetLimit、Offset、ContentOffset，距离公开为 DIP，速度为 DIP/s；
保留上游 Float 运算、钳制、方向、衰减后 snap 和返回剩余速度。默认 spline / snap 在实际 density
的物理像素中计算后换回 DIP，自定义规格按公开 DIP 单位工作。OffsetLimit 单独改变时不会自动钳制 Offset。
一个 behavior 只接入一个可见工具栏；脱离视觉树、更换 behavior 或触摸探索模式会取消旧 settling 和订阅。
工具栏自身可按 ExitDirection 拖动，上游释放速度不随 exit direction 再次反向的规则也保留。

`ScrollSource` 观察 ScrollViewer 的实际 OffsetDelta（转换符号），不把 wheel delta 当作已消费距离，
不额外消费输入。Avalonia 没有上游的 nested-scroll 剩余 fling velocity 回调；原生滚动不会自动调用
OnPostFlingAsync，自定义宿主在结束后显式传入速度（无剩余速度可传 0 来 snap）。
展开辅助订阅不隐藏整个工具栏、不写 Expanded，应用决定如何响应通知；同步回调后的状态立即读取，
其他应用状态改变在下次实际滚动前读取。阈值/方向在订阅时确定，修改时释放并重新订阅。

宿主使用 `TouchExplorationEnabled` 接入服务状态。启用时取消滚动接入并强制展开，automation 的
Expand/Collapse provider 保留上游独立的 forceCollapse 标志；Expand 只清除此标志，应用 Expanded
仍为 false 且未启用探索时不会强制展开。普通入口 Offset 非零时禁止后代聚焦，恢复时释放原属性值。

2026-10-04 维护者已确认共享 ContentControl / 槽 API、主题及 FAB 位置枚举、DIP 滚动状态与显式 fling、
宿主提供探索状态及 automation 适配，另行确认 IDisposable 展开辅助订阅。原生 ScrollViewer 的剩余速度
缺口属于上述已接受的框架适配。48 DIP IconButton 布局及内部交互对齐读数的确认见
[共享决定](../../../../docs/DECISIONS.md#最小交互布局与工具栏对齐读数)。

2026-10-04 维护者选择保留 ComposeAnimation 0.2.2-alpha，并记录动画缺口。
该依赖的 Transition 空闲唤醒使用 RunContinuationsAsynchronously，时钟订阅可能晚一帧；
展开/收起及中断的采样时间因此可能与 Compose 不同，也可能在重复捕获之间不同。
已观测到同一 FAB 展开输入在 64ms 的尺寸为 68 / 73.25 DIP。
本次未修改依赖、升级版本或新增动画引擎；保留原始差异与重复性产物。

## 验证边界

双端入口为 `tests/VisualParity/Material3Expressive.Parity/Cases/FloatingToolbarCases.cs` 与
`tests/VisualParity/reference/src/main/kotlin/cases/FloatingToolbarCases.kt`，调用真实组件 API。
覆盖方向、普通/FAB 两类入口、FAB 位置、Expanded、Standard/Vibrant、亮/暗色、RTL、槽组合、
空内容、受限/溢出布局、Padding/形状/颜色/elevation 覆盖、展开/收起/反向、主题和四方向滚动、snap/fling。
矩形内容使用受父约束的固有尺寸，对应 Modifier.size；Avalonia 的强制 Control.Width 在受限 Arrange
仍可能溢出，不用它替代参考的可被约束尺寸。文字、字体图标和实际桌面显示不在该 suite 的验证范围。

2026-10-04 的 4× Headless 全量对比执行 172 例，使用本次新准备的固定 Compose 参考并重新采集
Avalonia；末轮结果为 76 equal、87 withinTolerance、9 different，执行错误为 0。
130 个静态用例均为 equal / withinTolerance，几何读数一致；普通入口展开、收起和中断以及四方向
滚动、RTL、snap / fling 已对齐。9 个 different 均为 FAB 动画用例，保留上文已接受的唤醒时序缺口。
withinTolerance 不代表原始像素严格相等。对比产物位于
`artifacts/visual-parity/runs/20261003-230510-6f81b8b1/summary.md`。

新增场景的全量 self-test 执行 172 例，170 例稳定，2 例 Avalonia 捕获不稳定
（横向 balanced-padding 普通重复、横向 End FAB reverse 额外被动截图）；Compose 两项均稳定。
阈值和空内容修正后，普通入口 6 例、留白 2 例、空内容 4 例另行 self-test 均稳定，
其中 6 equal / 6 withinTolerance。自检独立重采双端，没有复用或发布缓存。
全量自检位于 `artifacts/visual-parity/runs/20261003-224008-8858d579/summary.md`；
补充自检为 `20261003-225724-9cecacc9` 至 `20261003-230022-b4912076` 的 12 个 run。
FAB 唤醒不稳定的具体诊断也保留在 `20261003-222732-84c6846f`，未因局部复测稳定而撤销该缺口。

`FloatingToolbarTests.cs` 检查 Token、状态钳制、占位、对齐、无障碍、实际 ScrollViewer 消费、订阅释放、
拖动、内容移出时的焦点恢复及 detach 清理。控件库全套 684 例通过；末轮布局/焦点修正后的
FloatingToolbar 14 例复跑通过，Catalog 构建通过。测试产物分别为
`tests/Material3Expressive.Tests/TestResults/floating-toolbar-final.trx` 和
`tests/Material3Expressive.Tests/TestResults/floating-toolbar-final-layout.trx`。
共享 IconButton 的 115 例回归及既有 ambient 颜色 / Outlined 描边差异见其
[组件说明](../IconButton/README.md)，本次提供的工具栏内容环境不会覆盖子控件显式选择的颜色。
实际桌面触屏惯性、宿主辅助技术服务和字体排版尚未经运行验证。

```powershell
./tests/VisualParity/parity.ps1 prepare-reference --family floating-toolbar
./tests/VisualParity/parity.ps1 run --family floating-toolbar
./tests/VisualParity/parity.ps1 self-test --family floating-toolbar
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --no-restore
dotnet run --project src/Material3Expressive.Catalog
```
