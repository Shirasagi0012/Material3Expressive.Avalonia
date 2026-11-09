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
    <m3:DateRangePicker SelectedRange="{Binding Dates, Mode=TwoWay}" Height="640" />
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

## 内容槽与定制

**自定义日期显示**

`DateFormatter` 接受 `IDatePickerFormatter`，单日与范围共用同一接口。`FormatMonthYear`
格式化月份标题，`FormatDate` 格式化已选日期；`forContentDescription` 为 true 时用于朗读描述。
两者接收控件的 `Culture`，日期为可空 `DateOnly`。返回 null 使用默认占位文案，返回空字符串则保持为空。

```csharp
sealed class IsoDateFormatter : IDatePickerFormatter
{
    public string? FormatMonthYear(DateOnly? date, CultureInfo culture)
        => date?.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    public string? FormatDate(DateOnly? date, CultureInfo culture, bool forContentDescription = false)
        => date?.ToString(forContentDescription ? "D" : "yyyy-MM-dd", culture);
}
// picker.DateFormatter = new IsoDateFormatter();
```

替换 formatter 会更新标题、月份、日按钮的无障碍描述和禁选日期错误文字，保留输入草稿、焦点和滚动位置。
它不改变日期输入的解析规则。修改配置时替换 formatter 实例；接口不监听实例内部状态。
显式 `Headline` / `HeadlineTemplate` 仍由应用负责标题内容与无障碍描述。
Catalog 的 “Custom date format” 按钮展示单日和范围入口。

**独立标题内容**

`DatePickerTitle` / `DateRangePickerTitle` 与 `DatePickerHeadline` / `DateRangePickerHeadline`
可以放入应用自己的页头、对话框或选择摘要；不需要创建月历。标题接受 `DisplayMode`、`Culture`，
大标题另外接受 `SelectedDate` 或 `SelectedRange`、`DateFormatter`。改变这些属性立即更新文案和朗读描述。

```xml
<StackPanel Spacing="8">
  <m3:DateRangePickerTitle DisplayMode="{Binding Mode}" Culture="{Binding DateCulture}"
                          m3:MaterialText.Style="{m3:Typography LabelLarge}" />
  <m3:DateRangePickerHeadline SelectedRange="{Binding Dates}" DisplayMode="{Binding Mode}"
                             Culture="{Binding DateCulture}" DateFormatter="{Binding Formatter}"
                             m3:MaterialText.Style="{m3:Typography TitleLarge}" />
</StackPanel>
```

这些内容不附加外层 Padding 或固定标题字号，排版由容器提供；`Foreground` 可覆盖默认标题颜色。
单日大标题只显示一行，范围使用三个独立文本槽，以 4 DIP 间距排列“起点 / - / 终点”，受限宽度内分别换行。
大标题提供 polite live region；范围朗读为一个合并描述，不重复朗读分隔符。
范围内容只显示传入的日期，不执行完整选择器的起止合法性校验。

完整 DatePicker / DateRangePicker 的默认标题也使用这些控件。已有 `Title`、`Headline` 和模板继续优先；
自定义模板的默认数据上下文仍是原字符串，应用无需迁移。若应用曾读取模板内部
`PART_Headline.Content` 并假定它是字符串，应改为读取新内容控件或实际文本节点。
`DateFormatter = null` 沿用完整选择器既有的平台日期格式；需要固定显示和描述格式时提供 formatter。

## 月份交互

单日月历的左右按钮平滑切月；触摸横滑会连续移动月历，松手后吸附到当前或相邻月份。
桌面触控板的横向滚动也会吸附；竖向滚轮留给外层页面，鼠标左键拖动不触发月份滚动。
可以在动画中再次点击箭头或开始触摸来中断。年份选择仍直接定位到该年同月。
日期按钮的方向键可跨月移动，PageUp / PageDown 移到上月 / 下月的相应日期。
左右方向键跳过禁选日期。首个可选日期的 Shift+Tab 返回“下月”按钮；最后一个可选日期的 Tab 向下移出月历。
打开年份网格后聚焦当前显示年份；方向键移动焦点，Enter 选择年份。
年份的 Shift+Tab 返回月份标题，Tab 离开年份网格；从月份标题按 Tab 可重新进入当前年份。

范围月历的每个月份标题提供本地化的“显示上个月”和“显示下个月”无障碍动作。
应用也可调用 `rangePicker.TryScrollToPreviousMonth()` / `TryScrollToNextMonth()`：
从当前第一个可见月份向前或向后定位，保留已选日期和日历实例，不播放切月动画。
到达对应边界、禁用、输入模式或未挂载时返回 false；接受定位请求时返回 true。
设置范围控件的 `DisplayedMonth` 同样定位已有月历；末尾受剩余内容高度约束。
Catalog 的 “Range: previous / next month” 演示这两个入口。

范围月历支持连续纵向触摸拖动、释放惯性以及竖向滚轮，不吸附到月份边缘。
拖动日期按钮时，超过手势阈值后取消日期点击；滚动中再次按下或设置 `DisplayedMonth` 会中断运动。
从任意日期按 Tab 直接离开月历到后续控件；Shift+Tab 返回标题中的模式切换按钮，隐藏该按钮时返回前一个控件。
左右与上下方向键继续在日期中导航。

## 对话框

确认和取消使用 [DatePickerDialog](../DatePickerDialog/README.md)。对话框只提供表面和按钮，`DatePicker` 或
`DateRangePicker` 放在 Content 里。
Catalog 的 “Picker dialogs” 页面在确认时才写回选中值；取消或关闭不改原值。

## 限制

控件选择的是日期，不包含时分秒。范围月历需要有限高度：放在有界 Grid 中会使用标题下方的剩余高度；
放在 StackPanel、WrapPanel 或外层 ScrollViewer 中时显式设置 `Height` / `MaxHeight`。
迁移：原 `DatePickerModal.RangeMonthListHeight` Token 已移除，改用范围选择器的标准高度约束；
范围日历不再固定为 400 DIP 的内部视口。单日月历的自然高度不变。
年份列表仍使用 Avalonia 滚动；两种月历都尚未提供通用 LazyList 的嵌套滚动协议。
视觉比较允许真实日期文字，但文字或像素不一致不代表已经接受为正确外观。维护说明见 [INTERNALS.md](INTERNALS.md)。
