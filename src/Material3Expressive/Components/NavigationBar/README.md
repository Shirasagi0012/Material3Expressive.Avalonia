# NavigationBar / NavigationBarItem

`NavigationBar` 是库提供的常规底部导航条，`NavigationBarItem` 表达目的地。
建议用于 3–5 个目的地，数量不作强制限制；更矮且支持横排图标的入口见
[ShortNavigationBar](../ShortNavigationBar/README.md)。首次使用请完成[公共接入](../../../../docs/GETTING_STARTED.md)。

## 最小示例

下面不依赖 ViewModel，显示默认 80 DIP 高的导航条和初始选中项。选择变化需由应用接入。

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:NavigationBar VerticalAlignment="Bottom">
    <m3:NavigationBarItem Label="主页" IsSelected="True">
      <m3:NavigationBarItem.Icon>
        <m3:MaterialSymbol Symbol="HomeAndGarden" Width="24" Height="24" />
      </m3:NavigationBarItem.Icon>
    </m3:NavigationBarItem>
    <m3:NavigationBarItem Label="搜索">
      <m3:NavigationBarItem.Icon>
        <m3:MaterialSymbol Symbol="Search" Width="24" Height="24" />
      </m3:NavigationBarItem.Icon>
    </m3:NavigationBarItem>
  </m3:NavigationBar>
</UserControl>
```

后续片段沿用这些命名空间。`Items` / `ItemsSource` / `ItemTemplate` 沿用 `ItemsControl`；
数据模板应生成 `NavigationBarItem`。导航项等宽分配可用空间，其他内容按普通无权重内容测量。

## 选择、命令与标签

点击仅请求导航，控件不会修改 `IsSelected`，也不会取消其他项的选择。应用应将各项状态绑定到同一
路由或页面状态，并维护互斥。以下片段假设 `DataContext` 提供 `IsHomeSelected` 和 `NavigateHome`：

```xml
<m3:NavigationBarItem Label="主页"
                      IsSelected="{Binding IsHomeSelected}"
                      Command="{Binding NavigateHome}" />
```

启用编译绑定时声明对应 `x:DataType`。也可使用原生 `Click` 事件，并在所属视图实现处理器。
`CommandParameter`、`Command.CanExecute` 和 `IsEnabled` 沿用 Button；Space / Enter 在松开时请求激活。
自动化 Select 同样请求激活，不代替应用更新选择。

- 默认 `AlwaysShowLabel="True"`，标签一直显示
- `AlwaysShowLabel="False"` 时只在选中状态显示标签，并随选择动画移动、淡入淡出
- `Label="{x:Null}"` 使用纯图标布局；此时应设置 `AutomationProperties.Name`
- `IsEnabled="False"` 禁止激活并显示禁用内容色

## 内容槽与外观

`Icon` / `IconTemplate`、`Label` / `LabelTemplate` 接受控件或数据。
只设置 `IconTemplate` 也会显示图标槽，即使 `Icon` 为 null；模板可针对 null 数据创建内容。
无显式尺寸的 MaterialSymbol 默认使用 NavigationBar IconSize Token 24 DIP；显式 `Width` / `Height` 优先，
自定义图标的实际尺寸参与布局，不会被 Bar 限制。字符串标签使用 LabelMedium，支持换行；显式内容控件可以覆盖自己的排版。
整个导航项矩形可交互，反馈只绘制在胶囊内。

```xml
<m3:NavigationBarItem Label="收藏" IsSelected="True"
                      SelectedIconColor="Teal" SelectedTextColor="Teal"
                      IndicatorColor="LightCyan" />
```

选中态使用 `SelectedIconColor`、`SelectedTextColor`、`IndicatorColor`；未选中和禁用态分别使用
`UnselectedIconColor` / `UnselectedTextColor` 与 `DisabledIconColor` / `DisabledTextColor`。
这些是 Item 的颜色属性。Bar 的 `Background` / `Foreground` 控制容器与内容环境，后者也供 ripple 使用，
不能用一次 Foreground 覆盖代替全部状态色。需要主题联动时使用[公共 Token 覆盖](../../../../docs/GETTING_STARTED.md)。

`TonalElevation` 默认 0；`WindowInsets` 是宿主提供的物理 left/top/right/bottom Thickness，
默认零，RTL 不交换其物理边。放入 [Scaffold](../Scaffold/README.md) 的 BottomBar 槽时，
仍由 Bar 自己处理所需 Insets。

`NavigationBarItem.InteractionSource` 接受 `MaterialInteractionSource`，null 使用内部源。可以与 TooltipLabel 共享：

```csharp
var source = new Material3Expressive.Foundation.Interaction.MaterialInteractionSource();
item.InteractionSource = source;
label.InteractionSource = source;
```

这里 item 和 label 是视图中现有的导航项和 TooltipLabel。外部 Press 使用导航项自身坐标；
组件按上游规则映射到反馈胶囊。Release/Cancel、Exit/Unfocus 必须引用对应的开始对象。
外部交互不执行 Click/Command 或修改 IsSelected；禁用只取消原生输入，不结束调用方发出的按压。
更换源清除旧反馈且不重放事件。Catalog 的 Tooltip 示例提供三种导航项接入。

## 使用边界

导航条不管理页面或路由。替换 ItemsPanel 或整个模板可能改变
等宽布局、命中区和反馈。真实文字、字体图标、桌面人工操作与屏幕阅读器体验尚不能由几何 parity 推断。

Catalog 的 [MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml) 包含真实标签、图标和外部选择示例。
布局、确认依据和测试入口见 [INTERNALS.md](INTERNALS.md)。

## 嵌套窗口 Insets

`WindowInsets` 自动扣除祖先通过 `MaterialWindowInsets.ConsumePadding`、`ConsumeInsets` 或
`MaterialWindowInsetsPadding` 已消费的部分，再把本控件实际应用的消费量传给项目内容和 Header。
例如外层处理了 12 DIP 顶部 Insets，本控件请求 20 DIP 时仅再添加 8 DIP。
物理四边与 RTL 无关，动态修改与重挂会重新计算。宿主仍负责提供系统 Insets。

```xml
<m3:MaterialWindowInsetsPadding Insets="12,24,20,16">
  <m3:NavigationBar WindowInsets="20,32,28,24" />
</m3:MaterialWindowInsetsPadding>
```

此前调用方若手工扣除了祖先 Insets，请改为传入完整窗口 Insets，避免重复扣除。
Catalog 的“导航容器消费 Insets”提供三种导航容器的嵌套示例。
