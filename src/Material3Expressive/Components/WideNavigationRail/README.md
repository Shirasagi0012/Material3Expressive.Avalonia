# WideNavigationRail / ModalWideNavigationRail

`WideNavigationRail` 在窄图标列与宽标签列之间展开/收起，目的地使用 `WideNavigationRailItem`。
`ModalWideNavigationRail` 在当前窗口客户区展开为浮层。只需要固定窄侧栏时见
[NavigationRail](../NavigationRail/README.md)。首次使用请完成[公共接入](../../../../docs/GETTING_STARTED.md)。

## 最小示例

Rail 默认创建独立的 Collapsed 状态，默认宽 96 DIP；展开宽度由内容决定，默认范围 220–360 DIP。
下面显示初始选择；应用负责选择和展开动作。

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <Grid ColumnDefinitions="Auto,*">
    <m3:WideNavigationRail x:Name="Rail">
      <m3:WideNavigationRail.Header>
        <m3:IconButton Symbol="Menu" />
      </m3:WideNavigationRail.Header>
      <m3:WideNavigationRailItem Label="主页" IsSelected="True">
        <m3:WideNavigationRailItem.Icon>
          <m3:MaterialSymbol Symbol="HomeAndGarden" Width="24" Height="24" />
        </m3:WideNavigationRailItem.Icon>
      </m3:WideNavigationRailItem>
      <m3:WideNavigationRailItem Label="搜索">
        <m3:WideNavigationRailItem.Icon>
          <m3:MaterialSymbol Symbol="Search" Width="24" Height="24" />
        </m3:WideNavigationRailItem.Icon>
      </m3:WideNavigationRailItem>
    </m3:WideNavigationRail>
    <TextBlock Grid.Column="1" Margin="24" Text="页面内容" />
  </Grid>
</UserControl>
```

后续片段沿用相同命名空间。`Items` / `ItemsSource` / `ItemTemplate` 沿用 ItemsControl，
数据模板应生成 WideNavigationRailItem。

## 展开状态与目的地

在事件处理器或 ViewModel 命令中调用同一个 `WideNavigationRailState`；动画方法必须在挂载后、UI 线程调用：

```csharp
// Rail 是上方已挂载的具名控件；这些是可选操作，不要求依次执行。
await Rail.State.ToggleAsync();
await Rail.State.ExpandAsync();
await Rail.State.CollapseAsync();
```

状态构造器或未挂载时的 SnapToAsync 可设置初值。State 可绑定到 ViewModel；同一宿主中多个 Rail 可共享状态，
最后一个消费者卸载时取消动画，重挂载从保存的目标恢复。方法接受 CancellationToken，反向操作可取消旧任务。
布局跟随 `TargetValue`；`CurrentValue` 不是稳定的“当前宽/窄”指示器，弹簧过冲时也可能返回 Collapsed。

```xml
<m3:WideNavigationRailItem Label="主页" IsSelected="{Binding IsHomeSelected}"
                          Command="{Binding NavigateHome}" />
```

片段假设 DataContext 提供相应状态和 ICommand，编译绑定需声明 x:DataType。
Click、Command、Enter/Space 松开、自动化 Select 都只请求导航；应用负责更新 IsSelected 和互斥选择。
支持 CommandParameter、CanExecute 与 IsEnabled。无标签目的地应设置 AutomationProperties.Name。

## 内容、排列与定制

Header / HeaderTemplate、Icon / IconTemplate、Label / LabelTemplate 接受内容和数据模板。
只设置 `IconTemplate` 也会显示图标槽，即使 `Icon` 为 null；模板可针对 null 数据创建内容。
无显式尺寸的 MaterialSymbol 默认使用 WideNavigationRail IconSize Token 24 DIP；显式 `Width` / `Height` 优先，
自定义图标尺寸参与布局，不会被 Rail 限制。字符串标签随布局使用 LabelMedium / LabelLarge；显式文字控件可覆盖排版。
Item 的可空 `IconPosition` 默认跟随 Rail 的展开目标；显式 Top / Start 可以独立固定位置。
独立 Item 可用 RailExpanded 选择目标形态。IndicatorPadding 非空时固定指示器留白，
否则在 CollapsedIndicatorPadding / ExpandedIndicatorPadding 之间插值。

```xml
<m3:WideNavigationRail Arrangement="{x:Static m3:NavigationRailArrangement.Center}"
                       ContentPadding="0,24,0,0">
  <m3:WideNavigationRailItem Label="主页" IsSelected="True" IconPosition="Start" />
</m3:WideNavigationRail>
```

Arrangement 还有 Top、Bottom、SpaceBetween、SpaceAround、SpaceEvenly；它们使用 x:Static 对象，
不是枚举字符串。自定义等距排列用 C# 的
`NavigationRailArrangement.SpacedBy(8, Avalonia.Layout.VerticalAlignment.Center)`。
标准 Center 相对整栏居中，有 Header 时其他排列使用 Header 下方空间；SpacedBy 的 Center 与标准 Center 不同。

WindowInsets 为物理 Thickness，桌面默认零，RTL 不交换物理边；ContentPadding 为逻辑边，默认顶部 44 DIP。
Background / Foreground / CornerRadius 控制容器。Item 可分别覆盖 SelectedIconColor、IndicatorColor、
两种位置的 SelectedTextColorTopIconPosition / SelectedTextColorStartIconPosition，以及未选中和禁用内容色。
环境 Foreground 不能代替这些状态色；统一定制见[公共 Token 接入](../../../../docs/GETTING_STARTED.md)。

## 模态 Rail

Modal 与普通 Rail 共用 State 和 Item API。`HideOnCollapse="False"` 保留收起态占位；true 不占布局空间，
并启用水平拖动关闭。展开浮层覆盖整个 TopLevel 客户区，与 Rail 在页面中的位置无关。

```xml
<m3:ModalWideNavigationRail x:Name="ModalRail" HideOnCollapse="True">
  <m3:ModalWideNavigationRail.Header>
    <m3:IconButton Symbol="Menu" />
  </m3:ModalWideNavigationRail.Header>
  <m3:ModalWideNavigationRail.ExpandedHeader>
    <m3:IconButton Symbol="Menu" />
  </m3:ModalWideNavigationRail.ExpandedHeader>
  <m3:WideNavigationRailItem Label="主页" IsSelected="True" />
</m3:ModalWideNavigationRail>
```

此片段默认关闭，可从页面外部按钮的处理器调用 `await ModalRail.State.ExpandAsync()`。
Header 用于收起态，ExpandedHeader 用于展开态；直接传 Control 时必须为两个实例。
使用 Header 数据及 HeaderTemplate 时，展开槽默认复用数据和模板生成第二份内容。
ExpandedHeaderTopPadding 提供展开 Header 的额外顶部留白。

宿主需要 PopupOverlayLayer，标准 Window 模板可提供；自定义 Window 模板须包含名为
`PART_VisualLayerManager` 的 VisualLayerManager。仅在页面中启用普通 OverlayLayer 不足以承载当前实现。
浮层背景/内容色使用 ModalBackground / ModalForeground，遮罩为 ScrimBrush；
CollapsedCornerRadius / ExpandedCornerRadius 默认 0 / 16 DIP。

Modal 阻挡背景输入并约束焦点，关闭后恢复原焦点。Escape 在 KeyUp 关闭，
ShouldDismissOnBackPress 只控制 TopLevel.BackRequested，不禁用 Escape。
默认辅助技术名称为 Navigation rail / Close rail，可通过 AutomationProperties.Name 提供本地化名称。

## 使用边界

展开/收起的纵向布局有已确认的连续化适配；自定义模板或 ItemContainerTheme 若给移动容器重新启用
UseLayoutRounding，会重新量化运动。真实文字、触屏拖动、屏幕阅读器以及跨 TopLevel 共享 State 尚未验收。
本组件面向桌面，未提供 Android predictive back；Indicator 混色仍有已记录的未解决差异。

Catalog 的 [MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml) 展示 Wide 与两种 Modal 模式；
状态生命周期、适配依据及复现入口见 [INTERNALS.md](INTERNALS.md)。
