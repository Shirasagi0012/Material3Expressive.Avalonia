# ComboBox

在原生 Avalonia `ComboBox` 上提供 Material Filled / Outlined 外观，以及只读 Exposed Dropdown Menu 的单组菜单。
选择、文本匹配、键盘、滚轮和自动化仍由 `ComboBox` 管理。

先完成[公共接入](../../../../docs/GETTING_STARTED.md)，包括主题、字体和窗口动画作用域。
实现与移植依据见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

以下内容放在已初始化的窗口或视图中；后续片段沿用同一命名空间。

```xml
<StackPanel xmlns="https://github.com/avaloniaui"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
            Spacing="16">
    <ComboBox m3:MaterialComboBox.Label="城市" PlaceholderText="选择城市" SelectedIndex="0">
        <ComboBoxItem>上海</ComboBoxItem>
        <ComboBoxItem>杭州</ComboBoxItem>
        <ComboBoxItem>南京</ComboBoxItem>
    </ComboBox>
    <ComboBox Theme="{StaticResource MaterialOutlinedComboBoxTheme}"
              m3:MaterialComboBox.Label="城市"
              IsEditable="True"
              PlaceholderText="输入或选择" />
</StackPanel>
```

未指定 `Theme` 的 `ComboBox` 使用 Filled。显式选择主题时：

| 外观 | Theme 资源 | 默认标签位置 |
| --- | --- | --- |
| Filled | `MaterialFilledComboBoxTheme` | `Inside`，容器内 |
| Outlined | `MaterialOutlinedComboBoxTheme` | `Cutout`，描边缺口 |

应用需要先加载基础主题（Catalog 使用 Fluent），再加载 `MaterialTheme`，这样隐式 `ComboBox` 主题才会替换基础主题。

## 文本、状态与事件

沿用原生 `Items`、`SelectedItem`、`SelectedIndex`、`SelectionChanged`、`IsDropDownOpen`、`PlaceholderText` 和 `IsEnabled`。
可编辑模式使用原生 `IsEditable` 与 `Text`。绑定示例假设视图已有包含 `City`、`HasError`、`CityHint` 的 DataContext；
启用编译绑定的项目还需声明对应 `x:DataType`。

```xml
<ComboBox SelectedItem="{Binding City, Mode=TwoWay}"
          m3:MaterialComboBox.Label="城市"
          m3:MaterialComboBox.IsError="{Binding HasError}"
          m3:MaterialComboBox.SupportingText="{Binding CityHint}"
          m3:MaterialComboBox.ErrorMessage="请选择城市" />
```

- `IsError` 或 Avalonia 绑定验证的 `:error` 都会触发错误外观；状态颜色优先级为禁用、错误、聚焦、普通
- `ErrorMessage` 提供错误状态的无障碍 HelpText，不会自动变成可见的错误文字；需要显示时同时设置 `SupportingText`
- 字符串 `Label` 自动提供无障碍名称，字符串 `SupportingText` 提供普通 HelpText；应用显式设置的 `AutomationProperties.Name` / `HelpText` 优先
- 可编辑时，完整文本的大小写不敏感匹配会更新选择，没有匹配则清空选择，选择也会写回文本。筛选列表由应用自己维护
- 打开菜单会把焦点交给当前项。可编辑模式关闭后回到文本框并选中全部文本
- 选择一项会立即关闭菜单。上下键、聚焦时的滚轮和 ComboBox 自动化语义保持原生行为

## 标签与内容槽

`Label`、`SupportingText`、`LeadingIcon` 接受任意内容及对应的 `*Template`。
前导图标默认 24 DIP，槽位保留 48 DIP。尾部箭头是模板的一部分，会在菜单打开时立刻旋转 180°，不能换成应用自己的尾部图标。
本主题没有 Prefix / Suffix。

```xml
<ComboBox m3:MaterialComboBox.Label="搜索"
          PlaceholderText="选择范围"
          m3:MaterialComboBox.LeadingIcon="{m3:Symbol Search}"
          m3:MaterialComboBox.SupportingText="菜单宽度跟随字段" />
```

复杂标签内容请显式设置 `AutomationProperties.Name`。

## 菜单

弹出层使用官方只读 Exposed Dropdown 样例的单组外观：Standard 容器、大圆角、可选中项和选中勾。
可编辑与只读共用这一套菜单。菜单宽度跟随整个控件，高度仍使用 Avalonia 的 `MaxDropDownHeight`（默认 200）。
菜单不能画出窗口之外。

Catalog 入口是主窗口的 ComboBox demo，也可以用 `dotnet run --project src/Material3Expressive.Catalog -- --combo-box` 直接打开。
