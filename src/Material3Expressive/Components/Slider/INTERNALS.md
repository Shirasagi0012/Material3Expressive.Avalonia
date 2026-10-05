# Slider / RangeSlider 实现说明

使用方法与单值 API 迁移见 [README](README.md)。单值保留原生交互，RangeSlider 保留独立状态及输入。

## 默认配置数据流

Track 的尺寸、间隙、内部圆角与 Range thumb 默认尺寸由 TokenSource 经主题接到有效属性；不在属性元数据重复基线。垂直圆角收缩默认由主题 MultiBinding 选择，保持普通主题优先级，因此用户普通 Style、Binding 与局部值可覆盖；NaN 圆角、完整 Colors 及 State 的语义不变。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [Slider.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Slider.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Slider.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。
不要用上游 HEAD 替代固定提交，也不要把当前实现直接视为一致性或设计授权的证明。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| `SliderDefaults.Thumb` / `Track` / `CenteredTrack`、`SliderTokens` | [SliderParts.cs](SliderParts.cs)、[SliderAppearance.cs](SliderAppearance.cs)、[SliderTokenSource.cs](SliderTokenSource.cs) 与 [Token](../../Tokens/MaterialTokens.Comp.Slider.cs) |
| 单值布局与原生输入契约连接 | [Slider.axaml](Slider.axaml)、[MaterialSliderLayout.cs](MaterialSliderLayout.cs) 的 MaterialSliderLayout / MaterialSliderTrack |
| `RangeSlider` / `RangeSliderState`、键盘、slop 与 semantics | [RangeSlider.cs](RangeSlider.cs)、[SliderState.cs](SliderState.cs)、[SliderAutomationPeer.cs](SliderAutomationPeer.cs) |
| Range 布局、不等宽 Thumb 修正 | [SliderLayout.cs](SliderLayout.cs) |

原生单值交互依据项目 Avalonia 12.1.2，对应上游 tag commit
`d3c867a9e2de379249b03dbeb3495bd7f076a81a`；不要把原生行为误写为 Compose 行为。

## 单值布局与绘制职责

MaterialSliderLayout / MaterialSliderTrack 只适配几何，不处理指针/键盘或写 Value。
它们保留物理像素取整、离散圆角偏移、Thumb 间距、48 DIP 最小交互空间与受限尺寸。
真正的 `PART_Track`、`PART_Thumb`、Increase/Decrease Button 继续处理完整 Track 边界上的原生输入。
默认 SliderThumb 观察原生 owner 的方向、焦点和 `:pressed`，沿运动轴即时缩窄绘制宽度，
布局尺寸不跟着变化；不人为延迟一帧以模拟 Compose 重组。

不规则 Ticks 优先于 TickFrequency，绘制侧只将有效范围内刻度转换为排序后的 fraction；
吸附及 tie rule 留给原生 Slider。运行时 Ticks 或圆角变化会重新排列离散 Thumb。
Track/Thumb 使用最终绘制颜色；默认禁用 Thumb 在 Surface 上合成，显式颜色不再次合成。
DrawTick/DrawStopIndicator 的坐标属于轨道局部 DIP，不是整个 owner 或物理像素。

## Range 状态、输入与自动化

RangeSlider.State 非空时拥有范围和值；便捷属性只驱动内部 State。构造先约束、排序而不吸附，
setter 先受另一端约束再吸附，顺序不能交换。OnValueChange 存在时只发请求，无回调则直接写 State。
范围和 steps 构造后固定，State 替换取消手势、重新订阅，并刷新默认/自定义槽与自动化。

SliderLayout 保留不等宽 Thumb 校正、minimumInteractiveComponentSize 和居中溢出。
RangeSlider 自行处理 RTL 并绕过自动镜像，避免方向被翻转两次。
捕获在模板布局节点的 tunnel 阶段处理，按下不自动请求鼠标焦点。
拖动中禁止端点交叉；Start/EndInteraction 分别反馈手势与焦点。

两端 presenter 独立聚焦并提供 IRangeValueProvider，父级为 Group；自动化边界沿轴扩展 10 DIP。
自定义 Thumb 的 AutomationProperties.Name 可成为本地化名称。完成通知仍按点击释放、
拖动结束/取消、受支持键 KeyUp 或产生变化的自动化赋值触发；取消回调若修改布局，
下一手势必须重新读取端点和边界，不能使用旧捕获几何。

## 已确认的组件选择

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Slider/README.md)。
以下只保留影响维护判断的决定和边界；旧测试数量、依赖版本及临时报告路径不是当前验证结果。

2026-10-03，维护者确认“原生 Slider + Material 主题，使用原生交互”，批准移除单值 State/API，
并明确“不补 ValueChangeFinished”。理由是将数值、输入和框架契约交还单一原生 owner，
Material 只提供外观与模板适配；这一范围不改变 RangeSlider / RangeSliderState。

单值接受范围包括轨道按下即时改值、原生捕获与拖动、鼠标自动聚焦、键盘/RTL/修饰键规则、
double 数值、范围约束、程序/自动化赋值不吸附、原生自动化边界/通知和无统一完成回调。
parity 的 `changes` 两端分别代表实际值变化事件与 Compose 请求回调，明确不可比较；
单值 `finished` 标为不支持，不得伪造回调让测试相等。

Range 的显式 State 与调用者写回职责、内容模板方案同日获确认。
另外维护者批准仅在按下时两端重合的指针手势中，按首次有效水平拖动方向选 Thumb，随后固定；
释放事件才出现水平位移也适用。无拖动点击、非重合手势、键盘、自动化以及拖动中才合并的端点
保留原规则。选择转移同时转移反馈，继续保留 slop、非交叉约束、取消和完成通知。
固定 Compose 在按下已选定 Thumb，只能朝被选端允许的方向拉开；真实差异必须保留。

## 尚未实现与未经验证

维护者明确延期可选 `LocalRippleThemeConfiguration.Focus.InsetRing` 及其 padding/轨道间距，
当前共享 Ripple 无对应配置；默认焦点可用不代表该测量分支存在。
真实桌面、触屏/触控笔、原生屏幕阅读器、平台 numpad 映射未经完整验收；
旧自动化证据来自 Windows Skia headless，不推广到实际 OS 客户端。

双端矩阵分别覆盖水平/垂直/区间、连续/离散、启用/禁用、LTR/RTL、边界、主题、
颜色、尺寸、圆角、Centered、自定义 Thumb、程序赋值、键盘/拖动与中断。
单值已接受的原生差异和不可比较字段仍会被报告，不能把 self-test 执行完成当成全量匹配。

## 验证入口

- [控件测试](../../../../tests/Material3Expressive.Tests/SliderTests.cs)：原生模板对照输入/事件顺序、绑定、程序/自动化赋值、Token、几何/不规则刻度及完整 Range 状态、自动化、重合/取消手势。
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/SliderCases.cs) /
  [Compose 双端用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/SliderCases.kt)
- [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml) 提供真实内容组合；其事件处理见
  [MainWindow.axaml.cs](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)

从仓库根目录执行，环境与结果分类见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~SliderTests
./tests/VisualParity/parity.ps1 self-test --family slider
```

`self-test` 重新采集双端并检查重复捕获及额外被动截图的稳定性；执行完成、稳定、
`withinTolerance` 与严格一致是不同结论。已接受差异仍保留真实参考和精确比较结果，
不得修改门禁来隐藏差异。复核时记录代码与依赖版本、执行环境和新生成的报告，
分别说明执行错误、双端差异、稳定性及未覆盖项。
