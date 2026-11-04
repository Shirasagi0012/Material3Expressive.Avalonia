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
| `Card.kt` 内容 `Column` | [CardColumn.cs](CardColumn.cs)：按剩余高度顺序测量，不是完整 ColumnScope |
| `tokens/FilledCardTokens.kt`、`ElevatedCardTokens.kt`、`OutlinedCardTokens.kt` | [MaterialTokens.Comp.Card](../../Tokens/MaterialTokens.Comp.Card.cs) 与 CardTokenSource |

## 2. 关键机制与架构契约

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

禁用等立即 snap 分支保留 Surface 当帧已提交高度，下一组合帧重新连接动画绑定。模板替换和卸载释放绑定，
延迟提交检查 Surface 与动画对象身份。同类型新开始对象由共享状态层按身份重新启动动画。

## 3. 已确认决定与已知差异

以下组件选择及范围来自 [拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Card/README.md)：
2026-10-04 维护者选择独立 Card 控件与显式 IsClickable，并批准复用原生 Button 键盘语义，
Space 松开、Enter 按下激活；同日批准 CardColumn，解决原生 StackPanel 在受限高度下无限测量的差别。

维护者同时接受了指定范围的 Tween/Bezier 数值精度差异。旧样本的默认 hover 约相差 10⁻⁷ DIP，
自定义高度中断最大约 0.0000022 DIP；截图在容差内，原始 motion 仍 different。
effect 取消和首帧时序已修复，该许可不覆盖时序偏离。共享数值决定见
[Tween / Bezier 精度](../../../../docs/DECISIONS.md)，
Card 的数值适配范围以组件确认记录为准。

半透明 `static.custom.elevated.colors.enabled / disabled` 有可见阴影差异。
该阴影问题保留为未解决观测，维护者此前允许暂缓处理。
已确认共享阴影坐标适配的范围仅见
[阴影坐标决定](../../../../docs/DECISIONS.md)，保留实际失败结果。

共享职责还应核对[交互/激活](../../../../docs/DECISIONS.md)、
[Core 焦点](../../../../docs/DECISIONS.md)与
[Surface 描边](../../../../docs/DECISIONS.md)，不在组件内建立另一套规则。

尚未实现任意路径/切角 Shape 和完整 ColumnScope。文字栅格和完整真实设备输入仍未经验证。共享源入口授权见引入提交 `e89f5fc8`。

## 4. 验证入口

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
./tests/VisualParity/parity.ps1 self-test --family card
./tests/VisualParity/parity.ps1 self-test --family focus-ring
```
