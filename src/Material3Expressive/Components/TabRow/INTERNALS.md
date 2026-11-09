# TabRow 实现说明

## Token 配置路径

内部必需 spring 的 stiffness 必须大于零。模板首次挂载、重挂或资源暂时撤回时，未就绪配置不构造或提交运行时 spring；有效配置恢复后重新提交当前视觉目标。正在运行的通道继续使用已验证并捕获的规格，不回退 Expressive、Snap 或另一套默认值。

主题把尺寸、指示器/滚动/内容运动绑定到内部配置属性；EdgePadding、MinTabWidth 及标准/图标加标签两种容器高度的基线也放入 TokenSource。Primary/Secondary/Legacy 切换内部颜色及指示器候选，公开 Foreground、ContentColor 与 Indicator 在普通主题优先级接入，应用 Style、Binding、LocalValue 可覆盖。

SelectedContentColor=null 的环境色继承、UnselectedContentColor=null 跟随选中色是明确语义，继续保留；不是逐字段 nullable Options 合并。指示器/颜色/滚动位置仍是运行时结果，选择及页面所有权不变。

本文面向维护者；使用入口见 [README.md](README.md)。固定基线以
[upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 1.5.0-alpha27），
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（v1.13.0-alpha01）。
默认逻辑对照 AndroidX，桌面输入与平台实现对照 Core，不用浮动 HEAD 替代。

## 上游与本地职责

主要入口是固定提交的 [AndroidX TabRow.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/TabRow.kt)
及 [Core TabRow.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/TabRow.kt)。
下文 Material3 路径均以 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/` 为前缀。

还需读取固定 `Tab.kt` 与 `tokens/PrimaryNavigationTabTokens.kt` / `SecondaryNavigationTabTokens.kt`。
原记录核对两个仓库的 TabRow.kt 和 Tab.kt 相同；旧 deprecated Row 此前未移植。
2026-11-09 本轮再次逐行比较两固定 TabRow.kt，无差异；依据用户自主补齐组件/变体授权增加 Legacy。

| 上游关键符号 | 本地职责 |
| --- | --- |
| PrimaryTabRow / SecondaryTabRow、PrimaryScrollableTabRow / SecondaryScrollableTabRow | [TabRow.cs](TabRow.cs)、[TabRowAppearance.cs](TabRowAppearance.cs)：Variant × IsScrollable、公开位置和模板槽 |
| deprecated TabRow / ScrollableTabRow、两个 WithSubcomposeImpl、tabIndicatorOffset(TabPosition) | Legacy × IsScrollable：Primary 内容色、Secondary 指示器、旧测量/布局/动画分支 |
| TabRow 布局、TabPosition、指示器位置与滚动居中 | [TabRowLayout.cs](TabRowLayout.cs)：TabRowChrome / ItemsPanel、intrinsic 查询、排列、动画及 ScrollViewer |
| Tab / LeadingIconTab、TabBaselineLayout | [Tab.cs](Tab.cs)、[TabHeader.cs](TabHeader.cs)：受控激活、内容布局、真实文字基线与颜色动画 |
| 默认颜色、Typography 与指示器 | [TabRowTokenSource.cs](TabRowTokenSource.cs)、[组件 Token](../../Tokens/MaterialTokens.Comp.TabRow.cs)、[TabRow.axaml](TabRow.axaml) |
| Avalonia 原生 TabControl / TabItem 合约 | 同一主题内的 MaterialTabControlTheme / MaterialTabItemTheme、两个 SelectedContentHost 和原生选择/页面过渡 |

## 选择与原生模板合约

独立 TabRow.SelectedIndex 只给指示器和居中使用；Tab.IsSelected 控制内容色与语义。应用必须同时维护。
Click / Command / automation Select 不修改选择；Space / Enter KeyUp 激活，失焦/禁用取消未完成的键盘激活。
ItemsSource 的模板应真实生成 Tab，不能让外层 ItemsControl 把任意内容隐式当作已选 Tab。

原生 TabControl 保留 SelectedIndex / SelectedItem、HeaderTemplate / ContentTemplate、SelectedContent、
原生键盘与 automation、PageTransition。模板内 PART_SelectedContentHost / PART_SelectedContentHost2
用于原生页面呈现；仅标签栏使用 TabRowChrome 和统一 Indicator，不让页面内容参与标签色动画。
原生 TabItem.IndicatorTemplate 被统一的 Row Indicator 槽替代。

## 测量、动画与槽

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

标准标签默认 TitleSmall / 居中。TabHeader 读取实际 TextBlock.TextLayout 的首/末基线；
非文字内容可通过根控件的 TextBlock.BaselineOffset 提供基线。只有标准 Stacked 分支提供 Badge bounds。
Stacked / Leading 分支把 IconTemplate 也视为图标槽存在，即使内容为 null；Custom 布局仍完全交给调用方。
TabRow 的 MaterialSymbol 默认尺寸使用通用 24 DIP，显式尺寸和其他自定义内容不受限制。
Leading 和 Custom 不应意外继承 Stacked 的基线、字体或 BadgeBounds 规则。
Tab 的 selectable 只有 bounded ripple，不包含内容 clip。两套项目模板因此把无内容的 MaterialSurface
作为同级反馈覆盖层，内容留在未裁剪的 TabHeader；沿用同一 controller、坐标、墨色与生命周期，
不更改共享 Surface 的背景/裁剪契约。原先用 Surface 包裹内容会裁掉 Leading 的高徽标，
也会截掉 Stacked 自定义大图标超出容器的部分。祖先 Row/滚动视口的裁剪保持各自语义。
TabHeader 的测量使用 FlowDirection 决定居中取整，方向变化必须重新测量缓存的槽矩形。
固定 AndroidX/Core `Tab.kt` 内容相同：LeadingIconTab 的 Row 先放 icon，再放 text；
Stacked 的 TabBaselineLayout 则先 text、后 icon。2026-11-05 当前 Badge 回归暴露 Leading 宽徽标
覆盖标签的失败：槽几何均相等，但本地沿用 Stacked 的绘制顺序。共享 TabHeader 按布局设置
标签 presenter 的 ZIndex，保持测量子项顺序和现有内容生命周期；切回 Stacked 恢复原序。
这是一项等价顺序移植，不改变公共 API 或徽标边界规则，无迁移动作。两种入口、LTR/RTL、
布局切换、方向反转与重挂的真实像素测试修改前 4/4 失败，修改后通过。
最终 `tab-layer-family` 的完整 143 例为 135 equal、6 withinTolerance、2 different（上述原生
按下/方向焦点选择时机），全部重复及额外被动截图稳定、零执行错误。`tab-layer-current` 的
Badge 组合 87 例为 85 equal、2 different，两个 Leading 宽徽标以及新增布局切换样本严格相等；
剩余两个差异为已记录的外层分数像素居中。相关控件回归 53/53、最终全量 2279/2279 通过。

Avalonia 没有 Compose intrinsic API，TabHeader 查询真实内容后必须按实际约束重新测量，
避免查询约束污染后续 Arrange。intrinsic 变化、父约束变化与选择变化分别遵循上游更新顺序。
模板替换与 detach 取消旧动画、滚动任务和帧订阅；重新布局按当前选择恢复位置。

`TabAppearance.InteractionSource` 同时服务受控 Tab 和原生 TabItem，Tab 的同名属性转发到同一附加属性。
共享模板的 TabHeader 在挂载时连接最近的 Tab/TabItem owner，卸载时断开订阅；null 使用模板局部源。
固定 Tab.kt 的 Tab / LeadingIconTab / 自定义内容重载将该源交给 selectable。换源只重建 indication，
TabHeader 自身的颜色 Transition 保留，原生 TabItem 的选择时机也保持既有适配。

颜色默认值和 TextStyle 的共享规则见
[聚合排版](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)、
[颜色转换](../../../../docs/DECISIONS.md#composeanimation-颜色转换与原始动画终值)。
焦点/激活边界见[共享交互](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)与
[桌面焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)。

## 已确认适配

以下确认与历史观测依据来自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/TabRow/README.md)；不以当前代码反推维护者授权。
2026-10-04 维护者确认两个入口：独立入口保留应用受控选择；原生入口保留 Avalonia 选择行为。
这样既支持外部路由，也不重写原生 TabControl 的页面、导航与 automation。
原生 TabItem 在指针按下和 Directional focus 时选择，Compose Tab 在激活回调时选择；
`dynamic.press.native` / `dynamic.directional.native` 保留差异。
范围是两个入口的状态职责与原生输入时机；布局、内容色、指示器动画仍以固定 Compose 为目标，
程序化选择继续走同样的双端回归，不能把其他差异归入“原生适配”。

局部 intrinsic 查询的当前机制如上；代码和编译成功不独自证明它覆盖所有自定义内容。
固定/滚动、四种 Row 与不同内容尺寸都应通过跟踪测试重新验证。

### Legacy 变体

旧入口使用 `SubcomposeLayout` 和 `Modifier.tabIndicatorOffset(TabPosition)`；不是 Primary 或 Secondary 的重命名。
固定栏位置/内容宽度同新版本。旧滚动栏先测 Tab，再以 `maxIntrinsicWidth(placeable.height)` 和实测宽度的较小值
减两侧文字 padding，保留小于 24 甚至负值；起点先将 edgePadding 舍入到物理像素，再逐项累加。
divider 按完整滚动 extent 测量、绘制于 Tab 上方，受限高度时仍位于实际 layoutHeight 底部。

旧指示器的 `fillMaxWidth -> wrapContentSize(BottomStart) -> offset -> width` 直接从逻辑起点平移，
不使用新滚动栏的选中 Tab 中心或 contentWidth coercion。默认宽度始终为整项；
IndicatorMatchContentSize 在 Legacy 无效，以保留旧 modifier 的语义。
`animateDpAsState` 在重定向前取消旧任务并清除速度，传入 MotionScheme spring 使用其 0.01f 向量阈值；
新 TabIndicatorOffsetNode 保留既有速度与 0.1f 阈值。两种机制仍共用本地生命周期、目标合并、滚动实现。
Legacy/新版之间切换对应上游替换组合函数，取消旧子作用域并重新创建状态，不遗留待提交动画。
没有破坏既有 Primary/Secondary API；默认 Variant 不变。

## 一致性边界

- 六种 Row 用 Primary/Secondary/Legacy × IsScrollable 表达；完整任意 TabIndicatorScope 自定义布局尚未提供
- 原生主题只支持顶部水平 TabStripPlacement；其他方向不在范围内
- 旧合并自检的静态用例严格相等，动态用例有按压/键盘采样的少量 raster 容差差异（1–4 像素、最大通道差 1），
  以及上述两个原生输入差异。双端重新采集、重复捕获与额外被动截图稳定，未改容差/遮罩/基线；不是本次验证结果
- headless 检查真实标准字体与基线，visual parity 使用真实槽中的确定几何标记；两者都不证明真实文字栅格一致
- 桌面触摸/惯性与原生 PageTransition 实际运行画面仍需人工验证

## 验证入口

2026-11-09 Legacy 单元：`LegacyTabRowTests` 8 项，加既有 TabRowTests 共 38 项全部通过；
Catalog 和独立 Release 双端执行器构建通过。`legacy-tab-first` 族 69 例（43 静态、26 动态）
全部 equal，执行错误零、重复和额外截图稳定。已查看 RTL 自定义分隔线/指示器与动画反向的真实原图。
覆盖两入口、固定/滚动、LTR/RTL、标签/图标/自定义内容、受限高度、空栏、分数 padding、深色，
以及选择、中断、内容更新、尺寸改变和初始滚动；没有放宽容差或改写基准。
既有 `tab-row.dynamic.reverse.primary.fixed` 与 `tab-row.dynamic.reverse.secondary.scrollable`
分别在 `legacy-modern-fixed-regression` / `legacy-modern-scroll-regression` 重跑，均精确相等且稳定。
此验证不扩大此前原生按压/方向键选择时机及真实文字栅格的确认范围。

`TabInteractionSourceTests` 检查两入口的输入身份、选择所有权、禁用、换源及真实模板替换。
双端 `tab-source` 覆盖 Stacked/Leading/Custom/仅图标/仅标签、RTL、重叠按压、禁用与换源；
原生按下选择的既有差异仍保留在结果中。

此前 `tab-source` 的 18 个双端用例均完成捕获：6 equal、6 withinTolerance、6 different，执行错误为零。
禁用/启用及换源仍有属性即时更新与 Core 下一重组帧的差异；受控入口后续帧收敛。
原生换源用例保留已确认的按下选择时机，因此指示器在 Core release 前已开始移动；800ms 采样收敛。
部分重叠波纹的 Compose 重复采集或额外截图不稳定，不能将整个新增套件宣称通过稳定性验收。

2026-11-05 在修复 Tab 内容溢出裁剪后重新采集 `tab-overlay-source`：18 例中 2 equal、
2 withinTolerance、14 different，执行错误为零；部分 Core 波纹重复捕获仍不稳定。
没有用历史结果替代本次结果。为排除模板调整造成回归，在初始提交 `e4342dd` 的独立工作区
捕获 `tab-source.dynamic.controlled.label.rtl`，其 13 张 Avalonia 原始截图与本次逐像素一致；
该例的波纹差异在初始代码也存在。此对照仅证明该例，不推断全部波纹差异的原因。
原始对照保留在 `tab-initial-source` 与 `tab-overlay-source` 运行目录。

固定栏内容增宽在 64ms 曾有时序差异：Core 宽 46，Avalonia 宽 39.5 DIP。
本轮重新采集 `tab-content-before` 重现，并核对上述两个固定提交的 TabRow.kt（内容相同）：
TabRowImpl 在布局中 setTabPositions 后立即测量指示器，TabIndicatorOffsetNode 直接启动目标动画，
没有固定栏 intrinsic 变化专用的第二次等待。本轮依据用户扩大授权重新决定：删除该额外等待，
保留共享的目标提交边界、弹簧规格和中断速度，不增加补帧或偏移。公开 API 不变，无迁移动作。
控件回归在修改前固定栏两例失败、滚动栏两例通过；修改后 TabRowTests 24/24 通过。

当前 `tab-content-current` 完整 143 例捕获成功、重复和额外被动截图稳定：135 equal、
6 withinTolerance、2 different（已确认的原生按下/方向焦点选择时机）。原有三个固定栏内容用例
现已严格相等；新增 20 例覆盖内容增宽后反向缩窄、同次布局前合并修改、两入口、固定/滚动与 RTL，
全部严格相等。结构化位置、指示器宽度和实际图像均参与比较，未改容差、时钟或基准。

此前 `tab-overlay-final` 的 123 例全部捕获成功、重复及额外截图稳定：118 例在既有门禁内，
5 例 different（上述 3 个固定栏内容变更例和 2 个已确认原生输入时机例）。90 个静态例严格相等。
Badge/Tab 相关控件测试 67/67、控件库全量 2204/2204、整仓 Release 构建通过。
首次全量测试曾在 ButtonLayoutTests 的清理阶段异常中止；未改测试，全量重跑得到上述通过结果。
本次增加的 Badge 组合覆盖及剩余分数居中差异见 [Badge 验证](../Badge/INTERNALS.md#一致性边界与验证)。

从仓库根目录执行，环境与结果含义见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~TabRowTests
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~LegacyTabRowTests
./tests/VisualParity/parity.ps1 self-test --family legacy-tab-row
./tests/VisualParity/parity.ps1 prepare-reference --family tab-row
./tests/VisualParity/parity.ps1 run --family tab-row
./tests/VisualParity/parity.ps1 self-test --family tab-row
```

固定参考源码与配置不变时，无需每次 prepare-reference。
[TabRowTests.cs](../../../../tests/Material3Expressive.Tests/TabRowTests.cs)、
[Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/TabRowCases.cs)和
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/TabRowCases.kt)是已跟踪的复现入口。
Catalog 入口为 [TabRowDemo.axaml](../../../Material3Expressive.Catalog/TabRowDemo.axaml)。

控件测试重点包括 IndependentTab_ClickAndAutomationRequest_DoNotMutateSelection、
NativeTabControl_DirectionalFocusRetainsNativeSelection、ContentResize_ReplaysRealMeasureAfterIntrinsicProbes、
ScrollableRow_UserInputInterruptsCenteringAndDetachCancelsFrames 与真实多行基线。
双端覆盖四种 Row、两入口、各内容布局、RTL/主题/颜色/内容变化、窄/受限尺寸、模板槽、反向与初始居中。

上述命令与覆盖说明不代表当前修订已执行全量验证。旧测试数量与未跟踪 artifacts 不作为当前验证结果；
详细历史结果见[固定修订的原记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/TabRow/README.md)，复核以跟踪测试和新生成报告为准。
`run` 执行完成、`self-test` 重复稳定与两端相等是不同结论，必须分别报告。

### 2026-11-09 指示器宽度策略

固定 AndroidX/Core TabRow.kt 的 TabIndicatorScope.tabIndicatorOffset 允许独立设置 matchContentSize；
旧实现将该参数固定为 Variant == Primary，保留了四种默认栏，却限制了自定义 Indicator。
本轮按用户授权增加 nullable IndicatorMatchContentSize（独立普通属性、原生同名附加属性）；
null 保留旧默认，显式 true/false 只切换 ContentWidth/Width 目标，沿用原来的 Animatable、取整与滚动定位。
无需破坏性迁移。未引入新的指示器布局框架；任意 tabIndicatorLayout 仍未移植。

行为测试覆盖两入口×固定/滚动、运行时切换/中断/恢复默认、Variant 改变和重挂。
双端新增 indicator-width 配置覆盖两入口、两 Variant、固定/滚动、LTR/RTL、切换反转与选择反转。

新增宽度策略矩阵发现原生 TabControl 的 AutoScrollToSelectedItem 会在 Material spring 前立即滚动，
产生 216 DIP 跳变。2026-11-09 在本轮授权范围内将 MaterialTabControlTheme 的该属性设为 false，
选择滚动统一由已有 UpdateScroll 执行；用户显式覆盖 true 仍可选择 Avalonia 原生滚入行为。
焦点滚入此前已禁用，本次不修改手势、惯性和方向键选择职责。


本轮验证：修改前完整 `tab-indicator-width`（tab-row family）207 例为 201 equal / 6 different，
其中四个新增原生滚动反转触发上述立即滚动；另两项原有 press.native / direction.native 保留报告。
修复后独立 `tab-indicator-width-verified` 的新增 64 例全部 equal、执行错误 0、重复稳定，
包含 32 个静态组合和 32 个宽度切换/反转组合；原始失败报告未覆盖或删除。
