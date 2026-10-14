# DatePicker / DateRangePicker 实现说明

使用方式见 [README](README.md)。原生滚轮和日历主题不在这里。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）。
日期核心与 Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的对应文件一致。
不要用上游 HEAD 替代固定提交。

| 上游                                                    | 本地                                                                                                                                              |
|---------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------|
| `DatePicker`、`DateInput`、`SwitchableDateEntryContent` | [DatePicker.cs](DatePicker.cs)、[DateEntryController.cs](DateEntryController.cs)、[DateEntryContent.cs](DateEntryContent.cs)                      |
| `DateRangePicker`、`DateRangeInput`、`setSelection`     | [DateRangePicker.cs](DateRangePicker.cs)、[DateRange.cs](DateRange.cs)                                                                            |
| 月网格、范围底色                                        | `DateMonthPanel`                                                                                                                                  |
| `DatePickerModalTokens`、`DateInputModalTokens`         | [DatePickerModalTokenSource.cs](DatePickerModalTokenSource.cs)、[DateInputModalTokenSource.cs](DateInputModalTokenSource.cs) 与 Tokens 下同名文件 |
| Core `material3/l10n` 的日期字符串                      | [DateTimeStrings.cs](../DateTimePicker/DateTimeStrings.cs)                                                                                        |

公开入口是控件属性，不是可提升的状态对象。维护者于 2026-10-11 选择直接绑定：
`SelectedDate` 为 `DateOnly?`，`SelectedRange` 为一次写回的 `DateRange`。
UTC 当天零点毫秒数只留在与 Compose 对照时使用，不进入公开 API。
因此多个控件默认只共享有效日期；未完成的输入文本、年份列表展开和显示月份不会跟着另一个控件走。
`DisplayedMonth` 是单独属性，应用需要同步当前月份时可以绑定它。

打开、关闭和确认不属于控件。点选一天会立刻更新 `SelectedDate` 或 `SelectedRange`，不会等应用提交。

## 选择、输入与模式

日历点选：单日直接替换。范围在没有终点或新日期早于起点时重新开始，否则补上终点。
不可选日期、年份范围外的日期不改变当前值。`NormalizeSelection` 会去掉越界日期；范围缺少起点，或终点早于起点时，两端一起清空。

输入模式的日期数字不超过 `yyyy`、`MM`、`dd` 拼出的 8 位；按固定 `DateVisualTransformation`
的字段边界自动显示分隔符，也接受粘贴同区域的带分隔符日期。未输满时清空这一侧并去掉错误；输满后按区域顺序解析，再检查闰日、年份、禁选和范围顺序。非法字符被拒绝，已输入的数字保留。
Avalonia TextBox 没有独立显示变换入口，因此内部 `DateInputTextBox` 在 `CoerceText`
中格式化显示文本，仍调用原生基类维护编辑事务及撤销栈；控制器验证时提取数字，不更改 TextField 的共享
API。光标和选区按插入的分隔符映射；分隔符旁的 Backspace/Delete 选择相邻数字一起交给原生删除，避免删除后立即补回分隔符而卡住。
占位符使用带分隔符的大写格式。格式来自 `CultureInfo.ShortDatePattern` 的日月年顺序，并把年份规范成四位。它不是 Compose
`DateInputFormat` 的完整 skeleton，区域差异未逐个核对。

`DateEntryContent` 在单日模式切换时做垂直滑入和淡化，范围模式切换时只做交叉淡化。
年份列表用 `AnimatedVisibility` 覆盖在保留的星期/月网格上展开；年份内容为 335 DIP 加 1 DIP divider，与下层的 48 + 288 DIP
高度一致。背景跟随当前日期控件，切换过程不隐藏或移除月视图，不通过 Dialog 最小高度补偿。动画规格来自 `SpatialDefault`、
`EffectsDefault` 和 `EffectsFast`。
`DateMonthPanel.Render` 绘制透明全区域命中面，使日期按钮之间和空日期单元格也能收到月份滚轮/横滑输入；范围底色仍单独绘制。

## 已确认的适配

月份横滑、滚轮换月和范围纵向列表使用 Avalonia `ScrollViewer`，范围列表用虚拟化堆叠并在滚动后更新 `DisplayedMonth`。
TimeScroll 同样不移植通用 Pager。维护者于 2026-10-11 接受这项适配：补循环、分页位置和键盘，不承诺拖动与惯性等于 Compose
Foundation。

`date-picker`
视觉用例允许真实日期文字。这只放宽“用例不得含文字”的限制，不放宽比较容差，也不把失配收成已接受差异。见 [AUTHORING.md](../../../../tests/VisualParity/AUTHORING.md)。

## 尚未实现或未验证

- 没有独立 `DateInput` 控件，也没有 `DatePickerDialog` 类型。
- 年份和月份列表没有 Compose LazyList 的吸附、甩动和键盘分页。
- 标题日期使用 `ddd, MMM d`，未对照 `DatePickerFormatter` 的完整 skeleton。
- 双端像素差异按维护者要求保留证据，不在本轮继续追修。
- 无障碍遍历顺序、触摸滑动阈值和全部区域的输入格式未逐项对照上游测试。

逻辑检查在 `tests/Material3Expressive.Tests/DateTimeEntryTests.cs`。
双端用例是 `tests/VisualParity/Material3Expressive.Parity/Cases/DatePickerCases.cs` 与 reference 侧同名 Kotlin。
命令是 `./tests/VisualParity/parity.ps1 list --family date-picker`，再对该 family 做 `prepare-reference` 和 `run`。
