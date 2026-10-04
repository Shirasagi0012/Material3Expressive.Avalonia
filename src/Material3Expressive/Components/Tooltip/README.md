# ToolTip / TooltipBox / PlainTooltip

TooltipBox / PlainTooltip 依据 `docs/upstream-baseline.json` 中固定的 AndroidX `Tooltip.kt`、
`internal/BasicTooltip.kt`、`PlainTooltipTokens.kt` 及 Core 对应实现；Popup 的平台行为取固定
Core 桌面实现。原生 ToolTip 复用静态外观 Token，采用下述已确认的 Avalonia 行为。

## 使用

引入 MaterialTheme 后，普通说明使用原生 `ToolTip.Tip`；主题为自动创建的 ToolTip 提供 PlainTooltip
Token、BodySmall 文字环境、8×4 DIP padding、40×24 DIP 最小尺寸及 200 DIP 最大宽度。
原生 Width / Height / MinWidth / MaxWidth 等约束正常生效；局部值可以覆盖主题默认值。

```xml
<Button Content="关闭" ToolTip.Tip="关闭当前窗口" />
<Button Content="帮助" ToolTip.Placement="Bottom" ToolTip.VerticalOffset="4">
  <ToolTip.Tip>
    <TextBlock TextWrapping="Wrap">
      <Run Text="使用说明" FontWeight="SemiBold" /><LineBreak />
      <Run Text="包含尚未保存的本地修改。" />
    </TextBlock>
  </ToolTip.Tip>
</Button>
```

原生提示直接显隐，不启动 ComposeAnimation。悬停延迟、连续切换、关闭、禁用控件处理和
程序化 `ToolTip.IsOpen` 均保留 Avalonia 服务规则，不增加行为控制器或修改 ToolTipService。
MaterialTheme 在锚点上提供 Top、VerticalOffset=-4、ShouldUseOverlayLayer=true 默认值；
方向翻转、滑动、边界尺寸调整及 RTL 定位由 Avalonia 处理，翻转后不保证仍有 4 DIP 间距。
原生默认 ShowDelay=400ms、BetweenShowDelay=100ms；调用方仍可覆盖已有附加属性。

2026-10-04，维护者选择原生入口以静态视觉接近为目标，接受其输入、定位、尺寸约束及动画
与 Compose 的差异。需要提示内按钮、长按、焦点输入或完整显式状态控制时使用 TooltipBox；
不通过原生提示的内容按钮承担这些需求。任意文本内容不代表已实现 Compose RichTooltip。

原生入口的 Headless 检查使用真实服务和 overlay Popup，覆盖显示与关闭、绑定内容、
内联格式、动态 Token 和显式原生约束。`native-tooltip` 双端用例只比较静态 surface，
以非文字内容覆盖明暗、RTL、尺寸边界和局部外观覆盖；不评价原生定位与 Compose 的一致性。
Catalog 的默认悬停显示路径已有 Headless 检查，真实桌面交互及双端文字栅格尚未经验证。

TooltipBox 的 Content / ContentTemplate 是锚点，Tooltip / TooltipTemplate
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
读取窗口原点时撤销 Avalonia 自动 RTL 镜像对起点的影响，保留显式 RenderTransform；
镜像可能来自锚点自身或父容器。
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

2026-10-04，维护者接受本组件的动画目标提交时序差异：ComposeAnimation 的 Transition
立即跟随 TargetState，固定 Core 则在采样当前动画后的重组帧末提交。保留现有共享 API，
不增加手动同步开关；进场首帧及中断时采样的值和速度可能不同，动态截图原始差异保留。
这项决定只适用于 Tooltip 的目标提交时序。

当前使用本地 NuGet `ComposeAnimation 0.2.5-alpha`；该版本修复空闲 Transition 的
异步唤醒竞态，不增加目标同步 API。依赖通过包接入，不引用相邻仓库的项目。

锚点移动场景保留移动后的首次采样及同一动画帧的再次绘制采样。诊断证实旧参考宿主
未处理完 `onPlaced` 写入引起的提示层重测；完善 Compose draw 完成检查后，
移动、尺寸变化和方向切换均在原时间点对齐，无需在产品中额外延迟 Popup 定位。

2026-10-04 合入主分支后：Release 全解决方案构建、880 项控件测试及 124 项 Parity 系统测试通过。
使用 ComposeAnimation 0.2.7-alpha 独立重采双端并执行普通重复与额外被动截图检查：

- PlainTooltip：30 例全部 withinTolerance，37 个采样点的 185 项结构化事实全部 equal。
- 原生 ToolTip 静态 surface：18 例全部 equal，90 项结构化事实全部 equal。
- TooltipBox：80 例为 1 equal / 69 withinTolerance / 10 different，204 个采样点的
  2,308 项结构化事实全部 equal；10 例保留上述已接受的动画时序截图差异。

三个套件全部重复稳定、执行错误为 0。与分支交付的 `20261004-063707-d8737f6c`、
`20261004-080637-63f70b7e`、`20261004-063439-fab49709` 对照，用例及采样事实的比较状态均无变化。
原始差分保留，未放宽容差或修改基线。结果分别位于：

- `artifacts/visual-parity/runs/plain-tooltip-merge-20261004/summary.md`
- `artifacts/visual-parity/runs/native-tooltip-merge-20261004/summary.md`
- `artifacts/visual-parity/runs/tooltip-box-merge-20261004/summary.md`

针对共享 Compose draw 完成检查的 4 个代表回归全部匹配且稳定：AlertDialog interrupt、
FloatingToolbar horizontal End FAB reverse、LoadingIndicator sparse plain、AppBar medium interrupt。
产物位于 `artifacts/visual-parity/runs/tooltip-host-merge-20261004-<case-id>/summary.md`。
这是代表场景回归，不等于重跑上述组件的全量套件；桌面设备与文字栅格验证范围保持上文限制。
