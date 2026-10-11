# Surface 维护说明

使用方法见 [README](README.md)。Surface 同时是公开静态容器与 Button、Chip 等控件的绘制部件，
修改前需要检查所有调用方，不能只验证独立表面。

## 上游与本地职责

固定版本以 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`；Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
下列 Material3 文件位于 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

| 上游文件 / 关键符号 | 本地入口 |
| --- | --- |
| [Surface.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Surface.kt)：`Surface`、`LocalAbsoluteTonalElevation` | [MaterialSurface.cs](MaterialSurface.cs)：Child、绘制层次与 tonal elevation 继承 |
| `ColorScheme.kt`：`surfaceColorAtElevation`、`applyTonalElevation` | `ResolveEffectiveBackground` 与 `TonalOverlayAlpha`，通过现有 ColorComposite 合成 |
| `compose/foundation/foundation/src/commonMain/kotlin/androidx/compose/foundation/Border.kt`：`drawRoundRectBorder` / `createRoundRectPath` | [SurfaceOutline.cs](SurfaceOutline.cs)：物理像素描边与圆角内缩 |
| `internal/AnimatedShape.kt` 与共享 indication / ripple | [AnimatedCornerRadius](../../Foundation/Interaction/AnimatedCornerRadius.cs)、[MaterialInteractionController](../../Foundation/Interaction/MaterialInteractionController.cs)，Surface 只呈现其结果 |
| Material 表面与内容颜色默认值 | [Surface.axaml](Surface.axaml)：隐式主题使用 Surface / OnSurface；组件模板的绑定优先 |

这里说明职责，不代表独立 Surface 已涵盖 Compose 所有 clickable / selectable / toggleable 重载。
交互输入、语义及点击执行属于外部 owner。

## 配置输入

Surface 主题将系统 Shadow、Surface、SurfaceTint、EffectsDefault 和四个 state-layer opacity
Token 绑定到内部配置属性；绘制、形状动画和共享交互控制器只消费属性有效值，不自行解析 Token
或回退到另一套 Material 常量。ShadowColor 的 null 仍明确表示使用主题 DefaultShadowColor。
模板的 ButtonSurfaceState.ShapeSpec 是可选的形状规格输入，非 null 时优先于 ShapeAnimation；
ShapeAnimation 是必须就绪的内部可空输入，null 仅表示绑定暂未提供规格，不代表另一个默认值。
在无可用或合法 spring 时不初建形变、不提交新目标；已运行的效果继续使用已捕获的合法规格。
规格恢复后沿原 composition 队列重读当前 pressed/checked 目标，保留 ResetOnShapeChange、defer
和 effect 取消/等待顺序，不补 Expressive fallback。显式 ShapeSpec 的 null 仍表示继承 ShapeAnimation，
显式无效规格不越过它选择主题默认。资源、主题和挂载位置的更新由绑定传入，状态与动画
生命周期仍由现有 Surface/controller 负责。

## 绘制与布局不变量

绘制顺序为 Surface 自身阴影与背景、`_chrome` 中被裁剪的 Child、交互覆盖、独立描边；
Toggle/Split 的内部选中覆盖按其额外状态接入。Avalonia Border 的自身 Render 不能覆写，且
BorderThickness 会改变内容测量，因此描边不能简单移回 `_chrome` 或 Surface 的前置 Render。

`_chrome` 承担 Child 的逻辑继承关系、Padding、测量和排列，Background 保持为空；
Surface.Render 按同一有效圆角绘制 tonal tint 后的背景，对应上游 background(shape).clip(shape) 顺序，
避免背景抗锯齿覆盖再次乘内容裁剪的覆盖。覆盖层仅为视觉子节点。
描边不占布局，不接受输入；它不再次套用背景 clip，避免圆角抗锯齿覆盖被乘两次。
均匀线宽沿固定上游物理像素 ceil 后向内绘制；非均匀 Thickness 保留 Avalonia Border 分支。
普通零线宽无描边，Toggle/Split 描边动画才启用内部 hairline 语义。确认原因、范围和依据见
[Surface 圆角描边决定](../../../../docs/DECISIONS.md#surface-圆角描边)。

Tonal elevation 在 `_chrome` 上传递累积值，当前 Surface 读取祖先值后加上自己的非负值。
着色门槛是纯色画刷的 Color 与当前 Surface Token 精确相等；Background 属性本身不被改写，
只换实际绘制画刷。主题、资源、父级与 elevation 变化需重新解析；阴影高度不参与累加。
着色 alpha 为 `((4.5 * log(elevation + 1)) + 2) / 100`，颜色合成保留画刷 Opacity。

## 交互和生命周期

仅当 TemplatedParent 是启用 `MaterialInteraction.IsInteractive` 的 Control 时，Surface 才在
attach 接入 owner 的控制器。独立 Surface 不注册一套输入状态机。detach 释放 attachment，
由控制器清理呈现者动画；不能为各个视觉层分别创建独立交互状态。

按压形状、选中形状、静止形状依次选择目标，形状更新在共享时钟帧合并。取消旧 effect 后再启动
下一目标，保留速度和上游记忆键规则；Button 与 Split 的配置更新策略并不完全相同。
背景、描边、阴影、ripple clip 必须共用实际圆角；尺寸变化与 spring 过冲还要归一化轮廓。
静态目标同样参与归一化，不再依赖是否存在形状动画。内部 CornerShape.Defined 将公开 CornerRadius
中精确的 1000 值按既有 Full 约定解析为 50%，普通数值保持 DIP；Split 的静止/按压 Full 为 100%，
选中 Circle 为 50%，由模板显式提供上下文。普通 1000 DIP 与 Full 仍无法由现有公开类型区分。
动画先按当前尺寸解析端点并插值，再按 CornerBasedShape 的两侧比例限制生成外圆角；后端圆角路径
仍执行 Skia 的重叠半径 fit。第三目标保存带固定进度的 CornerShape.Blend，不冻结为旧尺寸的数值。
反向保留进度/速度翻转，0/1 边界复用端点。Split 光学偏移读取归一化前的角值。
Chip/Menu 的外部形状动画通过 SetAnimatedCorners 提供已解析的 DIP 值，结束托管时清除；该值不会
再次被解释为 Full，也不覆盖 CornerRadius 的配置绑定。
通用输入/激活职责见[共享决定](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)，
具体公开形状 API 见 [Button](../Button/INTERNALS.md)、[ToggleButton](../ToggleButton/INTERNALS.md)
及 [SplitButton](../SplitButton/INTERNALS.md)。

## 边界与验证

- 尚未实现：任意 Shape、独立 Surface 的点击/选择/切换 API，以及完整 Compose semantics
- 非均匀描边沿用原生绘制，不把它当成 Compose 单一 border width 的等价证明
- 阴影的既有坐标/栅格差异只在[共享决定](../../../../docs/DECISIONS.md#本次保留阴影坐标接入差异)的特定范围保留，不据此接受其他颜色或布局偏离
- 未经验证：各真实桌面后端、设备 DPI 切换和全部调用方组合的视觉一致性

[MaterialSurfaceTests](../../../../tests/Material3Expressive.Tests/MaterialSurfaceTests.cs) 覆盖默认主题、
主题切换、组件模板优先级、描边不占布局、tonal gate、嵌套累积与重挂载；
[MaterialSurfaceRenderTests](../../../../tests/Material3Expressive.Tests/MaterialSurfaceRenderTests.cs)
用实际 Skia headless 渲染检查阴影、覆盖、均匀/非对称/重叠圆角和 hairline。
VisualParity 的 SurfaceCases 双端调用真实 Surface、ElevatedButton、FloatingActionButton，隔离无阴影
背景/贴边内容/描边、大圆角以及窗口左中右位置投影；Button 用例另覆盖大圆角按压与 RTL。
这些离屏用例不代替 Catalog 的真实 GPU、滚动和 DPI 切换验证。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~MaterialSurface
```

还应根据所改共享能力选择 Button、Chip、Toggle/Split 等调用方回归。测试源码是可复查入口，
不独自证明全部渲染检查通过；双端运行与比较规则见 [VisualParity](../../../../tests/VisualParity/README.md)。
