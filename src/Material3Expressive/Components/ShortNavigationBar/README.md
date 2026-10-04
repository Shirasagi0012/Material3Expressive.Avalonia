# ShortNavigationBar / ShortNavigationBarItem

`ShortNavigationBar` 是库提供的短导航条，默认高度 64 DIP；目的地使用 `ShortNavigationBarItem`。
它支持顶部/起始侧图标及两种排列，与常规 [NavigationBar](../NavigationBar/README.md) 是独立入口。
首次使用请完成[公共接入](../../../../docs/GETTING_STARTED.md)。

## 最小示例

下面采用默认 Top 图标和 EqualWeight 排列，显示初始选择；点击后的导航与选择由应用接入。
布局需要有限宽度，放入横向无界容器时应显式设置 Width。

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:ShortNavigationBar VerticalAlignment="Bottom">
    <m3:ShortNavigationBarItem Label="主页" IsSelected="True">
      <m3:ShortNavigationBarItem.Icon>
        <m3:MaterialSymbol Symbol="HomeAndGarden" Width="24" Height="24" />
      </m3:ShortNavigationBarItem.Icon>
    </m3:ShortNavigationBarItem>
    <m3:ShortNavigationBarItem Label="搜索">
      <m3:ShortNavigationBarItem.Icon>
        <m3:MaterialSymbol Symbol="Search" Width="24" Height="24" />
      </m3:ShortNavigationBarItem.Icon>
    </m3:ShortNavigationBarItem>
  </m3:ShortNavigationBar>
</UserControl>
```

后续片段沿用相同命名空间。`Items` / `ItemsSource` / `ItemTemplate` 使用 `ItemsControl`，
数据模板应直接生成 `ShortNavigationBarItem`。

## 排列、位置与选择

Top / EqualWeight 适合较窄布局，Start / Centered 适合较宽布局；控件不会按窗口宽度自动切换。
`IconPosition="Start"` 是逻辑起始侧，跟随 RTL。

```xml
<m3:ShortNavigationBar Arrangement="Centered">
  <m3:ShortNavigationBarItem Label="主页" IconPosition="Start"
                            IsSelected="{Binding IsHomeSelected}"
                            Command="{Binding NavigateHome}" />
  <m3:ShortNavigationBarItem Label="搜索" IconPosition="Start"
                            IsSelected="{Binding IsSearchSelected}"
                            Command="{Binding NavigateSearch}" />
</m3:ShortNavigationBar>
```

此片段假设 `DataContext` 提供相应状态和命令；图标槽为简洁起见省略。
启用编译绑定时声明对应 `x:DataType`。应用负责互斥选择，Click、Command、自动化 Select 不修改 `IsSelected`。
支持 `CommandParameter`、`IsEnabled`、`Command.CanExecute`；Enter / Space 在松开时请求激活。

- `EqualWeight` 均分宽度；`Centered` 依目的地数量和内容尺寸在两侧留白
- Label 非空就一直显示，null 使用纯图标布局；没有 `AlwaysShowLabel`
- 切换 `IconPosition` 会立即重新布局，不播放 Wide Rail 的位置动画
- 没有 `RailExpanded`，需要展开/收起时使用 [WideNavigationRail](../WideNavigationRail/README.md)

## 内容与外观

`Icon` / `IconTemplate` 与 `Label` / `LabelTemplate` 接受内容或数据；文字默认 LabelMedium，
显式子控件可覆盖排版。图标中可放 [BadgedBox](../Badge/README.md)，内置布局为其提供徽标边界。
图标与标签都保留可访问子内容；纯图标目的地应设置 `AutomationProperties.Name`。

```xml
<m3:ShortNavigationBarItem Label="收藏" IsSelected="True" IconPosition="Start"
                          SelectedIconColor="Teal"
                          SelectedTextColorStartIconPosition="Teal"
                          IndicatorColor="LightCyan" />
```

Top 与 Start 的选中标签色分别使用 `SelectedTextColorTopIconPosition` 和
`SelectedTextColorStartIconPosition`；另外可覆盖选中/未选中/禁用图标、未选中/禁用标签和 IndicatorColor。
Bar 的 Foreground 默认来自 SurfaceContainer 对应的 OnSurface；自定义 Background 不会自动重选内容色，
如需改变环境色应同时设置 Foreground。统一主题定制见[公共 Token 接入](../../../../docs/GETTING_STARTED.md)。

`WindowInsets` 是宿主提供的物理 Thickness，默认零，RTL 不交换边；没有嵌套 inset 消费。
内嵌 Surface 继承环境的累积 tonal elevation，但 Short Bar 没有独立的 TonalElevation 参数。

## 使用边界

显式 Width 使用 Avalonia 的容器宽度语义，各项仍按排列分配；与固定 Compose 直接 width 修饰的边界行为
存在已接受差异。旧验证在按压中切换 IconPosition 后观察到额外 hover；当前已有反馈节点重挂路径，
但该场景是否消除差异尚缺新的双端核验，不能由代码变化推断已对齐。
真实文字、图标栅格和桌面辅助技术仍需单独验证。

Catalog 入口在 [MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)；
测量规则、已确认适配和复现入口见 [INTERNALS.md](INTERNALS.md)。
