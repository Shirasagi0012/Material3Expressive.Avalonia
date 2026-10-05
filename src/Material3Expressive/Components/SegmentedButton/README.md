# SegmentedButton

分段按钮适合在少量同级选项间选择。库提供 `SingleChoiceSegmentedButtonRow`、`MultiChoiceSegmentedButtonRow`
和共享 `SegmentedButton`；行负责布局，应用负责选中状态。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)，实现依据见 [INTERNALS](INTERNALS.md)。

## 最小示例

在已加载 Material 主题的应用中，以下展示单选行的初始状态：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:SingleChoiceSegmentedButtonRow>
    <m3:SegmentedButton Content="日" IsSelected="True" />
    <m3:SegmentedButton Content="周" />
    <m3:SegmentedButton Content="月" />
  </m3:SingleChoiceSegmentedButtonRow>
</UserControl>
```

点击不会自动改 `IsSelected`。要交互切换，连接命令或事件并由应用写回；后续片段沿用上述命名空间。

## 选择职责与数据绑定

```xml
<m3:SingleChoiceSegmentedButtonRow>
  <m3:SegmentedButton Content="日" IsSelected="{Binding IsDay}"
                      Command="{Binding SelectDayCommand}" />
  <m3:SegmentedButton Content="周" IsSelected="{Binding IsWeek}"
                      Command="{Binding SelectWeekCommand}" />
</m3:SingleChoiceSegmentedButtonRow>
```

假设 `DataContext` 提供对应选择属性和 `ICommand`，由命令设置互斥状态；启用编译绑定需 `x:DataType`。
单选行不会清除其他按钮选择，多选行也不会替应用翻转值。事件方式例如 `Click="OnSegmentClick"`，
需在视图 code-behind 实现写回逻辑。[Catalog 处理器](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)
包含单选和多选做法。

两种行均为 `ItemsControl`，支持直接 `Items` 或 `ItemsSource`/`ItemTemplate`；模板根应生成 SegmentedButton。
以下片段假设 `Filters` 集合中的每项有 `Label`、`IsSelected` 和 `ToggleCommand`，显式使用运行时绑定：

```xml
<m3:MultiChoiceSegmentedButtonRow ItemsSource="{Binding Filters}" x:CompileBindings="False">
  <m3:MultiChoiceSegmentedButtonRow.ItemTemplate>
    <DataTemplate>
      <m3:SegmentedButton Content="{Binding Label}" IsSelected="{Binding IsSelected}"
                          Command="{Binding ToggleCommand}" />
    </DataTemplate>
  </m3:MultiChoiceSegmentedButtonRow.ItemTemplate>
</m3:MultiChoiceSegmentedButtonRow>
```

单选行提供 Selection group，按钮提供 RadioButton/SelectionItem；多选按钮提供 CheckBox/Toggle。
辅助技术 Select/Toggle 同样只请求应用动作。`Space` 和 Enter 均在释放时激活，失焦或禁用取消待激活按键。

## 外观、尺寸与图标

默认容器高 40、交互留白至少 48、按钮背景最小宽 58 DIP。
行以最大 intrinsic 项宽等权分配，默认 `Space=1` 表示相邻描边的重叠量，不是正向间隔。
首尾保留 `BaseShape` 外侧圆角，中间为矩形，单项使用完整形状；RTL 镜像首尾。
按钮的普通用户 Style、局部值或绑定设置的 `CornerRadius` 均优先于行生成值。

```xml
<m3:SingleChoiceSegmentedButtonRow Space="2" BaseShape="8">
  <m3:SegmentedButton Content="列表" BorderThickness="2" IsSelected="True" />
  <m3:SegmentedButton Content="网格" BorderThickness="2" />
</m3:SingleChoiceSegmentedButtonRow>
```

`Space` 应与描边宽度协调；行不自动修改按钮 `BorderThickness`。
`Content`/`ContentTemplate` 是 LabelLarge 标签槽，默认 `Padding` 为水平 12/垂直 8 DIP。
`ActiveIcon` 与 `ActiveIconTemplate` 均为空时使用默认 Check 向量；
`InactiveIcon` 与 `InactiveIconTemplate` 均为空时不显示未选中图标。
`ShowSelectionIcon=false` 关闭图标槽，但标签仍按上游的图标预留与居中规则布局。
只设置图标模板也会占用对应槽位，即使图标内容为 null；模板可以针对 null 数据生成内容。
无显式尺寸的 MaterialSymbol 默认使用 SegmentedButton IconSize Token 18 DIP；显式 `Width` / `Height` 优先，
自定义图标仍由自身内容和布局决定尺寸。

```xml
<m3:SegmentedButton Content="列表" IsSelected="True">
  <m3:SegmentedButton.InactiveIcon>
    <m3:MaterialSymbol Symbol="List" />
  </m3:SegmentedButton.InactiveIcon>
  <m3:SegmentedButton.Resources>
    <Color x:Key="{x:Static m3:MaterialTokens+Comp+SegmentedButton.ActiveContainerColor}">#126E60</Color>
    <Color x:Key="{x:Static m3:MaterialTokens+Comp+SegmentedButton.ActiveContentColor}">#FFFFFF</Color>
  </m3:SegmentedButton.Resources>
</m3:SegmentedButton>
```

该按钮通常放在行内使用。不要在两个图标槽共享同一个 Control 实例；复用表示时用数据和模板。
提供 `InactiveIcon` 或 `InactiveIconTemplate` 时切换使用 Crossfade，否则选择图标使用 fade/scale 进入动画。

## 定制与边界

`Colors` / `SegmentedButtonColors` 已删除。当前颜色用原生 `Background`、`Foreground`、`BorderBrush`，
每个状态的默认值用 `Comp.SegmentedButton` Token 或自己的状态 Style 定制。
十二种默认槽仍分别可覆盖，例如 `ActiveContainerColor` / `InactiveContainerColor`、
`DisabledActiveContainerColor` / `DisabledInactiveContainerColor`，相应 ContentColor 和 BorderColor 同理。
禁用 active 背景默认引用 ActiveContainerColor，所以修改上面的选中背景也会更新禁用选中背景；
如需分开配置，另覆盖 DisabledActiveContainerColor。默认禁用内容为 OnSurface×0.38，描边为 Outline×0.12。

原生颜色、`BorderThickness`、`Padding`、`CornerRadius` 都接受普通用户 Style、局部值和绑定；
主题的选中/禁用条件不会强制覆盖它们。下面的 Style 可放入视图的 `UserControl.Styles`：

```xml
<Style Selector="m3|SegmentedButton:selected">
  <Setter Property="Background" Value="#126E60" />
  <Setter Property="Foreground" Value="White" />
</Style>
```

该选择器也匹配禁用且选中的按钮；需要另一种禁用外观时再添加 `:disabled:selected` 样式。
Surface 反馈墨色跟随最终 Foreground。清除局部值后恢复剩余用户样式或当前状态、资源的主题默认值；
清除局部 CornerRadius 也会重新采用当前行位置/RTL 生成的圆角，除非仍有用户 Style 覆盖。
默认图标间距和动效可分别通过 `IconSpacing`、`IconEffectsAnimation`、`IconSpatialAnimation`、
`LabelOffsetAnimation` Token 配置；无需替换整个模板。

`BaseShape`/`CornerRadius` 只表达圆角矩形，不提供任意路径/切角 `Shape` 或外部 InteractionSource。
文字和图标高度会参与测量，自定义内容可能改变默认尺寸；真实文字 raster、系统屏幕阅读器和桌面体验
未由无字形双端场景证明。示例见 [Catalog MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)。
