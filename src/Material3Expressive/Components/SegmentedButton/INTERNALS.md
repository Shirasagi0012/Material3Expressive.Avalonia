# SegmentedButton 实现说明

使用入口见 [README](README.md)。外部选择职责与行布局、图标动画分别维护，不能仿照原生Toggle自动写值。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [SegmentedButton.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SegmentedButton.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SegmentedButton.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。
不要用上游 HEAD 替代固定提交，也不要把当前实现直接视为一致性或设计授权的证明。

旧核验记录中两端组件文件blob均为`7b7382eeb52153f86368551ef3754bd638dc118a`；桌面输入和动画辅助按Core核验。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| `SingleChoiceSegmentedButtonRow` / `MultiChoiceSegmentedButtonRow` | [SegmentedButtonRow.cs](SegmentedButtonRow.cs) 的ItemsControl、行角色和布局失效 |
| 两种`SegmentedButton`重载、`itemShape`、状态层级 | [SegmentedButton.cs](SegmentedButton.cs)、[SegmentedButton.axaml](SegmentedButton.axaml)、[SegmentedButtonItemsPanel.cs](SegmentedButtonItemsPanel.cs) |
| `SegmentedButtonContentMeasurePolicy` | [SegmentedButtonContentLayout.cs](SegmentedButtonContentLayout.cs) |
| `SegmentedButtonDefaults.Icon` / `ActiveIcon` | [SegmentedButtonIconSlot.cs](SegmentedButtonIconSlot.cs) |
| `SegmentedButtonColors`、`OutlinedSegmentedButtonTokens` | [SegmentedButtonColors.cs](SegmentedButtonColors.cs)、[SegmentedButtonTokenSource.cs](SegmentedButtonTokenSource.cs)、[Token](../../Tokens/MaterialTokens.Comp.SegmentedButton.cs) |

## 行、内容测量与RTL

行等权宽度取最大intrinsic项宽，再扣相邻重叠Space；按物理像素分配取整余数。
Avalonia没有多内容intrinsic measure policy，内部布局按必要约束重放测量：高度由label决定，
icon受此高度约束，宽度始终预留IconSize+8。即使图标不可见也不能移除预留，否则标签动画和行宽改变。
标签offset惰性建立Animatable<int>，使用FastSpatial与整数截断转换，保留空间overshoot。
ItemsSource的ContentPresenter只ApplyTemplate，不在每次测量强制重建非复用模板而丢失应用状态。

首尾形状由行按位置/RTL生成，局部CornerRadius保持较高优先级。
内容使用物理place坐标，但文字和图标继续继承调用方FlowDirection；内部Surface chrome固定LTR，
避免Avalonia再次镜像已生成的物理圆角。CornerBasedShape按min(width,height)归一化每个竖向角对，
不能改用Avalonia按两个轴分别归一化的默认行为。

绘制层级为selected×5 + press + focus，hover不抬高；模板化ItemsSource应在实际sibling ContentPresenter
设置层级，不能只提高它内部按钮。默认Padding使用旧ButtonDefaults.ContentPadding的12/8，
不是Expressive Small的垂直10。禁用描边按colors factory使用Outline×0.12，不能误用原始DisabledOutlineColor的OnSurface。

## 选择、图标动画与生命周期

按钮Click/Command不写IsSelected，单选行不强制互斥；automation Select/Toggle也仅请求应用动作。
Space/Enter释放激活，失焦/禁用清理待激活键；共享控制器仍只处理反馈。
有inactive icon时使用Crossfade，任意反向段继续用其自身Spec；无inactive时使用AnimatedVisibility
的fade+scale（origin 0,1）和Exit.None。不要把两条动画路径的fallback混用。
模板替换/detach释放动画作用域、帧请求与调用方图标父级；Surface墨色跟随最终content色，包括只覆盖颜色配置一槽。

## 已确认的组件选择

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/SegmentedButton/README.md)。
以下只保留影响维护判断的决定和边界；旧测试数量、依赖版本及临时报告路径不是当前验证结果。

2026-10-04，维护者确认行/按钮API、外部选择职责、十二槽颜色、原生属性覆盖及组件内部intrinsic测量适配。
目的在于保留Compose状态所有权和必要布局关系，不增加通用布局或Shape框架。
任意路径/切角Shape及外部hoisted InteractionSource明确排除本次范围，仍属于尚未实现能力。

进入途中反向曾暴露共享AnimatedVisibility缺口：Exit.None缺失fade/scale规格时错误用Snap，
且缩放中心使用像素Point/Snap而非比例坐标spring。修正的决定、适用范围和包接入沿革见
[AnimatedVisibility中断规格与缩放中心](../../../../docs/DECISIONS.md#animatedvisibility-中断规格与缩放中心)。
复核ResetNoSnap后保留状态改变时重建通道规则；fade/scale fallback stiffness400、scale阈值0.002，
origin为比例坐标默认spring、阈值0.01。组件不复制依赖动画，也不新增公共API。
历史记录中的打包版本不应覆盖当前中央依赖配置。

## 一致性边界与历史观测

尚未实现的Shape/InteractionSource范围如上；真实桌面、文字raster和屏幕阅读器体验未经无字形场景验收。
旧主线合入记录中，全量双端结构化事实相等、截图在既定容差内且重复捕获稳定，未放宽门禁。
当时代表性FAB Default Extended反向、菜单反向、Medium Toggle反向和FilterChip反向选择回归也稳定，
但不代表FAB/Chip全量验证；这些历史结果不可写成本次或当前依赖版本已通过。

双端用例覆盖单/多选、主题/状态、宽高约束、RTL、形状/内容/图标运行时更换、Crossfade、
选择反向、按压/取消/禁用、键盘释放、部分content颜色覆盖及空间overshoot。
非文字marker仅隔离布局、颜色和图标动效，不能替代真实标签验收。

## 验证入口

- [控件测试](../../../../tests/Material3Expressive.Tests/SegmentedButtonTests.cs)：外部选择/Automation、键盘释放、行intrinsic与RTL、ItemsSource/图标模板、Token与原生覆盖、空间overshoot、模板/detach清理。
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/SegmentedButtonCases.cs) /
  [Compose 双端用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/SegmentedButtonCases.kt)
- [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml) 提供真实内容组合；其事件处理见
  [MainWindow.axaml.cs](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)

从仓库根目录执行，环境与结果分类见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~SegmentedButtonTests
./tests/VisualParity/parity.ps1 self-test --family segmented-button
```

`self-test` 重新采集双端并检查重复捕获及额外被动截图的稳定性；执行完成、稳定、
`withinTolerance` 与严格一致是不同结论。已接受差异仍保留真实参考和精确比较结果，
不得修改门禁来隐藏差异。上述命令是复现入口，不表示本次文档整理已执行它们。
