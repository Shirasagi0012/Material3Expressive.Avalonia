# DatePicker / DateRangePicker

这两个控件对应 Compose Material3 的日期选择内容：月历与文本输入是同一控件的两种显示模式。
它们不包含打开、关闭或确认按钮。把选中值写回表单由应用负责。
Avalonia 原生年月日滚轮、文本框和下拉日历使用另一套主题，见
[原生日期与时间主题](../DateTimePicker/README.md)。

首次使用请完成[公共接入](../../../../docs/GETTING_STARTED.md)。命名空间为
`https://github.com/Shirasagi0012/Material3Expressive.Avalonia`。

## 最小示例

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel Spacing="24">
    <m3:DatePicker SelectedDate="{Binding Date, Mode=TwoWay}"
                   DisplayMode="Picker" />
    <m3:DateRangePicker SelectedRange="{Binding Dates, Mode=TwoWay}" />
  </StackPanel>
</UserControl>
```

`Date` 是 `DateOnly?`。`Dates` 是 `DateRange`，包含可空的 `Start` 和 `End`。
只有起点、终点为空表示范围还在选择中。片段假设 DataContext 提供这两个属性；编译绑定需声明 `x:DataType`。

没有公开的 `DatePickerState`。每个控件自己保存输入到一半的文本、当前月份和年份列表是否展开。
两个控件绑定同一个属性时，只同步已经形成的有效日期，不同步这些编辑过程。

## 常用属性

- `DisplayMode`：`Picker` 为月历，`Input` 为数字日期。`ShowModeToggle` 控制标题栏里的切换按钮。
- `DisplayedMonth`：第一个仍可见的月份，取该月 1 日。向后翻页动画中旧月份完全离开后才更新；向前翻页时前一月份一出现就更新。空闲时赋值直接定位，选择或切换模式时会跟着有效日期移动。
- `MinYear` / `MaxYear`：默认 1900 到 2100。超出范围的日期会被清空。
- `IsDateSelectable` / `IsYearSelectable`：返回 false 的日期或年份不可选。
- `Culture`：决定星期起始日、输入顺序、标题和校验文字。输入数字时自动显示区域日期分隔符，例如 `02/29/2024`
  ；也可以粘贴同格式日期。占位符使用大写格式，例如 `MM/DD/YYYY`。
- `Title`、`Headline` 及对应模板：不设置时使用当前语言的 Material 文案。`ShowHeadline` 可以隐藏大标题。

例如 `picker.Culture = CultureInfo.GetCultureInfo("zh-CN")` 使用简体中文，`zh-TW` 使用繁体中文，
`pt-PT` 使用葡萄牙地区文案。先查语言和地区，再查语言，最后回退英语；仅 `zh` 没有对应上游译表。
Catalog 的日期语言按钮会同时更新单日和范围选择器。

日期范围用一次赋值更新起止，避免两个独立双向绑定产生中间状态。终点早于起点，或只有终点没有起点时，范围会整个清空。这与固定版本
Compose 的 `setSelection` 相同。

## 月份交互

单日月历的左右按钮平滑切月；触摸横滑会连续移动月历，松手后吸附到当前或相邻月份。
桌面触控板的横向滚动也会吸附；竖向滚轮留给外层页面，鼠标左键拖动不触发月份滚动。
可以在动画中再次点击箭头或开始触摸来中断。年份选择仍直接定位到该年同月。
日期按钮的方向键可跨月移动，PageUp / PageDown 移到上月 / 下月的相应日期。
左右方向键跳过禁选日期。首个可选日期的 Shift+Tab 返回“下月”按钮；最后一个可选日期的 Tab 向下移出月历。
打开年份网格后聚焦当前显示年份；方向键移动焦点，Enter 选择年份。
年份的 Shift+Tab 返回月份标题，Tab 离开年份网格；从月份标题按 Tab 可重新进入当前年份。

## 对话框

确认和取消使用 [DatePickerDialog](../DatePickerDialog/README.md)。对话框只提供表面和按钮，`DatePicker` 或
`DateRangePicker` 放在 Content 里。
Catalog 的 “Picker dialogs” 页面在确认时才写回选中值；取消或关闭不改原值。

## 限制

控件选择的是日期，不包含时分秒。范围月历与年份列表仍使用 Avalonia 滚动；单日月历已移植月份吸附，尚未提供通用 LazyList 的嵌套滚动协议。
视觉比较允许真实日期文字，但文字或像素不一致不代表已经接受为正确外观。维护说明见 [INTERNALS.md](INTERNALS.md)。
