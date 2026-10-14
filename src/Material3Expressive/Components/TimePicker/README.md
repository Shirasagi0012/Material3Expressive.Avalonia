# TimePicker / TimeInput / TimeScroll

这三个控件对应 Compose Material3 的时间内容。`TimePicker` 是钟面，`TimeInput` 是小时和分钟文本框，`TimeScroll` 是上下滚动的小时和分钟。
它们都精确到分钟，不包含秒。打开、关闭和确认由应用负责，对话框容器是 [TimePickerDialog](../TimePickerDialog/README.md)。
Catalog 的 “Picker dialogs” 页面用模式按钮在 `TimePicker` 和 `TimeInput` 之间切换，确认后才写回时间。

Avalonia 原生时间滚轮，包括秒和步长，使用 [原生日期与时间主题](../DateTimePicker/README.md)，不要把 `TimeScroll` 当成那个控件。

首次使用请完成[公共接入](../../../../docs/GETTING_STARTED.md)。

## 最小示例

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel Spacing="24">
    <m3:TimePicker Name="Dial" SelectedTime="{Binding Time, Mode=TwoWay}" Is24Hour="True" />
    <m3:TimeInput SelectedTime="{Binding #Dial.SelectedTime, Mode=TwoWay}"
                  Is24Hour="{Binding #Dial.Is24Hour}" />
    <m3:TimeScroll SelectedTime="{Binding #Dial.SelectedTime, Mode=TwoWay}"
                   Is24Hour="{Binding #Dial.Is24Hour}" />
  </StackPanel>
</UserControl>
```

`Time` 是 `TimeOnly`。绑定同一个属性时，三个控件同步有效时间。
某一侧还没形成合法小时或分钟时，该控件的 `IsInputValid` 为 false，不会把草稿写进 `SelectedTime`。
换一个控件实例后，未完成的输入不会自动恢复。

## 常用属性

- `Is24Hour`：24 小时不显示 AM/PM。默认跟随当前文化的短时间格式里有没有 AM/PM 标记。
- `Selection`：`Hour` 或 `Minute`。钟面上选择小时后会切到分钟；文本框在小时输完两位后也会切过去。
- `Layout`：`Vertical`、`Horizontal` 或 `Automatic`。水平布局只影响钟面，把数字和时间盘左右排列。`Automatic` 在宿主宽度大于高度时使用水平布局。
- `Variant`：`Standard` 或 `Vibrant`。`TimeScroll` 默认是 `Vibrant`。
- `Toggle` / `ToggleTemplate`：放在时间数字旁边的应用内容，例如切换钟面和输入的按钮。库不替应用切换控件。

12 小时模式下，小时文本接受 1 到 12，午夜写回 `TimeOnly` 的 0 点，中午写回 12 点。分钟只接受 0 到 59。
钟面 24 小时的外圈是 0 到 11，内圈是 12 到 23。拖动分钟可以落到任意一分钟，刻度高亮离它最近的 5 分钟。

## 限制

滚动列使用 Avalonia 的循环选择面板，不是 Compose `VerticalPager`。拖动和惯性不保证相同。
视觉用例包含真实时间数字；失配需要单独看，不表示外观已经被接受。维护说明见 [INTERNALS.md](INTERNALS.md)。
Catalog 的 “Date / time pickers” 页面同时放了这三种入口。确认对话框见 “Picker dialogs”。
