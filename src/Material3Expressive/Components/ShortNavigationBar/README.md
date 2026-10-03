# ShortNavigationBar / ShortNavigationBarItem

本组件移植固定 Compose ShortNavigationBar，不改变常规 Bar 或 Wide Rail。
基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，桌面 Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
两端 ShortNavigationBar.kt 和 NavigationItem.kt 的源码 blob 分别相同。

## 上游与实现

- ShortNavigationBar.kt：两个公共入口、两种 ContentMeasurePolicy 与默认值。
- NavigationItem.kt：NavigationItem、TopIconOrIconOnlyMeasurePolicy、StartIconMeasurePolicy、三个 placement 函数，
  animateIndicatorProgressAsState、StyledLabel、MappedInteractionSource。
- tokens/NavigationBarTokens.kt、NavigationBarVerticalItemTokens.kt、NavigationBarHorizontalItemTokens.kt。
- Core ui/layout/MeasurePolicy.kt：默认 intrinsic 用 DefaultIntrinsicMeasurable 重放 measure。

[ShortNavigationBar.cs](ShortNavigationBar.cs) 提供容器与选择组语义，
[ShortNavigationBarItemsPanel.cs](ShortNavigationBarItemsPanel.cs) 翻译两种排列。
[ShortNavigationBarItem.cs](ShortNavigationBarItem.cs) 负责选择、内容色、指示器动画、键盘与自动化；
[ShortNavigationBarItemLayout.cs](ShortNavigationBarItemLayout.cs) 翻译非动画位置布局与固有尺寸，
[ShortNavigationBar.axaml](ShortNavigationBar.axaml) 保留 Surface、内容槽、Typography 和反馈组合。
Token 在 [ShortNavigationBarTokenSource.cs](ShortNavigationBarTokenSource.cs) 与
[MaterialTokens.Comp.ShortNavigationBar.cs](../../Tokens/MaterialTokens.Comp.ShortNavigationBar.cs)，由 MaterialTheme 装配。

默认高度 64 DIP，无项间距。EqualWeight 用整数除法分配等宽，余数留在逻辑末尾。
Centered 按数量计算留白：1～6 项单侧比例为 30/25/20/15/10/5%，超过 6 项为零。
先查询固有高度以统一增高，再按固有宽度扩大 Centered 项宽，最大不超过容器宽度 / 项数；
每项增量分别执行整数 / 2 减少留白。Top/无标签的默认 intrinsic 不采用实际胶囊宽度：
DefaultIntrinsicMeasurable 下空 ripple 槽宽度为零；Start 带标签则有显式宽度公式。

Top 使用上方 6 DIP 留白，24 DIP 图标的胶囊为 56×32，图标与标签间为 8 DIP。
Start 带标签时胶囊包住图标和标签，两侧 16 DIP、图标到标签 4 DIP、上下 8 DIP。
无标签仍使用 TopIconOrIconOnlyMeasurePolicy，但采用目标位置的 padding，Top/Start 胶囊高度为 32/40 DIP。
没有改成 Wide Item 的无标签圆形分支。

内容色直接更新，两种位置均使用 LabelMedium。唯一 DefaultSpatial 进度同时驱动指示器透明度及居中展开宽度。
宽度进度只下限钳制，保留过冲；绘制透明度限制在 0～1。目标下一帧提交，反向按 animateAsState
Stop/restart 重置旧速度与帧锚点。位置切换立即布局；不使用 AnimatedNavigationItem 或 Wide 的纵向平滑偏离。

## 使用

Top / EqualWeight 推荐用于小屏，Start / Centered 推荐用于较宽屏。

```xml
<m3:ShortNavigationBar xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
                       Arrangement="Centered">
  <m3:ShortNavigationBarItem Icon="{Binding HomeIcon}" Label="主页" IconPosition="Start"
                            IsSelected="{Binding IsHomeSelected}" Command="{Binding NavigateHome}" />
</m3:ShortNavigationBar>
```

```csharp
var bar = new ShortNavigationBar { Arrangement = ShortNavigationBarArrangement.EqualWeight };
var home = new ShortNavigationBarItem { Icon = icon, Label = "主页", IsSelected = true };
bar.Items.Add(home);
home.Click += (_, _) => NavigateHome(); // 应用维护 IsSelected 及各项互斥。
```

Items / ItemsSource / ItemTemplate 沿用 ItemsControl；模板直接生成 ShortNavigationBarItem。
Icon / Label 接受内容与 DataTemplate，字符串使用 ContentPresenter 的 MaterialText 默认排版。
标签存在就始终显示，没有 AlwaysShowLabel、RailExpanded 或位置动画。
Click / Command / CommandParameter / IsEnabled 使用 Button；Enter 与 Space 在 KeyUp 调用。
自动化提供 TabItem、当前 IsSelected 和所属选择组；Select 只请求导航。
NavigationItem 不清除图标语义，图标与标签均保留可访问子内容。

八项颜色属性与 Wide Item 同名：SelectedIconColor、SelectedTextColorTopIconPosition、
SelectedTextColorStartIconPosition、IndicatorColor、UnselectedIconColor、UnselectedTextColor、DisabledIconColor、DisabledTextColor。
Top 选中标签默认为 Secondary，Start 为 OnSecondaryContainer；禁用内容使用未选中色的 0.38 alpha。
默认 Foreground 使用默认 SurfaceContainer 对应的 OnSurface；自定义 Background 不自动改选内容色，
如需改变可显式设置 Foreground。内嵌 Surface 继承环境的累积 tonal elevation，Short 不新增 TonalElevation 参数。

## Avalonia 适配

复用 VisualAnimationScope / ComposeAnimation、MaterialInteractionController、Surface、MathUtils 和物理 Insets 转换。
整个 Item 矩形可点击与 hover，包括未绘制间隙。Top 按压坐标偏移为
((实际 Item 宽度 − Token 指示器宽度 56 DIP) / 2, 12 DIP)，Start 使用原始 Item 坐标。
不按真实胶囊位置补偿，宽图标仍使用 Token 宽度。

Avalonia 没有 Compose intrinsic API，组件局部测量内容槽来回答对应查询；其他任意内容使用原生 Measure。
固定槽位会隐藏 DesiredSize 变化，局部布局失效通知 Bar 重新查询固有尺寸。
卸载和模板替换取消旧动画。布局需要有限宽度；横向无界宿主需设置 Width 或提供有限容器。
WindowInsets 是物理 Thickness，桌面默认零，RTL 不交换物理边；没有嵌套 inset 消费环境。

2026-10-03 维护者确认保留 Avalonia Width 语义：固定容器宽度，各项按排列分配。
固定 Compose 的直接 Modifier.width / fillMaxWidth 把 minWidth 传给每项，再由 constraints.constrain
放大为整个 Bar 宽度；352 DIP、3 项时三项均为 352 DIP 并溢出。
正常 parity 由外层宿主提供有限最大宽度；static.upstream.direct-width 保留真实 different。
固定参考、容差与比较状态不修改。确认依据见 [DECISIONS](../../../../docs/DECISIONS.md)。

## 验证

独立双端入口为 [ShortNavigationBarCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ShortNavigationBarCases.cs)
和 [ShortNavigationBarCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ShortNavigationBarCases.kt)。
参考调用真实 Compose API，Avalonia 调用产品组件。非文字几何继承实际内容色，没有夹具偏移。
覆盖排列 × 位置 × 标签有无 × 选中 × 禁用，项数边界、余数、窄空间、宽/高图标及标签、
固定高度、暗色、RTL/inset、颜色覆盖、默认内容色和嵌套 Surface；动态覆盖选择、反向、按压、取消、
禁用、主题、键盘、完整 hover 区域、位置与排列切换、标签移除恢复和同一内容尺寸变化。

本次结果见 [移植产物](../../../../artifacts/short-navigation-bar-port/README.md)。
目前完整回归 354 项通过，parity 104 用例 / 326 采样：86 equal、16 withinTolerance、2 different，执行错误和未比较项均为零。
两项 different 为已接受的直接 Width 语义及按压中切换位置后额外 hover；后者内部 indication 重挂方案待确认。
Compose 104 个参考均为本轮新采集，完整比较重新采集 Avalonia；新增场景的双端重复捕获与被动截图自检稳定。
真实文字排版与 MaterialSymbols 栅格、人工桌面交互及屏幕阅读器体验未经验证；当前 parity 排除文字。

```powershell
./tests/VisualParity/parity.ps1 list --family short-navigation-bar
./tests/VisualParity/parity.ps1 prepare-reference --family short-navigation-bar
./tests/VisualParity/parity.ps1 run --family short-navigation-bar
```

直接 width 的已接受差异仍使整族 require-match 返回 1，须读取 summary 与对应采样。
