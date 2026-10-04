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

点击不会自动改IsSelected。要交互切换，连接命令或事件并由应用写回；后续片段沿用上述命名空间。

## 选择职责与数据绑定

```xml
<m3:SingleChoiceSegmentedButtonRow>
  <m3:SegmentedButton Content="日" IsSelected="{Binding IsDay}"
                      Command="{Binding SelectDayCommand}" />
  <m3:SegmentedButton Content="周" IsSelected="{Binding IsWeek}"
                      Command="{Binding SelectWeekCommand}" />
</m3:SingleChoiceSegmentedButtonRow>
```

假设DataContext提供对应选择属性和ICommand，由命令设置互斥状态；启用编译绑定需x:DataType。
单选行不会清除其他按钮选择，多选行也不会替应用翻转值。事件方式例如Click="OnSegmentClick"，
需在视图code-behind实现写回逻辑。[Catalog处理器](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)
包含单选和多选做法。

两种行均为ItemsControl，支持直接Items或ItemsSource/ItemTemplate；模板根应生成SegmentedButton。
以下片段假设Filters集合中的每项有Label、IsSelected和ToggleCommand，显式使用运行时绑定：

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

单选行提供Selection group，按钮提供RadioButton/SelectionItem；多选按钮提供CheckBox/Toggle。
辅助技术Select/Toggle同样只请求应用动作。Space和Enter均在释放时激活，失焦或禁用取消待激活按键。

## 外观、尺寸与图标

默认容器高40、交互留白至少48、按钮背景最小宽58 DIP。
行以最大intrinsic项宽等权分配，默认Space=1表示相邻描边的重叠量，不是正向间隔。
首尾保留BaseShape外侧圆角，中间为矩形，单项使用完整形状；RTL镜像首尾。
按钮本地CornerRadius优先于行生成值。

```xml
<m3:SingleChoiceSegmentedButtonRow Space="2" BaseShape="8">
  <m3:SegmentedButton Content="列表" BorderThickness="2" IsSelected="True" />
  <m3:SegmentedButton Content="网格" BorderThickness="2" />
</m3:SingleChoiceSegmentedButtonRow>
```

Space应与描边宽度协调；行不自动修改按钮BorderThickness。
Content/ContentTemplate是LabelLarge标签槽，默认Padding为水平12/垂直8 DIP。
ActiveIcon为空时使用默认Check向量；InactiveIcon为空时不显示未选中图标，二者各有对应Template。
ShowSelectionIcon=false关闭图标槽，但标签仍按上游的图标预留与居中规则布局。

```xml
<m3:SegmentedButton Content="列表" IsSelected="True">
  <m3:SegmentedButton.InactiveIcon>
    <m3:MaterialSymbol Symbol="List" Width="18" Height="18" />
  </m3:SegmentedButton.InactiveIcon>
  <m3:SegmentedButton.Colors>
    <m3:SegmentedButtonColors ActiveContainerColor="#126E60" ActiveContentColor="#FFFFFF" />
  </m3:SegmentedButton.Colors>
</m3:SegmentedButton>
```

该按钮通常放在行内使用。不要在两个图标槽共享同一个Control实例；复用表示时用数据和模板。
有InactiveIcon时切换使用Crossfade，否则默认选择图标使用fade/scale进入动画。

## 定制与边界

SegmentedButtonColors按active/inactive、enabled/disabled提供container/content/border十二个可空槽，
未指定槽跟随Token。默认禁用active背景保留SecondaryContainer，内容为OnSurface×0.38，描边Outline×0.12。
原生Background、Foreground、BorderBrush、BorderThickness、Padding、CornerRadius局部值/绑定仍有效，
Surface反馈墨色跟随实际内容色。

BaseShape/CornerRadius只表达圆角矩形，不提供任意路径/切角Shape或外部InteractionSource。
文字和图标高度会参与测量，自定义内容可能改变默认尺寸；真实文字raster、系统屏幕阅读器和桌面体验
未由无字形双端场景证明。示例见 [Catalog MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)。
