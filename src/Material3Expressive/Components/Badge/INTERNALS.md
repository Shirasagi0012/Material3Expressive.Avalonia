# Badge 实现说明

使用方法见 [README](README.md)。Badge 本身负责可选内容的表面，BadgedBox 负责不扩大锚点尺寸
的相对放置；导航布局提供的徽标边界不应变成一个额外裁剪层。

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

八项 Token 在 [BadgeTokenSource.cs](BadgeTokenSource.cs) 和
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

BadgedBox 模板的 `PART_ContentPresenter` / `PART_BadgePresenter` 对应锚点与徽标；
后者注册为内容 presenter，Badge 槽替换时维护逻辑子项。保持 DataTemplate、资源继承与重挂，
不能仅在视觉树塞入一份复制徽标。
共享文字环境规则见[排版决定](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)。

## 已确认适配与缺口

确认依据来自 [拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Badge/README.md)：

2026-10-03 维护者确认保留 Avalonia 原生布局，记录 BadgedBox 无法透传 Compose
FirstBaseline/LastBaseline 的缺口，不新增全库 alignment-line 机制。这项选择局限本次 BadgedBox，
不是说明基线透传已等价实现。

同日允许默认穿透、显式开启交互，以原生 IsHitTestVisible 表达，不新增命中属性或机制。
主题设 False；局部 True 使整个子树参与命中。Compose 背景不消费输入，任意内容中的输入 modifier
可自动命中，而本地交互内容需显式开启，这是已接受的输入适配。

## 一致性边界与验证

尚未实现向父布局透传基线。当前调用方包含四种导航项及 Stacked Tab，
不将这一接入事实表述为 Leading/Custom 也提供边界。
组件默认不添加专属语义，应用负责 AutomationProperties.Name；文字/MaterialSymbols raster、
实体桌面操作与屏幕阅读器未经验证。组件没有上游动画或主动交互状态，不需额外状态层。

[BadgeTests](../../../../tests/Material3Expressive.Tests/BadgeTests.cs) 检查 Token、尺寸分支、模板/替换、
内容色与 LabelSmall、本地覆盖、RTL、导航边界及重挂。
[BadgeCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/BadgeCases.cs) 与
[BadgeCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/BadgeCases.kt) 独立构造真实 API，
覆盖空/无/组合/宽高内容、颜色/环境、约束与宽度阈值、四种导航边界、RTL，以及内容/主题/方向/
锚点尺寸变更和指针输入。这些 Badge 用例中的“四种导航边界”不包含 Stacked Tab；
当前 TabHeader 的接入由源码确认，本段不把原有 Badge 验证范围自动扩大到 Tab。
可变 intrinsic 几何服从父最大约束，不以夹具补偿组件位置。

旧记录的结构化事实和原始 4× raster 均精确相等，双端重复及额外被动截图稳定；
它使用非文字夹具，不能据此声明字形一致。历史统计与环境见固定提交，原来的未跟踪产物链接
改由正式测试复现。本次文档拆分未重跑：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter "FullyQualifiedName~BadgeTests"
./tests/VisualParity/parity.ps1 self-test --family badge --require-equal
```

复用参考快照等流程见 [VisualParity](../../../../tests/VisualParity/README.md)，当前依赖见
[公共接入](../../../../docs/GETTING_STARTED.md)。
