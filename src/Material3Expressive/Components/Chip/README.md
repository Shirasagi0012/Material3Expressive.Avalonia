# Chip

Chip 适合紧凑的操作、建议、筛选和已输入项目。库提供 `AssistChip`、`SuggestionChip`、`FilterChip`、
`InputChip` 四个 Button 派生控件；先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
实现映射与适配依据见 [INTERNALS](INTERNALS.md)。

## 基础用法

在已加载 Material 主题的应用中：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel Spacing="8">
    <m3:AssistChip Content="添加到日历" />
    <m3:SuggestionChip Content="明天" IsElevated="True" />
    <m3:FilterChip Content="未读" IsSelected="True" />
    <m3:InputChip Content="联系人" IsEnabled="False" />
  </StackPanel>
</UserControl>
```

后续片段使用相同命名空间。Assist/Suggestion/Filter 以 `IsElevated` 选择 Elevated 外观，
Input 没有此入口。默认容器高 32、交互布局最小高 48 DIP，标签使用 LabelLarge。
Assist/Suggestion 使用固定 8 DIP 圆角；Filter/Input 默认具有未选中、选中、按压三种形状，见下文。

## 变体与属性

Assist/Suggestion 用原生 `Click` 或 `Command` 发起动作。
Filter/Input 的 `IsSelected` 由应用管理，点击、命令或 automation Toggle 都只请求动作，不自动改选中状态：

```xml
<m3:FilterChip Content="未读" IsSelected="{Binding IsUnread}"
               Command="{Binding ToggleUnreadCommand}" />
```

假设 `DataContext` 提供 `IsUnread` 和负责更改它的 `ICommand ToggleUnreadCommand`；
编译绑定需声明对应 `x:DataType`。事件方式可使用 `Click="OnFilterClick"`，并由视图处理器写回 `IsSelected`。
仅绑定 `IsSelected` 而不处理动作不会自动切换。
Filter/Input 提供 CheckBox/Toggle 自动化，选中值变化会通知自动化客户端。
Enter 按下激活，`Space` 沿用原生 ClickMode；`IsEnabled="False"` 禁止输入并显示禁用状态。

### 共享交互源

`InteractionSource` 可注入共享 `MaterialInteractionSource`，null 使用私有流。原生鼠标/键盘向同一流
发送 Press/Release/Cancel、Hover 和 Focus；外部事件改变反馈，不自动 Click 或修改 IsSelected。

```csharp
using Material3Expressive.Foundation.Interaction;

var source = new MaterialInteractionSource();
chip.InteractionSource = source;
var drag = new MaterialInteractionEvent.DragStart();
source.Emit(drag);
// 宿主负责移动；取消时发 DragCancel(drag)。
source.Emit(new MaterialInteractionEvent.DragStop(drag));
```

重叠事件按开始对象身份配对。更换源前应结束外部交互，旧外部交互仍可能影响高度，按压外观在新源下一事件时更新。外部 `Press.Position` 是 Chip 本地 DIP 坐标，包含最小交互布局留白。
Catalog **Drag handle demo** 展示拖动源同时驱动 Card、Chip 和 Handle。
Catalog **Inset focus ring** 展示四类 Chip 的键盘焦点。给 Chip 或其祖先设置
`MaterialInteraction.FocusFeedback="InsetRing"` 可启用内缩焦点环；环跟随当前容器形状，不占标签布局空间。

## 内容槽与定制

`Content` / `ContentTemplate` 为标签，`LeadingIcon` / `TrailingIcon` 及对应 `Template` 为图标槽。
只设置图标模板也会占用槽位，即使对应内容为 null；模板可据此创建内容。Suggestion 不显示 trailing 槽。
Input 另有 `Avatar` / `AvatarTemplate`；Avatar 或 AvatarTemplate 存在时优先于 leading 图标，默认圆形裁剪，
禁用时仅头像包装层应用 0.38 透明度。

```xml
<m3:InputChip Content="联系人">
  <m3:InputChip.Avatar>
    <Border Width="24" Height="24" Background="#6750A4" />
  </m3:InputChip.Avatar>
  <m3:InputChip.TrailingIcon>
    <m3:MaterialSymbol Symbol="Close" Width="18" Height="18" />
  </m3:InputChip.TrailingIcon>
</m3:InputChip>
```

无显式尺寸的 MaterialSymbol 在普通图标槽默认使用 Chip IconSize Token 18 DIP，在头像槽默认使用独立的 24 DIP。
显式 `Width` / `Height` 优先；自定义图标和头像仍按自身布局，不会被槽强制设为该尺寸。
不要将同一 Control 实例放进多个槽。
Trailing 仍属于整个 Chip 的点击区域，没有独立移除命令或事件；删除与选择动作由应用决定。
Leading/trailing 先测量，标签使用剩余宽度；RTL 自动镜像布局。

### 形状与间距

四类 Chip（含 Elevated 外观）可用 `Shape` 设置固定切角或自定义 `MaterialShape` 路径：

```xml
<m3:FilterChip Content="未读" IsSelected="{Binding IsUnread}" Command="{Binding ToggleUnreadCommand}">
  <m3:FilterChip.Shape><m3:CutCornerShape TopStart="12" TopEnd="4" BottomEnd="12" BottomStart="4" /></m3:FilterChip.Shape>
</m3:FilterChip>
```

显式 `Shape` 优先于 `Shapes` 和 `CornerRadius`，选择/按压不改变这个固定轮廓；颜色、间距、
图标进入退出动画继续独立工作。清除 Shape 后恢复当前 Shapes 的状态形变，无需修改主题默认值。
百分比切角按当前容器短边计算，逻辑四角支持 RTL；边框须均匀厚度。路径契约见
[Surface](../Surface/README.md)。Catalog 主窗口 Chip 区提供四类实例。



Filter/Input 的主题默认将 `UnselectedShape` / `SelectedShape` / `PressedShape` Token 组装成完整
`ChipShapes`，分别使用 Medium/Full/Small 圆角，按压优先于选中。不需要额外 class。
只想用固定圆角时设置 `Shapes="{x:Null}"`，此时使用原生 `CornerRadius`：

```xml
<m3:FilterChip Content="固定圆角" Shapes="{x:Null}" CornerRadius="8" />
```

自定义三种形状时整体给定配置；省略字段为零圆角，不会逐项补入主题默认值：

```xml
<m3:FilterChip Content="收藏" IsSelected="True">
  <m3:FilterChip.Shapes>
    <m3:ChipShapes>
      <m3:ChipShapes.Shape><m3:CutCornerShape TopStart="12" BottomEnd="12" /></m3:ChipShapes.Shape>
      <m3:ChipShapes.SelectedShape><m3:CutCornerShape TopStart="50%" BottomEnd="50%" /></m3:ChipShapes.SelectedShape>
      <m3:ChipShapes.PressedShape><m3:CutCornerShape TopStart="4" BottomEnd="4" /></m3:ChipShapes.PressedShape>
    </m3:ChipShapes>
  </m3:FilterChip.Shapes>
  <m3:FilterChip.LeadingIcon>
    <m3:MaterialSymbol Symbol="Check" />
  </m3:FilterChip.LeadingIcon>
</m3:FilterChip>
```

三个形状均为 `RoundedCornerShape` / `CutCornerShape` 时使用弹簧：同族连续插值，圆/切角混合时
按动画进度中点切换形状族；百分比随容器尺寸重新求值。任一状态为自定义 `MaterialShape` 时，
整组禁用插值，按当前状态直接切换路径。动态修改角参数会重置该组动画到当前状态，值相同的新配置
保留正在运行的动画。Catalog 主窗口 **State shape chips** 可交互查看。

迁移：`ChipShapes.Shape` / `SelectedShape` / `PressedShape` 现为 `MaterialShape`。
旧 XAML 的 `Shape="12"` 改为上例的形状子元素（需要圆角时使用 `RoundedCornerShape`）；
C# 的 `new ChipShapes(new CornerRadius(12), new CornerRadius(20), new CornerRadius(8))` 继续可用，
其旧 Full=1000 转为 50%。读取属性时按实际形状类型访问逻辑角。也可直接用
`new ChipShapes(new CutCornerShape(12), new RoundedCornerShape(8))`，两参数构造令按压态复用选中态。

`Shapes` 只配置容器形变。设置、更换或清除它不会同时切换间距、颜色或图标进出动效；
Filter/Input 的图标槽在 `Shapes=null` 时也保留进入/退出动画。
图标动效可独立覆盖 `Comp.Chip.IconSizeEnterAnimation` / `IconSizeExitAnimation` 和
`IconAlphaEnterAnimation` / `IconAlphaExitAnimation` Token；形状变化本身使用 `Comp.Chip.ShapeAnimation`。
`HorizontalArrangement` 可选 `Start`、`End`、`Center`、`SpaceBetween`、`SpaceAround`、`SpaceEvenly`，
用于显式宽度或父级分配剩余空间时安排三槽。例如：

```xml
<m3:FilterChip Width="180" Content="均匀分布" HorizontalArrangement="SpaceEvenly"
               LeadingIcon="{m3:Symbol Check}" TrailingIcon="{m3:Symbol Close}" />
```

这些排列的间距为零，覆盖专用 `Spacing`；清除为 null 后恢复最新的 `Spacing`。空图标槽仍占一个
排列位置，与上游三个始终存在的子项一致；因此 SpaceAround/SpaceEvenly 可能在空槽旁留下空隙。
方向为逻辑 Start/End，RTL 使用上游物理像素舍入顺序。图标进入/退出时按当前动画尺寸重新布局。
Assist/Suggestion 的标签槽填满剩余宽度，通常没有可供这些排列分配的空隙；Filter/Input 的标签槽按内容宽度布局。
排列还支持固定间距、绝对方向和自定义算法：

```xml
<m3:FilterChip Width="200" Content="固定间距">
  <m3:FilterChip.HorizontalArrangement>
    <m3:SpacedHorizontalArrangement Space="8" Alignment="Right" />
  </m3:FilterChip.HorizontalArrangement>
</m3:FilterChip>
<m3:InputChip Width="200" Content="保持物理顺序" HorizontalArrangement="Absolute.SpaceEvenly" />
```

`Space` 允许负值（内容重叠）；`Alignment` 的 Left / Center / Right 表示逻辑起点 / 居中 / 终点。
`Mirror="False"` 只固定子项物理顺序，组对齐仍按 RTL 解析，和 `Absolute.SpacedBy` 一致。
`Absolute.Left/Right/Center/SpaceBetween/SpaceAround/SpaceEvenly` 同时固定顺序与位置规则。
Chip 始终保留三个槽，空图标槽也参与间距。自定义排列通过派生不可变的 `MaterialHorizontalArrangement`
实现 `Spacing` 和 `Arrange`，后者接收整数物理像素与密度，输出各槽相对物理左侧的位置。

C# 原 `ChipHorizontalArrangement.Center` 改为 `MaterialHorizontalArrangement.Center`，命名空间为
`Material3Expressive.Foundation`；原六种 XAML 字符串继续有效。固定间距可使用
`MaterialHorizontalArrangement.SpacedBy(8, HorizontalAlignment.Right)`。属性现接受排列对象，
变更配置应替换对象；null 恢复原专用 `ChipSpacing` 及其选中动画。

`Spacing` 是完整的 `ChipSpacing` 值，逻辑前/后间距默认均为 8 DIP，独立定制例如：

```xml
<m3:InputChip Content="联系人">
  <m3:InputChip.Spacing>
    <m3:ChipSpacing Leading="4" Trailing="8" />
  </m3:InputChip.Spacing>
</m3:InputChip>
```

Input 的 `Padding` 仍随头像/图标组合选择默认值。普通用户 Style、局部值和绑定可覆盖 `Padding`、
`Spacing` 及其他公开配置；清除局部值后恢复剩余样式或当前资源、内容条件下的主题默认值。
例如 `chip.ClearValue(Chip.SpacingProperty)`（`chip` 为实例，`Chip` 位于 `Material3Expressive.Components`）
恢复实时默认间距；`filterChip.ClearValue(SelectableChip.ShapesProperty)` 恢复主题三形状配置。

### 颜色与高度



当前颜色通过原生 `Background` / `Foreground` / `BorderBrush` 定制，两个图标分别使用
`LeadingIconColor` / `TrailingIconColor`（类型均为 `IBrush?`）。图标与标签不是同一个前景槽。
普通 Style、局部值或绑定优先于选中/禁用等主题默认；设置固定颜色后，如需状态差异应自行写状态样式。
例如把下面的 Style 放入视图的 `UserControl.Styles`，沿用前述命名空间：

```xml
<Style Selector="m3|FilterChip:selected">
  <Setter Property="Background" Value="#E8DEF8" />
  <Setter Property="Foreground" Value="#211A2E" />
  <Setter Property="LeadingIconColor" Value="#211A2E" />
</Style>
```

这个样式也匹配禁用且选中的 Chip；如需另一个禁用外观，再添加对应的 `:disabled:selected` 样式。
只想改变共享的状态默认值，可覆盖 `Comp.Chip.SelectedContainerColor` / `SelectedContentColor` 等 Token，
写法见[主题与定制](../../../../docs/GETTING_STARTED.md#主题与定制)。移除覆盖后，默认值继续跟随当前 Token。

`Elevation` 是六个交互高度组成的完整 `ChipElevation` 值，由主题根据平面/Elevated 变体动态组装。
显式对象会整组替换，未写字段为 0；`Dragged=8` 是主题默认，而非构造器隐藏补值。
`Spacing` 和 `Elevation` 不接受 null。全零 elevation 关闭全部交互阴影；要恢复主题默认，清除局部值。
平面 Filter 默认 hover elevation 为 0，Elevated Filter 为 3。下面完整配置恢复平面 Filter 的上游悬停高度：

```xml
<m3:FilterChip Content="上游悬停高度">
  <m3:FilterChip.Elevation>
    <m3:ChipElevation Default="0" Pressed="0" Focused="0"
                      Hovered="1" Dragged="8" Disabled="0" />
  </m3:FilterChip.Elevation>
</m3:FilterChip>
```

只改默认悬停高度时也可覆盖 `Comp.Chip.FlatFilterHoverElevation`，其余高度仍跟随主题。
运行时可以替换整组高度配置；更新按显示帧合并，禁用使用 Disabled，恢复启用后跟随仍活动的交互。
Catalog **Drag handle demo** 提供配置切换、启用开关及与 Card 共享的拖动反馈。
透明背景下恢复 1 DIP 阴影可能重新出现已观察到的 GPU 内部灰环，详见维护说明。

## 使用限制与注意事项

Selectable Chip 默认最大宽度 1000 DIP，显式 Width / MinWidth 保留调用方约束。
`Shapes` 配置圆角/切角形变或路径状态切换；固定 `Shape` 也可使用自定义路径。布局支持前后槽间距与六种标准排列。
共享焦点环可通过 `MaterialInteraction.FocusFeedback="InsetRing"` 启用。
透明平面 Filter 恢复 1 DIP 悬浮阴影可能出现已知 GPU 灰环；当前双端验证范围见维护说明。
四族、Elevated、头像和禁用组合见 [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml)。

内部机制、移植差异与验证范围见 [INTERNALS.md](INTERNALS.md)。
