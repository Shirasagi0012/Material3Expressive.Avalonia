# AppBar

依据 `docs/upstream-baseline.json` 固定的 `AppBar.kt`，AndroidX 与 Core 的该文件相同。
提供七种顶部栏（Top、CenterAligned、Medium、Large、MediumFlexible、LargeFlexible、TwoRows）和
Bottom / FlexibleBottom，以及上游名称的 Defaults、Colors、State、ScrollBehavior / nested connection。
2026-10-04 维护者确认 AppBarScrollScope、ItemsControl actions、双标题生成模板和下述槽规则。

## 槽与布局

`Items / ItemsSource / ItemTemplate` 是动作行；`NavigationIcon`、`Title`、`Subtitle` 及对应模板是独立槽。
标题 Left/Right 表达逻辑 Start/End，Center 根据整栏宽度对齐再避让两侧槽。
动作沿上游 Row 的剩余宽度逐项测量。双行控件生成独立的标题树；直接 Control 须另提供折叠 Control
或生成模板，一个 Control 不能同时占据两行。

未设置 `CollapsedTitle / CollapsedSubtitle` 时回退到展开数据，显式 null 清空折叠槽。
未设置折叠模板时回退到展开模板，显式 null 取消模板回退。显式 null 的数据也取消隐式模板回退，
但可显式提供处理 null 的折叠模板。规则根据属性的实际有效值，支持 Binding、Style 和 ClearValue。

普通 Medium/Large 上游入口没有副标题；副标题及可选对齐对应带副标题的 Small、Flexible、TwoRows。
双行控件的 ContentPadding 不改变其固定双行布局。默认小栏/折叠行 64 DIP，Medium 112、Large 152；
MediumFlexible 112/136、LargeFlexible 120/152（无/有副标题），TwoRows 默认同 MediumFlexible。
双行高度须有限且展开高度不小于折叠高度；NaN/正无穷按上游未指定高度使用默认值。

Automation 将栏与标题声明为 Group；fraction < .5 只暴露展开标题，否则只暴露折叠标题，
导航/动作保留普通控件语义。上游 heightOffsetLimit 是普通字段，其 onSizeChanged 写入不主动使标题失效；
后续被观察的 offset 或配置更新才重读标题 fraction，容器 drawBehind 可当前帧读取新值。
Avalonia 对外仍保留上限属性通知，组件内部观察范围遵循上游。12.1.2 会剔除有效 opacity <= .003 的节点；极小非零标题 alpha
使用原生 opacity mask 保留实际透明度，其余值使用完整组透明度，不改写曲线或比较容差。

Bottom 默认动作项与可选 `FloatingActionButton / FloatingActionButtonTemplate`；FAB 顶部留 8、末端留 12 DIP。
CustomContent 或其模板非 null 时替换整个动作/FAB 区域，保留外部 padding、insets、背景、tonal elevation
与隐藏布局。默认槽保留数据，失活时不参与布局、输入、Tab 或 automation，清空自定义槽后恢复；模板优先。
Bottom 默认高度 80、逻辑 padding 4/4/4/0；Flexible 默认 64、16/0/16/0。
HorizontalArrangement 支持 Start/Center/End/SpaceBetween/SpaceAround/SpaceEvenly/SpacedBy；最后一种用 Spacing
（允许负间距）。自定义根自行决定内部排列。Flexible 继承基础槽；上游 Flexible 入口的 FAB 可放入自定义内容。

隐藏底栏按上游缩小外部布局盒、在 (0,0) 放置完整 Surface；浮置栏可画到布局盒之外，Scaffold 停靠由窗口边缘裁剪。
顶部直接拖动执行 heightOffset += delta，底部执行 -= delta；释放速度保留上游传入 settle 的原符号。
WindowInsets 是物理 left/top/right/bottom Thickness，桌面默认零；ContentPadding 为逻辑 start/top/end/bottom。
整数布局使用实际 density，MaterialWindowInsets.ConsumePadding 扣除祖先已消费的 inset。

## 滚动与模板

AppBarScrollScope 包围屏幕，栏和 Scope 引用同一个 behavior。顶部提供 Pinned/EnterAlways/ExitUntilCollapsed，
底部 ExitAlways。状态/协议为物理 px、速度 px/s；尺寸和 Avalonia Offset 为 DIP/原生逻辑单位。
默认 fling 从挂载的栏或 Scope 读取 density，snap 从 Material motion token 解析；显式 spec 重载允许
分别传 null 禁用 decay/snap。求解使用 ComposeAnimation NuGet，spring 参数保留上游 Float 精度。

pre 从外到内、每层 top→bottom；post 从内到外、每层 bottom→top。原生内嵌 viewer 在边界向祖先传递
剩余位移和速度；connection 返回消费量。行为替换、Scope detach、模板替换、新输入取消旧惯性/吸附。
Scope 只适配归它直接拥有的 viewer，嵌套作用域不会重复登记同一 viewer。

核验 Fluent 12.1.2 标准模板身份后，Scope 使用保留全部原生部件/绑定的模板，其中 PART_ContentPresenter
为 AppBarScrollContentPresenter。两个 ScrollBar、键盘、BringIntoView、水平/Shift 滚动和 native snap point
收尾仍走原生控件。适配只在 Scope 内启用；Template/ControlTheme/Style 提供的自定义模板不被覆盖，
清除覆盖可恢复标准适配。自定义模板可显式使用公开 AppBarScrollContentPresenter（保留原生 theme/bindings/parts）。
标准适配使用 SetCurrentValue 保留模板的值来源和优先级；后来加入的普通 Style 或更新的 Binding 仍可替换它。
模板须把 Background 绑定到 presenter，使空白正文区域参与原生命中测试，例如：

```xml
<m3:AppBarScrollContentPresenter Name="PART_ContentPresenter"
    Background="{TemplateBinding Background}" Padding="{TemplateBinding Padding}"
    ScrollViewer.IsScrollInertiaEnabled="{TemplateBinding IsScrollInertiaEnabled}" />
```

这是 presenter 接入片段；完整原生部件、snap 绑定和水平 recognizer 见同目录 AppBarScrollScope.axaml。
任意滚动宿主可在 UI 线程调用公开协议：

```csharp
Vector consumed = scope.DispatchScroll(deltaPx, remaining => ConsumeContentPixels(remaining));
Vector consumedVelocity = await scope.DispatchFlingAsync(velocityPxPerSecond,
    (remaining, token) => FlingContentAndReturnConsumedVelocity(remaining, token), clock, token);
// 内容 inertia 每帧也经 DispatchScroll(..., AppBarScrollSource.SideEffect)。
```

标准 viewer 保留 Avalonia 的 50 DIP wheel step、逻辑单位、Shift 与 RTL；不宣称 wheel 步长与 Compose 平台相同。
垂直 touch/pen 用原生 GestureRecognizer/capture 接入，保留边界识别与释放速度，正文惯性走 Compose spline；
水平保留原生 recognizer。12.1.2 关闭的 inertia-starting 事件构造函数用局部 UnsafeAccessor 接通 snap point
生命周期，升级时须复核。直接拖动使用 Core Desktop 的 18 DIP touch slop / .125 DIP mouse slop。
Lsq2 速度追踪从 Rail 原样提取共用。TouchExplorationEnabled 由宿主接入辅助服务，只禁用底栏直接拖动和
隐藏布局，nested connection 保留，对应上游 activeScrollBehavior 范围。

## 使用

先加载 FluentTheme、MaterialTheme，每个 TopLevel 加载内容前初始化 VisualAnimationScope.Initialize(window)。
ViewModel 提供 TopBehavior = TopAppBarDefaults.ExitUntilCollapsedScrollBehavior(new TopAppBarState())，
BottomBehavior = BottomAppBarDefaults.ExitAlwaysScrollBehavior(new BottomAppBarState())。

```xml
<m3:AppBarScrollScope TopScrollBehavior="{Binding TopBehavior}" BottomScrollBehavior="{Binding BottomBehavior}">
  <m3:Scaffold x:Name="Screen">
    <m3:Scaffold.TopBar>
      <m3:MediumFlexibleTopAppBar Title="{Binding Title}" Subtitle="{Binding Subtitle}" ScrollBehavior="{Binding TopBehavior}">
        <m3:MediumFlexibleTopAppBar.TitleTemplate>
          <DataTemplate><TextBlock Text="{Binding}" /></DataTemplate>
        </m3:MediumFlexibleTopAppBar.TitleTemplate>
        <m3:IconButton Symbol="Search" />
      </m3:MediumFlexibleTopAppBar>
    </m3:Scaffold.TopBar>
    <m3:Scaffold.BottomBar>
      <m3:BottomAppBar ScrollBehavior="{Binding BottomBehavior}"><m3:IconButton Symbol="Favorite" /></m3:BottomAppBar>
    </m3:Scaffold.BottomBar>
    <Border Width="{Binding Bounds.Width, ElementName=Screen}" Height="{Binding Bounds.Height, ElementName=Screen}"
            Padding="{Binding ContentPadding, ElementName=Screen}"
            m3:MaterialWindowInsets.ConsumePadding="{Binding ContentPadding, ElementName=Screen}">
      <ScrollViewer><!-- screen content --></ScrollViewer>
    </Border>
  </m3:Scaffold>
</m3:AppBarScrollScope>
```

Width/Height 绑定表达正文根 fillMaxSize；Scaffold 不自动给正文加 padding。
复杂副标题数据应另提供 SubtitleTemplate。`bar.CollapsedTitle = null` 清空折叠标题，
`bar.ClearValue(TopAppBar.CollapsedTitleProperty)` 恢复回退。Catalog 的 AppBar 按钮或 `--app-bars` 展示组件。

## 验证边界

AppBarTests 验证槽、实际模板树、样式/绑定/null/ClearValue、语义与布局；AppBarScrollTests 验证真实 RawTouch/
native wheel、消费顺序、真实剩余速度、嵌套/迁移、嵌入控件、上下栏 Scaffold、惯性/吸附/中断与 1×/2× density。
VisualParity 的 AppBarCases.cs / AppBarCases.kt 独立调用真实组件，覆盖非文字布局/颜色、排列边界、折叠、
动态配置、四种 behavior、fling/snap 端点与中断、直接鼠标拖动/fling。参考使用 Expressive motion 和
Core Desktop density-scaled slop。文字 glyph/hinting、真实屏幕阅读器和硬件触摸不由这些夹具证明。
桌面输入另见 tests/DesktopInput/AppBarInputProbe；RawTouch 模式验证 Win32 后端、实际 DPI、实时帧与渲染器，
不等于 OS WM_POINTER 投递或硬件触摸。OS 模式先核验命中自己的窗口，锁屏覆盖时报失败并停止注入。
本次执行结果及剩余差异见交付产物，不能以组件 parity 代替桌面集成。
