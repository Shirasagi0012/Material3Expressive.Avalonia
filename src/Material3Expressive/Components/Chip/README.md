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
    <m3:ChipShapes Shape="12" SelectedShape="20" PressedShape="8" />
  </m3:FilterChip.Shapes>
  <m3:FilterChip.LeadingIcon>
    <m3:MaterialSymbol Symbol="Check" />
  </m3:FilterChip.LeadingIcon>
</m3:FilterChip>
```

`Shapes` 只配置容器形变。设置、更换或清除它不会同时切换间距、颜色或图标进出动效；
Filter/Input 的图标槽在 `Shapes=null` 时也保留进入/退出动画。
图标动效可独立覆盖 `Comp.Chip.IconSizeEnterAnimation` / `IconSizeExitAnimation` 和
`IconAlphaEnterAnimation` / `IconAlphaExitAnimation` Token；形状变化本身使用 `Comp.Chip.ShapeAnimation`。
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
透明背景下恢复 1 DIP 阴影可能重新出现已观察到的 GPU 内部灰环，详见维护说明。

## 使用限制与注意事项

Selectable Chip 默认最大宽度 1000 DIP，显式 Width / MinWidth 保留调用方约束。
形状配置使用圆角矩形，布局支持前后槽间距。共享焦点环可通过 `MaterialInteraction.FocusFeedback="InsetRing"` 启用。
透明平面 Filter 恢复 1 DIP 悬浮阴影可能出现已知 GPU 灰环，自定义禁用高度也存在已记录的阴影差异。
四族、Elevated、头像和禁用组合见 [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml)。

内部机制、移植差异与验证范围见 [INTERNALS.md](INTERNALS.md)。
