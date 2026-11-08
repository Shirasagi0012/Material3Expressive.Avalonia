# ExposedDropdownMenu

独立的字段下拉菜单组合。输入文本、选择结果和展开状态由应用分别管理，支持可编辑和只读字段。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)并初始化窗口动画作用域。

## 基础用法

```xml
<m3:ExposedDropdownMenuBox xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
    x:Name="Options" Width="280" HorizontalAlignment="Left"
    ExpandedChangeRequested="OnExpandedChange">
  <m3:ExposedDropdownMenuBox.Menu>
    <m3:ExposedDropdownMenu>
      <m3:DropdownMenuItem Content="Coffee" Click="OnChoose" />
      <m3:DropdownMenuItem Content="Tea" Click="OnChoose" />
    </m3:ExposedDropdownMenu>
  </m3:ExposedDropdownMenuBox.Menu>
  <TextBox x:Name="Query" m3:MaterialTextField.Label="Drink"
           m3:ExposedDropdownMenuBox.AnchorType="PrimaryEditable" />
</m3:ExposedDropdownMenuBox>
```

```csharp
private void OnExpandedChange(object? sender, ExpandedChangeRequestedEventArgs e)
    => Options.IsExpanded = e.Expanded;

private void OnChoose(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    Query.Text = ((DropdownMenuItem)sender!).Content?.ToString();
    Options.IsExpanded = false;
}
```

类型位于 `Material3Expressive.Components`。事件处理器放在所属视图的代码中。

## 变体与属性

| 入口 | 行为 |
| --- | --- |
| `PrimaryEditable` | 点击/Enter 请求展开，Space 保留编辑；展开时 Up/Down/Tab 转入菜单导航 |
| `PrimaryNotEditable` | 用于只读字段；展开后菜单接管键盘导航 |
| `SecondaryEditable` | 用于可编辑字段旁的按钮/图标；按下不抢走字段焦点 |
| `IsAnchorEnabled=false` | 禁用该锚点的展开输入，不禁用文本编辑 |
| `IsExpanded` / `ExpandedChangeRequested.Expanded` | 应用持有状态；处理请求后显式写回 |
| `Menu.MatchAnchorWidth` | 默认匹配整个 Box 宽度，false 使用内容自然宽度 |

只读字段同时设置 `TextBox.IsReadOnly=true` 和 `AnchorType=PrimaryNotEditable`。
下拉菜单沿用 `DropdownMenu` 的表面、滚动、项目、开闭动画及外观属性。
可用继承的 `Shape` 设置 `CutCornerShape` 或自定义路径，null 保留 `CornerRadius`；用法见 [Menu](../Menu/README.md)。
显式 Width/Height/MinHeight 是调用方约束，可以覆盖默认锚点宽度或可用高度。

字段可选 `MaterialFilledExposedDropdownTextFieldTheme` 或 `MaterialOutlinedExposedDropdownTextFieldTheme`，
提供下拉字段的默认状态配色。普通 TextBox 主题也可继续用于自定义组合。

```xml
<TextBox Theme="{StaticResource MaterialFilledExposedDropdownTextFieldTheme}"
         m3:ExposedDropdownMenuBox.AnchorType="PrimaryEditable">
  <TextBox.InnerRightContent>
    <m3:ExposedDropdownMenuTrailingIcon IsExpanded="{Binding IsExpanded, ElementName=Options}"
        m3:ExposedDropdownMenuBox.AnchorType="SecondaryEditable" />
  </TextBox.InnerRightContent>
</TextBox>
```

箭头默认 24 DIP，继承 Foreground，随 IsExpanded 立即翻转。

## 内容槽与定制

`Content` 放字段与附属内容，`Menu` 放 `ExposedDropdownMenu`。使用附加属性在内容内指定锚点，
可将 `SecondaryEditable` 标在 TextBox 的尾部图标控件上。菜单项目支持 Click/Command，选择后不会自行改写文本。
Box 管理其 Menu 的 PlacementTarget、IsOpen 和 IsPopupFocusable；其他菜单外观由调用方配置。

独立使用 `ExposedDropdownMenu` 时也可直接设置 PlacementTarget 和 IsOpen，此时锚点输入由应用管理。

## 使用限制与注意事项

菜单在当前窗口内呈现。它会随锚点移动/改宽和窗口尺寸变化重新计算可用空间。
容器提供 ExpandCollapse 自动化操作，文本框继续提供原生编辑语义。
这不是原生 ComboBox 的新主题；原生 ComboBox 的选择与关闭行为仍保持原有契约。
Catalog 的 [MenuDemo](../../../Material3Expressive.Catalog/MenuDemo.axaml) 提供可编辑、只读示例。
平台边界及验证范围见 [INTERNALS.md](INTERNALS.md)。
