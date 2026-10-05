# TextField

在原生 Avalonia `TextBox` 上提供 Material Filled / Outlined 外观、浮动标签和内容槽。
文本、选区、撤销重做和输入法仍由 `TextBox` 管理，无需改用新的编辑控件。

先完成[公共接入](../../../../docs/GETTING_STARTED.md)，包括主题、字体和窗口动画作用域。
实现与移植依据见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

以下内容放在已初始化的窗口或视图中；后续片段沿用同一命名空间。

```xml
<StackPanel xmlns="https://github.com/avaloniaui"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
            Spacing="16">
    <TextBox m3:MaterialTextField.Label="姓名"
             PlaceholderText="请输入姓名" />
    <TextBox Theme="{StaticResource MaterialOutlinedTextFieldTheme}"
             m3:MaterialTextField.Label="金额"
             m3:MaterialTextField.Prefix="¥"
             m3:MaterialTextField.Suffix="CNY"
             Text="128.00" />
</StackPanel>
```

未指定 `Theme` 的 `TextBox` 使用 Filled。显式选择主题时：

| 外观 | Theme 资源 | 默认标签位置 |
| --- | --- | --- |
| Filled | `MaterialFilledTextFieldTheme` | `Inside`，容器内 |
| Outlined | `MaterialOutlinedTextFieldTheme` | `Cutout`，描边缺口 |

## 文本、状态与事件

沿用原生 `Text`、`TextChanged`、`IsReadOnly` 和 `IsEnabled`。绑定示例假设视图已有
包含 `Name`、`HasError`、`NameHint` 的 DataContext；启用编译绑定的项目还需声明对应 `x:DataType`。

```xml
<TextBox Text="{Binding Name, Mode=TwoWay}"
         m3:MaterialTextField.Label="姓名"
         m3:MaterialTextField.IsError="{Binding HasError}"
         m3:MaterialTextField.SupportingText="{Binding NameHint}"
         m3:MaterialTextField.ErrorMessage="姓名不能为空" />

<TextBox Text="可选择并复制" IsReadOnly="True"
         m3:MaterialTextField.Label="只读" />
<TextBox Text="暂时不可编辑" IsEnabled="False"
         m3:MaterialTextField.Label="禁用" />
```

- `IsError` 或 Avalonia 绑定验证的 `:error` 都会触发错误外观；状态颜色优先级为禁用、错误、聚焦、普通
- `ErrorMessage` 提供错误状态的无障碍 HelpText，不会自动变成可见的错误文字；需要显示时同时设置 `SupportingText`
- `IsReadOnly` 仍允许原生选择与复制；`IsEnabled="False"` 禁用整个输入框及其内容
- 字符串 `Label` 自动提供无障碍名称，字符串 `SupportingText` 提供普通 HelpText；应用显式设置的 `AutomationProperties.Name` / `HelpText` 优先

## 标签、内容槽与多行

两种外观均支持 `LabelPosition="Inside|Cutout|Above"`。`AlwaysMinimizeLabel="True"`
让标签始终保持最小化；`ExpandedLabelAlignment` / `MinimizedLabelAlignment` 分别控制两种状态的
`Start` / `Center` / `End` 对齐，Start / End 跟随 `FlowDirection`。

```xml
<TextBox Theme="{StaticResource MaterialOutlinedTextFieldTheme}"
         m3:MaterialTextField.Label="说明"
         m3:MaterialTextField.LabelPosition="Above"
         m3:MaterialTextField.MinimizedLabelAlignment="Start"
         m3:MaterialTextField.SupportingText="最多显示五行，更多内容在编辑区滚动"
         AcceptsReturn="True" TextWrapping="Wrap"
         MinLines="3" MaxLines="5" />
```

`Label`、`Placeholder`、`SupportingText`、`Prefix`、`Suffix` 接受任意内容及对应的
`*Template`。附加 `Placeholder` 非 null 时优先于原生 `PlaceholderText`。
复杂标签内容请显式设置 `AutomationProperties.Name`。

前后图标使用 `MaterialTextField.LeadingIcon` / `TrailingIcon`，在 Material 布局中分别是逻辑
leading / trailing，会跟随 RTL。图标默认尺寸为 24 DIP，两个槽仍保留各自的 48 DIP 交互空间。
交互内容可使用 `IconButton`，命令与可聚焦性由应用决定：

```xml
<TextBox m3:MaterialTextField.Label="搜索" PlaceholderText="输入关键词"
         m3:MaterialTextField.LeadingIcon="{m3:Symbol Search}">
  <m3:MaterialTextField.TrailingIcon>
    <m3:IconButton Symbol="Close" Command="{Binding ClearSearchCommand}"
                   AutomationProperties.Name="清空搜索" />
  </m3:MaterialTextField.TrailingIcon>
</TextBox>
```

`LeadingIconTemplate` / `TrailingIconTemplate` 可按图标数据创建内容。显式模板即使图标值为 null
也会显示模板生成的图形；例如，模板本身可绘制固定的矢量图标：

```xml
<StackPanel xmlns="https://github.com/avaloniaui"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel.Resources>
    <DataTemplate x:Key="CustomGlyphTemplate">
      <Path Width="24" Height="24" Data="M 3,3 L 21,21"
            Stroke="{Binding (TextElement.Foreground), RelativeSource={RelativeSource Self}}"
            StrokeThickness="2" />
    </DataTemplate>
  </StackPanel.Resources>
  <TextBox m3:MaterialTextField.LeadingIcon="{x:Null}"
           m3:MaterialTextField.LeadingIconTemplate="{StaticResource CustomGlyphTemplate}" />
</StackPanel>
```

`DataTemplate` 可使用 `Path` 等绘制自定义图形，
并从内容环境继承前景色。`MaterialSymbol` 图标可省略宽高并使用 `{m3:Symbol Search}`。
为兼容原生入口，当对应槽的 `Icon` 和 `IconTemplate` 都为 null 时，会继续使用 `TextBox.InnerLeftContent` /
`InnerRightContent`；任一非 null 时使用 Material 图标槽。此例假设 DataContext 提供 `ClearSearchCommand`，
且命令清空相应 `Text` 绑定。

完整事件处理示例见 [Catalog](../../../Material3Expressive.Catalog/TextFieldDemo.axaml)。
单独放入图标不会增加 SearchBar 的展开、建议列表或 IME 搜索动作流程。

## 尺寸与定制

- 默认最小尺寸计算以宽 280、高 56 DIP 为起点，并遵守父级约束；Above 标签、辅助文字、多行及 Cutout 顶部留白会影响总高度，不应把 56 当作固定高度
- 原生 `Width` / `Height` / `MinWidth` / `MinHeight` 可约束布局；默认 Padding 为 16 DIP，Inside 且有标签时上下为 8 DIP，显式 `Padding` 优先
- `CornerRadius`、`Background`、`Foreground`、`BorderBrush`、`CaretBrush` 与 `SelectionBrush` 可通过普通样式、绑定或局部值覆盖；此处 `BorderBrush` 是指示线/描边颜色，线宽由组件的焦点状态控制
- 共享软最小尺寸、Padding 与运动默认值位于 `MaterialTokens.Comp.TextField`；清除覆盖后恢复当前内容/状态下的主题默认值
- 系统颜色和排版定制从 [Token 与排版入口](../../../../docs/GETTING_STARTED.md#主题与定制)开始；组件颜色槽为 `MaterialTokens.Comp.FilledTextField` / `OutlinedTextField`
- 标签动画分别求 BodyLarge / BodySmall 的有效文字属性后插值；稀疏样式的空字段遵循现有排版环境和原生属性优先级，不再补固定标签字号。例如 BodySmall 仅设 FontWeight 时，浮起标签未必缩小；需要固定尺寸时显式提供 FontSize / LineHeight
- 内容默认继承槽位前景色，内容自身的显式局部值仍优先；显式设置固定颜色时，也需考虑禁用、错误与明暗外观

## 使用边界

- 替换整个 `Theme` 会重新应用原生模板，保留文本和选区，但丢失撤销历史；普通明暗资源更新不会替换编辑器
- 辅助文字属于 `TextBox` 模板，点击它可能聚焦编辑器并移动光标
- 标签位置保留弹簧过冲，字号、测量和缺口尺寸的插值限制在端点范围内；内容变化仍可改变高度，并不保证任意自定义内容动画时恒高
- 原生编辑行度量、换行与光标定位遵循 Avalonia/平台语义，自动高度可能与 Compose 不同
- 标签 LineHeight 为 NaN 时使用原生自动行高，与固定行高的切换在动画中点离散发生；布局预留单独采用 BodySmall.LineHeight 标量，不将它当作自动文字的实际度量
- 密码输入请阅读 [SecureTextField](../SecureTextField/README.md) 的安全边界；仅设置 `PasswordChar` / `RevealPassword` 不会启用其安全策略
- 没有 Compose `TextFieldState`、输入/输出转换或完整 semantics 树；真实 Windows IME、系统剪贴板、触控及屏幕阅读器的验收范围见 [维护说明](INTERNALS.md#尚未实现与未经验证)

查看运行示例：`dotnet run --project src/Material3Expressive.Catalog -- --text-fields`。
