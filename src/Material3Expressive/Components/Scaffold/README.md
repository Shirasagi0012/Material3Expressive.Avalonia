# Scaffold

依据 `docs/upstream-baseline.json` 固定版本的 `Scaffold.kt`：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 与 Compose Multiplatform Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的布局逻辑一致。
关键符号为 `Scaffold`、`ScaffoldLayout`、`FabPosition` 与 `ScaffoldDefaults.contentWindowInsets`。

## 范围与行为

`Scaffold : ContentControl` 提供正文、TopBar、BottomBar、SnackbarHost、FloatingActionButton
五个内容槽及各自 DataTemplate。它只组合和布局内容，不实现 TopAppBar、FAB 按钮、SnackbarHostState 或消息队列。
槽内可以使用已有组件，也可以提供任意内容控件或数据模板。

- 在有限的宽高约束下填满可用尺寸。和上游 SubcomposeLayout 一样，不支持 intrinsic/wrap-content
  测量；放在垂直 ScrollViewer/StackPanel 中时显式设置 Height，水平无限约束同理设置 Width。
- 测量顺序为 TopBar → Snackbar → FAB → BottomBar → 正文。所有槽使用零最小约束，只有
  Snackbar/FAB 的最大约束扣除剩余水平和底部 Insets；正文保留整个 Scaffold 的可用尺寸。
- 在测量正文前发布只读 ContentPadding。正文默认位于原点，不自动套 Padding。
  顶/底槽存在时使用实际槽高度，零高度槽也替换对应 Insets；空槽使用剩余 Insets。
- FAB 的 Start / Center / End / EndOverlay、16 DIP 留白、RTL 物理坐标以及 Snackbar 避让直接对应上游。
  FAB 任一测量维度为零时不参与避让；EndOverlay 的底部距离使用 Insets，不使用 BottomBar 高度。
- 绘制顺序为正文 → TopBar → Snackbar → BottomBar → FAB；模板树顺序保留
  TopBar → 正文 → Snackbar → FAB → BottomBar 的键盘遍历。Scaffold 是不合并子内容的无障碍 Group。
- MaterialSurface 承担实际绘制与裁剪；Background 默认引用 Sys.Color.Background，Foreground
  动态使用共享 contentColorFor 匹配或环境内容色。显式 Foreground 和资源/主题更新保持原生属性优先级。
- 布局计算在物理整数像素中执行，DIP 换算、居中整除和 RTL 转换保留固定上游规则。
  Scaffold 自身没有动画，内容槽中的组件继续拥有自己的动画和生命周期。

## 使用

```xml
<m3:Scaffold x:Name="Screen" ContentWindowInsets="8,24,12,16"
             FloatingActionButtonPosition="End">
  <m3:Scaffold.TopBar>
    <!-- Bar 自己负责 Insets 和所需宽高。 -->
    <YourTopBar />
  </m3:Scaffold.TopBar>
  <m3:Scaffold.BottomBar>
    <m3:NavigationBar><!-- destinations --></m3:NavigationBar>
  </m3:Scaffold.BottomBar>
  <m3:Scaffold.FloatingActionButton><YourFab /></m3:Scaffold.FloatingActionButton>
  <Border Padding="{Binding ContentPadding, ElementName=Screen}"
          m3:MaterialWindowInsets.ConsumePadding="{Binding ContentPadding, ElementName=Screen}">
    <YourContent />
  </Border>
</m3:Scaffold>
```

```csharp
var screen = new Scaffold
{
    TopBar = topBar,
    BottomBar = navigationBar,
    FloatingActionButton = fab,
    FloatingActionButtonPosition = FabPosition.End,
    ContentWindowInsets = new Thickness(8, 24, 12, 16),
};
var body = new Border { Child = content };
body.Bind(Border.PaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty));
body.Bind(MaterialWindowInsets.ConsumePaddingProperty,
    screen.GetObservable(Scaffold.ContentPaddingProperty));
screen.Content = body;
```

ContentWindowInsets 是物理 left/top/right/bottom；ContentPadding 与 ConsumePadding 是逻辑
start/top/end/bottom，可直接绑定 Avalonia Border.Padding。使用 ScrollViewer 时，将应用和消费
Padding 的容器放在滚动内容内。Scaffold 上继承的 Padding 不是 ContentPadding 的替代入口。
模板接入见 `Scaffold.axaml`，Catalog 提供位置、Bars、Snackbar 和 RTL 的切换示例。

## 适配与确认

2026-10-03，维护者选择“采用推荐 API 和共享 ConsumePadding”：

- Compose lambda 映射为 ContentControl 内容槽及模板；稳定的 ContentPadding 属性在每次
  测量前更新，不为 Padding 变化重建正文。内容使用 Avalonia 自身的布局属性。
- 宿主提供物理 Thickness，桌面默认零；不自动接管 Android 系统栏/IME。共享
  MaterialWindowInsets.ConsumePadding 对应 `consumeWindowInsets(PaddingValues)`，逐层累加
  已应用的逻辑 Padding，并向后代传播物理消费量，Scaffold 逐边排除并钳制到零。
  它只传播消费事实，不应用 Padding，也不扩大为完整的系统 Insets 框架。
- 动态内容色复用已确认的 ContentColorFor，绘制复用 MaterialSurface；槽 API 与继承的
  Background/Foreground 保留 C#/XAML 的标准绑定和模板使用方式。

共享入口及决定见 [架构说明](../../../../docs/ARCHITECTURE.md)和
[共享决定](../../../../docs/DECISIONS.md#padding-消费向嵌套-scaffold-传播)。

## 验证入口与边界

- `tests/Material3Expressive.Tests/ScaffoldTests.cs`：测量/Padding 时机、FAB 四位置/RTL、
  槽替换与模板、约束、零尺寸、嵌套 Insets、重挂载、实际命中层级、键盘与无障碍树。
- `tests/VisualParity/Material3Expressive.Parity/Cases/ScaffoldCases.cs` 与参考端同名 Kotlin 文件：
  62 个无文字用例，覆盖位置 × BottomBar × RTL、16 种槽存在组合、零尺寸、受限空间、
  小数取整、叠放、嵌套消费、颜色，以及槽/Insets/方向/主题/尺寸动态变化。
  参考直接调用固定依赖中的真实 Scaffold。
- 叶子夹具的 desired size 对应 Compose `Modifier.size`；不使用在排列时重新应用的原生
  Width/Height 冒充这一语义，也不计算或补偿 Scaffold 的 Padding、位置或约束。

实际运行结论与产物路径由交付报告给出。文字排版不在当前 parity 覆盖内；
原生桌面交互、屏幕阅读器和真实系统 Insets 接入需要应用环境验证，headless 结果不能替代这些验收。
