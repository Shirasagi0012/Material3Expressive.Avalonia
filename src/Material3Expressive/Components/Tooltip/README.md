# ToolTip / TooltipBox / PlainTooltip / RichTooltip

普通悬停说明使用 Avalonia 原生 `ToolTip.Tip`，Material 主题为它提供静态外观。
需要锚点焦点、长按、显示状态或提示内动作时，使用库的 `TooltipBox`，并以 `PlainTooltip`
提供提示表面。先完成[公共接入](../../../../docs/GETTING_STARTED.md)；TooltipBox 动画需要
窗口在加载内容前初始化动画作用域。实现与已确认差异见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

以下视图放在使用标准 Window 模板的窗口中，无需 ViewModel：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel Spacing="24">
    <Button Content="保存" ToolTip.Tip="保存当前更改" />
    <m3:TooltipBox AnchorPosition="Above">
      <m3:TooltipBox.Tooltip>
        <m3:PlainTooltip Content="查看帮助" />
      </m3:TooltipBox.Tooltip>
      <Button Content="帮助" />
    </m3:TooltipBox>
  </StackPanel>
</UserControl>
```

后续片段沿用相同命名空间。两种入口均在窗口内 overlay Popup 呈现，窗口模板需提供
原生 popup layer；自定义 Window 模板的要求见 [Overlays 接入](../../Foundation/Overlays/README.md#接入)。

## 原生 ToolTip

`ToolTip.Tip` 可接收字符串或任意内容，原生 `ToolTip.IsOpen`、显示延迟和定位属性继续有效：

```xml
<Button Content="同步" ToolTip.Placement="Bottom" ToolTip.VerticalOffset="4">
  <ToolTip.Tip>
    <TextBlock TextWrapping="Wrap">
      <Run Text="同步项目" FontWeight="SemiBold" /><LineBreak />
      <Run Text="包含尚未上传的本地更改。" />
    </TextBlock>
  </ToolTip.Tip>
</Button>
```

默认最小尺寸 40×24 DIP、最大宽度 200 DIP，padding 为 8×4 DIP，使用 BodySmall。
锚点默认 Top、VerticalOffset=-4、ShouldUseOverlayLayer=true；原生服务处理翻转与边界约束，
翻转后不保证仍有 4 DIP 间距。默认 ShowDelay=400ms、BetweenShowDelay=100ms，可按原生属性覆盖。
显隐直接完成，没有 ComposeAnimation 过渡；禁用控件、连续切换和关闭规则均沿用 Avalonia。
原生入口不承担提示内按钮交互，任意文本内容也不代表已实现 RichTooltip。

## TooltipBox 的状态与输入

`Content` / `ContentTemplate` 是锚点；`Tooltip` / `TooltipTemplate` 是提示槽。
未指定 `State` 时，每个 Box 自建非持续状态；直接 `new TooltipState()` 则默认持续显示。
需要绑定时由 ViewModel 提供 `TooltipState`，每个 Box 使用独立实例；编译绑定还需 `x:DataType`。

```xml
<m3:TooltipBox x:Name="Help" EnableUserInput="False" AnchorPosition="End">
  <m3:TooltipBox.Tooltip><m3:PlainTooltip Content="操作提示" /></m3:TooltipBox.Tooltip>
  <Button Content="帮助" />
</m3:TooltipBox>
```

在所属视图代码中可调用 `Help.State.ShowAsync()`；下面片段假设 `Help` 是上述 Box，
`cancellationToken` 来自页面或操作的生命周期：

```csharp
using System;
using ComposeAnimation.Runtime;

try
{
    await Help.State.ShowAsync(MutatePriority.Default, cancellationToken);
}
catch (OperationCanceledException)
{
    // 超时、其他提示抢占或调用方取消都会结束等待。
}
```

`ShowAsync` 等待显示操作结束，不是在首次出现时返回。`State.Dismiss()` 请求隐藏；
非持续 UserInput 请求即使隐藏也会继续等待打断、取消或卸载。
默认提示共享互斥组；相同或更高优先级会打断旧请求，较低优先级请求被取消。
需要独立组时向 `new TooltipState(isPersistent: false, mutatorMutex: group)` 传入自己的
`ComposeAnimation.Runtime.MutatorMutex`，不同组可同时显示。

鼠标进入显示、离开关闭非持续提示；焦点和触摸/笔长按也可显示，Escape KeyDown 关闭并消费按键。
`EnableUserInput="False"` 关闭这些输入入口，不影响程序调用。
外部按下默认隐藏；订阅 `DismissRequested` 后由应用负责决定，例如：

```csharp
Help.DismissRequested += (_, _) => Help.State.Dismiss();
```

`IsVisible` 包含退出阶段；Popup 到退出动画完成才移除。非持续 Default / PreventUserInput
请求默认在 1500ms 超时，焦点/长按流程另负责其关闭时机。

## 定位、内容与外观

`AnchorPosition` 支持 Above / Below / Left / Right / Start / End，`Spacing` 默认 4 DIP。
不足时翻到另一侧并限制在客户区内；Start / End 跟随 `FlowDirection`。
自定义 `ITooltipPositionProvider` 的输入与输出均为物理窗口像素。
提示不增加锚点的布局尺寸；PlainTooltip 也能单独用于普通布局。

PlainTooltip 用 `Background`、`Foreground`、`CornerRadius`、`Padding`、`ContentTemplate`
覆盖外观，`TonalElevation` / `ShadowElevation` 默认均为 0。
`TooltipMaxWidth` 默认 200；内部最小尺寸为 40×24 DIP。显式 `Width` / `Height` 及调用方约束
位于默认限制外，可让宽度小于 40 或大于 200。文字默认 BodySmall，长内容按可用宽度换行。
动态主题定制见[公共 Token 入口](../../../../docs/GETTING_STARTED.md#主题与定制)。

## RichTooltip

需要标题或动作时，在同一个 TooltipBox 中使用 RichTooltip：

```xml
<m3:TooltipBox x:Name="Details" HasAction="True">
  <m3:TooltipBox.Tooltip>
    <m3:RichTooltip Title="收藏夹" Content="把相关内容保存在一起，方便下次查找。">
      <m3:RichTooltip.Action>
        <Button Content="知道了" Theme="{StaticResource MaterialTextButtonTheme}" />
      </m3:RichTooltip.Action>
    </m3:RichTooltip>
  </m3:TooltipBox.Tooltip>
  <Button Content="收藏夹说明" />
</m3:TooltipBox>
```

`Title` / `TitleTemplate` 和 `Action` / `ActionTemplate` 是可选槽，`Content` / `ContentTemplate`
是正文。模板可在内容为 null 时单独提供槽。动作的命令或 Click 处理由应用负责；需要关闭时调用
`Details.State.Dismiss()`。需要持续显示时显式提供 `new TooltipState(isPersistent: true)`。

默认背景为 SurfaceContainer、圆角 Medium、阴影 Level2（3 DIP）、tonal elevation 为 0；
最大宽度 320、软最小尺寸 40×24、水平 Padding 16 DIP。显式尺寸的优先规则与 PlainTooltip 相同。
`Foreground`、`TitleForeground`、`ActionForeground` 分别覆盖正文、标题和动作颜色，
三个槽分别提供 BodyMedium、TitleSmall 和 LabelLarge。子控件的本地文字/颜色设置继续优先。

标题首基线距顶部 28 DIP；有标题或动作时，正文首基线距其槽顶部 24 DIP、底部留白 16 DIP。
仅有正文时上下留白为 4 DIP；动作区最小高度 36 DIP，底部留白 8 DIP。
普通 TextBlock 与显式 `TextBlock.BaselineOffset` 可提供基线；复杂自定义容器的基线传播尚未实现，
无基线内容采用上游的零基线分支。RichTooltip 也可以单独放入普通布局。

复杂槽已有确定首基线时，可显式提供，数值为槽根内容坐标内的 DIP：

```csharp
var titlePanel = new Grid { Children = { new TextBlock { Text = "收藏夹" } } };
TextBlock.SetBaselineOffset(titlePanel, 15);
tooltip.Title = titlePanel;
```

这里的 15 应替换为该布局实际的首基线；嵌套布局改变时由调用者同步更新。

## 使用边界

caret 路径尚未实现，没有会被忽略的 CaretShape 属性；PlainTooltip 和 RichTooltip 均实现默认无 caret 分支。
TooltipBox 按固定 Core 桌面行为不抢焦点；`HasAction="True"` 且可见时消费 Tab，
但不会将键盘焦点送到提示按钮，不宜把它当成完整键盘可操作弹窗。
提示提供 ToolTip automation 与 Assertive live setting，实际屏幕阅读器播报、物理触摸/笔设备、
文字像素尚未经验证；RichTooltip 的桌面点击打开/动作关闭已检查，未覆盖所有桌面输入路径。动画提交时序有已接受差异，见
[一致性边界](INTERNALS.md#一致性边界与验证)。完整使用例子见
[TooltipDemo](../../../Material3Expressive.Catalog/TooltipDemo.axaml)。
