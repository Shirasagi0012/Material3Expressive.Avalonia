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

## 四种栏与内容布局

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

Primary 指示器随标签内容宽度，Secondary 覆盖整个 Tab。滚动栏默认 EdgePadding=52、MinTabWidth=90 DIP。
初次布局和选择变化会自动居中；同一选择下调整窗口/内容宽度不会主动重新居中。
用户滚轮/滚动手势会取消正在进行的自动居中。

内容布局通过 Tab.ContentLayout 或原生 TabItem 的 `TabAppearance.ContentLayout` 设置：

- Stacked（默认）：只有标签或图标高 48 DIP，两者都有时至少 72 DIP，保留文字基线布局
- Leading：起始侧图标，高 48 DIP，水平 Padding 16 DIP，图标与标签间距 8 DIP
- Custom：使用整个自定义内容，不施加默认标签字体或最小高度

独立 Tab 的标签使用 Content / ContentTemplate，图标使用 Icon / IconTemplate。
只设置 `IconTemplate` 也会显示图标槽，即使 `Icon` 为 null；模板可针对 null 数据创建内容。
无显式尺寸的 MaterialSymbol 默认使用通用 24 DIP；显式 `Width` / `Height` 优先，自定义图标尺寸参与 Tab 测量。
标准标签默认 TitleSmall、居中文字；显式文字设置优先。自定义复合标签需要跨包装层提供基线时，
在标签根控件设置 TextBlock.BaselineOffset。只有标准 Stacked 布局提供 Badge bounds。

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

## 使用边界

原生主题仅实现顶部水平栏，不支持其他 TabStripPlacement。原生 TabItem 在指针按下和方向焦点时选择，
独立 Tab 在激活回调后由应用选择；两者输入时机不同。替换模板时需保留原生页面呈现合约。
真实文字栅格、桌面触摸/惯性及 PageTransition 的实际画面尚需单独验证。

Catalog 示例见 [TabRowDemo.axaml](../../../Material3Expressive.Catalog/TabRowDemo.axaml)；
测量与动画约束、确认依据和测试入口见 [INTERNALS.md](INTERNALS.md)。
