# TimePickerDialog / VibrantTimePickerDialog

`TimePickerDialog` 是库提供的时间编辑器对话框容器，继承 Foundation Dialog。
标题、编辑器与动作由调用方提供。对话框不持有时间状态，也不在钟面和输入之间切换。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)，包括 Window 动画初始化与
[Dialog 宿主模板](../../Foundation/Overlays/README.md#接入)。维护说明见 [INTERNALS](INTERNALS.md)。

## 使用

以下片段放入 Window 的 Grid；Window 声明
`xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"`。
打开、确认、取消和关闭请求处理器采用 [DatePickerDialog 示例](../DatePickerDialog/README.md#使用)
中的代码，控件同样命名为 PickerDialog。

```xml
<m3:TimePickerDialog x:Name="PickerDialog" DismissRequested="OnDismissRequested">
  <m3:TimePickerDialog.Title>
    <TextBlock Text="输入时间" Margin="0,0,0,20"
               m3:MaterialText.Style="{m3:Typography LabelMedium}" />
  </m3:TimePickerDialog.Title>
  <m3:TimePicker x:Name="DraftTime" Layout="Vertical" />
  <m3:TimePickerDialog.ModeToggleButton>
    <Button Content="Keyboard" Click="OnToggleMode" Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:TimePickerDialog.ModeToggleButton>
  <m3:TimePickerDialog.ConfirmButton>
    <Button Content="确定" Click="OnConfirm" Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:TimePickerDialog.ConfirmButton>
  <m3:TimePickerDialog.DismissButton>
    <Button Content="取消" Click="OnCancel" Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:TimePickerDialog.DismissButton>
</m3:TimePickerDialog>
```

`OnToggleMode` 由应用把 Content 换成 `TimeInput`，或再换回 `TimePicker`。Catalog 同时放着两个控件，用 `IsVisible` 切换，确认时读取有效的 `SelectedTime`。
IsOpen 可绑定 ViewModel；按钮可绑定业务 Command。Dialog 不创建按钮，不自动关闭或校验输入。
DismissRequested 仅表示外部点击、Escape/Back 请求，调用方决定是否修改 IsOpen。
完整可编译例子见 [PickerDialogDemo](../../../Material3Expressive.Catalog/PickerDialogDemo.axaml)。

## 槽和布局

Content/ContentTemplate 承载编辑器；Title、ConfirmButton 由调用方提供，DismissButton 和
ModeToggleButton 可省略，均有对应的 Template。槽支持控件和模板数据；显式模板表示槽存在。
ModeToggleButton 位于动作行 start，点击后由应用更新 Content、标题或图标，Dialog 没有内部模式状态。
Title 槽不附加排版或留白；示例的 LabelMedium 和底部 20 DIP 属于调用方标题内容。

自然宽度为内容实际宽度加 48 DIP。内容宽大于高且高至少 200 DIP 时使用横向布局，
填满宿主提供的有限高度；否则纵向包裹标题、内容、动作，两侧和上下留白为 24 DIP。
横向分支的标题和内容可能重叠，与固定上游相同；应用应提供适合该模式的编辑器。
动作按切换按钮、弹性间隔、取消、确认排列，间距 8 DIP，不换行；空间不足时后面的槽受剩余宽度约束。
UsePlatformDefaultWidth 默认 false，显式启用则应用 Foundation 平台宽度上限。

Background、Foreground、CornerRadius 可定制 Surface。默认 SurfaceContainerHigh / ExtraLarge，
色调高度从 Dialog.ContainerElevation（Level3）读取，无默认投影。标题和内容继承 Surface 内容色，
按钮由自身主题提供样式，局部原生属性仍优先。

## Vibrant 变体

`VibrantTimePickerDialog` 同样继承 Foundation Dialog，提供 Content、ConfirmButton、DismissButton、
ModeToggleButton 及对应 Template。它没有 Title 槽，使用 SurfaceContainer 配色和 ExtraLarge 圆角。
确认、取消、切换按钮都由应用处理，编辑器自身仍可带标题。

```xml
<m3:VibrantTimePickerDialog x:Name="PickerDialog" DismissRequested="OnDismissRequested">
  <m3:TimeInput SelectedTime="{Binding DraftTime, Mode=TwoWay}" />
  <m3:VibrantTimePickerDialog.ConfirmButton>
    <Button Content="确定" Click="OnConfirm" Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:VibrantTimePickerDialog.ConfirmButton>
  <m3:VibrantTimePickerDialog.DismissButton>
    <Button Content="取消" Click="OnCancel" Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:VibrantTimePickerDialog.DismissButton>
</m3:VibrantTimePickerDialog>
```

处理器与上面的普通变体相同。例中 DraftTime 是应用的 TimeOnly 编辑值。
两侧留白各 12 DIP；纵向顶部、底部和内容至动作间距均为 12 DIP。
横向条件仍为内容宽大于高且高度至少 200 DIP；横向顶部/底部各 12、内容至动作间距 8 DIP，
有限宿主高度下填满高度，不采用普通变体在 384 DIP 高度处的动作上移。
`Comp.TimePickerDialog.Vibrant*` Token 可局部覆盖布局和容器色。Catalog 提供独立的打开入口。

### 显式容器形状

`TimePickerDialog.Shape` 支持 `RoundedCornerShape`、`CutCornerShape` 和自定义 `MaterialShape`；
非 null 时覆盖 CornerRadius，清除后恢复圆角 Token/显式 CornerRadius。逻辑四角随 RTL 映射，
百分比相对容器短边，打开时修改形状对象会更新背景、内容裁剪和轮廓。

```xml
<m3:TimePickerDialog IsOpen="{Binding IsDialogOpen}">
  <m3:TimePickerDialog.Shape>
    <m3:CutCornerShape TopStart="40" TopEnd="8" BottomEnd="24" BottomStart="8" />
  </m3:TimePickerDialog.Shape>
  <!-- 内容、确认与取消槽沿用上面的示例 -->
</m3:TimePickerDialog>
```

`VibrantTimePickerDialog.Shape` 用法相同。

### 默认标题与模式切换

`TimePickerDialogTitle` 按 `DisplayMode` 选择本地化标题，采用 LabelMedium
和底部 20 DIP 留白。`Culture` 可显式指定；null 在配置更新时使用 CurrentUICulture。
`Comp.TimePickerDialog.TitlePadding` 可覆盖默认留白，普通 Text/字体局部设置仍优先。

`TimePickerDisplayModeToggle` 表达时钟/输入切换，`TimeScrollDisplayModeToggle` 表达滚轮/输入切换。
二者带默认上方 PlainTooltip、准确矢量图标和自动化名称，复用 TooltipBox 的悬停、焦点、长按及退出动画。
点击触发原生 `Click` 和 `Command`/`CommandParameter`，**不修改 DisplayMode**；应用决定何时切换编辑器。
继承 IsEnabled 控制按钮，可显式设置 Culture。滚轮变体默认 DisplayMode=Scroll，其余默认 Picker。

```xml
<m3:TimePickerDialogTitle x:Name="ModeTitle" DisplayMode="Picker" />
<m3:TimePickerDisplayModeToggle x:Name="ModeButton" DisplayMode="Picker" Click="ChangeMode" />
```

```csharp
private void ChangeMode(object? sender, RoutedEventArgs e)
{
    var next = ModeButton.DisplayMode == TimePickerDisplayMode.Picker
        ? TimePickerDisplayMode.Input : TimePickerDisplayMode.Picker;
    ModeTitle.DisplayMode = ModeButton.DisplayMode = next;
    DraftDial.IsVisible = next == TimePickerDisplayMode.Picker;
    DraftInput.IsVisible = next == TimePickerDisplayMode.Input;
}
```

将标题与按钮分别放进 Dialog.Title、Dialog.ModeToggleButton 槽。绑定应用模式属性也可；
两个编辑器用同一 SelectedTime。Catalog 的 Picker dialogs 已使用该组合，Date/time pickers 的
非包含式输入/滚轮例展示 TimeScrollDisplayModeToggle。替换已有手写标题时去掉重复的底部 Margin。

## 使用边界

默认标题与两种模式切换辅助组件见上文。
系统/IME inset 和平台安全窗口选项尚未实现；文字像素、真实输入法、屏幕阅读器模态隔离、
触屏及多显示器 DPI 切换未经桌面验收。离屏几何结果不证明这些能力。
