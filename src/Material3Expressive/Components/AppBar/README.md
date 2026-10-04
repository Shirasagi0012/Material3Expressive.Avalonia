# AppBar

依据 `docs/upstream-baseline.json` 固定的 `AppBar.kt`，AndroidX 与 Core 的该文件相同。
提供七种顶部栏（Top、CenterAligned、Medium、Large、MediumFlexible、LargeFlexible、TwoRows）和
Bottom / FlexibleBottom，以及上游名称的 Defaults、Colors、State、ScrollBehavior / nested connection。

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
Avalonia 对外仍保留上限属性通知，组件内部观察范围遵循上游。12.1.2 会剔除有效 opacity <= .003 的节点；
UI 布局下的标题对极小非零 alpha 使用原生 opacity mask 保留实际透明度，其余值使用完整组透明度。
合成路径目前直接驱动 opacity，仍受该极小透明度剔除限制；这是待解决的视觉差异，不是已批准的曲线改写。

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

## 物理滚动与合成布局

MaterialTheme 自带 ScrollViewer 主题，使用 SmoothScroll 的 ScrollViewerPresenter，不依赖 FluentTheme。
SmoothScroll 子模块通过 ProjectReference 接入，依赖准备方式见[根 README](../../../../README.md#本地开发)。
AppBarScrollScope 将标准 Scaffold 中的实际 TopAppBar / BottomAppBar 与物理主 ScrollViewer
连接到同一 compositor。使用方式是下方的 Scope + behavior + Scaffold.ContentPadding。

指针、直接滚轮和惯性帧共用 SmoothScroll 的常规增量入口，AppBar 是可选的纵向 pre/post 参与者；
一次增量按 pre → 正文 → post 分配。Material 层负责栏的消费规则、状态和布局桥接。

支持内置 Pinned / EnterAlways / ExitUntilCollapsed 和底栏 ExitAlways。内部事务用正向 DIP 表示内容
向前滚动；公开 AppBar 状态继续是负向折叠的物理 px。顶栏只消费实际变化量，剩余量在同一帧交给正文；
底栏观察正文消费量，不重复扣除。正文使用 SmoothScroll 惯性，结束后栏独立 spring 吸附；该路径不采用
behavior.FlingAnimationSpec 的 Compose decay。显式 spring snap 参数和 null 禁用 snap 仍生效。

栏高、标题位置/透明度、背景色、正文位置及裁剪视口由同一组合成输出驱动；底栏关联的 FAB/snackbar
位置随动。UI 线程恢复后，数值状态更新真实 Measure/Arrange、ContentPadding 和 ScrollViewer.Offset。
新 UI 布局成为下一次合成补偿的基准，不把栏的位移写进正文 Offset。直接拖动栏沿用已有 UI 路径。

Avalonia 12.1.2 的适配包括：首次 expression 主动求值、动画停止后恢复客户端基准值、绑定前同步原生
Size/Opacity，及动态纯色 visual 的重绘失效补齐。这些桥接依赖固定版本的 composition 行为，升级时需重验。

自动合成连接要求：

- Scope/栏引用同一个内置 behavior，使用标准栏模板；snap 为默认 spring、显式 SpringSpec 或 null。
- Scaffold 有一个物理主 ScrollViewer，正文根填满 Scaffold 并消费 ContentPadding（见示例）；允许正文内嵌套滚动器。
- 没有祖先 ScrollViewer、额外正文变换或原生 horizontal/vertical snap points。

主正文内的滚动器保留原生 SmoothScroll：内层自己滚动时不驱动外层 AppBar，也不改变主正文视口。
内层到边界后，下一笔滚轮输入按 IsScrollChainingEnabled 自然冒泡到主正文，才驱动正文和栏；
触摸沿用 SmoothScroll 原有捕获接力。单次输入跨边界的剩余量和已启动惯性的余速不额外分配给父层。
内层列表/编辑区保留各自的逻辑单位、虚拟化和编辑交互。

逻辑主正文、嵌套 AppBarScrollScope、自定义 behavior、非 spring snap、不同正文布局及主正文原生 snap points
暂不支持自动 AppBar 联动；正文继续由其 presenter 滚动。
Scope 只持有一个主正文及连接；多个主候选不按视觉树顺序任选。正文内部滚动器及 Scaffold 的栏/覆盖槽不参与主正文选择。
Scope 不修改 ScrollViewer 的模板或主题；自定义模板中实际的 SmoothScroll presenter 在满足相同布局条件时也可接入。
模板/行为/内容切换、detach/re-attach 会重建并释放连接。
自定义 ScrollViewer 模板应使用 SmoothScroll 的 ScrollViewerPresenter；内容等 owner 属性采用 TemplateBinding，
使模板退出时解除内容归属。默认主题及模板绑定说明见 [ScrollViewer](../ScrollViewer/README.md)。
`canScroll` 和 `isScrollingContentAtStart` 在 UI 输入前及布局时求值；UI 忙碌期间沿用最后快照。
直接 Offset、键盘、BringIntoView、滚动条定位和程序化定位动画不进入 pre/post 位移消费协议。

UI 忙碌时，公开状态、滚动条滑块/自动隐藏、辅助功能与命中测试仍依赖 UI 更新；虚拟化新内容不能凭空生成。
合成运动覆盖已呈现的正文和栏。AppBar 参与者接入时不支持缩放及 overscroll，正文在有效范围内夹取；
原生 snap points 场景只保留正文自身滚动，不自动联动 AppBar。

## 显式 UI 协议与直接拖动

AppBarScrollScope 包围屏幕，栏和 Scope 引用同一个 behavior。顶部提供 Pinned/EnterAlways/ExitUntilCollapsed，
底部 ExitAlways。状态/协议为物理 px、速度 px/s；尺寸和 Avalonia Offset 为 DIP/原生逻辑单位。
默认 fling 从挂载的栏或 Scope 读取 density，snap 从 Material motion token 解析；显式 spec 重载允许
分别传 null 禁用 decay/snap。求解使用 ComposeAnimation NuGet，spring 参数保留上游 Float 精度。

显式分发的 pre 从外到内、每层 top→bottom；post 从内到外、每层 bottom→top，connection 返回消费量。
这条协议用于自定义宿主和行为验证，不自动接管原生 viewer 的滚轮、触摸或惯性。
任意滚动宿主可在 UI 线程调用：

```csharp
Vector consumed = scope.DispatchScroll(deltaPx, remaining => ConsumeContentPixels(remaining));
Vector consumedVelocity = await scope.DispatchFlingAsync(velocityPxPerSecond,
    (remaining, token) => FlingContentAndReturnConsumedVelocity(remaining, token), clock, token);
// 内容 inertia 每帧也经 DispatchScroll(..., AppBarScrollSource.SideEffect)。
```

直接拖动栏使用 Core Desktop 的 18 DIP touch slop / .125 DIP mouse slop，并调用 behavior 的 settle。
Lsq2 速度追踪从 Rail 原样提取共用。TouchExplorationEnabled 由宿主接入辅助服务，只禁用底栏直接拖动和
隐藏布局，nested connection 保留，对应上游 activeScrollBehavior 范围。

## 使用

加载 MaterialTheme，每个 TopLevel 加载内容前初始化 VisualAnimationScope.Initialize(window)。
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
Catalog 默认使用物理主正文接入合成滚动；勾选 `Nested scrolling` 加入独立滚动的列表和编辑区，
主正文继续使用合成连接。切换该选项会重置正文和栏的位置。

## 验证边界

验证入口及覆盖范围：

- [AppBarTests](../../../../tests/Material3Expressive.Tests/AppBarTests.cs)：槽、实际模板树、样式/绑定/null/ClearValue、语义与布局。
- [AppBarScrollTests](../../../../tests/Material3Expressive.Tests/AppBarScrollTests.cs)：显式分发协议及栏的直接拖动、吸附与中断。
- [AppBarSmoothScrollTests](../../../../tests/Material3Expressive.Tests/AppBarSmoothScrollTests.cs)：主正文路由滚轮/RawTouch、布局衔接、独立嵌套、逻辑模式、模板/主题切换、迁移及连接生命周期。
- [AppBarCompositionTests](../../../../tests/Material3Expressive.Tests/AppBarCompositionTests.cs)：合成与 UI 布局路径的渲染对照、布局基准更新、分数偏移与断开后的恢复。
- VisualParity 的 [AppBarCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/AppBarCases.cs) / [AppBarCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/AppBarCases.kt)：真实组件的非文字布局/颜色、排列边界、折叠、动态配置、四种 behavior、fling/snap 端点与中断、直接鼠标拖动/fling。参考使用 Expressive motion 和 Core Desktop density-scaled slop；这些用例不替代 SmoothScroll 正文集成检查。

测试覆盖说明不代表当前修订已全部运行。RawTouch 测试不证明系统触摸投递；硬件触摸、文字 glyph/hinting、
真实屏幕阅读器及 UI 阻塞时的辅助功能/命中测试仍需单独验收。

2026-10-05 合入主分支后独立重采双端并检查普通重复及额外被动截图：AppBar 112 例为
110 equal / 2 withinTolerance，428 个采样点的 9,076 项结构化事实全部 equal；
Scaffold 62 例全部 equal，100 个采样点的 1,611 项结构化事实全部 equal。
两套全部重复稳定、执行错误为 0；AppBar 用例结果状态与分支交付的 `smooth-appbar-integration` 相同。
未调整容差，原始图像差异保留。结果分别位于
`artifacts/visual-parity/runs/scroll-capability-appbar-merge-20261005/summary.md` 和
`artifacts/visual-parity/runs/scroll-capability-scaffold-merge-20261005/summary.md`。
SmoothScroll 集成测试及版本核验见 [ScrollViewer](../ScrollViewer/README.md)；上述 parity 不评价正文惯性与 Compose 的曲线一致性。
