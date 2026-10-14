# CommandBar 主题

为 Avalonia 原生 `CommandBar`、`CommandBarButton`、`CommandBarToggleButton` 和
`CommandBarSeparator` 提供 Material 外观。先完成[公共接入](../../../../docs/GETTING_STARTED.md)。

| 主题键 | 主命令布局 | 默认最小高度 |
| --- | --- | --- |
| `MaterialCommandBarStackedTheme` | 图标上、标签下 | 60 DIP |
| `MaterialCommandBarInlineTheme` | 图标与标签横排 | 48 DIP |
| `MaterialCommandBarIconTheme` | 纯图标，名称通过 Tooltip 提供 | 48 DIP |

未显式选择 Theme 的原生 CommandBar 使用上下图文主题。容器铺满可用宽度，默认零圆角；
命令从逻辑起始侧依内容宽度排列，“更多”在末端。内容需要时允许超过默认最小高度。
Label 的自动 Tooltip 仅用于主栏纯图标模式；显示标签或进入溢出菜单时不自动提供 Tooltip。

```xml
<CommandBar Theme="{StaticResource MaterialCommandBarInlineTheme}">
  <CommandBar.PrimaryCommands>
    <CommandBarButton Label="复制" Icon="{m3:Symbol ContentCopy}"
                      Command="{Binding CopyCommand}" />
    <CommandBarSeparator />
    <CommandBarToggleButton Label="固定" Icon="{m3:Symbol PushPin}"
                           IsChecked="{Binding IsPinned, Mode=TwoWay}" />
  </CommandBar.PrimaryCommands>
  <CommandBar.SecondaryCommands>
    <CommandBarButton Label="属性" Icon="{m3:Symbol Info}"
                      Command="{Binding PropertiesCommand}" />
  </CommandBar.SecondaryCommands>
</CommandBar>
```

`m3` 命名空间沿用公共接入。Label 是原生字符串属性；Icon 接受控件或数据，
数据图标通过 Avalonia DataTemplates 呈现。主题不新增 IconTemplate API。

普通命令只在完整胶囊区域播放按压水波纹，不显示选中背景，也不因按压改变标签或图标颜色。
hover / focus 的填充状态层关闭；共享控制器不把 pressed 绘制成固定填充层，
但保留 pressed opacity Token 用于水波纹绘制。禁用态仍使用禁用内容色。
切换命令使用原生 `IsChecked`，接入与 NavigationBar / NavigationRail 相同的四通道选择动画。
三态的 null 按未选中呈现，状态转换本身仍由原生 ToggleButton 管理。

动态溢出默认开启，沿用原生优先级与数量估算。主题将 `ItemWidthBottom`、`ItemWidthRight`、
`ItemWidthCollapsed` 默认设为 76、104、48 DIP，调用方可以覆盖。原生算法仍固定为更多按钮预留
48 DIP，不逐项读取实际按钮宽度；长短标签混排及分隔符可能使估算与实际布局不同。
进入溢出后，原生统一设为横排且显示标签，保留同一个命令控件及切换状态。
溢出项沿用 Standard MenuFlyout 的 LabelLarge 排版、20 DIP 图标提示、8 DIP 图文间距、
内容内边距与项目外边距；布局尺寸及圆角直接消费 `MaterialTokens.Comp.Menu`。
切换项的背景改用 `Comp.Menu.SelectedItemShape` 圆角矩形，保留 CommandBar 的选择颜色与四条动画。
没有图标的命令从内容起始侧排列，不预留空图标槽。

主栏的颜色、图标尺寸、指示器尺寸、排版与运动规格由 `MaterialTokens.Comp.CommandBar` 提供。
切换内容色与未选中内容色独立消费 Token；原生 Foreground 用作水波纹的内容环境。
自定义图标自身的显式颜色和尺寸仍优先于继承提示。
横排图文主命令的胶囊内边距默认左右 12、上下 6 DIP，
可覆盖 `MaterialTokens.Comp.CommandBar.InlineContentPadding`；纯图标和溢出菜单不使用该值。

Catalog 的 `CommandBar demo` 同时展示三种主题。实现与验证边界见 [INTERNALS.md](INTERNALS.md)。
