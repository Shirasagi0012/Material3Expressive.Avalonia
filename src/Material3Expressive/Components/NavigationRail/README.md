# NavigationRail / NavigationRailItem

`NavigationRail` 是库提供的常规窄侧边导航，支持 Header 与目的地列表；`NavigationRailItem` 的选中状态
由应用持有。可展开或模态的侧栏见 [WideNavigationRail](../WideNavigationRail/README.md)。
首次使用请完成[公共接入](../../../../docs/GETTING_STARTED.md)。

## 最小示例

在有限高度的页面中放置 Rail；默认最小宽度为 80 DIP，填满可用高度。
下面显示初始选中项，点击后的页面切换与选择更新需由应用接入。

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <Grid ColumnDefinitions="Auto,*">
    <m3:NavigationRail>
      <m3:NavigationRail.Header>
        <m3:IconButton Symbol="Menu" />
      </m3:NavigationRail.Header>
      <m3:NavigationRailItem Label="主页" IsSelected="True">
        <m3:NavigationRailItem.Icon>
          <m3:MaterialSymbol Symbol="HomeAndGarden" Width="24" Height="24" />
        </m3:NavigationRailItem.Icon>
      </m3:NavigationRailItem>
      <m3:NavigationRailItem Label="搜索">
        <m3:NavigationRailItem.Icon>
          <m3:MaterialSymbol Symbol="Search" Width="24" Height="24" />
        </m3:NavigationRailItem.Icon>
      </m3:NavigationRailItem>
    </m3:NavigationRail>
    <TextBlock Grid.Column="1" Margin="24" Text="页面内容" />
  </Grid>
</UserControl>
```

后续片段沿用这些命名空间。`Items` / `ItemsSource` / `ItemTemplate` 使用普通 `ItemsControl` 入口，
数据模板应生成 `NavigationRailItem`。Header 可以是一个控件、数据对象或组合多个子项的 Panel。

## 选择与交互

```xml
<m3:NavigationRailItem Label="主页" AlwaysShowLabel="False"
                       IsSelected="{Binding IsHomeSelected}"
                       Command="{Binding NavigateHome}" />
```

上例假设 `DataContext` 提供布尔状态和 `ICommand`；使用编译绑定时还需对应 `x:DataType`。
也可绑定 `CommandParameter` 或使用原生 `Click` 事件，事件处理器由所属视图提供。
Item 不自动修改 `IsSelected`；应用负责页面导航及各项互斥选择。

默认标签一直显示；`AlwaysShowLabel="False"` 只在选中时显示标签，`Label="{x:Null}"` 使用纯图标布局。
Space / Enter 松开和自动化 Select 都请求激活；`IsEnabled` / `Command.CanExecute` 可阻止激活。
整个 Item 矩形都参与点击和悬停，状态层与 ripple 仍局限于图标胶囊。
有标签时自动抑制图标的重复描述；没有标签时为 Item 设置 `AutomationProperties.Name`。

## 内容与定制

`Header` / `HeaderTemplate`、`Icon` / `IconTemplate`、`Label` / `LabelTemplate` 接受内容和数据模板。
字符串标签使用 LabelMedium 并可换行；显式文字控件可覆盖字体和画刷，图标尺寸由调用方设置。

```xml
<m3:NavigationRailItem Label="收藏" IsSelected="True"
                       SelectedIconColor="Teal" SelectedTextColor="Teal"
                       IndicatorColor="LightCyan" />
```

Item 的选中、未选中、禁用内容色分别由 `SelectedIconColor` / `SelectedTextColor`、
`UnselectedIconColor` / `UnselectedTextColor`、`DisabledIconColor` / `DisabledTextColor` 控制。
Rail 的 `Background` / `Foreground` 控制容器和环境色，Foreground 也供 ripple 使用，不替代 Item 状态色。
动态主题统一定制见[公共接入](../../../../docs/GETTING_STARTED.md)。

`WindowInsets` 是物理 left/top/right/bottom，桌面默认零，RTL 不交换物理边；此入口不提供嵌套 inset 消费。
Rail 默认上/下留白各 4 DIP、项间距 4 DIP、Header 到第一项 16 DIP。需要展开布局、横向标签或不同整组排列时，
使用 Wide Rail 的公开入口，不以替换窄 Rail 布局面板模拟。

## 使用边界

该组件不管理应用路由，也不提供可展开或模态行为。真实文字/Material Symbols 的双端栅格、桌面人工交互
及屏幕阅读器体验仍需单独验收。默认动画与布局的实现边界见 [INTERNALS.md](INTERNALS.md)。
Catalog 的 [MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml) 展示 Header、真实目的地与外部选择。
