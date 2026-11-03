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
- TooltipBox 的 `AnchorPosition` 支持 Above / Below / Start / End，Spacing 默认 4 DIP。PositionProvider 可定制定位，输入输出使用物理窗口像素。

TooltipBox 未指定 State 时使用独立非持续状态，直接 `new TooltipState()` 则默认持续显示。每个 Box 使用独立实例。
在所属视图中，`await box.State.ShowAsync()` 等待整次显示操作结束，`box.State.Dismiss()` 请求隐藏。
同一互斥组内，相同或更高优先级的请求会打断旧请求，调用方应处理取消。自定义组可传入 TooltipState 构造器的 mutatorMutex 参数。

鼠标进入、焦点和长按可显示提示，鼠标离开关闭非持续提示。Escape 关闭，`EnableUserInput="False"` 可仅保留程序控制。
外部按下默认隐藏，订阅 DismissRequested 后由应用调用 Dismiss 决定关闭。IsVisible 包含退出阶段，提示在动画结束后移除。
非持续 Default / PreventUserInput 请求默认 1500ms 超时，非持续 UserInput 请求隐藏后仍可等待取消、抢占或卸载。

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
`Comp.RichTooltip` 支持标题与正文首基线距离、正文留白及动作区最小高度和底部留白的动态 Token 覆盖。默认值为 28、24、16 / 4、36、8 DIP。
普通 TextBlock 自动提供基线。复杂自定义槽可在根控件上调用 `TextBlock.SetBaselineOffset(control, baseline)`，由调用方随布局更新该值。

## 使用限制与注意事项

窗口模板需提供原生 popup layer，相关要求见 [Overlays](../../Foundation/Overlays/README.md)。两类提示表面均可独立放入普通布局，当前外形为无尖角矩形。
TooltipBox 显示时保持原焦点，HasAction=True 且可见时会消费 Tab，动作按钮的完整键盘导航尚未提供。需要完整键盘操作时使用合适的弹层组件。
文字像素、物理触摸/笔和屏幕阅读器播报仍需专项验证。
[Catalog 示例](../../../Material3Expressive.Catalog/TooltipDemo.axaml)展示原生、简单和富内容提示。内部机制与已确认差异见 [INTERNALS.md](INTERNALS.md)。
