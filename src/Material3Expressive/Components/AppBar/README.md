# AppBar

## 自适应动作行与列

`AppBarRow` / `AppBarColumn` 按可用宽度 / 高度排列动作，放不下的连续后缀进入独立
`DropdownMenu`。可以放在 TopAppBar 的 Items 中，也可独立使用。`MaxItemCount` 包含
溢出按钮：限制为 3 且项目超过 3 个时，最多显示两个动作和一个溢出按钮；恰好 3 个且
尺寸足够时显示全部动作。默认不限数量，仍受布局空间约束。
保留上游边界：0 或负数不触发数量截断；多个项目时设为 1 可只显示溢出按钮。

```xml
<m3:AppBarRow x:Name="Actions" MaxItemCount="3" MaxWidth="192">
  <m3:AppBarAction Label="保存" Icon="{m3:Symbol Save}" Command="{Binding SaveCommand}" />
  <m3:AppBarToggleAction Label="收藏" Icon="{m3:Symbol Favorite}"
                       IsChecked="{Binding IsFavorite}" CheckedChangeRequested="ChangeFavorite" />
  <m3:AppBarCustomItem>
    <m3:AppBarCustomItem.Content><Button Content="更多操作" /></m3:AppBarCustomItem.Content>
    <m3:AppBarCustomItem.MenuContent>
      <m3:DropdownMenuItem Content="自定义操作" Click="CustomClick" />
    </m3:AppBarCustomItem.MenuContent>
  </m3:AppBarCustomItem>
  <m3:AppBarAction Label="分享" Icon="{m3:Symbol Share}" Command="{Binding ShareCommand}" />
</m3:AppBarRow>
```

假设 ViewModel 提供上述命令和 `IsFavorite`，宿主处理事件：

```csharp
private void ChangeFavorite(object? sender, IconCheckedChangeEventArgs e) =>
    ViewModel.IsFavorite = e.Value;

private void CustomClick(object? sender, RoutedEventArgs e)
{
    ViewModel.PerformCustomAction();
    Actions.MenuState.Dismiss();
}
```

普通动作支持 Click、Command / CommandParameter / CanExecute。切换动作只请求新状态，
不会自动修改 IsChecked。两种便捷动作在菜单中均为纯文字项目；执行回调和命令后自动关闭。
自定义菜单内容自行决定何时关闭。栏内便捷动作带 Above TooltipBox，Label 同时是可访问名称。

Icon / IconTemplate 遵循统一图标槽：`Icon="{m3:Symbol Save}"`、直接 Control、数据模板均可；
模板非 null 时即使 Icon 为 null 也创建内容。直接 Control 继承外部 DataContext，数据模板
使用 Icon 数据。默认图标尺寸通过 MaterialIcon.DefaultSize 提供 24 DIP 提示，不缩放自定义控件。

ItemsSource / ItemTemplate 支持数据集合，模板根须是 AppBarAction、AppBarToggleAction 或
AppBarCustomItem。自定义项的 Content / ContentTemplate 与 MenuContent / MenuContentTemplate
分别构建两套树，不可把同一个 Control 同时放入两槽。集合、尺寸和 MaxItemCount 可动态变化。
AppBarColumn 使用完全相同的项目 API，例如 `new AppBarColumn { MaxHeight = 144 }`。

每个栏拥有独立的只读 MenuState（IsShowing、Show、Dismiss）。OverflowIndicatorTemplate 可替换
默认 MoreVert 按钮和提示，其 DataContext 是该 MenuState。自定义按钮的事件中可直接调用
`((AppBarMenuState)((Control)sender!).DataContext!).Show()`。默认英文提示可通过此模板本地化。
Row 支持 RTL 的逻辑起点，Column 从上到下、横向对齐逻辑起点。

菜单复用独立 DropdownMenu 的窗口内宿主、焦点、Escape、外部点击和退出保留；窗口初始化见
[公共接入](../../../../docs/GETTING_STARTED.md)。实现及验证边界见 [INTERNALS](INTERNALS.md#appbarrow--appbarcolumn)。

本目录提供库控件 `TopAppBar` 及六种顶部变体、`BottomAppBar` / `FlexibleBottomAppBar`，
以及可选的滚动状态和 `AppBarScrollScope`。栏内按钮仍使用普通事件和命令。
首次使用请完成[公共接入](../../../../docs/GETTING_STARTED.md)，包括主题及 TopLevel 动画初始化。

## 最小示例

静态栏不需要 ScrollBehavior。标题、导航按钮和动作是不同槽；直接子项进入动作行。

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:TopAppBar Title="收件箱" VerticalAlignment="Top">
    <m3:TopAppBar.NavigationIcon>
      <m3:IconButton Symbol="Menu" />
    </m3:TopAppBar.NavigationIcon>
    <m3:IconButton Symbol="Search" />
  </m3:TopAppBar>
</UserControl>
```

后续片段使用相同命名空间。按钮可设置 Click、Command、CommandParameter、IsEnabled；
AppBar 自己不发出导航或动作事件。绑定命令时由 DataContext 提供相应 ICommand；
启用编译绑定的视图还需正确的 `x:DataType`。

## 顶部变体与标题槽

- `TopAppBar` / `CenterAlignedTopAppBar`：默认高 64 DIP，分别使用起始侧/居中标题
- `MediumTopAppBar` / `LargeTopAppBar`：两行，默认展开高 112 / 152 DIP，折叠高 64 DIP
- `MediumFlexibleTopAppBar`：无/有副标题时默认展开高 112 / 136 DIP
- `LargeFlexibleTopAppBar`：无/有副标题时默认展开高 120 / 152 DIP
- `TwoRowsTopAppBar`：可配置两行入口，默认高度同 MediumFlexible

普通 Medium/Large 的上游入口没有副标题；带副标题时使用 Small、Flexible 或 TwoRows。
TitleHorizontalAlignment 的 Left / Right 表示逻辑 Start / End；Center 相对整栏居中并避让两侧。
双行 ExpandedHeight / CollapsedHeight 应有限且展开高度不小于折叠高度；ExpandedHeight 的 NaN / 正无穷使用默认值。
双行控件不以 ContentPadding 改写固定的双行布局。

`Title`、`Subtitle`、`NavigationIcon` 各有对应的 `...Template`；`Items` / `ItemsSource` /
`ItemTemplate` 提供动作行。标题字符串可直接使用，复杂数据配 DataTemplate。
双行需要两棵独立标题树：直接传 Control 时，还必须提供另一个 `CollapsedTitle` 控件；
使用数据和模板则可生成两份内容。

```xml
<m3:MediumFlexibleTopAppBar Title="收件箱" Subtitle="个人账户"
                            CollapsedSubtitle="{x:Null}">
  <m3:IconButton Symbol="Search" />
</m3:MediumFlexibleTopAppBar>
```

未设置折叠数据/模板时回退到展开槽；显式 null 清空相应折叠槽或取消模板回退。
显式 null 数据还会取消隐式模板回退，但可另设处理 null 的折叠模板。
`bar.ClearValue(TopAppBar.CollapsedTitleProperty)` 可恢复标题回退（C# 命名空间为 `Material3Expressive.Components`）。

## 底部栏与定制

BottomAppBar 默认高 80 DIP，FlexibleBottomAppBar 默认高 64 DIP。
直接子项为动作，`FloatingActionButton` / `FloatingActionButtonTemplate` 是额外槽。
`CustomContent` / `CustomContentTemplate` 非空时替换整个动作/FAB 区域，外部 Padding、Insets、背景与隐藏行为保留；
清空后恢复默认槽。模板优先于直接自定义内容。

```xml
<m3:FlexibleBottomAppBar HorizontalArrangement="SpacedBy" Spacing="12">
  <m3:IconButton Symbol="Search" />
  <m3:IconButton Symbol="Favorite" />
</m3:FlexibleBottomAppBar>
```

HorizontalArrangement 还支持 Start、Center、End、SpaceBetween、SpaceAround、SpaceEvenly。
Spacing 只用于 SpacedBy，允许负间距。自定义内容根负责自己的内部排列。

`WindowInsets` 使用物理 left/top/right/bottom，桌面默认零；`ContentPadding` 使用逻辑 start/top/end/bottom。
Top 的普通容器色使用 `Background`，滚动端色使用 `ScrolledContainerColor`；内容槽使用
`NavigationIconContentColor`、`TitleContentColor`、`SubtitleContentColor`、`ActionIconContentColor`。
Top 的颜色插值支持纯色 Background；显式 null 表示透明，非纯色在布局时抛出 ArgumentException。
Bottom 使用 Background / Foreground / TonalElevation，仍支持原有 Surface 刷子能力。
需要统一跟随主题的配置，优先使用[公共 Token 覆盖](../../../../docs/GETTING_STARTED.md)。

```xml
<m3:MediumFlexibleTopAppBar Title="收件箱" Subtitle="个人账户"
                            Background="#FFF7FF" ScrolledContainerColor="#F3EDF7"
                            TitleContentColor="#1D1B20" />
```

默认高度、ContentPadding、WindowInsets、底栏 TonalElevation / HorizontalArrangement 由主题接入 AppBar Token。
副标题数据或模板非 null 时，Flexible / TwoRows 的主题选择带副标题的高度；不覆盖显式 ExpandedHeight。
普通默认实例的 ExpandedHeight 返回主题选出的有限值；显式设置 NaN / 正无穷仍保留输入，并在布局时使用当前主题默认高度。
FlexibleBottomAppBar 的非有限或非正 ExpandedHeight 同样使用当前默认高度。
ClearValue 恢复当前主题配置，例如 `bar.ClearValue(TopAppBar.ExpandedHeightProperty)`。
动画只改变呈现结果，不写回 ExpandedHeight、Background 或端点颜色。

显式 fling 可直接使用 `DecayAnimationSpec.SplineBasedDecay<float>(density)`（ComposeAnimation.Runtime）；
显式 snap 传 `IAnimationSpec<float>`，null 仍禁用对应动画。

## 滚动联动

标准 Scaffold 的模板内置 AppBarScrollScope，自动读取实际呈现的 TopBar / BottomBar 的 ScrollBehavior。
直接声明 behavior 即可，不需要在 ViewModel 或后台代码中创建实例：

```xml
<m3:Scaffold x:Name="Screen">
  <m3:Scaffold.TopBar>
    <m3:MediumFlexibleTopAppBar Title="收件箱"
        ScrollBehavior="{m3:TopAppBarScrollBehavior ExitUntilCollapsed}" />
  </m3:Scaffold.TopBar>
  <m3:Scaffold.BottomBar>
    <m3:BottomAppBar ScrollBehavior="{m3:BottomAppBarScrollBehavior}">
      <m3:IconButton Symbol="Favorite" />
    </m3:BottomAppBar>
  </m3:Scaffold.BottomBar>
  <Border Width="{Binding Bounds.Width, ElementName=Screen}"
          Height="{Binding Bounds.Height, ElementName=Screen}"
          Padding="{Binding ContentPadding, ElementName=Screen}"
          m3:MaterialWindowInsets.ConsumePadding="{Binding ContentPadding, ElementName=Screen}">
    <ScrollViewer>
      <StackPanel Height="1600"><TextBlock Text="可滚动正文" /></StackPanel>
    </ScrollViewer>
  </Border>
</m3:Scaffold>
```

顶部 Mode 为 Pinned（默认）、EnterAlways 或 ExitUntilCollapsed；底部固定为 ExitAlways。
两种 behavior 都支持对象元素和 markup extension，属性可使用字面量或 Binding。
以下片段假设 DataContext 提供 Mode、CanScroll 和 TopState；编译绑定同时声明对应 x:DataType：

```xml
<m3:TopAppBar ScrollBehavior="{m3:TopAppBarScrollBehavior {Binding Mode}, CanScroll={Binding CanScroll}}" />
<m3:TopAppBar ScrollBehavior="{m3:TopAppBarScrollBehavior Mode={Binding Mode}, State={Binding TopState}}" />
<m3:MediumTopAppBar>
  <m3:MediumTopAppBar.ScrollBehavior>
    <m3:TopAppBarScrollBehavior Mode="ExitUntilCollapsed" CanScroll="{Binding CanScroll}">
      <m3:TopAppBarScrollBehavior.State>
        <m3:TopAppBarState />
      </m3:TopAppBarScrollBehavior.State>
    </m3:TopAppBarScrollBehavior>
  </m3:MediumTopAppBar.ScrollBehavior>
</m3:MediumTopAppBar>
```

behavior 是栏的逻辑子对象，继承 DataContext 和资源；每个实例有自己的默认 State，State 绑定为空时
回到该默认实例。一个 behavior 只能挂在一个栏上；模板应在各自实例内创建 behavior，避免多个栏共用资源实例。
更新 Mode / CanScroll 等配置保留 behavior 和 State；替换 State 会释放旧状态订阅并重建正文连接。
CanScroll 控制正文滚动参与，不改变栏的直接拖动条件。

顶部 IsScrollingContentAtStart 的 null（默认）自动跟随正文位置；显式布尔值或 Binding 覆盖自动判断。
未连接正文时按在起点处理。SnapAnimationSpec / FlingAnimationSpec 独立配置：未设置或 ClearValue
使用当前栏主题 spring / 实际密度 spline decay，显式 null 禁用对应动画。例如：

```xml
<m3:TopAppBar ScrollBehavior="{m3:TopAppBarScrollBehavior EnterAlways, SnapAnimationSpec={x:Null}}" />
```

C# 使用相同实例类型：`new TopAppBarScrollBehavior(TopAppBarScrollMode.ExitUntilCollapsed)` 或
`new BottomAppBarScrollBehavior()`；旧 TopAppBarDefaults / BottomAppBarDefaults 工厂已删除。

Width/Height 绑定让正文根填满 Scaffold；Scaffold 不会自动应用 Padding。
Material 的 ScrollViewer 主题提供 SmoothScroll presenter，详见 [ScrollViewer](../ScrollViewer/README.md)。
自动联动只连接唯一的物理主 ScrollViewer，使用标准栏模板和上述正文结构；支持默认/显式 spring snap 或 null。
TopBarTemplate / BottomBarTemplate 的根节点直接为 AppBar 时同样自动接入。
内层滚动器独立滚动，不直接驱动外层栏，到边界后的后续输入才按原生链路冒泡。
外层显式 Scope 不会重复接管标准 Scaffold 的正文；它暂时保留用于显式组合。
逻辑主正文、不同正文布局、非 spring snap、主正文原生 snap points 暂不自动联动。
Scaffold 外的栏支持显示和直接拖动，任意外部布局的正文联动尚未实现。
Scope 不替换调用方的 ScrollViewer 主题或模板；替换 Scaffold 模板时需保留 PART_ScrollScope 与五槽合约以保持自动连接。

## 验证边界

正文惯性来自 SmoothScroll，栏在正文结束后独立 spring 吸附，不承诺与 Compose decay 曲线相同。
直接设置 Offset、键盘定位、BringIntoView、滚动条定位和定位动画不进入 pre/post 位移消费协议。
UI 忙碌时只能合成移动已呈现内容；公开状态、命中测试、辅助功能与虚拟化新内容仍依赖 UI 更新。
AppBar 参与者路径不支持缩放及 overscroll。

TouchExplorationEnabled 需由宿主连接辅助服务；它仅关闭底栏直接拖动和隐藏布局，仍保留正文滚动联动。
合成标题在极小非零透明度下仍有原生剔除差异。硬件触摸、真实文字与屏幕阅读器需要另行验收。

Catalog 的 [AppBarDemo](../../../Material3Expressive.Catalog/AppBarDemo.cs) 可通过 `--app-bars` 打开，
Nested scrolling 展示独立内层列表/编辑区；切换该选项会重置正文和栏位置。
源码职责、未决问题和验证入口见 [INTERNALS.md](INTERNALS.md)。
