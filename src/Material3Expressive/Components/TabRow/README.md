# TabRow / Tab / 原生 TabControl 主题

依据 `docs/upstream-baseline.json` 固定的 AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 和
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`：
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/{TabRow,Tab}.kt`，
以及 Primary / SecondaryNavigationTabTokens。两个仓库的这两个组件源文件在本基线相同。

## 范围和入口

- `TabRow.Variant=Primary|Secondary` × `IsScrollable=false|true` 对应四个当前 Row 入口。
- `Tab.Content` 是文字/标签槽，`Icon` / `IconTemplate` 是图标槽；默认 `ContentLayout=Stacked`。
  只有标签或只有图标高 48 DIP；两者都有时至少 72 DIP，并保留第一/最后基线布局。
- `ContentLayout=Leading` 对应 LeadingIconTab：固定 48 DIP，水平 padding 16，图标与标签间距 8。
- `ContentLayout=Custom` 对应通用 Tab overload：调用者提供整个内容，不加标签字体或默认最小高度，居中放置。
- 原生 `TabControl` 的 Material 主题使用同一 Row 和 header 布局。主题范围是顶部水平标签栏；
  `TabStripPlacement` 的其他方向不在本次实现范围内。
- 未包含 deprecated `TabRow` / `ScrollableTabRow` 入口的旧实现。新的四个 Row 入口通过参数组合表达。

引用 `MaterialTheme.axaml` 并按项目架构初始化 `VisualAnimationScope` 后，两个入口均自动接入。
Catalog 的 `TabRow demo` 展示四种 Row、两个入口和标准文字/图标内容。

## 受控入口

`TabRow` 继承 `ItemsControl`，只绘制栏。应用同时管理 Row 的 `SelectedIndex` 和各 Tab 的 `IsSelected`。
`Click`、`Command`、键盘和 automation 的激活请求不隐式修改这两个属性。可以绑定到应用已有的路由或页面状态。
`ItemsSource` 配合 `ItemTemplate` 时，模板应产生 `Tab`，并绑定其 `IsSelected` 和 `Command`。

```xml
<m3:TabRow x:Name="Tabs" SelectedIndex="0" Variant="Primary">
  <m3:Tab Content="音乐" IsSelected="True" Click="OnTabClick" />
  <m3:Tab Content="视频" Click="OnTabClick" />
</m3:TabRow>
```

```csharp
private void OnTabClick(object? sender, RoutedEventArgs e)
{
    var selected = (Tab)sender!;
    Tabs.SelectedIndex = Tabs.Items.IndexOf(selected);
    foreach (var tab in Tabs.Items.OfType<Tab>())
        tab.IsSelected = ReferenceEquals(tab, selected);
}
```

## 原生入口

保持 Avalonia 自身的 `SelectedIndex` / `SelectedItem`、`HeaderTemplate` / `ContentTemplate`、
`SelectedContent`、键盘导航、automation 和 `PageTransition`。`TabItem.Header` 对应标签槽，
原生 `Icon` / `IconTemplate` 对应图标槽。原生页面内容不参与标签颜色动画。

```xml
<TabControl SelectedIndex="1"
            m3:TabRowAppearance.Variant="Secondary"
            m3:TabRowAppearance.IsScrollable="True">
  <TabItem Header="音乐" m3:TabAppearance.ContentLayout="Leading">
    <TabItem.Icon><PathIcon Data="{StaticResource MusicIcon}" /></TabItem.Icon>
    <TextBlock Text="音乐页面" />
  </TabItem>
  <TabItem Header="视频"><TextBlock Text="视频页面" /></TabItem>
</TabControl>
```

```csharp
var tabs = new TabControl { ItemsSource = pages, SelectedIndex = 0 };
TabRowAppearance.SetVariant(tabs, TabRowVariant.Secondary);
TabRowAppearance.SetIsScrollable(tabs, true);
TabRowAppearance.SetContentColor(tabs, Brushes.DarkSlateBlue); // 标签栏；页面仍使用 Foreground
```

原生入口使用 `TabRowAppearance` 的附加属性；独立 Row 提供同名 CLR 属性。
独立入口通过 `Foreground` 设置环境内容色，原生入口通过 `TabRowAppearance.ContentColor` 设置。
`TabAppearance` 的 `ContentLayout`、`SelectedContentColor`、`UnselectedContentColor` 同时适用于独立 Tab 和原生 TabItem。
默认未选中色等于选中色；禁用不额外降低颜色透明度，均保留上游规则。

## 布局、动画和模板槽

固定 Row 按物理像素整数均分，不分配余数。高度来自最大 intrinsic height。
Primary 的 ContentWidth 是 intrinsic width 减去 32 DIP，最低 24；Secondary 指示器覆盖整个 Tab。
滚动 Row 的默认 `EdgePadding=52`、`MinTabWidth=90`，内容宽度与 fixed Row 的约束规则分别保留。
RTL 的 Left/Right 是 start-relative，所有 `TabPosition` 数值单位为 DIP。

指示器宽度和位移使用 `Animatable<float>` 的 DefaultSpatial 弹簧，保留中断速度。
Tab 内容颜色使用 `Transition<bool>`，进入选择使用 DefaultEffects，其余使用 FastEffects。
初次布局或选择索引变化自动居中；同一索引下改变容器/内容宽度不会主动重新居中。
自动滚动保留物理整数位置及 Float 余数累计，用户滚轮/滚动手势会取消正在进行的自动居中。
手势、滚轮步长和惯性由 Avalonia ScrollViewer 提供。

`Indicator` / `Divider` 是 `IControlTemplate`，`null` 移除该槽。
指示器模板由 Row 放在动画后的目标矩形内；默认 Primary 高 3、圆角 3，Secondary 高 3、矩形。
它的 DataContext 是只供读取的 `IReadOnlyList<TabPosition>`，也可通过 `TabRow.TabPositions` 或
`TabRowAppearance.GetTabPositions(native)` 读取。原生 TabItem 的单项 `IndicatorTemplate` 由 Row 的统一模板槽替代。

```xml
<m3:TabRow SelectedIndex="0">
  <m3:TabRow.Indicator>
    <ControlTemplate><Border Height="4" Background="Teal" CornerRadius="2" /></ControlTemplate>
  </m3:TabRow.Indicator>
  <m3:TabRow.Divider><ControlTemplate><Border Height="1" Background="Gray" /></ControlTemplate></m3:TabRow.Divider>
  <m3:Tab Content="自定义指示器" IsSelected="True" />
</m3:TabRow>
```

标准标签的 `MaterialText.DefaultStyle` 是 TitleSmall（TextAlignment.Center），调用者的显式样式仍可覆盖。
实际 TextBlock 的 TextLayout 提供首/末基线；非文字控件可用 `TextBlock.BaselineOffset` 附加属性提供同一基线。
自定义复合标签若需要跨内部包装层传播基线，应在标签根控件设置该属性。
只有标准 Stacked 重载提供 Badge bounds，与上游 `modifier.badgeBounds()` 的适用范围一致。

## 已确认适配与证据边界

2026-10-04 维护者确认上述两个入口和 API：独立入口保留应用受控选择；原生入口保留 Avalonia 选择行为。
原生 TabItem 在指针按下和 Directional focus 时选择，Compose Tab 在激活回调时选择；
`dynamic.press.native`、`dynamic.directional.native` 保留这些差异，不将它们改成 Compose 时序。
布局、颜色和指示器动画仍以 Compose 为目标，程序化选择通过相同回归比较。

Avalonia 不提供 Compose 的 intrinsic measure API，因此 `TabHeader` 对真实内容进行测量查询，
随后重新执行实际约束下的测量；避免查询约束污染实际布局。对 intrinsic 变化、父约束变化和选择变化
分别保留上游 Layout / Modifier node 的更新顺序。
标准字体和基线在 headless 测试中检查；当前 visual parity 遵守测试框架的非文字比较规则，
通过真实槽放置确定性的标签/图标标记，不把文字渲染宣称为像素一致。

测试源码：`tests/Material3Expressive.Tests/TabRowTests.cs`；双端场景：
`tests/VisualParity/Material3Expressive.Parity/Cases/TabRowCases.cs` 和
`tests/VisualParity/reference/src/main/kotlin/cases/TabRowCases.kt`。
覆盖四个 Row、两种入口、内容布局、RTL、主题/颜色/内容变化、窄/受限尺寸、模板槽、选择反转、
初始居中、按压/取消/禁用/键盘；self-test 检查独立重复采集和额外被动截图。
桌面触摸/惯性、原生 PageTransition 的运行画面仍需人工验证，不能由 headless 或截图一致性推断。

2026-10-04 合并至主线后的验证：Release solution 构建通过；控件库 634 项测试通过（含 TabRow 专项 20 项），
Parity 工具 123 项测试通过。TabRow 的 123 例、406 个采样点双端 self-test 全部重复稳定、无执行错误：
115 例严格相等，6 例在现有 raster 容差内（按压/键盘采样仅 1–4 个像素，最大通道差 1），
2 例为上述已确认的原生输入差异；90 个静态用例全部严格相等。没有修改比较容差、遮罩或基线。
本轮 self-test 重新采集 Compose 和 Avalonia，检查独立重复捕获及额外被动截图，不消费或发布参考缓存。
结果见 [合并回归概览](../../../../artifacts/visual-parity/runs/tab-row-merge-20261004/summary.md)。
