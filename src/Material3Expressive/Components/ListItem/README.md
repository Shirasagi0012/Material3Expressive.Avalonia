# ListItem / SegmentedListItem

两个库内控件提供普通列表行和带分组圆角的列表行。
先按[公共接入](../../../../docs/GETTING_STARTED.md)安装 MaterialTheme、字体和 TopLevel 动画作用域。

## 基础用法

```xml
<StackPanel xmlns="https://github.com/avaloniaui"
            xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:ListItem Content="账户" OverlineContent="个人设置" SupportingContent="管理个人资料"
               LeadingContent="{m3:Symbol Person}" />
</StackPanel>
```

## 变体与属性

默认 `InteractionMode="None"` 仅展示内容。`Click` 提供 Click / Command；`SingleSelection` 额外读取 IsSelected；
`MultipleSelection` 读取 IsChecked，激活时发出 CheckedChangeRequested（Value 为请求的新值）并执行 Command，不发出行自己的 Click。
两个选择模式均不自动写回状态；单选互斥由调用方维护。原生 Button 的 Click 是路由事件，监听整行时应区分 `e.Source`，避免把嵌套按钮的冒泡事件当成整行激活。

以下片段假设 DataContext 提供 Checked 属性，代码后置持有对应 ViewModel：

```xml
<m3:ListItem Content="同步" InteractionMode="MultipleSelection"
             IsChecked="{Binding Checked, Mode=OneWay}" CheckedChangeRequested="OnCheckedChange" />
```

```csharp
void OnCheckedChange(object? sender, ListItemCheckedChangeEventArgs e)
    => ViewModel.Checked = e.Value;
```

`IsInteractionEnabled="False"` 用于禁用整行交互、切换禁用颜色和 automation 状态，槽内按钮仍可用。
`IsEnabled="False"` 则沿用 Avalonia 禁用整个子树的行为。Command.CanExecute 阻止该命令激活，不负责禁用子树或改变整行颜色。
Space / Enter 在释放时激活；鼠标左键和键盘支持 500ms 长按，通过 LongClick / LongClickCommand 接收，长按后不再触发该次短按。
LongClickCommandParameter 与 LongClickLabel 分别提供参数和说明。

LongClickLabel 作为辅助技术说明。需要辅助技术直接调用时，组合一个同命令按钮：

```xml
<m3:ListItem Content="文件" InteractionMode="Click" LongClickCommand="{Binding MoreCommand}" LongClickLabel="更多操作">
  <m3:ListItem.TrailingContent><Button Content="更多操作" Command="{Binding MoreCommand}" /></m3:ListItem.TrailingContent>
</m3:ListItem>
```

## 内容槽与定制

五个槽为 Content、LeadingContent、TrailingContent、OverlineContent、SupportingContent，各有同名前缀的 ContentTemplate（主槽为 ContentTemplate）。
槽可使用对象、控件或 IDataTemplate；文字继承对应排版和颜色。默认一/二/三行高度为 56/72/88 DIP，过大内容可以继续撑高。
普通 supporting TextBlock 的多行会选择三行高度；复杂模板不保证自动合并多个子元素的首末基线。
`ItemVerticalAlignment` 支持 Auto / Top / Center / Bottom；Auto 根据内部内容区域高度选择居中或顶部。Padding 使用逻辑起止边，随 RTL 镜像。

SegmentedListItem 通过 GroupIndex（从 0 起）和 GroupCount 决定默认首尾圆角；调用方须维护有效索引，并在插入、删除或排序后更新它们。
间距由容器提供：

```xml
<StackPanel Spacing="{m3:Token {x:Static m3:MaterialTokens+Comp+ListItem.SegmentedGap}}">
  <m3:SegmentedListItem GroupIndex="0" GroupCount="2" Content="第一项" />
  <m3:SegmentedListItem GroupIndex="1" GroupCount="2" Content="第二项" />
</StackPanel>
```

颜色、排版、尺寸、动画和默认圆角读取 `MaterialTokens.Comp.ListItem`。实例可覆盖 ContainerColor、ContentColor 及各槽/状态颜色、Elevation、DraggedElevation。
六状态圆角通过 Shapes 设置；显式 Shapes 取代分组默认形状。CornerRadius 使用 Avalonia 的物理角顺序；默认分组形状左右对称。

```xml
<m3:ListItem Content="自定义" InteractionMode="Click">
  <m3:ListItem.Shapes>
    <m3:ListItemShapes Shape="4" SelectedShape="16" PressedShape="24"
                      FocusedShape="16" HoveredShape="12" DraggedShape="16" />
  </m3:ListItem.Shapes>
</m3:ListItem>
```

外部拖拽可向 InteractionSource 发出配对事件；该源仅报告交互，不自动移动或重排列表项，也不改变选择值：

```csharp
using Material3Expressive.Foundation.Interaction;

var source = new MaterialInteractionSource();
item.InteractionSource = source;
var drag = new MaterialInteractionEvent.DragStart();
source.Emit(drag); // UI 线程
// 拖拽结束：取消时改用 DragCancel(drag)
source.Emit(new MaterialInteractionEvent.DragStop(drag));
```

前后槽提供缩小后的最小交互布局尺寸，默认 20 DIP；CheckBox / RadioButton 仍保留自身 24 DIP 绘制盒。
`MaterialInteractiveSize.Minimum` 可在祖先上调整这个继承环境，它不会创建额外触摸命中区。

默认使用 opacity 焦点反馈。可在条目或祖先上设置 `m3:MaterialInteraction.FocusFeedback="InsetRing"`，
内缩焦点环跟随当前形变轮廓；普通和分组条目使用同一入口：

```xml
<m3:ListItem Content="键盘焦点" InteractionMode="Click"
             m3:MaterialInteraction.FocusFeedback="InsetRing"
             m3:MaterialInteraction.FocusRingOuterColor="#006A6A"
             m3:MaterialInteraction.FocusRingInnerColor="#FFFFFF" />
```

可用 `MaterialInteraction.FocusRingMetrics` 调整描边和内缩，详见[交互反馈](../../Foundation/Interaction/README.md)。
切换 Opacity / InsetRing 保留正在进行的波纹和交互收集。

`Shape`、`SelectedShape`、`PressedShape`、`FocusedShape`、`HoveredShape`、`DraggedShape` 可分别覆盖六状态轮廓，
支持 `CutCornerShape` 和自定义 `MaterialShape`；未设置的状态使用 `Shapes` 中对应圆角。
切角采用逻辑起止角，随 RTL 调整，百分比随容器实际尺寸重算。例：

```xml
<m3:ListItem Content="切角列表项" InteractionMode="Click">
  <m3:ListItem.Shape><m3:CutCornerShape TopStart="20" BottomEnd="20" /></m3:ListItem.Shape>
  <m3:ListItem.PressedShape><m3:CutCornerShape TopStart="10%" BottomEnd="10%" /></m3:ListItem.PressedShape>
  <m3:ListItem.HoveredShape><m3:CutCornerShape TopStart="8" BottomEnd="8" /></m3:ListItem.HoveredShape>
</m3:ListItem>
```

全部状态使用圆角或切角时，同族角尺寸连续形变并支持反向及第三目标；跨圆角/切角切换在动画中点发生。
任一状态配置任意路径时，六状态整体直接切换轮廓。形状配置改变会重建形状动画，颜色和高度仍保持各自的过渡。
优先级为按下、拖动、选中、焦点、悬停、正常。显式 Shape 不受分组端点圆角控制。

## 使用限制与注意事项

复杂 SupportingContent 模板需要自行核对多行高度。分组的索引、总数与间距由外部容器维护。
最小交互留白仅影响布局。需要辅助技术调用长按命令时，可提供同命令的槽内按钮。
[Catalog 示例](../../../Material3Expressive.Catalog/ListItemDemo.axaml)包含普通、分组、选择与焦点反馈。

内部机制、移植差异与验证范围见 [INTERNALS.md](INTERNALS.md)。

形状配置也接受 `RoundedCornerShape`：逻辑四角可独立设置 DIP 或百分比（如 `TopStart="50%"`）。
它可与 `CutCornerShape` 组合为不同状态；同族连续插值，圆/切角混合时按上游在动画中点切换。
清除相应 Shape 属性后恢复原圆角配置。完整形状用法见 [Surface](../Surface/README.md)。
