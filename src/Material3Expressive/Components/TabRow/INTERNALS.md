# TabRow 实现说明

## Token 配置路径

主题把尺寸、指示器/滚动/内容运动绑定到内部配置属性；EdgePadding、MinTabWidth 及标准/图标加标签两种容器高度的基线也放入 TokenSource。Primary/Secondary 只切换内部颜色及指示器候选，公开 Foreground、ContentColor 与 Indicator 在普通主题优先级接入，应用 Style、Binding、LocalValue 可覆盖。

SelectedContentColor=null 的环境色继承、UnselectedContentColor=null 跟随选中色是明确语义，继续保留；不是逐字段 nullable Options 合并。指示器/颜色/滚动位置仍是运行时结果，选择及页面所有权不变。

本文面向维护者；使用入口见 [README.md](README.md)。固定基线以
[upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 1.5.0-alpha27），
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（v1.13.0-alpha01）。
默认逻辑对照 AndroidX，桌面输入与平台实现对照 Core，不用浮动 HEAD 替代。

## 上游与本地职责

主要入口是固定提交的 [AndroidX TabRow.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/TabRow.kt)
及 [Core TabRow.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/TabRow.kt)。
下文 Material3 路径均以 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/` 为前缀。

还需读取固定 `Tab.kt` 与 `tokens/PrimaryNavigationTabTokens.kt` / `SecondaryNavigationTabTokens.kt`。
原记录核对两个仓库的 TabRow.kt 和 Tab.kt 相同，旧 deprecated Row 实现未移植。

| 上游关键符号 | 本地职责 |
| --- | --- |
| PrimaryTabRow / SecondaryTabRow、PrimaryScrollableTabRow / SecondaryScrollableTabRow | [TabRow.cs](TabRow.cs)、[TabRowAppearance.cs](TabRowAppearance.cs)：Variant × IsScrollable、公开位置和模板槽 |
| TabRow 布局、TabPosition、指示器位置与滚动居中 | [TabRowLayout.cs](TabRowLayout.cs)：TabRowChrome / ItemsPanel、intrinsic 查询、排列、动画及 ScrollViewer |
| Tab / LeadingIconTab、TabBaselineLayout | [Tab.cs](Tab.cs)、[TabHeader.cs](TabHeader.cs)：受控激活、内容布局、真实文字基线与颜色动画 |
| 默认颜色、Typography 与指示器 | [TabRowTokenSource.cs](TabRowTokenSource.cs)、[组件 Token](../../Tokens/MaterialTokens.Comp.TabRow.cs)、[TabRow.axaml](TabRow.axaml) |
| Avalonia 原生 TabControl / TabItem 合约 | 同一主题内的 MaterialTabControlTheme / MaterialTabItemTheme、两个 SelectedContentHost 和原生选择/页面过渡 |

## 选择与原生模板合约

独立 TabRow.SelectedIndex 只给指示器和居中使用；Tab.IsSelected 控制内容色与语义。应用必须同时维护。
Click / Command / automation Select 不修改选择；Space / Enter KeyUp 激活，失焦/禁用取消未完成的键盘激活。
ItemsSource 的模板应真实生成 Tab，不能让外层 ItemsControl 把任意内容隐式当作已选 Tab。

原生 TabControl 保留 SelectedIndex / SelectedItem、HeaderTemplate / ContentTemplate、SelectedContent、
原生键盘与 automation、PageTransition。模板内 PART_SelectedContentHost / PART_SelectedContentHost2
用于原生页面呈现；仅标签栏使用 TabRowChrome 和统一 Indicator，不让页面内容参与标签色动画。
原生 TabItem.IndicatorTemplate 被统一的 Row Indicator 槽替代。

## 测量、动画与槽

固定 Row 按物理像素整数均分，不分配余数。高度来自最大 intrinsic height。
Primary 的 ContentWidth 是 intrinsic width 减去 32 DIP，最低 24；Secondary 指示器覆盖整个 Tab。
滚动 Row 的默认 `EdgePadding=52`、`MinTabWidth=90`，内容宽度与 fixed Row 的约束规则分别保留。
RTL 的 Left/Right 是 start-relative，所有 `TabPosition` 数值单位为 DIP。

指示器宽度和位移使用 `Animatable<float>` 的 DefaultSpatial 弹簧，保留中断速度。
Tab 内容颜色使用 `Transition<bool>`，进入选择使用 DefaultEffects，其余使用 FastEffects。
初次布局或选择索引变化自动居中；同一索引下改变容器/内容宽度不会主动重新居中。
自动滚动保留物理整数位置及 Float 余数累计，用户滚轮/滚动手势会取消正在进行的自动居中。
手势、滚轮步长和惯性由 Avalonia ScrollViewer 提供。

`Indicator` / `Divider` 是 `IControlTemplate`，`null` 移除该槽。
指示器模板由 Row 放在动画后的目标矩形内；默认 Primary 高 3、圆角 3，Secondary 高 3、矩形。
它的 DataContext 是只供读取的 `IReadOnlyList<TabPosition>`，也可通过 `TabRow.TabPositions` 或
`TabRowAppearance.GetTabPositions(native)` 读取。原生 TabItem 的单项 `IndicatorTemplate` 由 Row 的统一模板槽替代。

标准标签默认 TitleSmall / 居中。TabHeader 读取实际 TextBlock.TextLayout 的首/末基线；
非文字内容可通过根控件的 TextBlock.BaselineOffset 提供基线。只有标准 Stacked 分支提供 Badge bounds。
Stacked / Leading 分支把 IconTemplate 也视为图标槽存在，即使内容为 null；Custom 布局仍完全交给调用方。
TabRow 的 MaterialSymbol 默认尺寸使用通用 24 DIP，显式尺寸和其他自定义内容不受限制。
Leading 和 Custom 不应意外继承 Stacked 的基线、字体或 BadgeBounds 规则。

Avalonia 没有 Compose intrinsic API，TabHeader 查询真实内容后必须按实际约束重新测量，
避免查询约束污染后续 Arrange。intrinsic 变化、父约束变化与选择变化分别遵循上游更新顺序。
模板替换与 detach 取消旧动画、滚动任务和帧订阅；重新布局按当前选择恢复位置。

颜色默认值和 TextStyle 的共享规则见
[聚合排版](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)、
[颜色转换](../../../../docs/DECISIONS.md#composeanimation-颜色转换与原始动画终值)。
焦点/激活边界见[共享交互](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)与
[桌面焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)。

## 已确认适配

以下确认与历史观测依据来自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/TabRow/README.md)；不以当前代码反推维护者授权。
2026-10-04 维护者确认两个入口：独立入口保留应用受控选择；原生入口保留 Avalonia 选择行为。
这样既支持外部路由，也不重写原生 TabControl 的页面、导航与 automation。
原生 TabItem 在指针按下和 Directional focus 时选择，Compose Tab 在激活回调时选择；
`dynamic.press.native` / `dynamic.directional.native` 保留差异。
范围是两个入口的状态职责与原生输入时机；布局、内容色、指示器动画仍以固定 Compose 为目标，
程序化选择继续走同样的双端回归，不能把其他差异归入“原生适配”。

局部 intrinsic 查询的当前机制如上；代码和编译成功不独自证明它覆盖所有自定义内容。
固定/滚动、四种 Row 与不同内容尺寸都应通过跟踪测试重新验证。

## 一致性边界

- 尚未实现 deprecated TabRow / ScrollableTabRow 的旧入口；当前四种 Row 用 Variant × IsScrollable 表达
- 原生主题只支持顶部水平 TabStripPlacement；其他方向不在范围内
- 旧合并自检的静态用例严格相等，动态用例有按压/键盘采样的少量 raster 容差差异（1–4 像素、最大通道差 1），
  以及上述两个原生输入差异。双端重新采集、重复捕获与额外被动截图稳定，未改容差/遮罩/基线；不是本次验证结果
- headless 检查真实标准字体与基线，visual parity 使用真实槽中的确定几何标记；两者都不证明真实文字栅格一致
- 桌面触摸/惯性与原生 PageTransition 实际运行画面仍需人工验证

## 验证入口

从仓库根目录执行，环境与结果含义见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~TabRowTests
./tests/VisualParity/parity.ps1 prepare-reference --family tab-row
./tests/VisualParity/parity.ps1 run --family tab-row
./tests/VisualParity/parity.ps1 self-test --family tab-row
```

固定参考源码与配置不变时，无需每次 prepare-reference。
[TabRowTests.cs](../../../../tests/Material3Expressive.Tests/TabRowTests.cs)、
[Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/TabRowCases.cs)和
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/TabRowCases.kt)是已跟踪的复现入口。
Catalog 入口为 [TabRowDemo.axaml](../../../Material3Expressive.Catalog/TabRowDemo.axaml)。

控件测试重点包括 IndependentTab_ClickAndAutomationRequest_DoNotMutateSelection、
NativeTabControl_DirectionalFocusRetainsNativeSelection、ContentResize_ReplaysRealMeasureAfterIntrinsicProbes、
ScrollableRow_UserInputInterruptsCenteringAndDetachCancelsFrames 与真实多行基线。
双端覆盖四种 Row、两入口、各内容布局、RTL/主题/颜色/内容变化、窄/受限尺寸、模板槽、反向与初始居中。

上述命令与覆盖说明不代表当前修订已执行全量验证。旧测试数量与未跟踪 artifacts 不作为当前验证结果；
详细历史结果见[固定修订的原记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/TabRow/README.md)，复核以跟踪测试和新生成报告为准。
`run` 执行完成、`self-test` 重复稳定与两端相等是不同结论，必须分别报告。
