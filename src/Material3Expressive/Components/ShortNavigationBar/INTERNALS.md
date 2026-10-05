# ShortNavigationBar 实现说明

## Token 配置路径

指示器运动及几何通过主题绑定成为内部部件有效输入；按压映射与布局共用指示器宽度，不再在控件内查询 Token。默认 Insets 来自组件 Token；位置切换、intrinsic 算法和选择状态保持原职责。

本文面向维护者；使用入口见 [README.md](README.md)。固定基线以
[upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 1.5.0-alpha27），
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（v1.13.0-alpha01）。
默认逻辑对照 AndroidX，桌面输入与平台实现对照 Core，不用浮动 HEAD 替代。

## 上游与本地职责

主要入口是固定提交的 [AndroidX ShortNavigationBar.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ShortNavigationBar.kt)
及 [Core ShortNavigationBar.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ShortNavigationBar.kt)。
下文 Material3 路径均以 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/` 为前缀。

固定 NavigationItem.kt 是本族 Item 的依据，不能换成 Wide Rail 使用的 AnimatedNavigationItem。
原记录核对两个仓库的 ShortNavigationBar.kt / NavigationItem.kt 分别相同。

| 上游符号 / 文件 | 本地职责 |
| --- | --- |
| ShortNavigationBar、两种 ContentMeasurePolicy | [ShortNavigationBar.cs](ShortNavigationBar.cs)、[ShortNavigationBarItemsPanel.cs](ShortNavigationBarItemsPanel.cs)：选择组、EqualWeight / Centered |
| NavigationItem、TopIconOrIconOnlyMeasurePolicy、StartIconMeasurePolicy、placement 函数 | [ShortNavigationBarItemLayout.cs](ShortNavigationBarItemLayout.cs)：位置、固有尺寸与徽标边界 |
| animateIndicatorProgressAsState、StyledLabel、MappedInteractionSource | [ShortNavigationBarItem.cs](ShortNavigationBarItem.cs)、[ShortNavigationBar.axaml](ShortNavigationBar.axaml)：单条指示器轨迹、槽与反馈 |
| tokens/NavigationBarTokens.kt、NavigationBarVerticalItemTokens.kt、NavigationBarHorizontalItemTokens.kt | [ShortNavigationBarTokenSource.cs](ShortNavigationBarTokenSource.cs)、[组件 Token](../../Tokens/MaterialTokens.Comp.ShortNavigationBar.cs) |
| Core ui/layout/MeasurePolicy.kt 的默认 intrinsic 重放 | 局部查询实际内容，任意非 Item 内容使用原生 Measure；不引入通用 intrinsic 框架 |

## 测量与动画不变量

默认高度 64 DIP，无项间距。EqualWeight 用整数除法分配等宽，余数留在逻辑末尾。
Centered 按数量计算留白：1～6 项单侧比例为 30/25/20/15/10/5%，超过 6 项为零。
先查询固有高度以统一增高，再按固有宽度扩大 Centered 项宽，最大不超过容器宽度 / 项数；
每项增量分别执行整数 / 2 减少留白。Top/无标签的默认 intrinsic 不采用实际胶囊宽度：
DefaultIntrinsicMeasurable 下空 ripple 槽宽度为零；Start 带标签则有显式宽度公式。

Top 使用上方 6 DIP 留白，24 DIP 图标的胶囊为 56×32，图标与标签间为 8 DIP。
Start 带标签时胶囊包住图标和标签，两侧 16 DIP、图标到标签 4 DIP、上下 8 DIP。
Icon presenter 将 ShortNavigationBar.IconSize Token 24 DIP 作为 MaterialIcon.DefaultSize 传给无显式尺寸的
MaterialSymbol；自定义图标继续按实际尺寸布局。
无标签仍使用 TopIconOrIconOnlyMeasurePolicy，但采用目标位置的 padding，Top/Start 胶囊高度为 32/40 DIP。
没有改成 Wide Item 的无标签圆形分支。

Item 布局提供 Compose `badgeBounds()` 的上方和末端边界，供图标槽中的 BadgedBox 使用。
Start 带标签时按 `placeLabelAndStartIcon` 保留标签先于图标、反馈最后的绘制顺序，
因此图标中溢出的徽标可以覆盖标签。规则和双端覆盖见 [Badge / BadgedBox](../Badge/README.md)。

内容色直接更新，两种位置均使用 LabelMedium。唯一 DefaultSpatial 进度同时驱动指示器透明度及居中展开宽度。
宽度进度只下限钳制，保留过冲；绘制透明度限制在 0～1。目标下一帧提交，反向按 animateAsState
Stop/restart 重置旧速度与帧锚点。位置切换立即布局；不使用 AnimatedNavigationItem 或 Wide 的纵向平滑偏离。

Item 使用 Button 的事件/命令，但 Enter 与 Space 在 KeyUp 激活；IsSelected 始终由应用控制。
自动化报告 TabItem、选择状态及容器，Select 只请求导航；图标语义不随标签隐藏。

整个 Item 的透明布局矩形参与点击和 hover。Top 的按压偏移为
`((itemWidth - 56 DIP) / 2, 12 DIP)`；Start 使用原始 Item 坐标，不按实际胶囊或宽图标修正。
模板替换与卸载取消旧动画；内容固有尺寸失效必须通知 Bar 重新查询，不能只依赖固定槽的 DesiredSize。

共享[反馈/激活边界](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)、
[桌面焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)与
[聚合排版](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)按各自决定处理。

## 已确认适配

以下确认与历史观测依据来自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/ShortNavigationBar/README.md)；不以当前代码反推维护者授权。
2026-10-03 维护者确认“采用推荐 API 与测量适配”：使用原生 ItemsControl 内容入口、组件局部固有尺寸查询，
原因是 Avalonia 没有 Compose intrinsic API；范围仅为本族，不建立通用 intrinsic 能力，不统一 Short 与 Wide 的动画模型。
WindowInsets 是宿主提供的物理 Thickness，桌面为零、RTL 不交换边，没有嵌套 inset 消费环境。

同日确认“采用原生 Width 语义并记录差异”：显式 Width 固定 Bar 宽度，各项仍按排列分配。
固定 Compose 直接 Modifier.width / fillMaxWidth 会把 minWidth 传给每项，经 constraints.constrain
放大为整个 Bar 宽度；旧 352 DIP / 3 项场景每项均为 352 DIP 并溢出。
正常 parity 由外层宿主提供有限最大宽度；`static.upstream.direct-width` 保留真实 different。
范围仅为容器 Width，不改变 Centered / EqualWeight 整数算法、参考源码、容差或其他组件。

## 历史问题与待核验范围

旧验证在按压中切换 IconPosition 后观察到额外 hover，并记载 indication 重挂方案待确认。
当前 [ShortNavigationBarItem.cs](ShortNavigationBarItem.cs) 已在位置改变时从布局移除并重新加入 ripple Surface，
以重建 indication；这是当前代码事实，不独自证明方案已获批准或旧差异已消除。
现有记录没有提供这一变化后的同场景新结果及确认依据，需重跑 `dynamic.position-press` 并核对历史授权。
旧文把它与 Width 同列在两个 different 之后，不能据此视为已接受。
整族 require-match 至少受已接受 Width 差异影响；应逐项读取结果，不调整容差，也不将所有 different 解释为 Width。

内容色直接变化，默认 Foreground 不随自定义 Background 重做 contentColorFor；这是当前默认值关系，
不要为统一外观擅自改为常规 Bar 的动态内容色绑定。真实文字/Material Symbols 栅格、人工桌面交互及屏幕阅读器未经验收。

## 验证入口

从仓库根目录执行，环境与结果含义见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~ShortNavigationBarTests
./tests/VisualParity/parity.ps1 prepare-reference --family short-navigation-bar
./tests/VisualParity/parity.ps1 run --family short-navigation-bar
./tests/VisualParity/parity.ps1 self-test --family short-navigation-bar
```

固定参考源码与配置不变时，无需每次 prepare-reference。
[ShortNavigationBarTests.cs](../../../../tests/Material3Expressive.Tests/ShortNavigationBarTests.cs)、
[Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ShortNavigationBarCases.cs)和
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ShortNavigationBarCases.kt)是已跟踪的复现入口。
Catalog 入口为 [MainWindow.axaml](../../../Material3Expressive.Catalog/MainWindow.axaml)。

用例覆盖排列、位置、标签、选择、禁用、项数/余数、受限尺寸、颜色、主题、RTL/Insets 和嵌套 Surface；
动态覆盖反向、按压取消、禁用、完整 hover 区域、位置/排列切换、标签恢复与内容尺寸变化。
旧完整比较重新采集 Avalonia 与全部 Compose 参考，新增场景做了双端重复捕获与被动截图自检；
不将新增场景的稳定结论扩大为完整矩阵均做过同样自检。

上述命令与覆盖说明不代表当前修订已执行全量验证。旧测试数量与未跟踪 artifacts 不作为当前验证结果；
详细历史结果见[固定修订的原记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/ShortNavigationBar/README.md)，复核以跟踪测试和新生成报告为准。
`run` 执行完成、`self-test` 重复稳定与两端相等是不同结论，必须分别报告。
