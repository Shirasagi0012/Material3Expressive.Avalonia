# Card 内部机制与维护说明

## 1. 上游基线与职责映射

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
| `Card.kt` 内容 `Column`；Foundation `Column.kt` / `RowColumnMeasurePolicy.kt` / `Box.kt` | [CardColumn.cs](CardColumn.cs)、[CardColumnItem.cs](CardColumnItem.cs)：顺序测量及显式 Box + weight/fill/align 宿主 |
| `tokens/FilledCardTokens.kt`、`ElevatedCardTokens.kt`、`OutlinedCardTokens.kt` | [MaterialTokens.Comp.Card](../../Tokens/MaterialTokens.Comp.Card.cs) 与 CardTokenSource |

## 2. 关键机制与架构契约

`Shape` 由两个默认模板绑定到 MaterialSurface，三种派生卡片及模式切换共用相同入口。
对应固定 Card.kt 的 shape 参数；无状态副本或另一个裁剪节点。动态切角、路径边框、阴影及反馈
遵循 [Surface 契约](../Surface/INTERNALS.md)和[共享决定 §35](../../../../docs/DECISIONS.md#35-surface-与-card-的显式路径形状)，
null 保留既有 CornerRadius。此非破坏性扩展在本轮剩余移植自主决策授权内实施。

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

`CardColumnItem` 对应 `Box(Modifier.weight(...).align(...))`，不是对任意 Control 注入父最小约束。
固定 AndroidX/Core 的 Foundation RowColumnMeasurePolicy、Column 和 Box 规则为依据：先测所有
无权重项，再按剩余物理像素和 Float 权重分配；四舍五入后的正/负余数依次给前面的权重项。
Fill=false 的未用空间不重新分配；无界最大高度改用 Column 自身最小高度作为分配目标。
默认 Weight=NaN 表达无 modifier，与已有 ButtonGroupItem API 一致；正无穷按上游转为 Float.MaxValue。
Alignment 是 Start/Center/End 逻辑位置，宿主内容以 TopStart 放置。

Avalonia `Measure(Size)` 仅表达最大约束；宿主在自己的 MeasureOverride 中消化分配的最小高度，
Child 仍按 `0..max` 测量，等价于 Box 默认 `propagateMinConstraints=false`。没有临时写 Child.MinHeight，
没有新增共享约束协议。2026-11-09 本轮用户明确授权补基础能力与调整 API，批准此显式组合入口。
原 CardColumn 的引入理由（受限高度下 StackPanel 无限测量）仍有效，无权重的既有直接子项沿用原路径。

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

### 交互源与高度生命周期

`InteractionSource` 仅在可点击模式连接，null 使用私有源。组件按开始对象身份维护独立的高度交互列表。
替换源时先取消旧原生输入，再退订旧源并订阅新源；保留外部交互及运行中的高度动画，indication 独立重建。
模式切换和卸载清空高度列表，模板重建保留。源事件只参与反馈。

高度使用 Float Animatable/Tween，通过 `BindFrameCommittedValue` 保持动画属性贡献；
`ScheduleFrameCommit` 在捕获当帧显示值后运行 effect，禁用 snap 在下一批呈现。
模板替换和卸载释放绑定及待提交句柄，恢复底层属性源。同类型新开始对象由共享状态层按身份重新启动动画。

## 3. 已确认决定与已知差异

以下组件选择及范围来自 [拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Card/README.md)：
2026-10-04 维护者选择独立 Card 控件与显式 IsClickable，并批准复用原生 Button 键盘语义，
Space 松开、Enter 按下激活；同日批准 CardColumn，解决原生 StackPanel 在受限高度下无限测量的差别。

维护者同时接受了指定范围的 Tween/Bezier 数值精度差异。旧样本的默认 hover 约相差 10⁻⁷ DIP，
自定义高度中断最大约 0.0000022 DIP；截图在容差内，原始 motion 仍 different。
effect 取消和首帧时序已修复，该许可不覆盖时序偏离。共享数值决定见
[Tween / Bezier 精度](../../../../docs/DECISIONS.md)，
Card 的数值适配范围以组件确认记录为准。

历史半透明 `static.custom.elevated.colors.enabled / disabled` 曾有可见阴影差异，维护者此前允许暂缓。
当前 `card-elevation-current` 已验证这两个场景图像在原容差内；旧失败不再作为当前未解决依据。
已确认共享阴影坐标适配的范围仅见
[阴影坐标决定](../../../../docs/DECISIONS.md)，保留实际失败结果。

共享职责还应核对[交互/激活](../../../../docs/DECISIONS.md)、
[Core 焦点](../../../../docs/DECISIONS.md)与
[Surface 描边](../../../../docs/DECISIONS.md)，不在组件内建立另一套规则。

切角及自定义路径通过 Shape 接入共享 Surface；权重/Fill/逻辑对齐通过显式 CardColumnItem 提供。
任意 Control 的透明 ColumnScope.weight、alignBy 与 intrinsic weight 测量尚未实现；文字栅格和完整真实设备输入仍未经验证。共享源入口授权见引入提交 `e89f5fc8`。

切角卡片新增验证位于双端 `material-shape` 族，包含三种主题、普通/可点击、RTL、焦点、波纹及动态尺寸。
方向切换瞬间 Core 描边保留旧方向的六个卡片用例仍超差，详见 [Surface 已知差异](../Surface/INTERNALS.md#3-已确认决定与已知差异)；
没有放宽容差或从用例集中移除。此差异不属于此前允许的阴影/数值边界。

## 4. 验证入口

2026-11-09 新增 `CardColumnTests` 15 项，连同既有 Card 行为共 43 项通过；Catalog 构建通过。
双端 `card-column-verified` 30 例为 29 equal、1 different，执行错误零且重复稳定，已检查真实图像。
包含不等 Float 权重、正/负舍入余数、Fill=false、无界/最小高度、耗尽、RTL、内容与宿主几何及动态更新。
额外 1x 行为测试发现并修正 RTL 奇数像素居中：先在物理方向四舍五入，再转换为 Avalonia 逻辑位置。
保留的 `static.fractional-pixel.rtl` 差异来自外层居中：宽 101.25 DIP 时 Compose Card.x=109.5、
Avalonia Grid 的 Center 使 Card.x=109.25；所有宿主/内容的相对坐标及尺寸一致，整个子树平移 0.25 DIP。
这是宿主层的真实差异，没有移动截图、放宽容差或更改基准来消除。完整 ColumnScope 的未实现范围仍见上节。

当前全族 `card-elevation-current` 共 97 例：68 equal、27 withinTolerance、2 different，
全部重复稳定且无执行错误。剩余为 filled.enter 原生键盘激活时序及 filled.ambient-color 内容色，
不属于高度或半透明阴影失败；仍保留实际门禁结果。
后者已通过新增实际 ambient/content 读数定位为夹具的宿主输入不等价：Core 默认黑色、本地 Window
默认 OnSurface。只统一该场景的环境输入后，`card-ambient-matched` 颜色、几何、图像全部精确相等
且稳定；组件的 ContentColorFor 没有改动。证据与输入范围见 VisualParity DECISIONS。

2026-11-06 本轮依据用户扩大授权复用共享决定 §19/§26：移除 snap 时临时写普通属性、下一帧
重连绑定的路径。`elevation-commit-three` 的三个 Card 配置变更/禁用/帧内合并场景与
`card-elevation-frame` 的 elevated.source-disable 均读数精确相等、图像在既有容差内且重复稳定。
历史 double 精度许可不再作为这些当前场景的差异依据。`ComponentMotionContributionTests` 与
`ElevationSourceLifetimeTests` 验证底层动态绑定恢复、模板替换和待提交项清理；无公开 API 迁移。

- `CardTests` 覆盖普通/可点击模式、子控件输入、命令、布局、颜色、交互源和高度顺序。
- `ElevationSourceLifetimeTests` 与 `StateLayerTests` 覆盖独立 collector 生命周期和重叠交互中断。
- 双端 `CardCases.cs` / `CardCases.kt` 的族名为 `card`，包括三种变体的 source-drag、source-replace、source-disable 和 source-mode。
- 已有结果仍包含精确高度、Enter 时机和半透明阴影差异，复验应分别读取状态、截图与稳定性结果。
- 2026-11-04 焦点矩阵 `focus-ring` 运行 `20261104-080938-55d3d95b` 的 Card 子集 39 例均稳定、
  执行错误 0：21 例完全相等、18 例容差内。覆盖三变体的进入/退出/反向、悬浮叠加、尺寸、禁用、
  Opacity/InsetRing 切换、换源、普通/可点击切换、卸载重挂、深色和自定义环。全族 81 例无超差。
  `CardTests` 增加焦点与源生命周期行为断言；Catalog **Inset focus ring** 展示三变体。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter "FullyQualifiedName~CardTests|FullyQualifiedName~ElevationSourceLifetimeTests"
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~CardColumnTests
./tests/VisualParity/parity.ps1 self-test --family card-column
./tests/VisualParity/parity.ps1 self-test --family card
./tests/VisualParity/parity.ps1 self-test --family focus-ring
```
