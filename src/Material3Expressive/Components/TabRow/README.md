# TabRow / Tab / 原生 TabControl 主题

这里有两个入口：库控件 `TabRow` + `Tab` 只绘制受应用控制的标签栏；Avalonia 原生 `TabControl` + `TabItem`
使用 Material 主题，同时管理选择与页面内容。首次使用请完成[公共接入](../../../../docs/GETTING_STARTED.md)。

## 最小示例：原生 TabControl

已加载 Material 主题时，原生 TabControl 自动使用顶部水平标签栏，选择和内容切换仍由 Avalonia 负责。

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <TabControl SelectedIndex="0">
    <TabItem Header="音乐">
      <TextBlock Margin="24" Text="音乐页面" />
    </TabItem>
    <TabItem Header="视频">
      <TextBlock Margin="24" Text="视频页面" />
    </TabItem>
  </TabControl>
</UserControl>
```

后续片段沿用这些命名空间。原生入口保留 SelectedIndex / SelectedItem、HeaderTemplate / ContentTemplate、
SelectedContent、键盘导航、automation 与 PageTransition。TabItem.Header 是标签槽；Icon / IconTemplate 为图标槽。
原生页面内容不会参与标签颜色动画。

## 受应用控制的标签栏

TabRow 继承 ItemsControl，只绘制栏，不负责页面或选择更新。应用必须同时维护 Row.SelectedIndex
和各 Tab.IsSelected；Click、Command、键盘及自动化请求都不隐式更改这两个属性。

```xml
<m3:TabRow SelectedIndex="{Binding SelectedTabIndex}">
  <m3:Tab Content="音乐" IsSelected="{Binding IsMusicSelected}"
          Command="{Binding SelectMusic}" />
  <m3:Tab Content="视频" IsSelected="{Binding IsVideoSelected}"
          Command="{Binding SelectVideo}" />
</m3:TabRow>
```

片段假设 DataContext 提供索引、两个互斥布尔状态及命令；编译绑定需声明对应 x:DataType。
也可使用 Click 处理器，应用在一次选择中同步更新索引和所有 IsSelected。
`ItemsSource` + `ItemTemplate` 应生成 Tab，并绑定其状态和命令。
Tab 支持 CommandParameter、CanExecute、IsEnabled；Enter / Space 松开时请求激活。

独立 Tab 可通过 `InteractionSource` 共享输入反馈；原生 TabItem 使用 `TabAppearance.InteractionSource` 附加属性：

```csharp
var source = new Material3Expressive.Foundation.Interaction.MaterialInteractionSource();
TabAppearance.SetInteractionSource(tabItem, source);
label.InteractionSource = source;
```

这里 tabItem 和 label 是现有的 Tab/TabItem 与 TooltipLabel。null 使用内部源；外部事件只驱动反馈，
不点击或更改选择。Release/Cancel、Exit/Unfocus 必须引用开始对象；替换源不重放旧事件，禁用只取消自身输入。
Catalog 的 Tooltip 示例提供受控 Tab 接入。

## 栏变体与内容布局

`Variant="Primary|Secondary"` × `IsScrollable="False|True"` 组合四种栏。
独立 Row 使用同名属性；原生 TabControl 使用 `TabRowAppearance` 附加属性：

```xml
<TabControl m3:TabRowAppearance.Variant="Secondary"
            m3:TabRowAppearance.IsScrollable="True">
  <TabItem Header="音乐" m3:TabAppearance.ContentLayout="Leading">
    <TabItem.Icon><m3:MaterialSymbol Symbol="Favorite" Width="24" Height="24" /></TabItem.Icon>
    <TextBlock Text="音乐页面" />
  </TabItem>
  <TabItem Header="视频"><TextBlock Text="视频页面" /></TabItem>
</TabControl>
```

默认 Primary 指示器随标签内容宽度，Secondary 覆盖整个 Tab。滚动栏默认 EdgePadding=52、MinTabWidth=90 DIP。
初次布局和选择变化会自动居中；同一选择下调整窗口/内容宽度不会主动重新居中。
用户滚轮/滚动手势会取消正在进行的自动居中。

`Variant="Legacy"` 提供固定基线旧 `TabRow` / `ScrollableTabRow` 的两种外观和行为，两个入口均可使用：

```xml
<m3:TabRow Variant="Legacy" IsScrollable="True" SelectedIndex="0">
  <m3:Tab Content="Overview" IsSelected="True" />
  <m3:Tab Content="Recent activity" />
</m3:TabRow>
```

Legacy 使用 Primary 内容色与方角整项宽度指示器；它的宽度、位置动画在目标中断时重新启动，
滚动分隔线跨整个内容 extent。`IndicatorMatchContentSize` 在 Legacy 下不改变整项宽度策略。
`TabPositions.ContentWidth` 仍提供旧上游测量值：滚动 Legacy 不强制 24 DIP 最小值，自定义窄内容可能为负值。
在 Legacy 与新变体之间切换会重建栏动画和滚动状态；Primary/Secondary 之间继续复用当前动画。
Catalog **Legacy tabs** 展示固定、滚动与原生入口。

更新已选 Tab.Content 或原生 TabItem.Header 时，指示器随重新测量后的内容宽度运动；
动画中再次修改内容会保留当前速度并转向新目标。同次布局前的多次更新使用最终内容。
Catalog 的 Changing labels 示例同时演示这两个入口，无需额外刷新或重设选择。

内容布局通过 Tab.ContentLayout 或原生 TabItem 的 `TabAppearance.ContentLayout` 设置：

- Stacked（默认）：只有标签或图标高 48 DIP，两者都有时至少 72 DIP，保留文字基线布局
- Leading：起始侧图标，高 48 DIP，水平 Padding 16 DIP，图标与标签间距 8 DIP
- Custom：使用整个自定义内容，不施加默认标签字体或最小高度

独立 Tab 的标签使用 Content / ContentTemplate，图标使用 Icon / IconTemplate。
只设置 `IconTemplate` 也会显示图标槽，即使 `Icon` 为 null；模板可针对 null 数据创建内容。
无显式尺寸的 MaterialSymbol 默认使用通用 24 DIP；显式 `Width` / `Height` 优先，自定义图标尺寸参与 Tab 测量。
标准标签默认 TitleSmall、居中文字；显式文字设置优先。自定义复合标签需要跨包装层提供基线时，
在标签根控件设置 TextBlock.BaselineOffset。只有标准 Stacked 布局提供 Badge bounds。
Leading 布局允许图标中的 BadgedBox 溢出；与固定上游一致，标签在图标之后绘制，因此会覆盖
与它重叠的徽标区域。需要避免重叠时应缩小徽标或选择 Stacked 布局，库不会自动隐藏或裁掉徽标。

## 颜色与指示器槽

独立 Row 的 Foreground 设置标签环境色；原生 TabControl 使用 `TabRowAppearance.ContentColor`，
其 Foreground 继续用于页面。`TabAppearance.SelectedContentColor` / `UnselectedContentColor`
适用于 Tab 和 TabItem；独立 Tab 也提供同名 CLR 属性。默认未选中色等于选中色，禁用不额外降低颜色透明度。

Indicator / Divider 是 IControlTemplate，null 可移除槽；独立 Row 为普通属性，原生入口为 TabRowAppearance 附加属性。

```xml
<m3:TabRow SelectedIndex="0">
  <m3:TabRow.Indicator>
    <ControlTemplate><Border Height="4" Background="Teal" CornerRadius="2" /></ControlTemplate>
  </m3:TabRow.Indicator>
  <m3:TabRow.Divider>
    <ControlTemplate><Border Height="1" Background="Gray" /></ControlTemplate>
  </m3:TabRow.Divider>
  <m3:Tab Content="自定义指示器" IsSelected="True" />
</m3:TabRow>
```

Row 把 Indicator 放在动画后的目标矩形内，其 DataContext 是只读 `IReadOnlyList<TabPosition>`。
也可从 TabRow.TabPositions 或 TabRowAppearance.GetTabPositions(native) 读取；Left/Right 为 start-relative DIP。
原生 TabItem 的单项 IndicatorTemplate 不在此主题中生效，应使用 Row 的统一模板槽。
主题联动定制见[公共接入](../../../../docs/GETTING_STARTED.md)。

需要独立控制两端运动、整项外框或跨标签绘制时，将 Indicator 模板的根设为 `TabIndicatorLayout` 的派生类。
它是带一个 Child 的布局控件，重写 `MeasureIndicator` 和 `ArrangeIndicator`，使用只读 `TabPositions`、
`SelectedIndex`、`IsScrollable`、`MinimumSize` / `MaximumSize` 计算实际布局。这样不再叠加默认位移/宽度动画，
`IndicatorMatchContentSize` 也不参与此模板；标签选择与滚动居中仍由栏负责。

固定栏给出单项宽度约束；滚动栏给出 0 到选中内容宽度的约束，不是整个栏宽。
返回尺寸可超出约束，栏按实际尺寸和约束后的报告尺寸居中放置；Child 可以跨出布局盒绘制。
子项 Arrange 的 X 是逻辑起点坐标，RTL 由 Avalonia 镜像，不需要再次反向。
滚动栏还会在选中 Tab 内居中该布局盒，因此按完整 TabPosition.Left 定位时应扣除这段居中量。
边界计算按实际布局密度取整；完整实现可直接参考 Catalog 的
[ElasticTabIndicator.cs](../../../Material3Expressive.Catalog/ElasticTabIndicator.cs)。

Catalog 的 **Custom indicator** 展示同一模板用于独立 TabRow 和原生 TabControl，
两个边缘按移动方向采用不同弹簧，快速反向保留当前速度。自定义动画通过 `AnimationScope` 建立子作用域，
在卸载时释放；重挂时按当前 TabPositions 初始化。普通 Border 指示器不需要改动或迁移。

## 独立指示器与形状

`PrimaryTabIndicator` 和 `SecondaryTabIndicator` 可独立显示，也可放入 Indicator 模板。
Primary 默认绘制 24×3 DIP 的圆角形状；Secondary 默认填满可用宽度、高 3 DIP，保持方角。
两者用 `Background` 设置颜色，默认随主题 Primary 更新。

```xml
<m3:PrimaryTabIndicator IndicatorWidth="64" IndicatorHeight="10" Background="Teal">
  <m3:PrimaryTabIndicator.Shape>
    <m3:CutCornerShape TopStart="6" BottomEnd="6" />
  </m3:PrimaryTabIndicator.Shape>
</m3:PrimaryTabIndicator>
<m3:SecondaryTabIndicator Width="180" IndicatorHeight="4" HorizontalAlignment="Left" />
```

Primary 的 `Shape` 复用 `MaterialShape`，支持百分比圆角、切角和自定义路径；null 使用 `CornerRadius`。
`IndicatorWidth` / `IndicatorHeight` 是实际绘制尺寸；普通 `Width` / `Height` 是外部布局尺寸，
外部尺寸较小时 Primary 居中溢出，与上游 requiredWidth/requiredHeight 一致。
Secondary 的 `IndicatorHeight` 遵守外部约束，不产生这种尺寸溢出。

在普通行模板内跟随行已有的宽度动画时，Primary 设置 `IndicatorWidth="NaN"` 并水平拉伸：

```xml
<m3:TabRow.Indicator>
  <ControlTemplate>
    <m3:PrimaryTabIndicator IndicatorWidth="NaN" IndicatorHeight="6" HorizontalAlignment="Stretch">
      <m3:PrimaryTabIndicator.Shape><m3:CutCornerShape TopStart="4" TopEnd="4" /></m3:PrimaryTabIndicator.Shape>
    </m3:PrimaryTabIndicator>
  </ControlTemplate>
</m3:TabRow.Indicator>
```

此模板同样可用于原生 `TabRowAppearance.Indicator`。这两个绘制控件自身没有选择状态或动画；
默认位置动画由栏管理，独立边缘动画使用前述 `TabIndicatorLayout`。
Catalog 的 **Standalone indicators** 展示独立绘制和切角行指示器。

## 使用边界

原生主题仅实现顶部水平栏，不支持其他 TabStripPlacement。原生 TabItem 在指针按下和方向焦点时选择，
独立 Tab 在激活回调后由应用选择；两者输入时机不同。替换模板时需保留原生页面呈现合约。
真实文字栅格、桌面触摸/惯性及 PageTransition 的实际画面尚需单独验证。

Catalog 示例见 [TabRowDemo.axaml](../../../Material3Expressive.Catalog/TabRowDemo.axaml)；
测量与动画约束、确认依据和测试入口见 [INTERNALS.md](INTERNALS.md)。

### 独立设置指示器宽度策略

`TabRow.IndicatorMatchContentSize` 或原生 `TabRowAppearance.IndicatorMatchContentSize` 对应
上游 `tabIndicatorOffset(matchContentSize)`：true 跟随内容宽度，false 跟随整项宽度，null 恢复 Variant 的默认值。
该设置适用于 Primary/Secondary 的默认和普通自定义 Indicator 模板，不改变标签测量、选择或颜色；
`TabIndicatorLayout` 模板自己决定宽度与运动，不使用此设置。
运行时切换使用原有宽度弹簧，动画中反转保留当前位置和速度。

```xml
<m3:TabRow Variant="Secondary" IndicatorMatchContentSize="True" SelectedIndex="0">
  <m3:Tab Content="Inbox" IsSelected="True" />
  <m3:Tab Content="Saved messages" />
</m3:TabRow>
```

Catalog 的 Indicator width 示例让两个入口共享复选框，可在选择动画中切换宽度策略。
