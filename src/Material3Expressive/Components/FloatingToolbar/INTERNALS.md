# FloatingToolbar 维护说明

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

ScrollSource 观察实际 ScrollChanged 的 OffsetDelta 并转换符号，不消费额外输入。
OnPostFlingAsync 仅由自定义宿主显式调用；设置新 behavior、detach 或触摸探索启用时取消 settling
与订阅。behavior 只能有一个可见 owner，不能共享其运行时状态。

展开辅助策略读取应用 Expanded；同步回调后立即读回，其他外部变化在下一次实际滚动前观察。
通知不改 Expanded、不隐藏整个工具栏；阈值/方向固定在订阅期，释放后才可换配置。
触摸探索由宿主提供；automation 的 forceCollapse 独立于应用状态，Expand 只清除此标志。
普通入口 Offset 非零时临时抑制后代焦点，恢复时释放旧属性值而不是强制写默认值。

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
