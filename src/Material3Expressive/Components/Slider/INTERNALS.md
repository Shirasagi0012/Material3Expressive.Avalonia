# Slider / RangeSlider 实现说明

使用方法与单值 API 迁移见 [README](README.md)。单值保留原生交互，RangeSlider 保留独立状态及输入。

## 默认配置数据流

两种布局的子项测量空间下限由 `MinimumMeasureLength` / `MinimumMeasureThickness` Token 提供，默认分别为 4 / 16 DIP；单值垂直模式交换轴，最终尺寸仍按原顺序约束到调用者的可用空间。

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

`RangeSlider.Interaction.cs` 对应 `rangeSliderPressDragModifier` 的 Press → Cancel → Drag.Start → Stop/Cancel，
两端独立源，所有结束事件引用原始开始事件。纵向超过 slop 取消 Press；释放事件首次发生水平位移时
仍走立即完成的 Drag。同步回调换源会中止当前输入路径，旧源事件回调不会写入新 collector。
`StartInteraction` / `EndInteraction` 保留原来的按压、拖动和焦点位；hover 通过源提供，不扩展旧属性含义。

`SliderThumb` 对应 `ThumbContent`，按身份维护重叠事件，不通过布尔标志抹去外部事件。
换源时保留 remembered 活动列表。固定 Core 的 pointerInput key 重启没有向旧源发出 Drag.Cancel，
Thumb 可能继续缩小；双端 `source` 用例验证这一现象。本地保留该语义，未把它改成自动清空。
卸载会先退出 hover，再取消拖动，清除本地 collector；禁用取消活动输入与焦点。

Avalonia 捕获布局节点后不再把 Thumb 视为 pointer-over，故 SliderThumb 在捕获期间重算自己的
布局盒命中，并在布局后更新移动 Thumb 的 hover。非捕获场景仍检查实际视觉命中、遮挡及裁剪；
顶层观察者在卸载时释放。固定上游 Thumb 的 hoverable 不随 enabled 关闭。

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

新增 `range-slider-interaction` 的 36 个双端用例中，24 个全等，36 个重复稳定且捕获错误为零
（`20261103-143939-ffaf59ca`）。12 个存在差异，其中 8 个是 disable / release-jump 的双端点 × LTR/RTL：
事件身份及顺序、区间值和 Thumb 几何一致，Label 在生命周期边界的呈现帧不同；不是已接受差异。
原因定位到源事件与 Compose 重组提交的先后，不能用删除过渡帧或延迟全部事件隐藏。
后续需进一步核对 TooltipLabel collector 的提交调度；它影响共享 Label 消费者，不仅是 Slider 绘制。

另外 4 个 focus 时间线验证：更换另一端源不会清理本端焦点，更换聚焦端源会向旧源发出 Unfocus。
自定义 Thumb 与父 RangeSlider 分别赋源时仍有时序差异：本地先给父级换源会让旧 Thumb collector
收到 Unfocus 后恢复宽度；Core 在同次重组中先取消旧 collector，保留已记住的 Focus 并继续缩小。
事件序列一致不等于消费者结果一致。该差异未获接受，也未通过调整调用顺序掩盖；推荐保持源身份稳定，
后续需明确组件源绑定的提交边界，影响示例中 RangeSlider / SliderThumb / TooltipLabel 三者的共同换源。

## 验证入口

- [控件测试](../../../../tests/Material3Expressive.Tests/SliderTests.cs)：原生模板对照输入/事件顺序、绑定、程序/自动化赋值、Token、几何/不规则刻度及完整 Range 状态、自动化、重合/取消手势。
- [交互源测试](../../../../tests/Material3Expressive.Tests/RangeSliderInteractionTests.cs)：端点配对、重叠身份、生命周期、回调重入、遮挡和 Label 组合。
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/SliderCases.cs) /
  [Compose 双端用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/SliderCases.kt)
- `RangeSliderInteractionCases.cs` / `.kt` 记录真实输入的完整交互序列、Label 几何与截图。
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
