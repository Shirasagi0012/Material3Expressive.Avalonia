# ToggleButton 实现说明

使用入口见 [README](README.md)。模板保留原生 ToggleButton，形状、描边、反馈均连接已有共享能力。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [ToggleButton.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ToggleButton.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ToggleButton.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。
不要用上游 HEAD 替代固定提交，也不要把当前实现直接视为一致性或设计授权的证明。

相关固定上游文件还包括 `Button.kt`、`internal/AnimatedShape.kt` 及 Button 尺寸/外观 Token。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| `ToggleButton`、`FilledTonalToggleButton`、`ElevatedToggleButton`、`OutlinedToggleButton` | [ToggleButton.axaml](ToggleButton.axaml) 四种原生主题 |
| `ToggleButtonColors`、checked/pressed shapes | [ToggleButtonAppearance.cs](ToggleButtonAppearance.cs)、[ToggleButtonTokenSource.cs](ToggleButtonTokenSource.cs) |
| `ButtonDefaults.iconSizeFor` / `iconSpacingFor` | [ButtonContentRow.cs](ButtonContentRow.cs)，标签复用 ButtonContentPresenter |
| `shapeByInteraction` / `rememberAnimatedShape` | [ButtonSurfaceState.cs](ButtonSurfaceState.cs) 将三形状状态输入共享 MaterialSurface / AnimatedCornerRadius |
| `animateBorderStrokeAsState` | [AnimatedButtonBorder.cs](AnimatedButtonBorder.cs) 连接共享 SurfaceOutline |

## 模板与动画不变量

ButtonLayout 的 ContainerMinWidth 为 0，没有普通 Button 的 58 DIP 默认值；五档高度与外层 48 DIP 留白分开。
Icon 槽使用固定盒，XS 间距按 iconSpacingFor 为 4 DIP，不能使用原始 ButtonXSmall 的 8 DIP。
标签使用 ButtonContentPresenter，任意图标需自己消费前景色或用 MaterialSymbol。
颜色转换把实时 Token、可选六槽配置、原生 IsChecked/IsEffectivelyEnabled 组合为背景/内容色；
null 按未选中，禁用忽略选中状态。原生局部值/绑定仍优先。

三形状优先级为 pressed > checked > rest；Small 按压角 6 DIP。
形状用 FastSpatial，配置变化按上游重建状态，反向和第三目标复用共享 AnimatedCornerRadius。
轮廓在插值之后按边界归一化，不能预先以绘制轮廓替代动画输入。
SplitButton 也复用 ButtonSurfaceState，但它的配置重置、spring 和光学布局规则不同，不能统一成 Toggle 规则。

AnimatedButtonBorder 宽度使用 FastSpatial Dp spring（可见阈值 0.4 DIP），实色颜色用 DefaultEffects，
目标合并在下一时钟帧。动画值保持连续，SurfaceOutline 统一执行物理 ceil、hairline 和尺寸上限；
动画层不重复内缩圆角。非实色 Brush 按原画笔传给绘制层。
模板替换/卸载释放子动画作用域，重挂载建立新生命周期，不复用旧的 border 播放状态。
Elevation、状态层和 ripple 仍由共享层维护，Surface 内部裁剪而外层不裁剪。

## 已确认的组件选择

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/ToggleButton/README.md)。
以下只保留影响维护判断的决定和边界；旧测试数量、依赖版本及临时报告路径不是当前验证结果。

2026-10-04，维护者确认原生 ToggleButton 与附加外观 API：复用原生输入、命令、状态绑定与 automation peer，
IsChecked 先变更再触发 Click/Command，Enter 按下、Space 默认松开，nullable/三态按本地扩展保留。
与 Compose 受控 checked/onCheckedChange 不同，属性写入当帧与下一次重组更新的时机也不同。
这些适配适用于 SplitButton 尾部原生 ToggleButton，不能推广到外部选择职责的 Chip 或 SegmentedButton。

共享决定只链接原文范围：

- [Surface 圆角描边](../../../../docs/DECISIONS.md#surface-圆角描边)：合并时保留共享 Outline 并补齐非对称算法，避免两次半线宽内缩
- [Core Desktop 焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)与[键盘职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)
- [最小交互布局](../../../../docs/DECISIONS.md#最小交互布局与工具栏对齐读数)

## 一致性边界

尚未实现任意 Shape/CutCornerShape 与外部 MutableInteractionSource。
旧 4× headless 记录中，静态场景均在既定容差内，动态仍有原生 Enter 及属性/主题/禁用当帧时机差异。
三个 hover 场景在 64ms 的共享 elevation 残差约 0.0000003–0.0000006 DIP，旧记录明确未认定为已接受；
不能从[Chip 精度决定](../../../../docs/DECISIONS.md#本次保留-tween--bezier-精度差异)自动扩大授权。
这些观测应通过既有测试重新采集复查，不能据旧结论宣称当前全部 parity 通过。

合并描边曾新增像素回归，保留非对称轮廓、重叠角半径缩放和切换当帧差异；
相关共享回归亦不能当作 Button/IconButton/Chip 等全部通过。
非常大的非对称角、运行中约束更换、任意自定义内容组合尚未验证完整 Shape 等价性。
无字形 marker 不证明字体栅格、系统屏幕阅读器或真实桌面体验一致。

## 验证入口

- [控件测试](../../../../tests/Material3Expressive.Tests/ToggleSplitButtonTests.cs)：三形状/尺寸、原生键盘顺序、双向/nullable 绑定、图标色与生命周期，以及 Split 独立按钮和光学布局。
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ToggleButtonCases.cs) /
  [Compose 双端用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ToggleButtonCases.kt)
- [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml) 提供真实内容组合；其事件处理见
  [MainWindow.axaml.cs](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)

从仓库根目录执行，环境与结果分类见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~ToggleSplitButtonTests
./tests/VisualParity/parity.ps1 self-test --family toggle-button
```

`self-test` 重新采集双端并检查重复捕获及额外被动截图的稳定性；执行完成、稳定、
`withinTolerance` 与严格一致是不同结论。已接受差异仍保留真实参考和精确比较结果，
不得修改门禁来隐藏差异。复核时记录代码与依赖版本、执行环境和新生成的报告，
分别说明执行错误、双端差异、稳定性及未覆盖项。
