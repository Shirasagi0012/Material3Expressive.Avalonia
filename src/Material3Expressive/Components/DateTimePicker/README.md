# 原生 DatePicker / TimePicker / CalendarDatePicker 主题

加载 Material 主题后，Avalonia 这三个控件使用 Material 颜色、字体、形状和状态反馈。
选择、弹出、键盘和确认仍然由 Avalonia 12.1.2 的原控件负责。

这不是 Compose 的月历、钟面或时间输入。那些入口见
[DatePicker](../DatePicker/README.md) 和 [TimePicker](../TimePicker/README.md)。

## 会保留的行为

- `DatePicker` 仍是年、月、日滚轮。`DayVisible`、`MonthVisible`、`YearVisible`、`MinYear`、`MaxYear` 继续有效。
  弹出层里的修改先留在 Presenter，点接受才写回 `SelectedDate`，取消则恢复原值。
- `TimePicker` 仍支持 `ClockIdentifier`、`UseSeconds`、`MinuteIncrement` 和 `SecondIncrement`。接受后才写回 `SelectedTime`。
- `CalendarDatePicker` 仍是文本框加下拉 `Calendar`。选择一天后写回原生的 `SelectedDate`。

```xml
<StackPanel xmlns="https://github.com/avaloniaui" Spacing="16">
  <DatePicker DayVisible="False" />
  <TimePicker UseSeconds="True" MinuteIncrement="5" SecondIncrement="5" />
  <CalendarDatePicker PlaceholderText="输入日期" />
</StackPanel>
```

片段假设窗口已经加载 Material 主题。不需要额外附加属性。

## 外观范围

主题替换字段按钮、滚轮项、接受和取消按钮、弹出容器，以及日历的日期和年月按钮。
滚轮列的显隐、循环和步长仍由原生面板决定。主题不改变这些属性的含义。

颜色尽量使用日期和时间 Token。原生滚轮没有一一对应的 Compose 组件，所以验收标准是 Material 外观下原功能仍然完整，而不是宣称它等于 `TimeScroll` 或月历 `DatePicker`。
