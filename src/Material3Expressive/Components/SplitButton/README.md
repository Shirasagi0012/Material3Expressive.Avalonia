# SplitButton

`m3:SplitButton` 将主操作与相关副操作放在同一组，父级负责两槽布局，实际子按钮负责命令和反馈。
它是库的独立控件，不继承 Avalonia SplitButton。先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
布局、形状与原生适配依据见 [INTERNALS](INTERNALS.md)。

## 最小示例

在已加载 Material 主题的应用中：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:SplitButton>
    <m3:SplitButton.LeadingButton>
      <Button Content="保存" Theme="{StaticResource MaterialFilledSplitButtonLeadingTheme}" />
    </m3:SplitButton.LeadingButton>
    <m3:SplitButton.TrailingButton>
      <ToggleButton Theme="{StaticResource MaterialFilledSplitButtonTrailingTheme}"
                    AutomationProperties.Name="更多保存选项">
        <m3:MaterialSymbol Symbol="ArrowDropDown" Width="22" Height="22" />
      </ToggleButton>
    </m3:SplitButton.TrailingButton>
  </m3:SplitButton>
</UserControl>
```

后续片段沿用上述命名空间。此例展示副区的原生切换，不自动创建或展开菜单。

## 独立动作与主题

`LeadingButton`/`TrailingButton` 都是 Control 槽，通常前者为原生 Button，后者为 Button 或 ToggleButton。
`Command`、`CommandParameter`、`Click` 和 Flyout 均配置在实际子控件上；父级不转发 `Click`/`Command`，
也没有自动 Flyout 绑定。父级 `IsEnabled` 的继承按 Avalonia 规则生效。

```xml
<m3:SplitButton Spacing="2">
  <m3:SplitButton.LeadingButton>
    <Button Content="保存" Command="{Binding SaveCommand}"
            Theme="{StaticResource MaterialTonalSplitButtonLeadingTheme}" />
  </m3:SplitButton.LeadingButton>
  <m3:SplitButton.TrailingButton>
    <ToggleButton Content="…" IsChecked="{Binding Expanded, Mode=TwoWay}"
                  Theme="{StaticResource MaterialTonalSplitButtonTrailingTheme}" />
  </m3:SplitButton.TrailingButton>
</m3:SplitButton>
```

假设 `DataContext` 提供 `ICommand` `SaveCommand` 和可写 `Expanded`；编译绑定需 `x:DataType`。
`Expanded` 在这里仅表示副按钮状态，应用自行把它与需要显示的内容连接。
ToggleButton 先改 `IsChecked` 再触发 `Click`/`Command`；Enter 按下、`Space` 默认松开，null 按未选中显示。
事件方式需要在视图 code-behind 实现对应子按钮处理器。

四种配套主题均成对使用：

| 外观 | 主区 `Theme` | 副区 `Theme` |
| --- | --- | --- |
| Filled | `MaterialFilledSplitButtonLeadingTheme` | `MaterialFilledSplitButtonTrailingTheme` |
| Tonal | `MaterialTonalSplitButtonLeadingTheme` | `MaterialTonalSplitButtonTrailingTheme` |
| Elevated | `MaterialElevatedSplitButtonLeadingTheme` | `MaterialElevatedSplitButtonTrailingTheme` |
| Outlined | `MaterialOutlinedSplitButtonLeadingTheme` | `MaterialOutlinedSplitButtonTrailingTheme` |

## 尺寸、内容与定制

尺寸 class 分别设置在两个子按钮上，父级 class 不会自动传递。
默认 Small；其他为 `xsmall`/`medium`/`large`/`xlarge`。对应默认背景最小高为 40/40/56/96/136 DIP：
XS 虽然尺寸 Token 为 32，固定上游 helper 内部仍保留 40 DIP 最小高；显式 `Height` 或更紧父约束可以限制它。
子按钮默认最小宽 48 DIP，父级整体预留至少 48 DIP 交互布局空间，不给两个子区分别额外加留白。
所有尺寸标签默认 LabelLarge。`Spacing` 默认 2 DIP，RTL 会镜像两槽。

主副区都使用原生 `Content`/`ContentTemplate`；副区图标尺寸由调用方设置，XS/S/M/L/XL 参考值为
22/22/26/38/50 DIP。`ToggleButtonAppearance.Icon`、`Colors`、`CheckedCornerRadius` 属于独立 ToggleButton 主题，
Split 子主题不消费这些配置；这里通过 `Content`、`Background`/`Foreground` 或 Split 相关 Token 定制。

静止圆角用 `CornerRadius`，按压目标用 `MaterialButton.PressedCornerRadius`；默认主区外侧全圆、内侧小角，
副区镜像。可切换副区选中后变为圆形，并显示 contentColor×0.10 覆盖层，禁用时该选中层仍存在。
颜色、描边、`Padding` 可使用原生属性或[Token 覆盖](../../../../docs/GETTING_STARTED.md)。

## 使用边界

Trailing 优先获得宽度，两区使用共同高度，主区消费剩余空间；极窄宽度下 Trailing 可能溢出，容器不裁剪。
自定义 Control 槽需要正确实现 Measure/DesiredSize；任意自定义内容的 intrinsic 等价性尚未全面验证。
不提供任意 Compose `Shape` 或 MutableInteractionSource 映射；真实文字、桌面手工操作和屏幕阅读器体验
未由无字形 headless 场景证明。
四种配对和普通/切换副区见 [Catalog MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)。
