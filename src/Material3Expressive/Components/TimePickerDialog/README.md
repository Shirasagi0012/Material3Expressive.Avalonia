# TimePickerDialog

`TimePickerDialog` 是库提供的时间编辑器对话框容器，继承 Foundation Dialog。
标题、编辑器与动作由调用方提供，当前不包含 TimePicker/TimeInput 本体或时间状态。
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
  <TextBox Text="09:30" Width="256" Margin="0,0,0,20" />
  <m3:TimePickerDialog.ConfirmButton>
    <Button Content="确定" Click="OnConfirm" Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:TimePickerDialog.ConfirmButton>
  <m3:TimePickerDialog.DismissButton>
    <Button Content="取消" Click="OnCancel" Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:TimePickerDialog.DismissButton>
</m3:TimePickerDialog>
```

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

## 使用边界

Defaults 中独立的标题/带 Tooltip 切换按钮辅助组件、VibrantTimePickerDialog 不在此入口范围。
系统/IME inset 和平台安全窗口选项尚未实现；文字像素、真实输入法、屏幕阅读器模态隔离、
触屏及多显示器 DPI 切换未经桌面验收。离屏几何结果不证明这些能力。
