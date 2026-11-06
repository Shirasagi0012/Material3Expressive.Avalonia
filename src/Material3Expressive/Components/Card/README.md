# Card

`Card`、`ElevatedCard`、`OutlinedCard` 分别提供填充、阴影和描边卡片，适合组织同一主题的内容。
三者默认只是内容容器；显式设置 `IsClickable="True"` 才让卡片自身成为按钮。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)；实现与移植边界见 [INTERNALS.md](INTERNALS.md)。

## 基础用法

下面不需要 ViewModel。卡片默认无 padding，用 `CardColumn.Margin` 为内容留白：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:Card>
    <m3:CardColumn Margin="16">
      <TextBlock Text="项目概览" m3:MaterialText.Style="{m3:Typography TitleMedium}" />
      <TextBlock Text="最近更新的内容" />
    </m3:CardColumn>
  </m3:Card>
</UserControl>
```

后续片段沿用相同命名空间。替换为 `ElevatedCard` 或 `OutlinedCard` 即可选择另外两种外观。

## 变体与属性

卡片可复用原生 Button 的 `Click`、`Command` 和 `CommandParameter`：

```xml
<m3:ElevatedCard IsClickable="True" Command="{Binding OpenCommand}"
                 CommandParameter="{Binding SelectedItem}">
  <m3:CardColumn Margin="16">
    <TextBlock Text="打开项目" />
  </m3:CardColumn>
</m3:ElevatedCard>
```

该片段假设 DataContext 提供命令和参数，编译绑定视图还需对应的 `x:DataType`。
若用 `Click="OnCardClick"`，在视图 code-behind 中实现处理器。

普通模式不激活自身、不提供 Invoke automation，默认不可聚焦；内容里的原生按钮仍可交互，
`Command.CanExecute` 不影响普通卡片的启用状态。`IsEnabled="False"` 仍禁用整个内容树，
但普通模式不会选择可点击卡片的 disabled 颜色/高度。

可点击模式有至少 48 DIP 的默认交互布局分配，视觉 Surface 可以更小；这不是额外触屏命中扩展。
默认 Space 松开、Enter 按下时激活，使用原生 Button 键盘行为。
切到普通模式会取消卡片自身的按压与焦点，不应将模式切换当成一次点击。

### 共享交互源

可点击模式的 `InteractionSource` 接收已有共享事件流；null 使用私有 source。普通模式忽略此属性。
外部拖动可驱动阴影与状态层，不会触发 Click、移动卡片或改变选中状态：

```csharp
using Material3Expressive.Foundation.Interaction;

var source = new MaterialInteractionSource();
card.IsClickable = true;
card.InteractionSource = source;
var drag = new MaterialInteractionEvent.DragStart();
source.Emit(drag);
// 宿主完成拖动；取消时使用 DragCancel(drag)。
source.Emit(new MaterialInteractionEvent.DragStop(drag));
```

原生指针/键盘也发到同一个流。高度使用最后存活的交互，重叠事件按开始对象身份配对。
更换源前应配对结束外部交互；尚未结束的外部交互会继续影响高度。切成普通模式或卸载时清理这些状态。
Catalog **Drag handle demo** 把宿主拖动同时传给 handle 和 ElevatedCard。

## 内容槽与定制

`Content` / `ContentTemplate` 接收任意内容。`CardColumn` 从上到下、Start 对齐、零间距，
按顺序把剩余高度交给子项；高度耗尽后后续子项收到零高度约束。
默认 Stretch 在此按 Start 放置，显式 HorizontalAlignment 可指定其他对齐，RTL 跟随布局方向。
需要权重或复杂对齐时，可在内容槽组合其他布局容器。
卡片 `Padding` 或内容 `Margin` 都可用于留白。

正常态与禁用态分别使用 `ContainerColor` / `ContentColor`、`DisabledContainerColor` /
`DisabledContentColor`。容器色为 Color，内容色为 IBrush，保留环境非纯色画刷。
这些都是独立 Avalonia 属性，支持 Style、Binding、局部值和 ClearValue。
`Elevation` 是完整、不可变的六状态配置；设置一个新配置会整体替换，省略成员就是 0，
不会逐字段从主题补值。清除 Elevation 的局部值恢复当前主题配置：

```xml
<m3:OutlinedCard IsClickable="True" IsEnabled="False"
                 ContainerColor="#E8DEF8" ContentColor="#1D192B"
                 DisabledContentColor="#79747E">
  <m3:OutlinedCard.Elevation>
    <m3:CardElevation Default="0" Hovered="2" Pressed="0"
                      Focused="0" Dragged="3" Disabled="0" />
  </m3:OutlinedCard.Elevation>
  <TextBlock Text="暂不可用" Margin="16" />
</m3:OutlinedCard>
```

原生 `Background` / `Foreground` 是当前状态的最终画刷；它们以及 `BorderBrush` /
`BorderThickness` / `CornerRadius` 的普通用户 Style、显式值或绑定优先于主题默认值。未设置内容色时，默认从启用容器颜色推导匹配的 On 色，
无法匹配主题角色时继承外层内容色。统一定制见[公共 Token 接入](../../../../docs/GETTING_STARTED.md#主题与定制)。

普通卡片会记住初次挂载的 Default elevation；后来替换 Elevation 或默认 Token 不会更新此高度，
切换模式或重新挂载才重建，单独替换模板不重置。可点击卡片跟随最后一个活动交互更新高度，
禁用时直接切到目标值。这里控制的是阴影高度，不是 tonal elevation。
可点击卡片支持运行时替换整组高度配置，更新按显示帧合并；Catalog **Drag handle demo**
提供配置切换和启用开关，并与 InputChip 共享拖动交互。

## 使用限制与注意事项

形状配置使用四角 CornerRadius。可在条目或祖先设置 `MaterialInteraction.FocusFeedback="InsetRing"`。
Catalog **Inset focus ring** 提供三种可点击卡片的键盘焦点示例；普通卡片不会显示自身焦点环。
普通卡片记住首次挂载的默认阴影高度，模式切换或重挂后重新读取。
当前已验证的半透明颜色组合及其他一致性边界见维护说明。
[Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml)展示三种外观，Drag handle demo 展示共享拖动反馈。

内部机制、移植差异与验证范围见 [INTERNALS.md](INTERNALS.md)。
