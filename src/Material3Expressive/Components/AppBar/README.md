# AppBar

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

将标准 Scaffold 包在 AppBarScrollScope 中，Scope 和栏引用同一个 behavior 实例。
以下片段假设 DataContext 提供 TopBehavior、BottomBehavior，且视图使用运行时绑定或声明了对应 x:DataType：

```xml
<m3:AppBarScrollScope TopScrollBehavior="{Binding TopBehavior}"
                      BottomScrollBehavior="{Binding BottomBehavior}">
  <m3:Scaffold x:Name="Screen">
    <m3:Scaffold.TopBar>
      <m3:MediumFlexibleTopAppBar Title="收件箱" ScrollBehavior="{Binding TopBehavior}" />
    </m3:Scaffold.TopBar>
    <m3:Scaffold.BottomBar>
      <m3:BottomAppBar ScrollBehavior="{Binding BottomBehavior}">
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
</m3:AppBarScrollScope>
```

ViewModel 中的属性可这样创建（只展示相关成员）：

```csharp
using Material3Expressive.Components;

public TopAppBarScrollBehavior TopBehavior { get; } =
    TopAppBarDefaults.ExitUntilCollapsedScrollBehavior(new TopAppBarState());
public BottomAppBarScrollBehavior BottomBehavior { get; } =
    BottomAppBarDefaults.ExitAlwaysScrollBehavior(new BottomAppBarState());
```

顶部还可选择 PinnedScrollBehavior / EnterAlwaysScrollBehavior。Width/Height 绑定让正文根填满 Scaffold；
Scaffold 不会自动应用 Padding。Material 的 ScrollViewer 主题提供 SmoothScroll presenter，详见
[ScrollViewer](../ScrollViewer/README.md)。

自动联动只连接唯一的物理主 ScrollViewer，使用内置 behavior、标准栏模板和上述正文结构；
支持默认/显式 spring snap 或 null。内层滚动器独立滚动，不直接驱动外层栏，到边界后的后续输入才按原生链路冒泡。
逻辑主正文、嵌套 AppBarScrollScope、不同正文布局、自定义 behavior、非 spring snap、主正文原生 snap points
暂不自动联动。Scope 不替换调用方的 ScrollViewer 主题或模板。

自定义宿主可在 UI 线程使用 DispatchScroll / DispatchFlingAsync；它们消费物理像素和 px/s，
不是 Avalonia Offset 的 DIP/逻辑单位。显式协议与直接拖动的约束见 [INTERNALS](INTERNALS.md#显式协议与直接拖动)。

## 验证边界

正文惯性来自 SmoothScroll，栏在正文结束后独立 spring 吸附，不承诺与 Compose decay 曲线相同。
直接设置 Offset、键盘定位、BringIntoView、滚动条定位和定位动画不进入 pre/post 位移消费协议。
UI 忙碌时只能合成移动已呈现内容；公开状态、命中测试、辅助功能与虚拟化新内容仍依赖 UI 更新。
AppBar 参与者路径不支持缩放及 overscroll。

TouchExplorationEnabled 需由宿主连接辅助服务；它仅关闭底栏直接拖动和隐藏布局，仍保留 nested connection。
合成标题在极小非零透明度下仍有原生剔除差异。硬件触摸、真实文字与屏幕阅读器需要另行验收。

Catalog 的 [AppBarDemo](../../../Material3Expressive.Catalog/AppBarDemo.cs) 可通过 `--app-bars` 打开，
Nested scrolling 展示独立内层列表/编辑区；切换该选项会重置正文和栏位置。
源码职责、未决问题和验证入口见 [INTERNALS.md](INTERNALS.md)。
