# DatePickerDialog

`DatePickerDialog` 是库提供的日期编辑器对话框容器，继承 BasicAlertDialog。
编辑器由调用方放入 Content；当前不包含 DatePicker 本体、日期选择状态或输入校验。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)，包括 Window 的动画初始化和
[Dialog 宿主模板](../../Foundation/Overlays/README.md#接入)。维护说明见 [INTERNALS](INTERNALS.md)。

## 使用

以下片段放入 Window 的 Grid；Window 声明
`xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"`。
示例采用普通 TextBox 作为应用编辑器，另放一个 `Click="OnOpen"` 的打开按钮。

```xml
<m3:DatePickerDialog x:Name="PickerDialog" DismissRequested="OnDismissRequested">
  <StackPanel Margin="24" Spacing="16">
    <TextBlock Text="输入日期" />
    <TextBox Text="2026-10-11" />
  </StackPanel>
  <m3:DatePickerDialog.ConfirmButton>
    <Button Content="确定" Click="OnConfirm" Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:DatePickerDialog.ConfirmButton>
  <m3:DatePickerDialog.DismissButton>
    <Button Content="取消" Click="OnCancel" Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:DatePickerDialog.DismissButton>
</m3:DatePickerDialog>
```

对应 code-behind（窗口初始化沿用公共接入）：

```csharp
using Avalonia.Interactivity;
using Material3Expressive.Foundation.Overlays;

private void OnOpen(object? sender, RoutedEventArgs e) => PickerDialog.IsOpen = true;
private void OnConfirm(object? sender, RoutedEventArgs e)
{
    // 在这里校验并保存应用编辑器的值。
    PickerDialog.IsOpen = false;
}
private void OnCancel(object? sender, RoutedEventArgs e) => PickerDialog.IsOpen = false;
private void OnDismissRequested(object? sender, DismissRequestedEventArgs e)
    => PickerDialog.IsOpen = false;
```

`IsOpen` 可绑定 ViewModel，动作可使用 Button 的 Command/CommandParameter。
Dialog 不连接按钮事件、不自动关闭；外部点击和 Escape/Back 只发出 DismissRequested。
IsPresented 包括退出动画期间；卸载立即清理。
完整例子见 [PickerDialogDemo](../../../Material3Expressive.Catalog/PickerDialogDemo.axaml)。

## 内容和尺寸

Content/ContentTemplate 承载编辑器；ConfirmButton 由调用方提供，DismissButton 可省略，
两者都有对应的 Template。可传控件或模板数据；非 null 模板表示槽存在，即使数据或模板输出为空。
一个槽内可以组合多个控件，组合布局由调用方负责。

内部 Surface 默认宽 360 DIP、最大高 568 DIP；先给动作留出高度，再测编辑器。
内容不主动撑满，换成较矮的编辑器时对话框会收缩；需要滚动时由内容自行使用 ScrollViewer。
按钮水平间距 8 DIP，空间不足时确认在上、取消在下，行间距 12 DIP；end 留白 6、底部 8 DIP。
RTL 下动作靠左，内容按 start 对齐。

Width/Height 等原生属性约束外层 BasicAlertDialog 分配；内部固定宽度是上游 requiredWidth，
外层较窄时仍居中溢出，不缩小日期表面，最终仍受窗口裁剪。
显式较大 Height 不会拉高内部表面，而是将包裹内容的表面放在分配区域中央。
UsePlatformDefaultWidth 默认 false；显式启用则应用 Foundation 平台宽度上限。

## 外观和边界

Background、Foreground、CornerRadius 定制 Surface；动作提供 Primary / LabelLarge 环境。
TonalElevation 默认 Level0，仅背景等于 Surface 色时参与色调叠加，没有默认投影。
Comp.DatePickerModal Token 保留颜色、圆角及尺寸动态引用；当前没有完整 DatePickerColors 类型。
内容的局部字体、Width/Height 等继续按 Avalonia 属性语义执行。

系统/IME inset 和平台安全窗口选项尚未实现；文字像素、真实输入法、屏幕阅读器模态隔离、
触屏及多显示器 DPI 切换尚未经桌面验收。具体证据边界见 INTERNALS。
