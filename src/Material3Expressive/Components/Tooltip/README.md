# TooltipBox / PlainTooltip

依据 `docs/upstream-baseline.json` 中固定的 AndroidX `Tooltip.kt`、`internal/BasicTooltip.kt`、
`PlainTooltipTokens.kt` 及 Core 对应实现。Material 布局、Token、定位和状态规则相同；
Popup 的平台行为取固定 Core 桌面实现。

## 使用

引入 MaterialTheme。TooltipBox 的 Content / ContentTemplate 是锚点，Tooltip / TooltipTemplate
是提示槽。PlainTooltip 提供提示的 Surface，也可以单独用于布局。

```xml
<m3:TooltipBox AnchorPosition="Above" State="{Binding HelpTooltip}">
  <m3:TooltipBox.Tooltip>
    <m3:PlainTooltip Content="关闭" />
  </m3:TooltipBox.Tooltip>
  <m3:IconButton Symbol="Close" />
</m3:TooltipBox>
```

```csharp
var group = new MutatorMutex(); // ComposeAnimation.Runtime
var state = new TooltipState(isPersistent: false, mutatorMutex: group);
var box = new TooltipBox
{
    Content = new Button { Content = "帮助" },
    Tooltip = new PlainTooltip { Content = "查看说明", TooltipMaxWidth = 240 },
    State = state,
    AnchorPosition = TooltipAnchorPosition.Above
};
// ShowAsync 持续等待，不是在首次出现时返回。
try { await state.ShowAsync(MutatePriority.Default, cancellationToken); }
catch (OperationCanceledException) { /* 超时、被另一提示打断或调用方取消 */ }
state.Dismiss();
```

同一 MutatorMutex 串行管理显示请求：相同或更高优先级打断旧请求，较低优先级被取消。
默认使用 TooltipDefaults.GlobalMutatorMutex，让提示互斥；独立组可同时显示。
每个 TooltipBox 使用独立 TooltipState，不将同一状态连接到多个 Transition。
直接构造 TooltipState 默认持续显示，与上游 TooltipState 工厂一致；
TooltipBox 未指定 State 时创建非持续状态，与 rememberTooltipState 一致。

未提供 DismissRequested 处理器时，外部按下调用 State.Dismiss()。有处理器时应用拥有关闭决定：

```csharp
box.DismissRequested += (_, _) => state.Dismiss();
```

## 布局与行为

PlainTooltip 默认反色容器与内容色、ExtraSmall 圆角、BodySmall 文字环境，padding 为 8×4 DIP，
内部默认宽度 40–200 DIP、高度至少 24 DIP，tonal / shadow elevation 均为 0。
Background、Foreground、CornerRadius、Padding、ContentTemplate、TonalElevation、ShadowElevation
可覆盖默认外观。TooltipMaxWidth 表达上游 maxWidth；显式 Width / Height 及调用方约束在
默认尺寸约束之外，因此 Width=300 或 Width=20 仍按明确尺寸布局。

AnchorPosition 支持 Above / Below / Left / Right / Start / End，Spacing 默认 4 DIP。
上游算法以物理像素计算：居中采用整数截断，空间不足时翻到另一侧，再限制在客户区内；
提示大于客户区时原点限制为 0。Start / End 依照 FlowDirection 解析。
变换后的锚点保留 Core Popup 的 `positionInWindow + local size`，不改成变换四角的包围盒。
PositionProvider 可替换计算，ITooltipPositionProvider 的参数及返回值均为物理窗口像素。

鼠标进入以 UserInput 显示，离开仅关闭非持续状态。锚点焦点、触摸或笔长按以
PreventUserInput 显示；Escape KeyDown 关闭并消费事件。EnableUserInput=false 关闭这些入口，
应用仍可调用 ShowAsync。长按阈值取平台 HoldWaitDuration，消费长按后的释放事件。
Automation Invoke 表达锚点的长按操作；提示提供 ToolTip 类型与 Assertive live setting。

非持续 Default / PreventUserInput 请求在 1500ms 超时。UserInput 请求等待打断或卸载，
非持续 Dismiss 不结束该显示任务；PreventUserInput 的 finally 不自动关闭，由焦点或
长按释放流程负责。这些看起来特殊的分支直接保留固定上游行为。
IsVisible 是 Transition.CurrentState || Transition.TargetState，退出期间保持 Popup，
到真实 Transition 完成后才卸载；锚点卸载会取消自己的等待与输入任务。

Popup 使用 Avalonia overlay layer，窗口模板须提供 VisualLayerManager / OverlayLayer。
独立 Popup 显示任意提示内容，锚点仍是唯一参与父布局的内容。
graphicsLayer 的缩放与整体透明度映射为同一容器的 RenderTransform 与整体透明度合成，
局部关闭 Popup 宿主裁剪，使显式阴影按 Core clippingEnabled=false 绘制。

## 已确认范围与验证边界

2026-10-04，维护者批准上述 TooltipBox / TooltipState / 定位 API。
本轮仅实现默认无 caret 分支；caret 形状及背景、裁剪、阴影的路径并集尚未实现。
不提供会暗中忽略的 CaretShape 属性；RichTooltip 不属于本轮范围。

维护者选择固定 Core 桌面语义：Popup 不抢焦点。HasAction=true 且可见时消费 Tab，
但不会把键盘焦点转移到提示动作；这是固定 Core 的已知桌面限制。
本轮没有套用 AndroidX 的强制 focusable 分支。

Headless 测试覆盖状态默认值、优先级与超时、显式尺寸、Token 和文字环境、
Popup 保留与卸载、鼠标、焦点 / Escape / Tab、外部按下、自动化入口与合成触摸 / 笔事件。
双端 `PlainTooltipCases` 覆盖外观、尺寸边界和主题 / 内容变化，`TooltipBoxCases` 覆盖
定位方向、窗口边缘、RTL、明暗与阴影，以及进出场、中断、鼠标、焦点、关闭请求和布局变化。
动态几何观察未变换 Popup 与局部尺寸；实际动画由同时间线截图比较，避免将
Compose Float 与 Avalonia Double 矩阵的末位表示差异当作精确布局事实。

双端夹具使用非文字内容；文字排版与栅格一致性、物理触摸 / 笔设备、屏幕阅读器播报
及操作系统桌面交互尚未经验证。

当前动态移植尚未完成：ComposeAnimation 0.2.3-alpha 的 Transition 自动立即提交目标，
而固定 Core 在重组帧末提交；空闲 Transition 的异步唤醒还使退出首帧存在重复性差异。
锚点移动后的 Popup 更新也比固定 Core 提前一帧。上述问题尚未修复或被接受，
需要补齐目标同步能力后继续动态回归；不能把静态容差通过解释为 TooltipBox 完成。
