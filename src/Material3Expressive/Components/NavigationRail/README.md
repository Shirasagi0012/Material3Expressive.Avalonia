# NavigationRail / NavigationRailItem

常规窄 NavigationRail，包含可选 header 和外部持有选中状态的 NavigationRailItem。
不包含 WideNavigationRail、ModalWideNavigationRail 或应用路由管理。

## 依据与实现

固定版本以 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，Compose Multiplatform Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。两端的
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/NavigationRail.kt`
内容相同；桌面交互、ripple、animate*AsState 的时序按 Core 固定源码核对。

- `NavigationRail.axaml` 组合 MaterialSurface、header、Insets Border 和 ItemsPresenter。
  `NavigationRailLayout` 保留填满有限高度、80 DIP 最小宽、上下各 4 DIP、item 间距 4 DIP、
  header 到第一项 16 DIP 的关系。
- `NavigationRailItemLayout` 翻译 placeIcon / placeLabelAndIcon：独立测量 icon、label、
  动画指示器与完整 ripple 区域；保留实际 icon 尺寸推导 pill、至少 4 DIP 垂直留白、
  受约束布局和按物理像素舍入。selected-only label 在 sizeProgress 为零时不绘制。
- Item 的 icon/text 颜色、指示器透明度和尺寸分别使用 ComposeAnimation Animatable。
  EffectsDefault 控制颜色与透明度，SpatialFast 控制尺寸；尺寸只下限裁到零，保留过冲。
  固定 AnimateAsState 的 SideEffect 对应下一帧目标提交；反向中断先取消旧任务、清零速度与帧锚点。
  detach / 模板替换释放作用域，重挂载按当前目标初始化。
- 复用共享 ripple 与状态层。MappedInteractionSource 按 token 宽度差映射 `(12, 0)`，
  不减去实际 pill 位置。无标签的 pill/ripple 为圆形；有标签使用 ActiveIndicatorShape。
  透明度在独立合成层处理，避免把 graphicsLayer alpha 提前量化进背景或标签画刷。
- `NavigationRailTokenSource` 保留动态 System Token 引用和禁用色 0.38 alpha 派生。
  容器默认内容色按上游 contentColorFor 的角色顺序匹配，未知容器色继承父级 Foreground；
  明确设置 Foreground 具有优先级。Item 的图标/标签颜色与 ripple 的容器环境色各自消费。

## 使用

引入 MaterialTheme，并在 TopLevel 加载内容前调用 `VisualAnimationScope.Initialize(topLevel)`；
准备字体与 Material Symbols 的方式见项目 [README](../../../../README.md)。

```xml
<m3:NavigationRail xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:NavigationRail.Header>
    <m3:IconButton Symbol="HomeAndGarden"
                   Theme="{StaticResource MaterialFilledIconButtonTheme}" />
  </m3:NavigationRail.Header>
  <m3:NavigationRailItem Label="主页" IsSelected="{Binding IsHomeSelected}"
                         Command="{Binding NavigateHome}">
    <m3:NavigationRailItem.Icon>
      <m3:MaterialSymbol Symbol="HomeAndGarden" Width="24" Height="24" />
    </m3:NavigationRailItem.Icon>
  </m3:NavigationRailItem>
  <m3:NavigationRailItem Label="搜索" AlwaysShowLabel="False"
                         IsSelected="{Binding IsSearchSelected}" Command="{Binding NavigateSearch}">
    <m3:NavigationRailItem.Icon>
      <m3:MaterialSymbol Symbol="Search" Width="24" Height="24" />
    </m3:NavigationRailItem.Icon>
  </m3:NavigationRailItem>
</m3:NavigationRail>
```

```csharp
using Material3Expressive.Components;

var home = new NavigationRailItem { Icon = homeIcon, Label = "主页", IsSelected = true };
var search = new NavigationRailItem { Icon = searchIcon, Label = "搜索", AlwaysShowLabel = false };
var rail = new NavigationRail();
rail.Items.Add(home);
rail.Items.Add(search);
search.Click += (_, _) => { home.IsSelected = false; search.IsSelected = true; };
```

Rail 使用 ItemsControl 的 Items / ItemsSource / ItemTemplate；数据模板应生成 NavigationRailItem。
Header、Icon、Label 接受内容对象和对应 DataTemplate；字符串 Label 使用 LabelMedium 字体、
字号、字重、行高与字距，支持换行。显式子控件仍可自行覆盖字体和画刷。

Item 使用 Button 的 IsEnabled、Click、Command、CommandParameter、CanExecute。
鼠标点击或 Space / Enter 松开时请求导航，控件不自动改变 IsSelected，也不自动取消其他 item 的选择。
自动化暴露 TabItem 与外部选中状态，Select 请求 Click；显示 label 时抑制 icon 描述，避免重复朗读。
退选时 icon 语义立即恢复，仍被放置的退场 label 保留语义，直到尺寸进度裁到零。
应用应维护互斥选择，并为无 label 的 icon 提供 AutomationProperties.Name。

| 配置 | 用途 |
| --- | --- |
| Rail Background / Foreground | 容器色与默认内容环境；后者也控制 item ripple 的环境色 |
| Rail WindowInsets | 宿主提供的物理 left/top/right/bottom Thickness；默认零，RTL 不交换物理边 |
| Item AlwaysShowLabel | 默认 true；false 时 label 随选中动画显示，null Label 为纯图标项 |
| SelectedIconColor / SelectedTextColor / IndicatorColor | 选中图标、标签、指示器色 |
| UnselectedIconColor / UnselectedTextColor | 未选中内容色 |
| DisabledIconColor / DisabledTextColor | 禁用内容色，可含 alpha |

Compose 的内容 lambda 映射为 Avalonia 内容对象/模板，WindowInsets 映射为宿主显式提供的 Thickness；
没有 Compose 的嵌套 inset 消费环境。桌面默认 inset 为零。应用可用 StackPanel 等作为单个 header 根内容。
这些入口与内部按压映射已经维护者确认，见 [DECISIONS](../../../../docs/DECISIONS.md)。

## 验证与保留问题

独立双端入口为
[NavigationRailCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/NavigationRailCases.cs) 与
[NavigationRailCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/NavigationRailCases.kt)。
51 个配置包含 34 个静态与 17 个动态用例：三种 label 策略 × selection × enabled，
暗色、header、物理 inset/RTL、独立 item、内容尺寸与宽高约束、七项颜色覆盖、容器内容色匹配/继承/覆盖，
选择/退选/反向中断、两种 motion scheme、hover、按压/取消/快点、按压中禁用、换主题及 Space/Enter。
两端调用真实组件 API，以相同事件帧与 16ms 帧网格捕获；几何 icon/label 按当前 AUTHORING 排除字体栅格因素。
产品仍实现真实文字槽。临时 size-progress 诊断因内部浮点表示差异移除，原诊断产物保留；
最终几何、实际颜色、回调、语义及截图继续验证反向中断。

控件库测试覆盖 Token 完整性、布局、Click/Command、CanExecute、自动化、动态内容色、
模板替换与重挂载；共享交互补充 IconButton 键盘按压中禁用及 ripple 生命周期回归。
Catalog 已接入实际 header、三项导航和显式外部选择处理。

当前验证（2026-10-03，ComposeAnimation `0.2.1-alpha`）：控件库 222/222、parity 框架 101/101，通过；
Catalog Release 构建通过。[完整 Rail 比较](../../../../artifacts/visual-parity/runs/color-converter-0.2.1-alpha-rail/summary.md)
51 个用例、195 个样本，无执行错误或未比较项；49 个 equal，退选与反向中断两个用例 withinTolerance，
`--require-match` 通过。此次完整 suite 为单次双端捕获；
[反向中断双端重复自检](../../../../artifacts/visual-parity/runs/color-converter-0.2.1-alpha-reverse/summary.md)
另行确认 stable 并通过门禁，不将这一项的重复性结论扩展到整套新包捕获。
退选 16ms 差异只在指示器边缘：4× 原始 32 像素不同，最大通道差 7；1× 最大差 1，item 区域均值 0.00223。
反向 160ms 的 4× 截图有 1 个像素的 G=1 差异，1× 比较精确相等。
结构化事实相等，使用已有容差判断，未调整阈值。逻辑结果为
[material-package.trx](../../../../artifacts/color-converter-0.2.1-alpha/tests/material-package.trx) 和
[parity-framework-package.trx](../../../../artifacts/color-converter-0.2.1-alpha/tests/parity-framework-package.trx)。

此前保留的颜色差异已修复：维护者确认后，相邻 ComposeAnimation 的现有 ColorConverter
改为固定上游的 sRGB / XYZ / Oklab 矩阵、Float 边界与 Half / 10 位 alpha 中间存储，
TargetBasedAnimation 保留原始终值。通过本地 NuGet `0.2.1-alpha` 接入，公开颜色 API 不变。
反向 65ms 的实际图标色两端均为 `[46,43,58,255]`，读数与该帧截图精确相等。
依赖完整回归 772/772；独立上游数值、包消费证据和保留的 core 精度边界见
[修复交付报告](../../../../artifacts/color-converter-0.2.1-alpha/README.md)。
原来失败的[历史自检](../../../../artifacts/visual-parity/runs/20261002-183206-242f4921/summary.md)保留，未修改参考基线或失败证据。

未经验证：真实文字/Material Symbols 与 Compose 的栅格一致性、桌面人工操作及实际屏幕阅读器体验。
此次测试不构成这些场景的验收。Wide / ModalWide 属于本次范围之外。

共享交互的 Button parity 回归仍有既有失败：键盘圆角时序、Enter 回调时机、禁用内容色与
elevation / tonal 绘制差异。在修改前 `20261002-171655-937f8736` 与本轮产物中均可观察，
例如 Space 197ms 的圆角 `13.137107849121094 / 11.302458763122559`、Elevated 禁用 400ms
内容色 `[73,69,79,97] / [29,27,32,97]`；未扩展本次范围重写 Button。
新包下 8 个 Button 回归中 baseline-press / quick-click 通过现有容差，其余 6 个仍 different；
98 个样本的事实、完整 raster 记录及已捕获双端 PNG 与此前产物一致。
结果索引为 [button-regressions.json](../../../../artifacts/color-converter-0.2.1-alpha/button-regressions.json)，
逐样本对照见 [button-before-after-evidence.json](../../../../artifacts/color-converter-0.2.1-alpha/button-before-after-evidence.json)。
