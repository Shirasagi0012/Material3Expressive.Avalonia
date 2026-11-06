# ToolTip / TooltipBox / PlainTooltip / RichTooltip

普通悬停说明使用 Avalonia 原生 `ToolTip.Tip`。库内 `TooltipBox` 提供可控状态、焦点和长按显示，提示内容可选 PlainTooltip 或带标题、动作的 RichTooltip。

## 基础用法

先完成[公共接入](../../../../docs/GETTING_STARTED.md)，在窗口加载内容前初始化动画作用域。

```xml
<StackPanel xmlns="https://github.com/avaloniaui"
            xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <Button Content="保存" ToolTip.Tip="保存当前更改" />
  <m3:TooltipBox AnchorPosition="Above">
    <m3:TooltipBox.Tooltip>
      <m3:PlainTooltip Content="查看帮助" />
    </m3:TooltipBox.Tooltip>
    <Button Content="帮助" />
  </m3:TooltipBox>
</StackPanel>
```

## 变体与属性

- 原生 ToolTip 使用原生显隐、延迟和定位属性。默认最大宽度 200 DIP，最小尺寸 40×24 DIP，锚点默认 Top、VerticalOffset=-4，并使用窗口内覆盖层。
- PlainTooltip 提供简单提示表面，默认最大宽度 200 DIP、Padding 为 8×4 DIP，使用 BodySmall。
- RichTooltip 提供标题、正文和动作，默认最大宽度 320 DIP、水平 Padding 16 DIP、圆角 Medium、阴影 3 DIP。显式尺寸可覆盖两类提示的软尺寸限制。
- TooltipBox 的 `AnchorPosition` 支持 Above / Below / Left / Right / Start / End，Spacing 默认 4 DIP。PositionProvider 可定制定位，输入输出使用物理窗口像素。
- PlainTooltip 和 RichTooltip 的 `CaretSize="16,8"` 显示指向锚点的三角箭头；null（默认）不显示。可以调整宽高，箭头不增加布局尺寸，背景、内容裁剪和阴影使用同一外形。

TooltipBox 未指定 State 时使用独立非持续状态，直接 `new TooltipState()` 则默认持续显示。每个 Box 使用独立实例。
在所属视图中，`await box.State.ShowAsync()` 等待整次显示操作结束，`box.State.Dismiss()` 请求隐藏。
同一互斥组内，相同或更高优先级的请求会打断旧请求，调用方应处理取消。自定义组可传入 TooltipState 构造器的 mutatorMutex 参数。

鼠标进入、焦点和长按可显示提示，鼠标离开关闭非持续提示。Escape 关闭，`EnableUserInput="False"` 可仅保留程序控制。
关闭 EnableUserInput 会取消当前悬停显示并释放其互斥请求；不会取消应用随后接管的 ShowAsync。重新启用后可再次通过鼠标进入显示。
若长按已经触发，关闭输入会释放长按的优先级请求，但按固定 Core 规则保留当前可见状态；需要隐藏时显式调用 State.Dismiss()。
外部按下默认隐藏，订阅 DismissRequested 后由应用调用 Dismiss 决定关闭。IsVisible 包含退出阶段，提示在动画结束后移除。
非持续 Default / PreventUserInput 请求默认 1500ms 超时，非持续 UserInput 请求隐藏后仍可等待取消、抢占或卸载。
超时以取消结束 ShowAsync；挂载后的显示等待与长按等待均不随动画速度缩放，卸载或更换 State 会取消旧请求。

## 内容槽与定制

TooltipBox 的 Content / ContentTemplate 是锚点，Tooltip / TooltipTemplate 是提示内容。
RichTooltip 的 Title / TitleTemplate、Content / ContentTemplate、Action / ActionTemplate 分别提供标题、正文和动作，单独设置模板也能创建槽。

```xml
<m3:TooltipBox x:Name="Details" HasAction="True">
  <m3:TooltipBox.Tooltip>
    <m3:RichTooltip Title="收藏夹" Content="把相关内容保存在一起。">
      <m3:RichTooltip.Action>
        <Button Content="知道了" Theme="{StaticResource MaterialTextButtonTheme}" />
      </m3:RichTooltip.Action>
    </m3:RichTooltip>
  </m3:TooltipBox.Tooltip>
  <Button Content="收藏夹说明" />
</m3:TooltipBox>
```

此片段沿用前述命名空间，并需声明 `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"`。
动作处理由应用提供，需要关闭时调用 `Details.State.Dismiss()`。

Background、CornerRadius、Padding 和 elevation 可定制表面。RichTooltip 的 Foreground、TitleForeground、ActionForeground 分别控制三个槽，默认文字角色为 BodyMedium、TitleSmall、LabelLarge。

```xml
<m3:TooltipBox AnchorPosition="End" Spacing="8">
  <m3:TooltipBox.Tooltip>
    <m3:PlainTooltip CaretSize="16,8" Content="侧边提示" />
  </m3:TooltipBox.Tooltip>
  <Button Content="详情" />
</m3:TooltipBox>
```

箭头根据窗口边界翻转，并支持 RTL；`TooltipLabel` 内的提示同样可设置 CaretSize。
定位方向应在首次挂载前确定：动态改变 AnchorPosition 或 PositionProvider 会移动弹层，但箭头保留初始定位器的方向规则。
自定义 PositionProvider 的箭头使用上/下规则。箭头可超出布局盒，应保留宿主的非裁剪区域；独立使用表面时不提供锚点定位。

`Comp.RichTooltip` 支持标题与正文首基线距离、正文留白及动作区最小高度和底部留白的动态 Token 覆盖。默认值为 28、24、16 / 4、36、8 DIP。
普通 TextBlock 自动提供基线。复杂自定义槽可在根控件上调用 `MaterialTextBaseline.SetFirst(control, baseline)`
（`Material3Expressive.Foundation.Typography`），由调用方随布局更新从顶边起算、排除 Margin 的 DIP 值。
尺寸不变时也会重新布局；清除/设 null 恢复自动读取。已有 `TextBlock.SetBaselineOffset` 保留兼容，显式 First 优先于它。

**TooltipLabel：跟随交互的标签**

`TooltipLabel` 为锚点提供跟随交互显示的标签，与 Avalonia 原生文字 `Label` 不同。
`Content` / `ContentTemplate` 是锚点，`Label` / `LabelTemplate` 是标签槽；默认在锚点上方，
空间不足时翻到下方，并约束到窗口边界。标签不增加锚点的布局尺寸，显隐没有动画。

```xml
<m3:TooltipLabel x:Name="SaveLabel">
  <m3:TooltipLabel.Label><m3:PlainTooltip Content="保存到收藏夹" /></m3:TooltipLabel.Label>
  <m3:AssistChip x:Name="SaveChip" Content="保存" />
</m3:TooltipLabel>
```

```csharp
SaveLabel.InteractionSource = SaveChip.InteractionSource = new();
```

标签和锚点共享同一个 `MaterialInteractionSource`。Press、Drag、Hover 任一交互仍活跃时显示；
例如拖动期间退出 hover 不会提前隐藏。Focus 不触发显示，包装器不自行捕获鼠标或合成事件。
未提供源时不会因悬停自动显示。可用 `IsPersistent="True"` 始终显示，此时不订阅交互源。
各标签独立，可同时显示，也不抢占 TooltipBox 的显示互斥组。

外部按下关闭非持续标签且继续传递输入；在所有现有交互结束前，新的 start 不会重新打开它。
持续标签忽略该关闭请求。Escape、焦点、长按不会由包装器额外处理；提示不会夺取焦点。
更换源、切换持续模式、卸载会结束旧订阅；重新挂载只接收后续事件，不回放旧状态。
PlainTooltip/RichTooltip 的箭头定位规则仍适用；不把它当作交互式弹窗或 Slider 输入适配层。
锚点自身的 RenderTransform 只改变锚点外观，提示仍对齐未变换的布局位置；祖先的变换、布局移动及提示内容尺寸变化会更新定位。
具体范围见 [TooltipLabel 实现边界](INTERNALS.md#3-已确认决定与已知差异)。

## 使用限制与注意事项

窗口模板需提供原生 popup layer，相关要求见 [Overlays](../../Foundation/Overlays/README.md)。两类提示表面均可独立放入普通布局；指向锚点的箭头需搭配 TooltipBox 或 TooltipLabel。
TooltipBox 显示时保持原焦点，HasAction=True 且可见时会消费 Tab，动作按钮的完整键盘导航尚未提供。需要完整键盘操作时使用合适的弹层组件。
触摸或笔长按默认等待 500ms，可用 `LongPressDuration="0:0:0.75"` 设置为 750ms；时长在按下时确定，修改只影响下一手势。
短按、捕获丢失、关闭 EnableUserInput 或卸载会取消尚未触发的长按。
等待时间不随动画速度改变。Catalog 的无焦点长按区域可单独体验这一交互。
文字像素、物理触摸/笔和屏幕阅读器播报仍需专项验证。
[Catalog 示例](../../../Material3Expressive.Catalog/TooltipDemo.axaml)展示原生、简单和富内容提示。内部机制与已确认差异见 [INTERNALS.md](INTERNALS.md)。
