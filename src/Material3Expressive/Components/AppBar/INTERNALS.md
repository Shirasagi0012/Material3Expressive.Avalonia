# AppBar 实现说明

本文面向维护者；使用入口见 [README.md](README.md)。固定基线以
[upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 1.5.0-alpha27），
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（v1.13.0-alpha01）。
默认逻辑对照 AndroidX，桌面输入与平台实现对照 Core，不用浮动 HEAD 替代。

## AppBarRow / AppBarColumn

`AppBarOverflowIndicator` 对应固定 `AppBarDsl.kt` 的同名公开函数；两仓固定实现相同。
组合现有 TooltipBox、PlainTooltip 和 IconButton，显式绑定现有 AppBarMenuState，不从 DataContext 隐式查找状态。
默认行/列模板也使用该入口。内部 MoreVert 路径直接翻译固定 `internal/Icons.kt`，不再使用字体图标代替。
Shape 与 InteractionSource 转发已有能力；四个可空颜色属性表达 IconButtonColors 的四个通道，
空值保留透明容器及环境内容色关系，禁用默认 alpha 是替换而非相乘。点击读取当时的 MenuState，
换模板退订旧按钮，卸载的 Tooltip/交互源清理由已有组件负责。
固定上游 enabled 只交给内部 IconButton。Avalonia 默认会把包装层 IsEnabled 传播到 Tooltip，
因此该非交互包装层的 IsEnabledCore 保持 true，并将 IsEnabled 显式转交按钮；颜色读取按钮实际启用状态。
包装层无激活动作，真实按钮提供 disabled 自动化语义；祖先禁用仍按 Avalonia 继承禁用整个组合。

本轮维护者已授权新增组件 API 和必要职责调整，因此把原先仅供默认模板使用的内部按钮提升为公开组合入口。
默认提示继续为英文，新增 Description 可显式本地化；没有新增通用颜色或菜单状态框架，没有公共破坏性迁移。
验证入口为 AppBarOverflowIndicatorTests 和双端 `appbar-overflow-indicator`；实际结果见该族报告。

`overflow-indicator-current` 的 42 例、354 个样本均成功采集，布局和菜单/启用状态读数全部相等；
18 个静态配置与 4 个外部 hover/focus/press 时间线均在既有图像容差内且重复稳定。
总图像分类为 26 withinTolerance、16 different。真实提示保留英文与固定 Roboto：已检查禁用 hover 的中间帧，
两端均显示提示且不能激活菜单，但文字栅格仍超差；换交互源的 65ms 当帧旧波纹保留范围也不同。
Core 的 8 个真实点击/状态替换用例在重复或额外截图中有波动；Avalonia 全部稳定。
这些差异没有被接受为一致，也没有放宽容差。控件行为回归 25 项通过，覆盖真实键盘/指针、
局部与祖先禁用、独立提示、换状态、四通道颜色、换源、换模板与卸载重挂。

固定基线的 AppBarRow.kt、AppBarColumn.kt、AppBarDsl.kt 在 AndroidX 与 Core 的 Git blob
逐一相同。`AppBarOverflowLayout` / `AppBarItemsPanel` 对应 `OverflowMeasurePolicy`：
先预留指示器主轴尺寸，内容按完整 loose constraints 逐个测量，在第一个不合适的项目处停止；
最后一个项目可取回预留空间。MaxItemCount 保留源码的 `i == maxItemCount - 1` 分支，
不额外限制成正数；0 / 负数因该分支不命中而只受尺寸限制。像素舍入消费现有 MathUtils。
行和列仅交换主 / 交叉轴，使用 Avalonia 的 RTL 子树镜像对应 placeRelative。
当栏的约束小于指示器时，上游仍以预留尺寸测量指示器，栏自身尺寸服从约束，指示器可画到栏外。
主题明确关闭 TemplatedControl 默认裁剪以保留这条边界行为。

2026-10-11 维护者确认 ItemsControl + 三种项目、受控菜单状态、可替换指示器模板及公开
IconToggleButton，并随后要求遵循统一图标 API。因此 AppBarAction / AppBarToggleAction 的
图标采用 Icon / IconTemplate；独立 IconToggleButton 作为 IconButton 家族保留 Symbol。
上游 DSL 映射到 Items / ItemsSource / ItemTemplate，数据模板根为 AppBarItem；没有第二套
项目模型或命令系统。两端呈现必须分别生成，避免一个 Avalonia Control 拥有两个视觉父节点。

`AppBarSlot` 包装原生生成容器：不放置的项目隐藏包装层，内容仍可测量并保持原始 IsVisible。
这样隐藏项目不参与输入、Tab 或 automation，扩大尺寸时仍能重新测量原树。
Avalonia 无 Compose intrinsic API；指示器按交叉轴原约束、主轴无界测量其 DesiredSize 作为预留，
最终再按剩余约束测量。自定义内容的显式 Width / Height 仍遵循 Avalonia 的强制尺寸语义；
需要 Compose `size` 的受约束首选尺寸时，应通过原生 MeasureOverride 表达。测试用同义首选尺寸
内容，不用显式 Width / Height 的溢出画面冒充 Compose size()。

菜单复用独立 DropdownMenu 的定位、退出保留、真实输入与动画。不复制弹层或动画引擎。
溢出后缀在布局变化时更新，即使栏最终 DesiredSize 相同也更新菜单。释放菜单内容前先解除
Content / ContentTemplate，避免旧的 detached presenter 保留直接 Control 的视觉父节点。
模板替换解除旧 MenuState 订阅及菜单内容，状态对象属于栏实例，重挂后可恢复显示目标。

便捷动作的菜单内容为 baseline 纯文字 DropdownMenuItem，上游 toggleableItem 在菜单中也
不绘制勾选或图标。事件与 Command 执行后再 Dismiss；回调抛异常时不抢先关闭。
底层 Button 保留原生 CanExecute 订阅，但通过内部只转发 CanExecute 的 ICommand 避免原生
Click 之后再次执行命令。自定义菜单不隐式关闭。状态变更通知由调用方写回，模板不自动切换。

便捷动作和默认指示器的 Standard 颜色使用实际 ambient 前景；toggle 选中使用 Primary，
禁用使用 ambient RGB 与 disabled opacity。颜色是固定 IconToggleButtonColors 的
rememberUpdatedState 路径，不添加颜色动画。ripple 的调用在图标内容色 provider 外，因此
选中时 ink 仍读取父级 ambient，不随图标变为 Primary。状态层和 ripple 复用已有 MaterialSurface /
MaterialInteractionController；独立 IconToggleButton 的 Checkbox automation 请求同一受控回调。
原有 IconButton 四主题没有因此整体迁移 ambient 颜色策略。

验证入口：AppBarOverflowTests、Fixtures/AppBarOverflow.axaml；VisualParity 的
AppBarOverflowCases（appbar-overflow）和 IconToggleButtonCases（icon-toggle-button）。
用例独立调用真实 Compose AppBarRow / AppBarColumn / IconToggleButton，覆盖两轴、RTL、
尺寸/数量边界、运行中缩放、开闭、反向、外部关闭及受控回调。非文字图形参与正常布局，
菜单动画沿用真实 DropdownMenu；字体图标的矢量替代仅用于测试绘制，不修改产品模板尺寸。

未经验证：OS 桌面实际输入、屏幕阅读器播报、Label / Tooltip 文字栅格、任意用户模板的
intrinsic 等价性。继承的 Material 焦点环、任意 Compose Shape 和外部 InteractionSource
能力仍以 IconButton / Tooltip / Menu 文档的缺口为准，不能由本组件测试认定已补齐。

## 顶栏和底栏职责

主要入口是固定提交的 [AndroidX AppBar.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/AppBar.kt)
及 [Core AppBar.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/AppBar.kt)。
下文 Material3 路径均以 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/` 为前缀。

原记录核对两个固定仓库的 AppBar.kt 相同，直接拖动的桌面输入分支另按固定 Core 读取。

| 上游符号 / 文件 | 本地职责 |
| --- | --- |
| TopAppBar、CenterAlignedTopAppBar、Medium/Large、Flexible、TwoRows 入口 | [TopAppBar.cs](TopAppBar.cs)、[AppBarTopLayout.cs](AppBarTopLayout.cs)、[AppBar.axaml](AppBar.axaml)：槽、标题树、测量/摆放和视觉桥接 |
| BottomAppBar、FlexibleBottomAppBar | [BottomAppBar.cs](BottomAppBar.cs)、[AppBarBottomLayout.cs](AppBarBottomLayout.cs)：动作/FAB/自定义内容、排列与隐藏布局 |
| TopAppBarState / BottomAppBarState、行为配置基线 | [AppBarState.cs](AppBarState.cs)、[AppBarTokenSource.cs](AppBarTokenSource.cs)、[组件 Token](../../Tokens/MaterialTokens.Comp.AppBar.cs) |
| ScrollBehavior、settleAppBar | [AppBarScrollBehavior.cs](AppBarScrollBehavior.cs)、[AppBarDirectDrag.cs](AppBarDirectDrag.cs)、[AppBarVerticalGesture.cs](AppBarVerticalGesture.cs) |
| nestedScrollConnection、显式嵌套分发 | [测试协议](../../../../tests/Shared/AppBar/AppBarNestedScrollConnection.cs)、[测试分发](../../../../tests/Shared/AppBar/AppBarScrollScopeExtensions.cs)：仅驱动测试正文 |
| 本地原生滚动接入 | [AppBarScrollScope.cs](AppBarScrollScope.cs)、[AppBarSmoothScrollConnection.cs](AppBarSmoothScrollConnection.cs)：主视口筛选、接入/释放与布局衔接 |
| 本地合成参与者 | [AppBarScrollParticipant.cs](AppBarScrollParticipant.cs)、[AppBarCompositionVisuals.cs](AppBarCompositionVisuals.cs)：DIP 事务与物理状态桥接、合成输出 |

## 配置组织试点（2026-10-05）

维护者本轮授权将默认配置接到主题 / Avalonia 属性，并确认：删除 nullable `TopAppBarColors` 包装，
普通容器复用 Background，其余五槽采用独立 Color 属性；Top Background 的 null 表示透明，非纯色明确报错。
旧实现忽略 null / 非纯色并再次读取默认 Token 的行为不再保留。Bottom 的 Surface 刷子不受影响。
本任务未要求保留旧发布版本的源 / 二进制兼容；仓内调用方同步迁移，不保留旧配置入口或兼容包装。

| 原入口 / 成员 | 当前归属与理由 |
| --- | --- |
| TopAppBarExpandedHeight、MediumAppBarCollapsedHeight、LargeAppBarCollapsedHeight | SmallExpandedHeight / CollapsedHeight Token；折叠高度引用小栏默认高度 |
| MediumAppBarExpandedHeight、LargeAppBarExpandedHeight | MediumExpandedHeight / LargeExpandedHeight Token |
| MediumFlexibleAppBarWithoutSubtitleExpandedHeight、MediumFlexibleAppBarWithSubtitleExpandedHeight | MediumFlexibleExpandedHeight / MediumFlexibleWithSubtitleExpandedHeight Token |
| LargeFlexibleAppBarWithoutSubtitleExpandedHeight、LargeFlexibleAppBarWithSubtitleExpandedHeight | LargeFlexibleExpandedHeight / LargeFlexibleWithSubtitleExpandedHeight Token |
| Top ContentPadding、WindowInsets | 同名 Token → 主题 Setter → 有效配置属性 |
| Bottom ContainerHeight、FlexibleBottomAppBarHeight、ContentPadding、FlexibleContentPadding、WindowInsets | Bottom / FlexibleBottom 对应高度、Padding、Insets Token；删除属性元数据和类型静态构造器的数字副本 |
| Bottom ContainerElevation、FlexibleContainerElevation | BottomTonalElevation / FlexibleBottomTonalElevation Token → TonalElevation |
| 标题逻辑对齐 | TitleAlignment / CenterTitleAlignment Token → TitleHorizontalAlignment；保留整个顶部族的 StyleKey |
| Bottom / Flexible 默认排列 | BottomArrangement / FlexibleBottomArrangement Token → HorizontalArrangement；Spacing 的零值和 SpacedBy 的负间距仍是原算法语义 |
| Colors()、TopAppBarColors、TopAppBar.Colors | 删除；颜色 Token → Background / 五槽 Color 属性；无 nullable 逐字段优先级 |
| 排版角色选择与内部 Token 绑定 | 主题变体样式 → 展开 / 折叠标题部件的 TextStyle 配置；仍保留系统排版 Token 引用与资源更新 |
| 单行颜色动画内部 motion 查询 | ContainerColorAnimation Token 引用 EffectsDefault → 模板内部配置 → UI / compositor 求值器；每次新目标捕获当前规格 |
| Top / Bottom SnapAnimationSpec 固定属性 | 删除重复的固定规格入口；动态 behavior 默认路径按下述确认保留 |
| Top / Bottom FlingAnimationSpec 工厂 | 删除重复的默认 API；behavior 仍按 density 调用既有 SplineBasedDecay，显式调用方可直接使用动画库 |
| Pinned / EnterAlways / ExitUntilCollapsed / ExitAlwaysScrollBehavior 及显式 spec 重载 | 保留原行为工厂、状态身份、回调、订阅和挂载生命周期，不把运行时对象放入 TokenSource |
| 插值、TitleAlpha、TitleBottomPadding、4 DIP 标题留白、FAB 8 / 12 DIP 留白 | 保留实际布局 / 绘制职责中的算法与常量，不机械公开为 Token |

Top 主题通过变体伪类和 `:subtitle`（Subtitle 或 SubtitleTemplate 非 null）选出一个内部 DefaultExpandedHeight。
主题把它绑定到 ExpandedHeight 的主题优先级；用户 Style / Binding / LocalValue 仍由 Avalonia 合并。
这个内部值还用于上游指定的 NaN / 正无穷（FlexibleBottom 另有非有限 / 非正）规范化，保留原输入，
不是一般性的用户选项回退链，也没有新增只读公共 Effective / Actual 属性。负无穷 Top 高度仍触发原有有限值检查。
底栏变体默认值与居中标题对齐通过主题内部 MultiBinding 选择候选 Token。局部
AppBarVariantDefaultConverter 只表达这两种类型条件，避免伪类 Setter 的 StyleTrigger 优先级压过普通用户 Style，
同时保留原组件族 StyleKey；未新增公共 Options 或变体主题切换 API。

完整示例：AppBarTokenSource 提供 MediumFlexibleWithSubtitleExpandedHeight = 136 → 控件有副标题事实 →
主题 :medium-flexible:subtitle 选择该 Token → 默认 ExpandedHeight = 136 → AppBarTopRow 计算两行几何。
局部 ExpandedHeight = 180 由 Avalonia 覆盖主题；滚动偏移只改变呈现高度，ClearValue 后恢复当前主题选择。
颜色、排版和颜色动画基线保留 TokenRef，不在 TokenSource 中解析成实例快照。

维护者本轮确认保留 behavior 的动态 snap 特例：挂载的栏 / Scope 仍读取系统 motion Token，未挂载时仍使用
Expressive fallback；显式 spec / null 继续由 behavior 持有。它尚未经过“主题 → 配置属性”的统一路径，
后续需要决定动画配置 API 与 behavior 的职责后再迁移。本轮不改变 state 所有权，不将该特例推广给呈现配置。

## 槽、标题与布局不变量

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
HorizontalArrangement 消费共享 MaterialHorizontalArrangement，包含逻辑/绝对方向、带对齐的正负间距和自定义算法。自定义根自行决定内部排列。Flexible 继承基础槽；上游 Flexible 入口的 FAB 可放入自定义内容。

隐藏底栏按上游缩小外部布局盒、在 (0,0) 放置完整 Surface；浮置栏可画到布局盒之外，Scaffold 停靠由窗口边缘裁剪。
顶部直接拖动执行 heightOffset += delta，底部执行 -= delta；释放速度保留上游传入 settle 的原符号。
WindowInsets 是物理 left/top/right/bottom Thickness，桌面默认零；ContentPadding 为逻辑 start/top/end/bottom。
整数布局使用实际 density，MaterialWindowInsets.ConsumePadding 扣除祖先已消费的 inset。

## 物理正文与合成连接

Material 的 ScrollViewer 主题使用 SmoothScroll presenter，当前以
[主库项目](../../Material3Expressive.csproj)中的 Shirasagi0012.SmoothScroll.Avalonia 包接入；
恢复方式与中央版本见[公共接入](../../../../docs/GETTING_STARTED.md)，不沿用旧记录的子模块 ProjectReference 指引。
职责与批准依据见[普通物理滚动与 AppBar 的合成接入](../../../../docs/DECISIONS.md#普通物理滚动与-appbar-的合成接入)。

指针、直接滚轮与惯性帧经过 SmoothScroll 增量入口，按 pre → 正文 → post 分配。
内部事务用正向 DIP 表示内容向前滚动，公开 AppBar 状态仍为负向折叠的物理 px；
顶栏只消费实际变化量，剩余量同帧给正文，底栏只观察正文消费量。
正文使用 SmoothScroll 惯性，结束后栏独立 spring 吸附；不使用 behavior.FlingAnimationSpec 的 Compose decay。
显式 spring snap 与 null 禁用 snap 仍有效，直接拖动栏保留独立 UI 路径。

2026-11-09 搜索自动接入的边界复核修正了共享 snap 的单位生命周期：旧参与器用启动密度生成
物理像素动画值，却在每帧除以当前密度；20 DIP 的活动 snap 在 density 1→2 后的 t=0
帧错误变成 10 DIP。现每次 top/bottom snap 分别捕获启动单位比例，整个 evaluator 使用同一
比例回到 DIP；当前 density 仍用于绘制量化。搜索显式 spec 保留单位比例 1，与其直接 settle
路径捕获 units 的方式一致。搜索默认、显式及普通 top/bottom 的 1↔2 / 1.25→2 组合由
`SearchScrollEdgeTests` 在真实 compositor 线程验证；不是动态切换显示器的桌面实测。

栏高、标题位置/透明度、背景色、正文位置与裁剪视口由同一组合成输出驱动；
底栏关联 FAB/Snackbar 位置随动。UI 恢复后更新真正 Measure/Arrange、ContentPadding 和 ScrollViewer.Offset；
新 UI 布局是下一次合成补偿基准，不能把栏位移写进正文 Offset。

Avalonia 12.1.2 的桥接包括首次 expression 主动求值、停止后恢复客户端基准、
绑定前同步原生 Size/Opacity、动态纯色 visual 重绘失效。升级框架时必须重验，不把版本行为当成永久保证。

连接条件及失效边界：

- Scope/栏引用同一个内置 behavior，标准栏模板，snap 为默认 spring、显式 SpringSpec 或 null
- 唯一的物理主 ScrollViewer 位于标准 Scaffold 正文，正文根填满屏幕并消费 ContentPadding；正文内可有独立滚动器
- 无祖先 ScrollViewer、额外正文变换或主正文原生 snap points；多个主候选不能按遍历顺序任选
- 逻辑主正文、嵌套 Scope、自定义 behavior、非 spring snap 与其他正文结构暂不自动联动，正文仍由原 presenter 滚动
- Scope 不改写主题/模板；自定义模板的实际 SmoothScroll presenter 满足同样布局条件也可接入。
  owner 内容使用 TemplateBinding，模板退出时应解除归属；参见 [ScrollViewer](../ScrollViewer/README.md)
- 模板、行为、内容切换及 detach/re-attach 重建并释放连接；正文内部滚动器及 Scaffold 栏/覆盖槽不参与主正文选择

Scope 对三种 behavior 的 PropertyChanged 订阅也只存在于 attached 生命周期，卸载时解除，
重新挂载时订阅当前值。此前即使 Scope 从未挂载，外部应用持有的 behavior 也会通过事件委托
强引用 Scope；搜索/普通 top/bottom 三个弱引用用例均实际复现。修正后的未挂载及实际卸载
回收、重新挂载配置响应，与密度及真实 wheel 边界共 15 项由 `SearchScrollEdgeTests` 覆盖。
含连接/合成回归的 54 项通过，日志 `artifacts/search-scroll-edge-final.log`；失败前证据保留在
`artifacts/search-scroll-edge-density-before.log`、`artifacts/search-scroll-edge-lifecycle-before.log`。
普通 AppBar 滚动、行为和密度 Token 的另 58 项回归通过，日志 `artifacts/search-scroll-edge-shared-regression.log`。
两项原生 wheel 边界（reverse 顶端剩余消费、limit 缩小后下一笔输入重新夹取）在修正前后均通过，
没有据静态推测改写边界 admission 谓词。

内层滚动器自己滚动不驱动外层栏，到边界后的下一笔滚轮按 IsScrollChainingEnabled 冒泡；
触摸保持 SmoothScroll 捕获接力。单笔跨边界剩余量与已启动惯性余速不额外分给父层，
内层列表/编辑区保留逻辑单位、虚拟化与编辑输入。
CanScroll / IsScrollingContentAtStart 属性在 UI 输入前和布局时捕获，UI 忙碌时沿用最后快照。
Offset、键盘、BringIntoView、滚动条与程序化定位动画不进入 pre/post。

## 直接拖动与测试协议

标准 Scaffold 的 PART_ScrollScope 自动引用呈现栏的 behavior。顶部提供 Pinned/EnterAlways/ExitUntilCollapsed，
底部 ExitAlways。状态为物理 px、速度 px/s；尺寸和 Avalonia Offset 为 DIP/原生逻辑单位。
默认 fling 从挂载的栏读取 density，snap 从 Material motion token 解析；独立 spec 属性允许
分别传 null 禁用 decay/snap。求解使用 ComposeAnimation NuGet，spring 参数保留上游 Float 精度。

直接拖动栏使用 Core Desktop 的 18 DIP touch slop / .125 DIP mouse slop，并调用 behavior 的 settle。
Lsq2 速度追踪从 Rail 原样提取共用。TouchExplorationEnabled 由宿主接入辅助服务，只禁用底栏直接拖动和
隐藏布局，正文滚动联动保留，对应上游 activeScrollBehavior 范围。

维护者于 2026-10-06 确认：显式嵌套协议的现有用途限于测试，将整套实现移出控件库。
删除公开 AppBarScrollSource / AppBarNestedScrollConnection、behavior 的 NestedScrollConnection 属性、
Scope 的 DispatchScroll / DispatchFlingAsync；不保留自定义宿主 API 或兼容包装。
真实 SmoothScroll 连接、直接拖动、SettleAsync 和取消任务管理继续留在控件库。

[tests/Shared/AppBar](../../../../tests/Shared/AppBar) 保存显式协议与扩展分发方法，
由 Material3Expressive.Tests 和 Material3Expressive.Parity 链接同一份源文件，不随控件库编译。
分发仍为 pre 从外到内、每层 top→bottom；post 从内到外、每层 bottom→top，消费物理 px 或 px/s。
它只支持内置 behavior，用真实 State、CanScroll 属性和内部 SettleAsync 驱动测试正文，保留原有边界消费规则。
两测试程序集通过 InternalsVisibleTo 访问 SettleAsync 和 Register/Unregister；测试 fling 接入 Scope 原有取消集合，保持新输入和 Interrupt 的取消语义。
SmoothScroll 生命周期断言仍在测试侧反射 Scope._connection 和连接的 _viewer，不新增生产查询入口。

显式分发断言验证测试辅助代码；双端用例验证真实栏的状态、布局和收尾动画。
生产正文的滚动分配及连接行为由 AppBarSmoothScrollTests / AppBarCompositionTests 验证，
不能用测试协议的结果代替真实 SmoothScroll 路径的证据。

## XAML behavior 与自动连接（2026-10-06）

维护者确认合并抽象基类与 Default 实现为两个 sealed StyledElement 类型；ProvideValue 返回自身，
Mode 字面量 / Binding 使用构造器重载与 ConstructorArgument，其他属性走 Avalonia 原生绑定。
State 使用 DirectProperty 和每实例默认值，不把可变 State 放进静态属性默认值；null / 未就绪绑定保留默认实例。
栏唯一持有 behavior 的逻辑父关系；Scope 只引用它，避免破坏 DataContext / 资源继承。

动画属性使用真实默认规格实例区分默认配置与显式 null，内部按引用识别默认配置并在使用时解析当前栏主题 / density。
这使 null、Binding 和 ClearValue 都产生正常属性通知，避免用 IsSet 加 null 默认值时无法观察默认→禁用的变化。
Snap / Fling 各自判断；Mode 切换不丢弃显式规格。SettleAsync 转为内部入口，算法不变。

Scope 按最近所有权边界筛选正文，仅连接自身 Scaffold 的正文槽。标准 Scaffold 在模板中创建 Scope，
OnApplyTemplate 提供宿主和实际 ContentPresenter；连接读取 presenter.Child 的 AppBar，兼容数据模板根栏。
外部 Scope 不抢占内部 Scope 的正文；任意非 Scaffold 布局的正文连接仍未实现。
替换 State 时 chrome 与连接释放各自捕获的旧 State 订阅；Mode / spec 变更取消旧动画并刷新配置；CanScroll 变更只结束受影响的合成栏吸附，
CanScroll 和起点覆盖不打断正文惯性或直接拖动。
更换 Scaffold 模板释放旧 Scope 的宿主与 behavior 引用。原物理状态、测量和滚动消费算法保持不变。

## 已确认选择与未决范围

以下确认与历史观测依据来自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/AppBar/README.md)；不以当前代码反推维护者授权。
2026-10-04 维护者确认修订后的 Scope 与标题/Bottom 自定义内容槽规则，保留自定义模板、嵌套及生命周期。
双行标题采用独立内容/模板，是因为 Avalonia Control 不可有两个父节点；未设置、显式 null 和 ClearValue
分别保留回退、清空和恢复默认语义。Bottom 的自定义内容替换动作/FAB，但不改变外围背景、Insets 与隐藏布局。
此处只记录组件槽的影响；原生滚动接入与后续合成演进的原因、授权和范围统一引用
[AppBar 接入决定](../../../../docs/DECISIONS.md#appbar-与原生滚动接入)与
[合成决定](../../../../docs/DECISIONS.md#普通物理滚动与-appbar-的合成接入)，不能把旧 Fluent 自动模板替换方案恢复为当前能力。

双行入口的 CollapsedHeight 与 ExpandedHeight 都保留固定 AppBar.kt 的 NaN / 正无穷默认值语义。
主题分别提供 DefaultCollapsedHeight / DefaultExpandedHeight，布局只消费归一化值，不写回公开配置。
资源覆盖继续更新默认值，有限性与展开不小于折叠的检查在归一化后进行；负无穷不作为默认值。
该缺口在 2026-11-02 的已授权上游对齐批次补齐，不扩大滚动连接或改变已确认布局规则。
本批次 appbar 双端 152 例为 150 equal、2 withinTolerance，重复检查均稳定；新增 40 个
height-defaults 用例全为 equal。容差内两例是既有 dynamic.scroll.exit 与 dynamic.snap.medium.0.25，
不是全部截图精确一致。测试使用临时恢复的依赖环境，具体边界见 [IconButton 验证环境](../IconButton/INTERNALS.md#当前验证)。

已观察到的未解决差异是合成路径极小非零标题 opacity 仍被 Avalonia 剔除；UI 路径的 opacity mask 处理
不等于合成路径已修复，也不是授权改写上游曲线。当前合成能力不支持 zoom/overscroll，
只能移动已经呈现的内容；UI 忙碌时公开状态、滚动条滑块/自动隐藏、辅助功能、命中测试与虚拟化新内容仍受 UI 更新限制。

RawTouch 测试不证明系统或硬件触摸投递；真实文字 glyph/hinting、屏幕阅读器、
UI 阻塞时辅助功能与命中测试仍需专项验收。组件 parity 不评价 SmoothScroll 正文惯性与 Compose 曲线一致性。

## 验证入口

从仓库根目录执行，环境与结果含义见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~AppBar
./tests/VisualParity/parity.ps1 prepare-reference --family appbar
./tests/VisualParity/parity.ps1 run --family appbar
./tests/VisualParity/parity.ps1 self-test --family appbar
```

固定参考源码与配置不变时，无需每次 prepare-reference。
[AppBarTests.cs](../../../../tests/Material3Expressive.Tests/AppBarTests.cs)、
[Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/AppBarCases.cs)和
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/AppBarCases.kt)是已跟踪的复现入口。
Catalog 入口为 [AppBarDemo.cs](../../../Material3Expressive.Catalog/AppBarDemo.cs)。

- [AppBarConfigurationTests](../../../../tests/Material3Expressive.Tests/AppBarConfigurationTests.cs)：默认基线、局部资源、普通样式 / 绑定 / ClearValue、副标题及模板变化、主题 / 挂载位置更新、高度特殊值、颜色动画与输入隔离、独立颜色迁移
- [AppBarScrollTests](../../../../tests/Material3Expressive.Tests/AppBarScrollTests.cs)：显式协议、直接拖动、吸附/中断
- [AppBarSmoothScrollTests](../../../../tests/Material3Expressive.Tests/AppBarSmoothScrollTests.cs)：主正文滚轮/RawTouch、嵌套、逻辑模式、模板/主题切换及连接生命周期
- [AppBarCompositionTests](../../../../tests/Material3Expressive.Tests/AppBarCompositionTests.cs)：合成/UI 渲染对照、布局基准、分数偏移与断连恢复

双端用例覆盖真实栏的非文字布局/颜色、排列边界、折叠、配置变更、四种 behavior、fling/snap 端点及中断、
直接鼠标拖动；参考使用 Expressive motion 与 density-scaled Core Desktop slop，不替代正文集成测试。
原记录最后一次主线双端重采中结构化事实相等，AppBar 有两例 raster withinTolerance，
Scaffold 严格相等，两族普通重复与额外被动截图稳定，未改容差；该历史结果不作当前通过声明。

上述命令与覆盖说明不代表当前修订已执行全量验证。旧测试数量与未跟踪 artifacts 不作为当前验证结果；
详细历史结果见[固定修订的原记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/AppBar/README.md)，复核以跟踪测试和新生成报告为准。
`run` 执行完成、`self-test` 重复稳定与两端相等是不同结论，必须分别报告。

## 底栏任意排列

固定 AndroidX/Core AppBar.kt blob 相同：FlexibleBottomAppBar 直接将 Arrangement.Horizontal 传入 Row。
原枚举与独立 Spacing 便于表达常用布局，但遗漏绝对方向、组对齐与自定义算法；2026-11-09 本轮维护者
明确授权新增 API 和破坏性迁移后，改用现有 MaterialHorizontalArrangement，复用共享决定 §36。
两个排列 Token 保留身份与默认 Start/SpaceBetween，类型改为共享对象。旧枚举与 Spacing 删除，仓内调用同步迁移。

测量继续按 RowColumnMeasurePolicy 逐项分配剩余宽度，使用排列的 Spacing；摆放消费物理整数坐标后抵消
Avalonia 的 RTL 子树镜像。运行中替换排列或 FlowDirection 使实际 ItemsPanel 重新测量/排列，卸载解除 owner 订阅。
不改变底栏槽、FAB 留白、滚动或按钮输入 owner；CustomContent 继续自行排列。完整 RowScope weight/alignment 尚未提供。

新增 AppBarArrangementTests 验证方向切换、清除后的主题恢复、动态项目及点击；appbar-arrangement 双端用例
覆盖完整新排列集合、窄宽、负间距、空/单项与运行时切换。原 appbar 用例保留，测试结果见实际生成报告。

## 2026-11-09：底栏向内容传播 Insets

原先底栏只读取共享祖先消费，再在 Row 内应用剩余量；缺少完整 windowInsetsPadding 的发布阶段。
本轮维护者明确授权共享能力和职责调整，依据固定 AppBar.kt 的两个底栏入口，在 BottomAppBar owner
持有应用/消费作用域，Row 读取 UnconsumedInsets。Actions 的逻辑项、FAB 与 CustomContent
因此继承同一总量，未增加组件局部消费机制。折叠外层布局仍按完整高度缩小，不改变滚动状态算法。
当时顶栏双行分别应用全部/仅水平 Insets，必须在各内容 scope 明确处理，因此先保留原祖先扣除路径。
本轮后续明确授权下，新增内部消费父级绑定补齐发布：Small/Expanded 两个真实行分别应用完整/仅水平 Insets；
仅对直接槽根及生成的 action 容器绑定消费源，避免逻辑 owner 越过行作用域。
不递归覆盖任意内容内部的 presenter，不覆盖用户 ConsumePadding/ConsumeInsets；换槽、模板替换和卸载释放贡献。
原有顶栏自身物理边距、折叠 offset 和合成行高计算不变，新增的是后代对剩余 Insets 的观察与布局。
`TopAppBarInsetsTests` 覆盖直接/模板内容、两行/RTL、父级变化、重挂与模板替换；
双端 `top-app-bar-insets` 使用真实 TopAppBar / TwoRowsTopAppBar 和嵌套 windowInsetsPadding。
`top-app-bar-insets-box-equivalent` 的普通/双行顶栏 20 例及搜索顶栏 10 例全部 equal、
零错误且重复稳定；本地输入槽夹具的 Box 语义修正及原失败证据见 SearchBar/INTERNALS.md。

本轮 navigation-insets-bottom-delivery 的 70 例全部 equal，其中两个底栏新增 20 例；错误为零且重复稳定。
含 Actions/FAB/CustomContent 的消费与动态修改测试在 200 项相关回归中全部通过，整仓构建通过。
原有 appbar-arrangement-regression 152 例中 interrupt.bottom/medium 的取消/完成/消费速度读数仍不同，
几何及图片相同；现有 SettleAsync 在动画被取消后返回速度，与 Compose Job 取消状态不同。
这是当前回归观察到的未接受差异，不能继续以旧 150 equal 的记录描述当前全矩阵。
