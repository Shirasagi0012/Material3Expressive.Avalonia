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

`Icon` / `IconTemplate` 与 `Label` / `LabelTemplate` 接受内容或数据；只设置 `IconTemplate` 也会显示图标槽，
即使 `Icon` 为 null，模板仍可针对 null 数据创建内容。无显式尺寸的 MaterialSymbol 默认使用
ShortNavigationBar IconSize Token 24 DIP；显式 `Width` / `Height` 优先，自定义图标不会被布局强制限制。
文字默认 LabelMedium，显式子控件可覆盖排版。图标中可放 [BadgedBox](../Badge/README.md)，内置布局为其提供徽标边界。
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

`WindowInsets` 是宿主提供的物理 Thickness，默认零，RTL 不交换边。
内嵌 Surface 继承环境的累积 tonal elevation，但 Short Bar 没有独立的 TonalElevation 参数。

`ShortNavigationBarItem.InteractionSource` 接受 `MaterialInteractionSource`，null 使用内部源。可以与 TooltipLabel 共享：

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

显式 Width 使用 Avalonia 的容器宽度语义，各项仍按排列分配；与固定 Compose 直接 width 修饰的边界行为
存在已接受差异。IconPosition 切换重建反馈层并保留原生按压；
逐帧验证仍有已记录的边界，见维护说明。
真实文字、图标栅格和桌面辅助技术仍需单独验证。

Catalog 入口在 [MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)；
测量规则、已确认适配和复现入口见 [INTERNALS.md](INTERNALS.md)。

## 嵌套窗口 Insets

`WindowInsets` 自动扣除祖先通过 `MaterialWindowInsets.ConsumePadding`、`ConsumeInsets` 或
`MaterialWindowInsetsPadding` 已消费的部分，再把本控件实际应用的消费量传给项目内容和 Header。
例如外层处理了 12 DIP 顶部 Insets，本控件请求 20 DIP 时仅再添加 8 DIP。
物理四边与 RTL 无关，动态修改与重挂会重新计算。宿主仍负责提供系统 Insets。

```xml
<m3:MaterialWindowInsetsPadding Insets="12,24,20,16">
  <m3:ShortNavigationBar WindowInsets="20,32,28,24" />
</m3:MaterialWindowInsetsPadding>
```

此前调用方若手工扣除了祖先 Insets，请改为传入完整窗口 Insets，避免重复扣除。
Catalog 的“导航容器消费 Insets”提供三种导航容器的嵌套示例。
