# Chip 实现说明

使用入口见 [README](README.md)。本族将四种 Chip 组合为独立 Button 派生控件，不由基类代写选择。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [Chip.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Chip.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Chip.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。
不要用上游 HEAD 替代固定提交，也不要把当前实现直接视为一致性或设计授权的证明。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| `AssistChip` / `SuggestionChip` / `FilterChip` / `InputChip`、`Chip` / `SelectableChip` | [Chip.cs](Chip.cs)、[Chip.axaml](Chip.axaml) |
| colors、elevation、Shapes / ChipArrangement | [ChipAppearance.cs](ChipAppearance.cs) 的配置对象及 Chip.cs 解析 |
| `ChipContent` / `AnimatingChipContent` | [ChipLayout.cs](ChipLayout.cs)、[ChipIconSlot.cs](ChipIconSlot.cs) |
| 四族 Token 与 `ChipsTokens` | [ChipTokenSource.cs](ChipTokenSource.cs)、[Token 入口](../../Tokens/MaterialTokens.Comp.Chip.cs) |
| `ChipElevation` | Chip.cs 的 ElevationTarget / 交互目标更新，连接共享 MaterialInteraction |

## 内容、布局与状态不变量

默认容器 32 DIP、交互最小高 48 DIP、Small 圆角 8 DIP、LabelLarge。
固定入口前后间距 8 DIP；Shapes 非空按实际槽采用 4/8 DIP 紧凑间距，Input Padding 保留 avatar/leading/trailing
组合规则。布局按物理像素舍入，leading/trailing 先测量，标签消费剩余约束。
Selectable 的默认 1000 DIP 最大宽度不吞掉显式 Width/MinWidth；RTL 使用 Avalonia 子树反射。

头像优先于 leading 图标，默认圆形裁剪，禁用只降低头像包装层 opacity，不降低整条内容。
Suggestion 不显示 trailing；其他 trailing 与 Chip 共享动作区域。
Filter/Input 的 Click、Command、automation Toggle 只发请求，不写 IsSelected。
自动化身份为 CheckBox，外部 selection 变化发出 ToggleState 通知。

原生 paint/padding 局部值保留属性优先级。Surface 的反馈墨色通过实时 contentColorFor 绑定，
透明或非主题背景沿用外层内容环境；标签和两个图标分别解析自己的颜色 provider。
因此只改背景、只改标签色或外层内容环境都需要检查反馈颜色，不可缓存初始化时的色值。

## 形状、槽动画与 elevation

Shapes=null 不启用按压形变；非空采用未选中/选中/按压三个圆角及 FastSpatial。
配置按解析后的三个圆角值比较：等值对象不重置动画，不同值对应上游 key(shapes) 重建。
容器尺寸变化后必须重新计算轮廓，包括动画已经结束的选中状态。

Filter/Input 槽以 Transition 的 size/alpha 轨迹进入退出，退出保留最后非空内容，反向保持运行状态；
固定与形状重载使用各自 motion Token。模板替换和 detach 取消旧 effect/作用域，
释放调用方图标的父级关系，重新挂载按当前状态建立动画。

Elevation 选择最后活跃的 press/focus/hover/drag；禁用 snap。
进入为 120ms FastOutSlowIn，退出 150ms Bezier(0.4,0,0.6,1)，hover 退出 120ms。
选中不改变高度策略；平面 Filter 默认 hover 为 0 而 Elevated 为 3 DIP。
null Elevation 表示变体默认，全零配置才关闭所有交互阴影。

## 已确认的组件选择

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Chip/README.md)。
以下只保留影响维护判断的决定和边界；旧测试数量、依赖版本及临时报告路径不是当前验证结果。

2026-10-03，维护者批准四个 Button 派生控件、外部选中职责、IsElevated、内容槽及颜色/elevation 配置，
并以 ChipShapes 圆角和 ChipSpacing 前后间距表达当前能力；不由此扩大到任意 Shape 或通用 Arrangement。

2026-10-04，维护者批准平面 Filter 的默认 hover elevation 从固定 Compose 的 1 改为 0 DIP，
范围为 Shapes=null/非空、未选中/选中。透明平面 Filter 悬浮时，Skia Ganesh GPU drawShadow 会暴露容器内部灰环；
固定 Skiko `0.152.0-alpha02` 原生 GPU 探针与 Avalonia 桌面均复现，CPU 未出现同样灰环。
修改仅在该默认高度，保留 hover 状态层、形状动画和其余高度，不修改共享阴影算法。
显式 Hovered=1 或覆盖 `Comp.Chip.FlatFilterHoverElevation` 可恢复上游高度及相应 GPU 效果。
这与下述阴影坐标接入是独立决定，真实 Compose 默认仍留在参考端。

共享决定按原文范围使用，不复制或扩大其授权：

- [Tween / Bezier 精度](../../../../docs/DECISIONS.md#本次保留-tween--bezier-精度差异)：Chip 的 double/Float 差异继续精确报告，不取整读数
- [阴影坐标接入](../../../../docs/DECISIONS.md#本次保留阴影坐标接入差异)：透明 AssistChip、6 DIP 阴影的 DIP+CTM / 物理像素路径 raster 差异
- [Surface 圆角描边](../../../../docs/DECISIONS.md#surface-圆角描边)：物理像素 ceil、内缩与不占用内容空间
- [Core Desktop 焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)：真实焦点影响 elevation，indication 单独过滤首次鼠标聚焦
- [共享键盘职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)：保留原生 Enter 按下激活

这些记录中的历史包版本不指定当前依赖版本；当前包接入以中央依赖配置和公共接入说明为准。

## 一致性边界

尚未实现任意路径/切角 Shape、通用 Compose Arrangement、hoisted MutableInteractionSource、
可配置 inset focus ring。圆角配置和逻辑前后间距不能描述这些上游能力的全部范围。

旧双端观测中的 64ms elevation 约 0.0000006 DIP 数值差异、阴影坐标差异、平面 Filter hover 高度及
Enter 时机分别按上述明确范围处理。没有把其余时序、布局或视觉差异一并认定为已接受。
双端使用无字形 marker，文字 raster/字体、屏幕阅读器及真实桌面手工体验未经这些用例验收。

## 验证入口

- [控件测试](../../../../tests/Material3Expressive.Tests/ChipTests.cs)：容器/交互尺寸、avatar 优先级、外部选择/自动化、实时颜色与原生 paint 优先级、编译 XAML 配置、形状更换/完成后 resize、elevation 顺序及模板/卸载清理。
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ChipCases.cs) /
  [Compose 双端用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ChipCases.kt)
- [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml) 提供真实内容组合；其事件处理见
  [MainWindow.axaml.cs](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)

从仓库根目录执行，环境与结果分类见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~ChipTests
./tests/VisualParity/parity.ps1 self-test --family chip
```

`self-test` 重新采集双端并检查重复捕获及额外被动截图的稳定性；执行完成、稳定、
`withinTolerance` 与严格一致是不同结论。已接受差异仍保留真实参考和精确比较结果，
不得修改门禁来隐藏差异。复核时记录代码与依赖版本、执行环境和新生成的报告，
分别说明执行错误、双端差异、稳定性及未覆盖项。
