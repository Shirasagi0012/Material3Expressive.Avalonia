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

## 绘制与布局不变量

绘制顺序为 Surface 自身阴影、`_chrome` 背景与被裁剪的 Child、交互覆盖、独立描边；
Toggle/Split 的内部选中覆盖按其额外状态接入。Avalonia Border 的自身 Render 不能覆写，且
BorderThickness 会改变内容测量，因此描边不能简单移回 `_chrome` 或 Surface 的前置 Render。

`_chrome` 承担 Child 的逻辑继承关系、Padding、测量和排列，覆盖层仅为视觉子节点。
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

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~MaterialSurface
```

还应根据所改共享能力选择 Button、Chip、Toggle/Split 等调用方回归。测试源码是可复查入口，
不独自证明全部渲染检查通过；双端运行与比较规则见 [VisualParity](../../../../tests/VisualParity/README.md)。
