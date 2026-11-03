# CheckBox 内部机制与维护说明

## 1. 上游基线与职责映射

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [Checkbox.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Checkbox.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Checkbox.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| `Checkbox` / `TriStateCheckbox`、`CheckboxImpl` | [CheckBox.axaml](CheckBox.axaml) 隐式原生主题 |
| `CheckboxColors`、`CheckboxDefaults` 与 Checkbox Token | [CheckBoxAppearance.cs](CheckBoxAppearance.cs)、[CheckBoxTokenSource.cs](CheckBoxTokenSource.cs)、[Token 入口](../../Tokens/MaterialTokens.Comp.CheckBox.cs) |
| `minimumInteractiveComponentSize`、padding、requiredSize | [CheckBoxLayout.cs](CheckBoxLayout.cs) 区分指示器与可选内容 |
| `CheckboxImpl` 的 Transition、勾号路径与绘制分支 | [CheckBoxIndicator.cs](CheckBoxIndicator.cs)；Skia 绘制与 VisualAnimationScope 生命周期 |
| Core `CanvasDrawScope` / `SkiaBackedPaint` | 指示器中 Float 物理像素路径、Stroke 的实际 Paint 配置 |

## 2. 关键机制与架构契约

指示器画布尺寸与外留白由组件 `IndicatorSize` / `IndicatorPadding` Token 同步传入内部布局、Surface 和绘制；布局槽取画布加两侧留白，并分别与最小交互宽高比较。默认值仍为 20 DIP 与四边 2 DIP，描边、波纹和焦点配置独立保留。

内部 motion 输入以 null 表示尚未绑定；挂载、重挂或资源临时失效时不构造无效弹簧，恢复后提交当前目标。已有动画保留其已捕获规格，不以 Expressive 常量补齐配置。

勾号形变及选中/取消颜色动画规格由组件 Token 引用系统 motion，经主题传入 Indicator 配置属性；动画只读取当前有效规格，不在内部重新解析 Token 或补 Expressive 默认。完整 CheckBoxColors 与描边的 null 语义保持不变。

固定基线 `isCheckboxStylingFixEnabled=false`：画布为 20 DIP、外留白 2 DIP、圆角 2 DIP、
默认布局槽 48 DIP。不提前采用 flag 开启后的 18 DIP 画布或新勾号坐标。
`DisabledCheckmarkColor` 保留公开槽，但此分支仍用 checkedCheckmarkColor 绘制禁用勾号。
透明色使用透明黑，不能用 Avalonia 透明白替换而改变颜色插值。
默认描边按 `floor(2 * density)` 取物理像素宽度，显式 Stroke 则将 DIP 直接乘 density；
Dashes / DashOffset 也是绝对 DIP，不是 Avalonia Pen 的笔宽倍数。

CheckBoxLayout 的受限居中先按正常 48 DIP 槽舍入，再对受约束偏移截断，不能合并为一次连续计算。
有 Content 时透明根布局让整行参与命中；无 Content 时只保留默认 24×24 DIP 指示器命中区域。
该组合对应上游 `CheckboxWithTextSample` 把 toggleable 放在外层行上的用法，
不意味着 Checkbox Composable 自带标签槽。RTL 镜像内容行，指示器绕过自动镜像以保留勾号方向。

Indicator 不写原生状态。勾号片段以 Skia PathMeasure 绘制，draw fraction 和中心吸附共享 Transition：
进入选中/不确定使用 DefaultSpatial；离开至 false 的相关分支保留 100ms Snap delay。
颜色消费 Animatable，选中使用 DefaultEffects，未选中使用 FastEffects；
禁用 box/border 直接到目标，重新启用时从新目标建立颜色状态，勾号颜色仍走自身动画。
目标在共享时钟的相应重组帧合并；只有状态目标改变才启动 Transition 帧循环，完成即取消等待，
避免 idle 异步唤醒让首帧注册漂移。detach 和模板替换释放订阅、帧等待与动画。

Surface 与共享交互控制器提供无界、固定 20 DIP 半径的 ripple。输入、命令和自动化仍属于
原生 CheckBox；不要用共享反馈控制器再次切换状态。

`CheckBoxAppearance.InteractionSource` 将源通过模板传到 Indicator，挂载后连接已有交互控制器。
null 使用该模板实例的内部源；替换源重建 indication，清除旧 ripple 和状态层，不重放旧事件。
卸载/模板替换断开源，原生未结束输入通过对应 Cancel/Exit 事件结束。选择和三态循环仍由原生控件负责。
此入口对应固定 CheckboxImpl 的 triStateToggleable interactionSource 参数，两份固定上游文件一致。
焦点环使用同一 ripple 的订阅生命周期，切换 Opacity / InsetRing 更新配置并保留活动反馈。
描边、波纹和焦点轮廓配置独立于 IndicatorSize / IndicatorPadding。共享规则见 [Interaction](../../Foundation/Interaction/README.md)。

## 3. 已确认决定与已知差异

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/CheckBox/README.md)。

2026-10-03，维护者要求合并为原生 CheckBox，并确认“这两方面保留原生状态切换和键盘行为即可。按这个方案实施”。
因此复用两态/三态循环、默认双向绑定、状态先于 Click/Command 的顺序和 Enter 按下激活，
避免两套相似控件及转发 API。Compose 的外部状态写回、三态只请求动作及 Enter 松开激活不同；
这一接受仅覆盖原生适配，不批准其他视觉或语义偏离。

自定义描边另有明确例外：固定 Core 的 SkiaBackedPaint 缓存初始 Join 为 Round，底层实际为 Miter；
首次请求 Round 可跳过底层赋值。独立 Paint 诊断将 `checkbox.static.stroke.round.on` 差异定位在勾号转角。
维护者同日要求“这是上游 bug，始终按参数画出圆角”。本地始终将 Cap / Join / MiterLimit 写入真实 Paint，
默认 Miter 不受影响；仅适用于 CheckBox 自定义描边，不构成修复所有上游 bug 的通用授权。

同日维护者选择“本次保留原生范围，记录 null 回调模式未实现（推荐）”。原生控件没有移除
ToggleProvider 的开关，因此本阶段不增加独立只显示指示器，也不伪装完全等价的 null 回调模式。

共享反馈与桌面焦点由[键盘职责](../../../../docs/DECISIONS.md)及
[Core Desktop 焦点](../../../../docs/DECISIONS.md)维护。

尚未实现任意 PathEffect 映射和完全等价的 null 回调模式。默认焦点轮廓是 24 DIP 反馈区域的 25% 圆角。
文字栅格、真实辅助技术、完整桌面输入和非 Windows 平台未经双端验收。selection-source 部分 Core 重复捕获出现最大单通道 1 的变化，稳定性门禁保留原始失败。

## 4. 验证入口

- `CheckBoxTests` 和 `SelectionControlHitTests` 检查原生输入、状态/命令顺序、内容命中、主题及生命周期。
- `SelectionDensityTokenTests` 检查尺寸与留白的动态资源覆盖。
- `SelectionInteractionSourceTests` 检查配对、源替换、重挂及外部交互反馈。
- 双端 `CheckBoxCases.cs` / `CheckBoxCases.kt` 的族名为 `checkbox`，共享源用例为 `selection-source`。精确比较保留第 3 节已接受的原生适配及已知像素差异。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter 'FullyQualifiedName~CheckBoxTests|FullyQualifiedName~SelectionDensityTokenTests|FullyQualifiedName~SelectionInteractionSourceTests'
./tests/VisualParity/parity.ps1 self-test --family checkbox
./tests/VisualParity/parity.ps1 self-test --family selection-source
```
