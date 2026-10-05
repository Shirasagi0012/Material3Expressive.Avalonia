# Tooltip 实现说明

使用入口见 [README](README.md)。维护时必须区分原生 ToolTip 的主题与服务适配、PlainTooltip
的内容表面，以及 TooltipBox 的状态/Popup 路径，不能以原生静态测试证明 Box 的行为一致。

## 默认配置数据流

TooltipBox 的缩放/透明度规格由 Box Token 引用系统 motion，经主题绑定内部配置属性；Transition 捕获当前属性，不再自行解析 Token 或补 Expressive 默认。PlainTooltip 仍只负责静态表面。
原生/Plain Tooltip 的 Padding、最小/最大尺寸及 Box Spacing 基线统一在 TokenSource；Plain 的软最小值绑定内部布局，不转成会抬高显式 Width/Height 的硬约束。位置工厂、互斥状态与超时职责保留。

## 固定上游与本地入口

固定版本以 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）为桌面分支依据。
下述 Material3 文件路径均相对于 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

| 上游文件 / 关键符号 | 本地职责 |
| --- | --- |
| `Tooltip.kt`：`PlainTooltip`、`TooltipBox`、`TooltipState`、`TooltipDefaults` | [PlainTooltip.cs](PlainTooltip.cs)、[TooltipBox.cs](TooltipBox.cs)、[TooltipState.cs](TooltipState.cs)、[TooltipDefaults.cs](TooltipDefaults.cs) |
| `internal/BasicTooltip.kt`：显示互斥、鼠标/焦点/长按与 dismiss 分支 | TooltipState 的 MutatorMutex 等待与 TooltipBox 的输入生命周期 |
| `tokens/PlainTooltipTokens.kt` | [PlainTooltipTokenSource.cs](PlainTooltipTokenSource.cs)、[Tooltip.axaml](Tooltip.axaml)：反色 Surface、BodySmall、默认尺寸 |
| `Tooltip.kt` 的位置提供器及 Core Popup 桌面定位 | [TooltipPosition.cs](TooltipPosition.cs) 的物理像素算法、TooltipBox.PlacePopup 的锚点窗口坐标 |
| Avalonia 原生 ToolTip 服务（本地适配） | Tooltip.axaml 的原生 ToolTip ControlTheme 和锚点附加属性；不增加输入控制器 |

AndroidX 是 Material 逻辑依据，Popup 的平台行为取固定 Core，尤其不套用 AndroidX 强制 focusable 分支。
原生 ToolTip 只共享静态 PlainTooltip Token，不是整个 Compose Tooltip 的等价入口。

## 三条路径的边界

原生主题提供 8×4 DIP padding、40×24 最小尺寸和 200 最大宽度。
定位默认值必须设置在锚点上，因为 ToolTip 服务从锚点读取 Top / -4 / overlay；
仅修改 ToolTip 自身 ControlTheme 不会改变定位。
原生显隐无动画，ShowDelay、BetweenShowDelay、禁用与关闭规则归 Avalonia 服务。

PlainTooltipLayout 的 `sizeIn(40×24, maxWidth)` 位于调用方尺寸内，显式 Width 可以超出默认范围。
其 Surface 默认 tonal/shadow elevation 均为 0，内容通过独立 presenter 消费 BodySmall 和前景。
Box 自身只测量锚点，提示属于独立 overlay Popup；`PART_Root` 在换模板时必须正确移除/重接 Popup。
Popup 同时在逻辑树与可视树接入，维持模板、资源和卸载行为。

## 状态、输入与动画不变量

TooltipState 的直接构造默认 persistent；TooltipBox 隐式实例默认 nonpersistent。
每个 Box 连接独立状态。默认 GlobalMutatorMutex 使同优先级/高优先级请求打断旧请求，
低优先级被取消；不能把它改成 Snackbar 式 FIFO。
`ShowAsync` 等待整个请求结束，非持续 Default / PreventUserInput 设 1500ms 超时，UserInput 不设超时。
非持续 Dismiss 只改变目标，不结束等待；PreventUserInput 的 finally 不自动关闭，由焦点或长按流程负责。

鼠标进入使用 UserInput，离开仅关闭非持续提示；焦点与长按使用 PreventUserInput。
长按阈值取平台 HoldWaitDuration，触发后消费释放。Escape KeyDown 关闭并消费，
HasAction 且可见时消费 Tab，但固定 Core Popup 不抢焦点，也不把焦点送入动作。
外部按下默认 Dismiss；存在 DismissRequested 订阅者时转交应用决定，外部点击仍传递。
Automation Invoke 表达锚点的长按显示，提示提供 ToolTip / Assertive live setting。

`IsVisible = CurrentState || TargetState`，退出期间保留 Popup，Transition 真正完成后移除。
更换状态或卸载取消旧等待、输入、帧与动画作用域，不影响其他 Box。
缩放目标在 0.8/1 之间，使用 SpatialFast；透明度目标在 0/1 之间，使用 EffectsFast。
规格通过主题绑定保持动态系统 Motion 引用，Float 可见阈值为 .01。
缩放和透明度作用于同一容器，父级开启整层透明度合成；局部解除 Popup 宿主裁剪以保留
Core clippingEnabled=false 的阴影，不应为了匹配截图裁掉显式 shadow elevation。

## 定位与采样

位置提供器使用物理窗口像素：居中整数除法截断，先翻边再夹到客户区，超大提示原点夹到 0。
Start/End 按 FlowDirection 解析。变换锚点仍使用 Core 的 `positionInWindow + local size`，
不能替换成变换四角后的包围盒。读取原点时撤销 Avalonia 自动 RTL 镜像，保留显式 RenderTransform，
镜像可能来自锚点或父容器。

动态几何读取未变换 Popup 与局部尺寸，真实动画用同时间线截图比较；Float/Double 矩阵末位
不伪装成布局差异。锚点移动场景保留首次与同帧再次绘制采样：旧参考宿主曾未处理完 onPlaced
触发的重测，完善 draw 完成检查后原时间点已对齐。不要在产品中人为延迟 Popup 定位规避测试问题。

## 已确认选择

确认依据为 [拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Tooltip/README.md)：

- 2026-10-04 维护者批准 TooltipBox / TooltipState / 定位 API，范围仅默认无 caret 分支；不公开会被忽略的 CaretShape
- 同日选择固定 Core 不抢焦点与 HasAction/Tab 的桌面语义；不是已实现可键盘访问提示动作的承诺
- 同日选择原生 ToolTip 以静态视觉接近为目标，接受原生输入、定位、尺寸和无动画差异；不扩展到 TooltipBox
- 同日接受 Tooltip 的动画目标提交时序差异：ComposeAnimation Transition 立即跟随 TargetState，Core 在采样旧动画后的重组帧末提交；保留现有共享 API，不加手动同步开关。仅覆盖该目标时序，不接受其他定位或布局偏离

当前依赖来源见[公共接入](../../../../docs/GETTING_STARTED.md)；旧记录提及的 0.2.5/0.2.7-alpha
是当时验证背景，不能继续当作当前安装说明。排版机制复用[共享排版决定](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)。

## 一致性边界与验证

尚未实现 caret 的形状及背景/裁剪/阴影路径并集、RichTooltip。实际 OS 桌面交互、物理触摸/笔、
文字排版和像素、屏幕阅读器播报未经验证；合成输入测试不等于真实设备验收。
已接受的动态截图差异仅是上节 Tooltip 目标提交时序；原生静态测试不评价定位一致性。

- [NativeToolTipTests](../../../../tests/Material3Expressive.Tests/NativeToolTipTests.cs)：真实服务/overlay、内容绑定、内联样式、Token 与原生约束
- [PlainTooltipTests](../../../../tests/Material3Expressive.Tests/PlainTooltipTests.cs)、[TooltipPositionTests](../../../../tests/Material3Expressive.Tests/TooltipPositionTests.cs)：尺寸、文字环境、像素定位
- [TooltipStateTests](../../../../tests/Material3Expressive.Tests/TooltipStateTests.cs)、[TooltipBoxTests](../../../../tests/Material3Expressive.Tests/TooltipBoxTests.cs)：优先级、超时、鼠标/焦点/Tab/Escape、外部请求、合成长按、模板/状态替换与卸载
- [PlainTooltipCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/PlainTooltipCases.cs) / [PlainTooltipCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/PlainTooltipCases.kt)、[TooltipBoxCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/TooltipBoxCases.cs) / [TooltipBoxCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/TooltipBoxCases.kt)、[NativeToolTipCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/NativeToolTipCases.cs) / [NativeToolTipCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/NativeToolTipCases.kt)：三个独立 suite，非文字内容

旧合并记录中三个 suite 结构化事实均相等、重复稳定；Plain/native 静态图匹配，TooltipBox
保留上述时序造成的 different。代表性共享 draw 回归涉及 AlertDialog 中断、FloatingToolbar
反向、LoadingIndicator 稀疏帧、AppBar 中断，不等于这些族的全量复验。
详细历史统计见固定提交；未跟踪的 artifacts 不作为当前可访问证据。以下为复现入口，实际结果需以新生成报告为准：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter "FullyQualifiedName~Tooltip|FullyQualifiedName~ToolTip"
./tests/VisualParity/parity.ps1 self-test --family plain-tooltip --require-match
./tests/VisualParity/parity.ps1 self-test --family native-tooltip --require-match
./tests/VisualParity/parity.ps1 self-test --family tooltip-box
```

参考环境与原始 different 的读取方式见 [VisualParity](../../../../tests/VisualParity/README.md)，
TooltipBox 不以 require-match 掩盖或重分类已保留的原始差异。
