# RadioButton 内部机制与维护说明

## 1. 上游基线与职责映射

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [RadioButton.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/RadioButton.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/RadioButton.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| 公开 `RadioButton` / `RadioButtonImpl` | [RadioButton.axaml](RadioButton.axaml) 原生主题 |
| `RadioButtonColors`、`RadioButtonDefaults`、`RadioButtonTokens` v0_117 | [RadioButtonAppearance.cs](RadioButtonAppearance.cs)、[RadioButtonTokenSource.cs](RadioButtonTokenSource.cs)、[Token 入口](../../Tokens/MaterialTokens.Comp.RadioButton.cs) |
| requiredSize、padding 与 minimumInteractiveComponentSize | [RadioButtonLayout.cs](RadioButtonLayout.cs) |
| dotRadius / color 的 animateAsState 与圆环绘制 | [RadioButtonIndicator.cs](RadioButtonIndicator.cs) |

内部 StyleableRadioButton 不属于此处公开 API 的移植范围。

## 2. 关键机制与架构契约

指示器画布尺寸与外留白由组件 `IndicatorSize` / `IndicatorPadding` Token 同步传入内部布局、Surface 和绘制；布局槽取画布加两侧留白，并分别与最小交互宽高比较。默认值仍为 20 DIP 与四边 2 DIP，描边、波纹和焦点配置独立保留。

内部 motion 输入以 null 表示尚未绑定；挂载、重挂或资源临时失效时不构造无效弹簧，恢复后提交当前目标。已有动画保留其已捕获规格，不以 Expressive 常量补齐配置。

圆点及颜色动画规格由组件 Token 引用系统 motion，经主题传入 Indicator 配置属性；状态切换捕获当前有效规格，不在内部解析 Token 或补 Expressive 默认。完整 RadioButtonColors 的整组替换语义保持不变。

指示器画布 20 DIP，外留白 2 DIP，默认布局槽 48 DIP；外圈中心线半径 9 DIP、描边 2 DIP。
选中 dotRadius 动画目标为 6 DIP，实际填充半径减去半个描边即 1 DIP。
RadioButtonLayout 保留 requiredSize 的受限居中计算，没有将 48 DIP 强写为 owner MinWidth/MinHeight。
可选 Content 是本地组合，对应上游 RadioGroupSample 在整行使用 selectable 的方式；
透明根布局仅在有 Content 时让整行命中。RTL 由 Avalonia 镜像内容行。

dotRadius 使用 FastSpatial、颜色使用 DefaultEffects，目标在对应重组帧合并，重定向先取消旧任务。
传入的 motion spring 未指定 Dp threshold，固定 vectorization 取 0.01，不能误用未消费的
animateDpAsState 默认 dpDefaultSpring 的 0.1。禁用颜色直接到目标；重新启用从新目标建立颜色状态，
但禁用不停止选择圆点动画。VisualAnimationScope 负责注册、detach 清理与重新挂载。

默认色保留动态引用：Primary、OnSurfaceVariant、OnSurface×0.38、OnSurface×0.38。
Skia 使用 Float 物理像素运算；Surface 与共享控制器提供无界、固定半径 20 DIP ripple。
默认焦点使用 opacity indication；可选 inset focus-ring 已通过共享部件接入，
FocusRingCornerRadius=12 对应 24 DIP 反馈区域上的 CircleShape，不改变原有无界 ripple。

`RadioButtonAppearance.InteractionSource` 经模板交给 Indicator，挂载连接现有交互控制器、卸载断开。
null 使用内部源；替换源重建 indication，旧 starts 不回放，按压结束按对象身份配对。
源事件只影响反馈，原生选择、组互斥和命令次序保持不变。对应固定 RadioButtonImpl 的 selectable
interactionSource 参数；AndroidX/Core 两份固定组件文件一致。
焦点环使用同一 ripple 的订阅生命周期，切换 Opacity / InsetRing 更新配置并保留活动反馈。
描边、波纹和焦点轮廓配置独立于 IndicatorSize / IndicatorPadding。共享规则见 [Interaction](../../Foundation/Interaction/README.md)。

## 3. 已确认决定与已知差异

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/RadioButton/README.md)。

2026-10-03，维护者选择“采用原生模板并接受上述范围（推荐）”：保留原生状态更新、组互斥、
Enter 按下激活和四槽颜色 API；状态/组处理先于 Click/Command，与 Compose 仅回调并由应用写回不同。
使用原生主题复用输入与自动化，不额外建立相似子类；接受范围不包含其他视觉差异。
同时明确该阶段不实现完全等价的 `onClick=null` 模式，原生选择自动化不能靠禁用普通输入移除。

另一个已确认视觉适配是消除负半径闪点。固定 Skiko `0.152.0-alpha02` 的 drawCircle 转到 drawOval，
负半径仍可通过排序后椭圆绘出中心点，使取消选择的尾段圆点消失后又闪现。
维护者选择“修正绘制，消除闪点（推荐）”：仅在最终物理绘制半径
`dotRadius * scale - strokeWidth / 2 > 0` 时绘制，保留弹簧状态、参数、阈值和时序。
正半径继续走相同椭圆路径，也消除了选中起始阶段小于 1 DIP 动画半径的幻影点。
这是有意偏离固定桌面基线，不应通过截断 spring 值或删掉尾段采样实现。

共享职责见[交互反馈与键盘激活](../../../../docs/DECISIONS.md)和
[Core Desktop 焦点](../../../../docs/DECISIONS.md)。

尚未实现完全等价的 null 回调模式。默认焦点轮廓是 24 DIP 反馈区域的圆形。
文字栅格、真实辅助技术、完整桌面输入和非 Windows 平台未经双端验收。selection-source 部分 Core 重复捕获出现最大单通道 1 的变化，稳定性门禁保留原始失败。

## 4. 验证入口

- `RadioButtonTests` 和 `SelectionControlHitTests` 检查原生输入、状态/命令顺序、内容命中、主题及生命周期。
- `SelectionDensityTokenTests` 检查尺寸与留白的动态资源覆盖。
- `SelectionInteractionSourceTests` 检查配对、源替换、重挂及外部交互反馈。
- 双端 `RadioButtonCases.cs` / `RadioButtonCases.kt` 的族名为 `radio-button`，共享源用例为 `selection-source`。精确比较保留第 3 节已接受的原生适配及已知像素差异。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter 'FullyQualifiedName~RadioButtonTests|FullyQualifiedName~SelectionDensityTokenTests|FullyQualifiedName~SelectionInteractionSourceTests'
./tests/VisualParity/parity.ps1 self-test --family radio-button
./tests/VisualParity/parity.ps1 self-test --family selection-source
```
