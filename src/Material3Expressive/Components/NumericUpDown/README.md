# NumericUpDown / ButtonSpinner

M3 没有 NumericUpDown。加载 `MaterialTheme` 后，原生 Avalonia `NumericUpDown` 显示为
Material [TextField](../TextField/README.md)，加减步进器是一对标准 IconButton 样式的 `RepeatButton`。
数值解析、格式化、`Minimum` / `Maximum` / `Increment` 与事件仍由原生控件处理。

```xml
<NumericUpDown Value="{Binding Quantity}" Minimum="0" Maximum="10"
               m3:MaterialTextField.Label="数量"
               m3:MaterialTextField.SupportingText="0 到 10" />
<NumericUpDown Theme="{StaticResource MaterialOutlinedNumericUpDownTheme}"
               Value="1.5" Increment="0.5" FormatString="0.0"
               m3:MaterialTextField.Label="重量" m3:MaterialTextField.Suffix="kg" />
```

| 外观 | Theme 资源 | 内部 TextBox 主题 |
| --- | --- | --- |
| Filled（默认） | `MaterialFilledNumericUpDownTheme` | `MaterialFilledTextFieldTheme` |
| Outlined | `MaterialOutlinedNumericUpDownTheme` | `MaterialOutlinedTextFieldTheme` |

- 结构：`PART_TextBox` 就是 Material TextField，`NumericUpDown` 自身不画边框或背景，因此与 Fluent 主题同时加载时也只有一层容器
- 步进器：`PART_Spinner` 放在 TextField 的 trailing 图标槽，`ButtonSpinnerLocation="Left"` 时放在 leading 槽；两个 Small 标准图标按钮（40 DIP 容器、48 DIP 布局槽、24 DIP 图标）横向排列为 `remove` / `add`，字段保持 56 DIP 高。选择横向而非上下堆叠的箭头，是因为堆叠需要低于 IconButton 规格的容器与点击区域
- 按钮状态：到达 `Maximum` / `Minimum`、`IsReadOnly`、`Increment=0` 时由原生 `ValidSpinDirection` 禁用对应按钮；`AllowSpin="False"` 禁用两者；`ShowButtonSpinner="False"` 移除步进器并释放图标槽。悬停、按压与水波纹来自 `MaterialInteraction`
- 焦点与输入：步进器不可聚焦，点击时焦点和聚焦外观留在编辑框；编辑框未处理的 Up / Down 键以及聚焦时的滚轮会转给 spinner，规则与原生 `ButtonSpinner` 相同（XY 方向导航模式的例外未复刻）
- 转发：`MaterialTextField.Label`、`SupportingText`、`Prefix`、`Suffix`、`LeadingIcon`（及各自 `*Template`）和 `IsError` 可直接设在 `NumericUpDown` 上；原生 `PlaceholderText`、`IsReadOnly`、`TextAlignment`、`HorizontalContentAlignment`、`InnerLeftContent` / `InnerRightContent` 按 Fluent 的方式转给内部 TextBox。`Padding`、颜色与 `VerticalContentAlignment` 不转发，保持 TextField 默认值
- 槽位：`LeadingIcon` 与 `InnerLeftContent` / `InnerRightContent` 沿用 TextField 规则（图标优先，图标为空时显示 Inner 内容）。spinner 占据的槽不再显示这些内容：默认右侧时 `InnerRightContent` 不显示，左侧时 `LeadingIcon` 与 `InnerLeftContent` 不显示
- 错误：`MaterialTextField.IsError` 或 `NumericUpDown` 的数据验证错误（`DataValidationErrors.HasErrors`）都会让内部字段显示错误颜色；不会显示错误文字，需要时自行设置 `SupportingText`
- RTL、禁用与明暗主题沿用 TextField；步进器图标颜色继承所在图标槽的颜色

## 独立 ButtonSpinner

未指定 `Theme` 的 `ButtonSpinner`（`MaterialButtonSpinnerTheme`）在 Outlined TextField 同款形状、描边和颜色的
Material 表面上显示 `Content` 与同一对步进器，支持 `:left`、`:focus-within`、`:error` 与禁用。
与原生一致，独立使用时步进器可聚焦（`IsTabStop` 跟随 spinner）。Fluent 的 DatePicker / TimePicker 等不使用
`ButtonSpinner`，普通 `RepeatButton` 的隐式主题也保持不变。

查看运行示例：`dotnet run --project src/Material3Expressive.Catalog -- --numeric-up-down`。
