# NavigationBar / NavigationBarItem

常规底部导航条，按固定 Compose 的 `NavigationBar.kt` 移植。`ShortNavigationBar` 是独立组件，
不属于此目录的实现范围。建议放置 3–5 个目的地，数量不作强制限制。

## 依据与实现

基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`、Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/NavigationBar.kt`
内容相同。布局同时对照 Foundation 的 `RowColumnMeasurePolicy` 和 `Arrangement.SpacedAligned`。

关键实现：[NavigationBar.cs](NavigationBar.cs)、[NavigationBarItem.cs](NavigationBarItem.cs)、
[NavigationBarItemsPanel.cs](NavigationBarItemsPanel.cs)、[NavigationBarItemLayout.cs](NavigationBarItemLayout.cs)、
[NavigationBar.axaml](NavigationBar.axaml) 与 [NavigationBarTokenSource.cs](NavigationBarTokenSource.cs)。

- 模板组合 MaterialSurface、物理 Insets 和 ItemsPresenter。有限宽度默认填满，默认高度 80 DIP；
  `TonalElevation` 使用入口的 Level0，未将生成 Token 中未使用的 Level2 代入入口。
  Surface 保留累积 tonal elevation 及仅对主题 Surface 色施加 tint 的规则。
- NavigationBarItem 等权分配扣除 8 DIP 间距后的宽度，按上游 float 权重舍入，将余数逐个分给前面的项。
  数据模板生成的 Item 使用同样的布局。其他内容保留普通 Row 内容的无权重测量，居中排列在交叉轴上。
- ItemLayout 翻译独立 icon / label / indicator / ripple 测量及 placeIcon / placeLabelAndIcon。
  胶囊尺寸由实际 icon 尺寸加 Token padding 推导；纯图标项也使用 32 DIP 指示器高度。
  指示器选择动画从中心扩展。选中态标签与 icon 的组合居中，最少保留 4 DIP 垂直 padding；
  selected-only 标签随 icon 移动并淡入，尺寸进度为零时不绘制。
- icon/text 颜色与指示器 alpha 使用 DefaultEffects，尺寸使用 FastSpatial；保留 spring 过冲。
  使用 ComposeAnimation `0.2.1-alpha`、下一帧目标提交、取消旧 AnimateAsState 任务后清零速度的重启动作。
  detach 和模板替换释放作用域，重挂载按当前选中/禁用状态初始化。
- 整个 Item 矩形参与输入，包括胶囊与标签的空隙；反馈只在胶囊区域绘制。
  复用已有共享交互控制器，按实际 Item 宽度与 Token 指示器宽度映射 `((itemWidth − 56) / 2, 12)`。
  这一偏移不由实际 ripple 位置、实际 icon 尺寸或标签位置反算。
- 状态颜色及 LabelMedium TextStyle 保留动态 System Token 引用，禁用色按 0.38 alpha 派生。
  容器 contentColorFor 角色匹配与未知色的父级 Foreground 回退使用 Foundation 的共享绑定，
  显式 Foreground 具有优先级。布局计算消费现有 MathUtils。

## 使用

加载 MaterialTheme，并在加载 TopLevel 内容前调用 `VisualAnimationScope.Initialize(topLevel)`。

```xml
<m3:NavigationBar xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
                  WindowInsets="0,0,0,16">
  <m3:NavigationBarItem Label="主页" IsSelected="{Binding IsHomeSelected}"
                        Command="{Binding NavigateHome}">
    <m3:NavigationBarItem.Icon>
      <m3:MaterialSymbol Symbol="HomeAndGarden" Width="24" Height="24" />
    </m3:NavigationBarItem.Icon>
  </m3:NavigationBarItem>
  <m3:NavigationBarItem Label="搜索" AlwaysShowLabel="False"
                        IsSelected="{Binding IsSearchSelected}" Command="{Binding NavigateSearch}">
    <m3:NavigationBarItem.Icon>
      <m3:MaterialSymbol Symbol="Search" Width="24" Height="24" />
    </m3:NavigationBarItem.Icon>
  </m3:NavigationBarItem>
</m3:NavigationBar>
```

```csharp
var home = new NavigationBarItem { Icon = homeIcon, Label = "主页", IsSelected = true };
var search = new NavigationBarItem { Icon = searchIcon, Label = "搜索", AlwaysShowLabel = false };
var bar = new NavigationBar();
bar.Items.Add(home);
bar.Items.Add(search);
search.Click += (_, _) => { home.IsSelected = false; search.IsSelected = true; };
```

Items / ItemsSource / ItemTemplate 是普通 ItemsControl 入口，ItemTemplate 应生成 NavigationBarItem。
Icon / Label 接受内容对象和相应 DataTemplate。字符串标签使用 LabelMedium，可换行；
显式控件可覆盖自身排版与画刷。Label 为 null 时走纯图标布局。

Item 复用 IsEnabled、Click、Command、CommandParameter、CanExecute；点击或 Space / Enter 松开时请求导航。
选中状态由应用持有，需由应用维持互斥选择。自动化暴露 TabItem、选中状态及选择容器；
Select 请求 Click。标签显示时抑制 icon 子树描述，退选后仍放置的标签保留语义至其退出。
无标签目的地应设置 AutomationProperties.Name。

七项颜色入口为 SelectedIconColor、SelectedTextColor、IndicatorColor、UnselectedIconColor、
UnselectedTextColor、DisabledIconColor、DisabledTextColor。Background / Foreground 控制容器及其内容环境，
后者也供 ripple 消费；Item 图标与标签分别消费状态颜色。

Compose 内容 lambda 映射为 Avalonia 内容槽与模板，WindowInsets 为宿主提供的物理 Thickness，
桌面默认零；RTL 不交换其物理边。没有 Compose 的嵌套 inset 消费环境。
这些入口保留 Compose RowScope 的内容组合与 selectable 的外部选择语义，同时支持 C#/XAML。
2026-10-03，维护者确认“采用推荐 API 与内部共享适配”；范围为常规 NavigationBar / NavigationBarItem，
不决定 ShortNavigationBar 的入口，不扩展 Wide 的纵向平滑偏离。
contentColorFor 与物理 Insets 的共享职责见[架构说明](../../../../docs/ARCHITECTURE.md#surface-与交互组合)。

## 验证范围

双端用例位于 [NavigationBarCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/NavigationBarCases.cs)
及 [NavigationBarCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/NavigationBarCases.kt)。
60 个静态配置覆盖 3/4/5 项 × 三种标签策略 × 选中 × 禁用，以及主题、物理 Insets / RTL、
空容器、单项、余数像素、狭窄宽度、实际内容尺寸、高度约束、颜色覆盖、内容环境和累积 tonal elevation。
20 个动态用例覆盖选择/退选/反向、Standard Motion、hover 全矩形命中、按压/取消/快点、
按压中禁用、禁用输入、主题切换、Space / Enter、RTL 和非标准 icon 尺寸的按压映射。
两端调用真实组件，使用相同输入顺序、事件帧及连续 16ms 帧网格。

2026-10-03 的[完整双端自检](../../../../artifacts/visual-parity/runs/navigation-bar-initial/summary.md)
共 80 个用例、254 个样本，77 个用例 equal、3 个 withinTolerance；结构化事实全部相等，
无执行错误、未比较项或自检不稳定，`--require-match` 通过。
Compose 与 Avalonia 均在本轮重新采集并重复自检，未复用旧截图、未只做重比较。
3 个原始图像差异为：退选 16ms 的 32 个边缘像素（最大通道差 4）、按压中禁用 65ms 的
2 个像素（最大差 1）、RTL 按压 400ms 的 2 个像素（最大差 1）。1× 比较的最大差分别为 1、1、0，
保留原始证据，使用既有容差，未修改比较规则。

控件库[完整回归](../../../../artifacts/navigation-bar-port/tests/material-regression.trx) 289/289 通过，
其中 NavigationBar 16 项覆盖布局、数据模板、Token、Click / Command / CanExecute、自动化及动画清理。
Catalog Release 构建通过。共享绑定变动后的 5 个 Rail / Wide / Modal 双端自检也通过既有容差且重复性稳定；
运行路径索引见[测试产物说明](../../../../artifacts/navigation-bar-port/README.md)。

后续回归入口：

```powershell
./tests/VisualParity/parity.ps1 prepare-reference --family navigation-bar
./tests/VisualParity/parity.ps1 run --family navigation-bar --require-match
```

固定参考实现与配置未变时直接执行 run，无需重复准备参考。

当前 parity 规范使用非文字几何内容验证槽布局、实际继承颜色和绘制。
真实文字 / Material Symbols 的双端排版与栅格一致性、原生桌面人工操作、屏幕阅读器体验不属于这轮自动验证。
产品保留真实文字入口，Catalog 展示真实标签及图标。
