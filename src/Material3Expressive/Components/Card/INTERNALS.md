# Card 实现说明

使用方法见 [README](README.md)。三个 Card 控件共享普通/可点击模式，原生 Button 负责激活，
Material 层负责 Surface、颜色、交互反馈与高度，不应把普通卡片改成隐式按钮。

## 固定上游与本地入口

固定版本见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）与 Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）。下述 Material3 路径均相对于
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

两固定版本的 `Card.kt` 相同；桌面 indication 的鼠标焦点过滤取 Core 的共享行为。

| 上游文件 / 关键符号 | 本地职责 |
| --- | --- |
| `Card.kt`：`Card`、`ElevatedCard`、`OutlinedCard` 普通/onClick 重载 | [Card.cs](Card.cs)、[Card.axaml](Card.axaml)：IsClickable、原生输入、Surface 和 automation |
| `Card.kt`：`CardDefaults`、`CardColors` | [Card.axaml](Card.axaml)、[CardThemeConverters.cs](CardThemeConverters.cs)、[CardTokenSource.cs](CardTokenSource.cs)：独立颜色属性与主题派生 |
| `Card.kt`：`CardElevation.shadowElevation`，`internal/Elevation.kt` | Card 的高度选择、effect 替换与生命周期；共享交互源读数来自 Foundation |
| `Card.kt` 内容 `Column` | [CardColumn.cs](CardColumn.cs)：按剩余高度顺序测量，不是完整 ColumnScope |
| `tokens/FilledCardTokens.kt`、`ElevatedCardTokens.kt`、`OutlinedCardTokens.kt` | [MaterialTokens.Comp.Card](../../Tokens/MaterialTokens.Comp.Card.cs) 与 CardTokenSource |

## 两种模式与模板合约

普通模板直接持有 `MaterialSurface#PART_Container`，可点击模板外包 ButtonLayout，默认
48 DIP 交互布局空间与可见容器分开；内容都由 `PART_ContentPresenter` 呈现。
原生 `Click` / `Command` 属 Button，普通模式不运行这些激活路径，也不提供 Invoke，
默认不可聚焦且不被 Command.CanExecute 禁用；子控件仍可接收输入。
普通卡片显式 IsEnabled=false 会禁用内容树，但不选择可点击重载的 disabled 外观。

IsClickable 改变会更新原生启用判断、automation 与反馈接入；进入普通模式清理按压和焦点，
下一次交互重新建立。换模板先释放反馈/动画，再接新 Surface；detach 释放作用域。
不能让子 Button 的冒泡焦点或按压变成父 Card 的操作。

CardColumn 从上到下、Start 对齐、零间距。每个子项使用剩余高度，包括耗尽后的零约束；
默认 Stretch 在此按 Start 放置，显式对齐保留。RTL 消费原生布局方向。
直接放入原生 StackPanel 的行为仍归 StackPanel，不借文档扩大为完整 ColumnScope。

## 默认值与高度不变量

颜色 Token 经主题绑定提供四个独立有效属性；主题派生颜色后再以普通 Setter 选择当前状态画刷。
Elevation 是六个非 nullable double 的完整不可变值，主题通过动态 MultiBinding 组装默认组；
用户 Style、Binding 或局部值整体替换，不在控件内逐字段解析 Token 或补默认值。
普通内容色按启用容器的 contentColorFor 推导，不匹配时继承外层前景；禁用内容色默认替换
alpha 为 0.38。Filled 禁用背景与默认 Filled 容器合成，Elevated 与 Surface 合成，
Outlined 禁用 outline 与 Elevated 容器合成，不能改成一律跟自定义容器混合。
原生 Background/Foreground/BorderBrush/BorderThickness/CornerRadius 的用户 Style、显式值和绑定优先。
IsClickable 只选择内部模板/Cursor 候选，最终 Template、Cursor、Focusable 保持普通主题优先级。
Outlined 默认 press/focus/hover 跟 Default，不消费 raw hover elevation Token。

高度是 shadowElevation，不冒充 tonalElevation。可点击模式选择最后一个活动交互；
禁用直接 snap。目标替换先采样当前帧再取消旧 effect，退出分支按前一目标高度反推。
普通 `shadowElevation(interactionSource=null)` 对应无参数 remember，所以普通卡片保留初始
Default，即使后来替换 Elevation 或默认 Token；模式切换/重挂才清空，仅换模板保留。
这个固定基线行为有双端和控件回归，不能“修复”为每次 Refresh 都取新 Default。

## 已确认选择与未决差异

以下组件选择及范围来自 [拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Card/README.md)：
2026-10-04 维护者选择独立 Card 控件与显式 IsClickable，并批准复用原生 Button 键盘语义，
Space 松开、Enter 按下激活；同日批准 CardColumn，解决原生 StackPanel 在受限高度下无限测量的差别。

同日维护者接受本次 Tween/Bezier 数值精度差异。旧样本的默认 hover 约相差 10⁻⁷ DIP，
自定义高度中断最大约 0.0000022 DIP；截图在容差内，原始 motion 仍 different。
effect 取消和首帧时序已修复，该许可不覆盖时序偏离。共享数值决定见
[Tween / Bezier 精度](../../../../docs/DECISIONS.md#本次保留-tween--bezier-精度差异)，
Card 本次授权依据仍以上面的组件历史记录为准，不把共享决定自动扩成所有差异许可。

半透明 `static.custom.elevated.colors.enabled / disabled` 有可见阴影差异。
维护者当时说明已知问题、本次不用修；这属于未解决观测，不是接受所有透明卡片差异。
已确认共享阴影坐标适配的范围仅见
[阴影坐标决定](../../../../docs/DECISIONS.md#本次保留阴影坐标接入差异)，保留实际失败结果。

共享职责还应核对[交互/激活](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)、
[Core 焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)与
[Surface 描边](../../../../docs/DECISIONS.md#surface-圆角描边)，不在组件内建立另一套规则。

## 一致性边界与验证

维护者明确暂缺的能力仍是“尚未实现”：公开可注入 MutableInteractionSource、可配置 inset
focus ring、任意路径/切角 Shape、完整 ColumnScope。Drag 高度分支已有选择逻辑，但缺少
外部注入且未经端到端验证；默认 opacity 焦点反馈不等于 focus ring 已完成。
文字 raster、实体桌面手动输入未经验证。

[CardTests](../../../../tests/Material3Expressive.Tests/CardTests.cs) 检查普通/可点击尺寸、子控件输入、
命令、动态 automation、键盘、颜色与局部优先级、Column 剩余高度、高度顺序和生命周期。
[CardCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/CardCases.cs) 与
[CardCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/CardCases.kt) 独立构造三种外观、
约束/RTL、状态、主题和配置变更，测真实几何、Surface 高度、回调及截图。
嵌套输入场景给两端真实子 Button 相同 Padding；已有 Button Expressive 默认差别不归因于 Card。

旧合并记录全部用例重复稳定，但仍保留动画数值、Enter 时机与半透明阴影的 different，
不得称为全量通过。历史统计/环境可查固定提交，未跟踪 artifacts 不作长期链接。
以下为复现入口，实际结果需以新生成报告为准：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter "FullyQualifiedName~CardTests"
./tests/VisualParity/parity.ps1 self-test --family card
```

当前依赖见[公共接入](../../../../docs/GETTING_STARTED.md)，参考环境、采样及原始差异读取见
[VisualParity](../../../../tests/VisualParity/README.md)。
