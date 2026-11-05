# Badge 实现说明

使用方法见 [README](README.md)。Badge 本身负责可选内容的表面，BadgedBox 负责不扩大锚点尺寸
的相对放置；导航布局提供的徽标边界不应变成一个额外裁剪层。

## 默认配置数据流

内容条件只选择内部 Padding、Shape、文字默认输入，再由主题普通 Setter 绑定公开属性。用户普通 Style、Binding 与局部值不被 :has-content 默认值压过；清除覆盖后恢复当前内容分支。MinimumSize 继续由主题直接绑定内部布局，BadgedBox 的定位算法不变。

## 固定上游与本地入口

基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）与 Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）。下述 Material3 源码路径相对于
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

两端的 Badge.kt、BadgeTokens.kt 相同。

| 上游文件 / 关键符号 | 本地职责 |
| --- | --- |
| `Badge.kt`：`Badge`、`BadgeDefaults` | [Badge.cs](Badge.cs)、[Badge.axaml](Badge.axaml)、[BadgeLayout.cs](BadgeLayout.cs)：内容存在性、默认尺寸和内容色 |
| `Badge.kt`：`BadgedBox` | [BadgedBox.cs](BadgedBox.cs)、[BadgedBoxLayout.cs](BadgedBoxLayout.cs)：双槽、锚点测量与相对定位 |
| `Badge.kt`：`badgeBounds()` | BadgedBoxLayout 中的内部 BadgeBounds、四种导航项布局与 Stacked TabHeader 提供者 |
| `tokens/BadgeTokens.kt` | [BadgeTokenSource.cs](BadgeTokenSource.cs)、[MaterialTokens.Comp.Badge.cs](../../Tokens/MaterialTokens.Comp.Badge.cs) |

## 布局、Token 与生命周期

[Badge.cs](Badge.cs) 和 [Badge.axaml](Badge.axaml) 提供可选内容、颜色环境及模板；
[BadgeLayout.cs](BadgeLayout.cs) 保留 `defaultMinSize → background → padding → Row` 的布局顺序。
没有内容时默认最小尺寸为 6 DIP；内容或内容模板存在时为 16 DIP，水平内边距各 4 DIP，
内容居中。存在但为空的内容仍进入大徽标分支。Width / Height / MinWidth / MinHeight
覆盖默认最小尺寸，有限最大约束可缩小徽标。组合内容可在 Content 中使用水平 StackPanel。

组件 Token 在 [BadgeTokenSource.cs](BadgeTokenSource.cs) 和
[MaterialTokens.Comp.Badge.cs](../../Tokens/MaterialTokens.Comp.Badge.cs) 定义，由 MaterialTheme 装配。
保留引用关系：两种内容分支均消费 `Comp.Badge.Color`，默认 Error；前景经共享 `contentColorFor`
按容器颜色匹配 On 色，未匹配时继承外层内容色。`LargeColor` 和 `LargeLabelTextColor`
保留在 Token 表，但固定上游的 Badge 并不消费它们。大徽标使用 LabelSmall 和 LargeShape。
字体、颜色和形状支持现有 Token 动态覆盖与控件本地覆盖。

[BadgedBox.cs](BadgedBox.cs) 使用 Content / ContentTemplate 作为锚点槽，Badge / BadgeTemplate
作为徽标槽，保留逻辑子内容、DataTemplate、替换与重挂。
[BadgedBoxLayout.cs](BadgedBoxLayout.cs) 先测徽标，再测锚点；自身尺寸由锚点决定，徽标可以溢出。
徽标包装槽测量宽度大于舍入后的 6 DIP 时使用水平 12、垂直 14 DIP 偏移，否则使用 6、6。
它按实际测量宽度判断，固定宽度的圆点也可能进入大徽标偏移分支。
锚点在自己的槽内居中，徽标按 Compose `placeRelative` 放置，RTL 不镜像文字。

NavigationRailItem、NavigationBarItem、ShortNavigationBarItem 和 WideNavigationRailItem 的内部布局
提供上方/末端边界。Avalonia 在祖先 ArrangeOverride 返回后才更新 Bounds，因此内部布局在
LayoutUpdated 读取最近提供者，边界变化时再安排一次。保持上游物理 ruler 与随后 RTL 相对放置的顺序，
包括 Start 图标不居中时的行为；这里没有额外裁剪。当前 [TabHeader](../TabRow/TabHeader.cs)
的 ApplyContentPolicy 也在 Stacked 布局启用 BadgeBounds，Leading/Custom 不启用；
拆分前“Tab 尚未移植”的说明已不适用于当前源码。
组件没有上游动画或主动交互状态，也没有借用 Surface 造成输入阻挡。

BadgedBox 模板的 `PART_Layout` 提供锚点测量基线，`PART_ContentPresenter` / `PART_BadgePresenter` 对应锚点与徽标；
后者注册为内容 presenter，Badge 槽替换时维护逻辑子项。保持 DataTemplate、资源继承与重挂，
不能仅在视觉树塞入一份复制徽标。
共享文字环境规则见[排版决定](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)。

## 已确认适配与缺口

确认依据来自 [拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Badge/README.md)：

2026-10-03 维护者确认保留 Avalonia 原生布局，记录 BadgedBox 无法透传 Compose
FirstBaseline/LastBaseline 的缺口，不新增全库 alignment-line 机制。这项选择局限本次 BadgedBox，
不是说明当时基线透传已等价实现。

**2026-11-05：本轮依据用户扩大授权重新决定。** 原先为避免全库 alignment-line 框架而推迟，
现在 Snackbar / RichTooltip 已消费共享 MaterialTextBaseline，实际组合仍丢失 BadgedBox 锚点基线。
固定两端 Badge.kt 相同，明确发布 anchorPlaceable 的 FirstBaseline/LastBaseline，排除 badgePlaceable。
在现有读取器增加内部可选提供接口，由 BadgedBox 的真实模板布局转发锚点；不建立树搜索或注册表。
显式 First / 原生 BaselineOffset 仍优先，Last-only 仍覆盖自动末基线；margin 只在读取器加入一次。
固定高度时以测量尺寸计算锚点居中偏移，采用 Compose 像素舍入，不读取上次 Arrange 的 Bounds。
真实 TextBlock、多行、margin、嵌套、ContentTemplate 替换与重挂由行为测试验证；真实 Snackbar /
RichTooltip 的双端用例直接比较锚点、宿主及首末基线，并验证徽标自身基线变化不影响宿主。
手工在每个 BadgedBox 上维护覆盖值会随内容变更失效；重建全布局树 AlignmentLine 超出这些调用方
需要，均未采用。普通用法见 README；无公开 API 删除。自定义模板不含 PART_Layout 时不自动转发，
仍可发布显式基线。此决定替代以上缺口记录，不追认为 10 月已批准；共享范围见 DECISIONS §24。

同日允许默认穿透、显式开启交互，以原生 IsHitTestVisible 表达，不新增命中属性或机制。
主题设 False；局部 True 使整个子树参与命中。Compose 背景不消费输入，任意内容中的输入 modifier
可自动命中，而本地交互内容需显式开启，这是已接受的输入适配。

## 一致性边界与验证

2026-11-05 本轮基线转发验证：`TextBaselineIntegrationTests` 10/10 通过，控件全量 2275/2275 通过。
新增 `badge-baseline` 双端 16 例（Snackbar / RichTooltip × LTR / RTL × 单层 / 嵌套 × 自然 / 固定高度），
包含首末基线改变、仅徽标基线改变、清除与恢复。全部布局和基线读数严格相等、图像在原容差内，
执行错误 0、全部重复稳定；原始细微阴影 raster 仍保留。运行目录 `badge-baseline-verified`。
原 `badge` 85 例回归（`badge-baseline-regression`）81 equal、4 different，均稳定、零执行错误。
两个分数像素 RTL 差异见下文；另两个 Leading 宽徽标用例的槽几何相等，但标签被徽标盖住，
原因是 TabHeader 绘制顺序与 LeadingIconTab 相反，修复见 [TabRow](../TabRow/INTERNALS.md)。
修复并增加两个宽徽标布局切换用例后，`tab-layer-current` 为 87 例：85 equal、2 different，
全部稳定、执行错误 0；仅余相同的分数像素 RTL 居中差异。最终控件全量 2279/2279 通过；
另追加模板撤回/恢复断言，单层/嵌套两例均通过，没有为此前未复现的生命周期问题增加防御代码。
首轮新夹具漏配 SurfaceContainer / InverseSurface 与嵌套 Badge 的 Error 角色；对照固定
lightColorScheme 和原有 Snackbar / RichTooltip / BadgeCases 补齐同样输入，未更改组件默认值或容差。
基线用例不绘制文字，实际多行字形与模板生命周期由行为测试覆盖，不能宣称字形像素已一致。

基线通过共享读取器向 Snackbar / RichTooltip 转发，不扩展 Avalonia 任意父布局。徽标边界的
当前调用方包含四种导航项及 Stacked Tab，
不将这一接入事实表述为 Leading/Custom 也提供边界。
组件默认不添加专属语义，应用负责 AutomationProperties.Name；文字/MaterialSymbols raster、
实体桌面操作与屏幕阅读器未经验证。组件没有上游动画或主动交互状态，不需额外状态层。

[BadgeTests](../../../../tests/Material3Expressive.Tests/BadgeTests.cs) 检查 Token、尺寸分支、模板/替换、
内容色与 LabelSmall、本地覆盖、RTL、导航边界及重挂。
[BadgeCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/BadgeCases.cs) 与
[BadgeCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/BadgeCases.kt) 独立构造真实 API，
覆盖空/无/组合/宽高内容、颜色/环境、约束与宽度阈值、四种导航边界、RTL，以及内容/主题/方向/
锚点尺寸变更和指针输入。Tab 组合另覆盖 Stacked/Leading、仅图标/图标标签、宽/高徽标、
方向、内容、锚点尺寸和布局切换；控件测试使用真实文字检查选择、重挂及方向变化后的边界。
可变 intrinsic 几何服从父最大约束，不以夹具补偿组件位置。

2026-11-05 的 `badge-tab-unclipped` 双端自检共 85 例，83 例严格相等，全部重复稳定且执行错误为零。
新增 22 例中 20 例严格相等；两个 `badge.dynamic.tab.rtl.*` 使用 80.25 DIP 外层宽度，
Core 外层居中 x=60，Avalonia x=59.75，子槽随之整体偏移 0.25 DIP；局部边界关系一致。
这是尚未解决的外层分数像素布局差异，不是已接受的组件偏离，也未用夹具位移或容差规避。
新用例发现并修复了 Tab 模板裁剪溢出徽标、方向变化未重测槽缓存的问题，机制见
[TabRow](../TabRow/INTERNALS.md)。原有 63 例仍严格相等。原始结果位于
`artifacts/visual-parity/runs/badge-tab-unclipped/summary.md`，应通过跟踪用例复现。

旧记录的结构化事实和原始 4× raster 均精确相等，双端重复及额外被动截图稳定；
它使用非文字夹具，不能据此声明字形一致。历史统计与环境见固定提交，原来的未跟踪产物链接
改由正式测试复现。以下为复现入口，实际结果需以新生成报告为准：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter "FullyQualifiedName~BadgeTests"
./tests/VisualParity/parity.ps1 self-test --family badge --require-equal
```

复用参考快照等流程见 [VisualParity](../../../../tests/VisualParity/README.md)，当前依赖见
[公共接入](../../../../docs/GETTING_STARTED.md)。
