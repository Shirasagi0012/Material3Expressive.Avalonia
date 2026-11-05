# Tooltip 内部机制与维护说明

## 1. 上游基线与职责映射

固定版本以 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）为桌面分支依据。
下述 Material3 文件路径均相对于 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

| 上游文件 / 关键符号 | 本地职责 |
| --- | --- |
| `Tooltip.kt`：`PlainTooltip`、`RichTooltip`、`TooltipBox`、`TooltipState`、`TooltipDefaults` | [PlainTooltip.cs](PlainTooltip.cs)、[TooltipBox.cs](TooltipBox.cs)、[TooltipState.cs](TooltipState.cs)、[TooltipDefaults.cs](TooltipDefaults.cs) |
| `internal/BasicTooltip.kt`：显示互斥、鼠标/焦点/长按与 dismiss 分支 | TooltipState 的 MutatorMutex 等待与 TooltipBox 的输入生命周期 |
| `tokens/PlainTooltipTokens.kt` | [PlainTooltipTokenSource.cs](PlainTooltipTokenSource.cs)、[Tooltip.axaml](Tooltip.axaml)：反色 Surface、BodySmall、默认尺寸 |
| `Tooltip.kt` 的位置提供器及 Core Popup 桌面定位 | [TooltipPosition.cs](TooltipPosition.cs) 的物理像素算法、TooltipBox.PlacePopup 的锚点窗口坐标 |
| Avalonia 原生 ToolTip 服务（本地适配） | Tooltip.axaml 的原生 ToolTip ControlTheme 和锚点附加属性；不增加输入控制器 |

AndroidX 是 Material 逻辑依据，Popup 的平台行为取固定 Core，尤其不套用 AndroidX 强制 focusable 分支。
原生 ToolTip 只共享静态 PlainTooltip Token，不是整个 Compose Tooltip 的等价入口。

RichTooltip 的三槽表面由 RichTooltip.cs / RichTooltipLayout.cs 承担，RichTooltipTokenSource 保留 RichTooltipTokens.kt 的系统引用。两端固定 RichTooltip 与 textVerticalPadding 实现相同。

TooltipLabel 对应固定 AndroidX/Core 的 Label.kt，两端文件相同；名称避免与原生文字 Label 冲突。内容槽沿用 Content/ContentTemplate 与 Label/LabelTemplate，事件复用 MaterialInteractionSource。

## 2. 关键机制与架构契约

TooltipBox 的缩放/透明度规格由 Box Token 引用系统 motion，经主题绑定内部配置属性；Transition 捕获当前属性，不再自行解析 Token 或补 Expressive 默认。PlainTooltip 仍只负责静态表面。
原生/Plain Tooltip 的 Padding、最小/最大尺寸及 Box Spacing 基线统一在 TokenSource；Plain 的软最小值绑定内部布局，不转成会抬高显式 Width/Height 的硬约束。位置工厂、互斥状态与超时职责保留。

原生主题提供 8×4 DIP padding、40×24 最小尺寸和 200 最大宽度。
定位默认值必须设置在锚点上，因为 ToolTip 服务从锚点读取 Top / -4 / overlay；
仅修改 ToolTip 自身 ControlTheme 不会改变定位。
原生显隐无动画，ShowDelay、BetweenShowDelay、禁用与关闭规则归 Avalonia 服务。

PlainTooltipLayout 的 `sizeIn(40×24, maxWidth)` 位于调用方尺寸内，显式 Width 可以超出默认范围。
其 Surface 默认 tonal/shadow elevation 均为 0，内容通过独立 presenter 消费 BodySmall 和前景。
Box 自身只测量锚点，提示属于独立 overlay Popup；`PART_Root` 在换模板时必须正确移除/重接 Popup。
Popup 同时在逻辑树与可视树接入，维持模板、资源和卸载行为。

TooltipState 的直接构造默认 persistent；TooltipBox 隐式实例默认 nonpersistent。
每个 Box 连接独立状态。默认 GlobalMutatorMutex 使同优先级/高优先级请求打断旧请求，
低优先级被取消；不能把它改成 Snackbar 式 FIFO。
`ShowAsync` 等待整个请求结束，非持续 Default / PreventUserInput 设 1500ms 超时，UserInput 不设超时。
非持续 Dismiss 只改变目标，不结束等待；PreventUserInput 的 finally 不自动关闭，由焦点或长按流程负责。
挂载时 Box 把已有状态子作用域接给 State，显示超时与长按均使用根 TimeProvider 的普通时间；不要求动画帧，也不随 DurationScale 缩放。
请求开始时选择时间源，挂载前单独调用 ShowAsync 仍使用系统时间，挂载不会重置已经开始的等待。
卸载或换 State 先取消旧请求，再解除时间源连接；超时仍以取消结束任务并释放互斥，不转换成成功完成。

鼠标进入使用 UserInput，离开仅关闭非持续提示；焦点与长按使用 PreventUserInput。
悬停请求单独由鼠标输入子作用域持有，对应 Core 第二个 pointerInput 的 coroutineScope。
关闭 EnableUserInput、换 State 或卸载时取消此作用域；persistent 悬停同样取消，应用或焦点接管后的请求不属于它。
长按另有对应第一个 pointerInput 的手势子作用域。移除输入处理器时既取消阈值等待，也取消已开始的 PreventUserInput show，释放互斥。
固定 Core 的取消态 StateFlow collector 不再执行 dismiss，TooltipState 的 PreventUserInput finally 也不关闭；
所以禁用已触发长按会保留可见目标，而后续 Default 请求应可进入。不能用仅解除按下状态、继续等待 1500ms/无限等待来替代取消。
长按阈值为 `LongPressDuration`，默认来自固定 Core `PlatformContext.DefaultViewConfiguration.longPressTimeoutMillis` 的 500ms。
Avalonia 默认 HoldWaitDuration 为 300ms，不是同一桌面基线，不能直接替代。组件级覆盖对应上游 LocalViewConfiguration 的这一个输入，
不创建公共共享配置框架；按下时捕获时长，运行中修改只影响下一手势。经作用域 DelayAsync 使用根 TimeProvider，不依赖动画帧或 DurationScale；触发后消费释放。
PointerCaptureLost 是 Direct 事件，必须以 Direct 订阅；Avalonia 在真实捕获转移时向旧捕获路径逐项投递，不能等待 Bubble。Escape KeyDown 关闭并消费，
HasAction 且可见时消费 Tab，但固定 Core Popup 不抢焦点，也不把焦点送入动作。
外部按下默认 Dismiss；存在 DismissRequested 订阅者时转交应用决定，外部点击仍传递。
Automation Invoke 表达锚点的长按显示，提示提供 ToolTip / Assertive live setting。

`IsVisible = CurrentState || TargetState`，退出期间保留 Popup，Transition 真正完成后移除。
更换状态或卸载取消旧等待、输入、帧与动画作用域，不影响其他 Box。
缩放目标在 0.8/1 之间，使用 SpatialFast；透明度目标在 0/1 之间，使用 EffectsFast。
规格通过主题绑定保持动态系统 Motion 引用，Float 可见阈值为 .01。
缩放和透明度作用于同一容器，父级开启整层透明度合成；局部解除 Popup 宿主裁剪以保留
Core clippingEnabled=false 的阴影，不应为了匹配截图裁掉显式 shadow elevation。

位置提供器使用物理窗口像素：居中整数除法截断，先翻边再夹到客户区，超大提示原点夹到 0。
Start/End 按 FlowDirection 解析。Core BasicTooltipBox 的外层 Box 同时承载 Popup 和 WrappedAnchor，传入的 modifier 仅作用于后者。
TooltipAnchor 因此从父坐标转换 owner.Bounds 的物理起点，保留祖先变换、排除 owner 自身 RenderTransform，
再加未变换的局部尺寸；不使用变换四角后的包围盒。读取原点时撤销祖先的 Avalonia 自动 RTL 镜像。
祖先 Bounds、RenderTransform、Origin 和可变 Transform.Changed 触发重定位；detach 解除所有监听。

动态几何读取未变换 Popup 与局部尺寸，真实动画用同时间线截图比较；Float/Double 矩阵末位
不伪装成布局差异。锚点移动场景需保留首次与同帧再次绘制采样，并等待 onPlaced 触发的重测完成。

### 富内容布局与尺寸 Token

RichTooltip 按标题、正文、动作顺序分配剩余高度，三槽分别提供文字角色与颜色。内容或模板决定槽可见性，动态添加槽会重新测量。
外层复用 PlainTooltipLayout 的软尺寸约束，控件与内部布局分别绕过自动镜像并解析逻辑 start。

`Comp.RichTooltip` 的 SubheadFirstBaselineOffset、TextFirstBaselineOffset、TextBottomPadding、TextVerticalPadding、ActionMinHeight、ActionBottomPadding
经主题绑定到 RichTooltipLayout 的可测量属性。默认值分别为 28、24、16、4、36、8 DIP，局部及祖先资源覆盖会触发重新测量。
标题和正文保留 alignmentLineOffsetMeasure 顺序，缺失基线按零计算，before/after 依次受剩余空间限制；未指定 after 的零距离仍参与计算。
动作的最小高度在底部留白之前应用，受限时保持 requiredHeightIn 的居中溢出。

普通 TextBlock、显式原生 BaselineOffset 和 ContentPresenter 可提供基线。复杂容器应在槽根提供实际基线，自动透传任意布局需要全库 alignment-line 合约。

TooltipLabel 的 HandleInteractions 保留有序 start 列表、按对象身份移除与 wasVisible 聚合边沿；Press、Drag、Hover 任一活跃即显示，Focus 不计入。外部 dismiss 只隐藏，不重置 collector，尚有活动交互时新 start 不会重开。
上游每个 Label 使用私有 MutatorMutex，只有 collector 发起 UserInput show，无超时或公开 Show API；本地以局部显隐状态表达同一所有权，不引入等待任务或全局互斥。
IsPersistent 对应始终可见、dismiss 无效且不订阅源的分支。换源、切换持续模式与卸载清理 collector，重新连接不重放；换模板只重接宿主，不重置 collector。
Label 调用 BasicTooltipBox 而非 TooltipBox 动画包装，所以显隐立即完成；复用 overlay Popup、TooltipPresenter 自动化与 TooltipDefaults 物理像素定位，不夺焦点或追加 Escape、长按、焦点输入。外部按下仍向下传递。

## 3. 已确认决定与已知差异

确认依据为 [拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Tooltip/README.md)：

- 2026-10-04 维护者批准 TooltipBox / TooltipState / 定位 API，范围仅默认无 caret 分支；不公开会被忽略的 CaretShape
- 同日选择固定 Core 不抢焦点与 HasAction/Tab 的桌面语义；不是已实现可键盘访问提示动作的承诺
- 同日选择原生 ToolTip 以静态视觉接近为目标，接受原生输入、定位、尺寸和无动画差异；不扩展到 TooltipBox
- 同日接受 Tooltip 的动画目标提交时序差异：ComposeAnimation Transition 立即跟随 TargetState，Core 在采样旧动画后的重组帧末提交；保留现有共享 API，不加手动同步开关。仅覆盖该目标时序，不接受其他定位或布局偏离
- 本次剩余移植的非破坏性组件 API 授权包含 `TooltipBox.LongPressDuration`；此入口仅配置该组件的长按等待，不改原生 ToolTip 服务或 TooltipState 的显示时长。

尚未实现 caret 的形状及背景、裁剪、阴影路径并集，任意自定义容器的基线透传也尚未实现。
TooltipBox 的运动截图保留已确认的目标提交时序差异。文字栅格、物理触摸/笔及屏幕阅读器未经验证，Windows Catalog 已检查 RichTooltip 点击打开和动作关闭路径。

RangeSlider 的 StartThumbInteractionSource / EndThumbInteractionSource 可分别连接自定义 Thumb 内的 TooltipLabel。
单值原生 Slider 尚无公共交互源接线；跨屏 DPI、字体栅格和真实触摸设备未经验收。

## 4. 验证入口

- `NativeToolTipTests` 检查原生服务和静态主题。
- `PlainTooltipTests`、`RichTooltipTests` 检查尺寸、槽位、文字角色、基线及 Token 覆盖。RichTooltip 的 density 回归覆盖动态资源、RTL、动作最小高度及槽位切换。
- `TooltipPositionTests`、`TooltipStateTests`、`TooltipBoxTests` 检查定位、互斥、输入、动画与卸载。
- 手动普通时钟覆盖 500/750ms 长按、时长中途修改、真实捕获转移、短按/禁用/换状态/卸载、1500ms 显示超时与不同优先级，以及释放先于/晚于超时的关闭顺序；检查取消后的计时器和帧等待清理。
- 双端族为 `plain-tooltip`、`rich-tooltip`、`native-tooltip`、`tooltip-box`。Rich 的 show / interrupt / resize 位于 TooltipBox 族，运动截图按已确认的时序差异单独读取。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter 'FullyQualifiedName~Tooltip|FullyQualifiedName~ToolTip'
./tests/VisualParity/parity.ps1 self-test --family rich-tooltip --require-match
./tests/VisualParity/parity.ps1 self-test --family tooltip-box
```

`tooltip-box` 的 97 个用例执行错误为零且重复/额外截图稳定（`20261104-183945-c09d6a80`）：
71 equal、2 withinTolerance、24 different。长按新增 9 例：禁用和短按全等；Touch/Pen、持续/非持续及 750ms 覆盖
在触发时刻的目标状态一致，但当刻 Popup 是否已创建及随后两个过渡截图仍有差异，稳定显示后的样本相等。
其目标提交差异按前述范围保留，不能把整个族视为视觉验收通过。
悬停禁用/重新启用、应用接管 × 持续/非持续四例的全部状态读数一致，动画截图仍保留过渡差异。
两例已触发长按的禁用场景在禁用、释放和随后 Default 请求的采样全等，包括请求未被旧高优先级拒绝；
实际显示目标按 Core 保留。此项同时有计时器清理及任务取消测试，不能只凭 Popup 仍可见推断请求尚未释放。
缩放锚点场景的 Core modifier 已改放在 TooltipBox 本身，与本地自身 RenderTransform 对应；
旧夹具放在包围 TooltipBox 的祖先 Box，测到的是另一种输入。修正后 LTR/RTL 两例均全等，没有改变组件定位算法或比较门禁。

显示超时的参考时间源按 [共享测试决定](../../../../tests/VisualParity/DECISIONS.md#compose-普通延迟与-timeout-共用宿主时间)
接入虚拟时间；原固定 Core 未重写 invokeOnTimeout，旧报告中的墙钟结果不用于评判组件期限。
当前 `timeout-tooltip-current` 的 dynamic.timeout 捕获成功且重复稳定：1000µs 发起请求，
1500000µs 尚未取消，1501000µs 双端均取消 Show 并设置 target=false，最后帧仍为1488000µs，
1904000µs 均完成退出。全部结构化读数一致，10 个采样中 6 个逐像素相等；
其余 4 个入场/退出途中截图仍保留本节已确认的目标提交时序差异，没有改容差或补帧。

TooltipLabelTests 与 TooltipAnchorTests 覆盖身份、重叠、换源、卸载、持续模式、外部关闭及自身/祖先变换。
`tooltip-label` 扩充后的 38 个双端用例全部截图逐像素相等、重复稳定、零捕获错误
（运行 `20261103-142626-64eb60bc`）。其中 34 个用例整体 equal；4 个旋转时间线因锚内容的
float/double AABB 坐标约 10^-6 DIP 差异被精确比较标为 different，Popup 和内容几何仍相等。
未对坐标取整或修改门禁。变换时间线包含缩放、平移、旋转、原位修改变换、内容变宽、布局移动、清除变换和重开。
