# Chip

Chip 适合紧凑的操作、建议、筛选和已输入项目。库提供 `AssistChip`、`SuggestionChip`、`FilterChip`、
`InputChip` 四个 Button 派生控件；先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
实现映射与适配依据见 [INTERNALS](INTERNALS.md)。

## 最小示例

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
Input 没有此入口。默认容器高 32、交互布局最小高 48 DIP，圆角 8 DIP，标签使用 LabelLarge。

## 选择、命令与事件

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

## 标签、图标与头像

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

## 形状与局部定制

Filter/Input 的 `Shapes` 为 null 时使用固定形状；显式启用后，未选中/选中/按压采用
Medium/Full/Small 圆角并带形状与槽动画：

```xml
<m3:FilterChip Content="收藏" IsSelected="True" Shapes="{m3:ChipShapes}">
  <m3:FilterChip.LeadingIcon>
    <m3:MaterialSymbol Symbol="Check" Width="18" Height="18" />
  </m3:FilterChip.LeadingIcon>
  <m3:FilterChip.Colors>
    <m3:ChipColors SelectedContainerColor="#E8DEF8" SelectedLabelColor="#211A2E" />
  </m3:FilterChip.Colors>
</m3:FilterChip>
```

`ChipShapes.Shape` / `SelectedShape` / `PressedShape` 可分别覆盖圆角。
`Colors` 的未指定槽继续跟随 Token；`Background`、`Foreground`、`BorderBrush`、`BorderThickness`、`Padding`
的原生局部值/绑定优先于默认样式。不同图标与标签拥有各自颜色，不能把所有槽都视为单一 `Foreground`。
`Spacing` 可用 `ChipSpacing` 定义逻辑前/后间距；null 使用固定入口的 8 DIP，或形状入口按槽存在选择 4/8 DIP。
Input `Padding` 还随头像/图标组合调整。

`Elevation=null` 使用当前变体默认高度；显式 `ChipElevation` 覆盖交互高度。需要完全无阴影时将
`Default`/`Pressed`/`Focused`/`Hovered`/`Dragged`/`Disabled` 全设 0（`Dragged` 的构造默认值为 8）。
平面 Filter 默认 hover elevation 为 0，Elevated Filter 为 3；需要恢复上游平面悬停高度可写：

```xml
<m3:FilterChip Content="上游悬停高度">
  <m3:FilterChip.Elevation>
    <m3:ChipElevation Hovered="1" />
  </m3:FilterChip.Elevation>
</m3:FilterChip>
```

透明背景下恢复 1 DIP 阴影可能重新出现已观察到的 GPU 内部灰环，详见维护说明。

## 使用边界

Selectable Chip 默认最大宽度 1000 DIP，显式 `Width`/`MinWidth` 保留调用方约束。
当前 `Shapes` 仅支持圆角矩形；任意路径/切角 `Shape`、通用 Compose Arrangement、外部
MutableInteractionSource 和可配置 inset focus ring 尚未实现。
精度、阴影与原生键盘的已知差异不等于完整视觉一致；真实文字/字体、系统屏幕阅读器和桌面手工体验
未由无字形 headless 用例证明。
四族、Elevated、形状、头像和禁用组合见 [Catalog MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)。
