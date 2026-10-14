# TimePicker / TimeInput / TimeScroll 实现说明

使用方式见 [README](README.md)。

## 固定上游与本地入口

基线为 AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 的 `TimePicker.kt`，
与 Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的同名文件一致。
身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)。

| 上游 | 本地 |
| --- | --- |
| `TimePicker`、钟面、`AnalogTimePickerState` | [TimePicker.cs](TimePicker.cs)、[ClockFace.cs](ClockFace.cs) |
| `TimeInput`、`timeInputOnChange`、小时/分钟合法性 | [TimeInput.cs](TimeInput.cs)、[TimeEntryController.cs](TimeEntryController.cs) |
| `TimeScroll` | [TimeScroll.cs](TimeScroll.cs)，列用 Avalonia `DateTimePickerPanel` |
| `TimePickerTokens`、`TimeInputTokens` | [TimePickerTokenSource.cs](TimePickerTokenSource.cs)、[TimeInputTokenSource.cs](TimeInputTokenSource.cs) |
| 布局、变体、当前字段 | [TimePickerOptions.cs](TimePickerOptions.cs) |

三个控件共用 `TimeEntryController`，但各自持有一份。公开值是 `TimeOnly` 和 `Is24Hour`，没有可在控件之间传递的 `TimePickerState`。
时间输入字段在派生 TextField 主题中设置 `MaterialText.Style`：Standard 使用 TimeInput 的 DisplayMedium，Vibrant 使用 DisplayLarge，并由原生继承传给 TextPresenter。旧 `DefaultStyle` 会被基础 TextField 的 BodyLarge `Style` 覆盖，不用于这两个字段。
字段的 `HorizontalContentAlignment` 使用 `Stretch`，由 `TextAlignment.Center` 在完整编辑视口内居中文字。若把窄 TextPresenter 本身设为 `Center`，SmoothScroll 12.0.0 的合成平移会抵消其布局居中位移，出现布局坐标居中但实际数字靠左的情况；回归检查同时读取实际渲染像素。
维护者于 2026-10-11 确认这种直接绑定。`SelectedTime` 会被收成整分。无效草稿留在文本里，并用 `IsInputValid` 表示；AM/PM 在小时无效时禁用。

## 钟面与滚动

角度以 12 点为 0，顺时针增加。24 小时根据触点是否落在内圈决定加 12。
点击按 5 分钟取整，拖动按 1 分钟。松手后如果原来在选小时，短延迟后切到分钟。
指针保留 `Animatable<float>` 的即时 `SnapToAsync` 与弹簧 `AnimateToAsync`，不改成帧前合并目标的 `AnimatedState`。
控件通过 `AnimationScope.Get` 取得框架管理的作用域，在独立子作用域中运行、订阅角度和等待切到分钟；运行显式传入作用域。
分离视觉树时只释放自己的子作用域，立即取消表盘工作；重新附着从当前选中时间建立角度状态。
祖先作用域重建时通过 `ScopeChanged` 重建子作用域及订阅，从保留的角度重新运行到当前目标。
这种手动恢复不承诺内置动画宿主的速度及帧基准恢复。
键盘左右或上下按 1 调整当前字段，并在 0 到 23、0 到 59 上循环。

`TimeScroll` 的小时列在 12 小时模式是 1 到 12 且循环，24 小时模式是 0 到 23。分钟列是 0 到 59。
12 小时的 12 写回 0 或 12，取决于当前是 AM 还是 PM。

## 已确认的适配

滚动承载和日期月份列表一起，于 2026-10-11 被接受为局部适配：不移植 Compose `VerticalPager` 或 LazyList 物理。
`time-picker` 视觉用例允许真实时间文字，失配不记为已接受差异。见 [AUTHORING.md](../../../../tests/VisualParity/AUTHORING.md)。

固定版本的 `isUpdatedTimepickerToggleEnabled` 默认为 true。时段按钮因此使用更新后的容器色，而不是旧的描边样式。

## 尚未实现或未验证

- 钟面标签的焦点环、内外圈遍历顺序和 `ClockDialNode` 的命中细节没有逐项对照。
- `Automatic` 在钟面上比较 TopLevel 客户区的宽高，并在客户区尺寸变化后，仅当横竖结果改变时重建。初次套用模板时如果还没有 TopLevel，会保持纵向，直到控件进入窗口。
- 秒、分钟步长只存在于原生 `TimePicker` 主题。
- 双端像素差异保留证据，不在本轮追修。

逻辑检查在 `tests/Material3Expressive.Tests/DateTimeEntryTests.cs`。
双端用例是 `tests/VisualParity/Material3Expressive.Parity/Cases/TimePickerCases.cs` 与 reference 侧同名 Kotlin。
