# Surface 内部机制与维护说明

## 1. 上游基线与职责映射

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
| `Surface(onClick)` / `Surface(selected, onClick)` / `Surface(checked, onCheckedChange)` | `ClickableSurface` / `SelectableSurface` / `ToggleableSurface`；复用 MaterialSurface、ButtonLayout 与共享输入事件流 |

静态 MaterialSurface 的交互输入、语义及点击执行仍属于外部 owner；三个交互组件作为独立 owner
消费相同绘制能力。固定 AndroidX 与 Core 的 Surface.kt 内容相同。

## 2. 关键机制与架构契约

`Shape` 接入共享 `MaterialShape`，映射固定 Foundation `shape/CutCornerShape.kt` / `CornerBasedShape.kt`，
通用边框和焦点边框分别映射 Foundation 与 material3-ripple 的 `Border.kt::drawGenericBorder`。
四个 `RelativeScalar` 保留 DIP/百分比，先在物理 Float 坐标归一化，再返回 DIP。
切角全部为零时按 Rectangle 分支呈现边框与焦点，不能当作通用路径而丢失内环。
路径快照的 FillRule 与 Transform 必须传到 Skia；多轮廓 EvenOdd 尤其不能在 AddPath 后恢复为 Winding。
Shape 变更与尺寸/方向变化同步更新几何；只在挂载期间订阅其属性，换源先退订，重挂读取最新值。
普通 `CornerRadius` 仍独立保留；Shape 清空后恢复。同族 CutCornerShape 复用现有播放器的四角尺寸插值，
自定义路径直接切换，不执行任意路径插值。通用边框的渐变尺寸取 ceil 后的路径缓存，原点保持在布局坐标原点；
遮罩单独移到路径 bounds，不能把画刷原点一起移到 bounds，也不能只移动裁剪区域而不移动遮罩。
API 与历史范围修订见 [共享决定 §35](../../../../docs/DECISIONS.md#35-surface-与-card-的显式路径形状)。

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

绘制顺序为 Surface 自身阴影与背景、`_chrome` 中被裁剪的 Child、交互覆盖、独立描边；
Toggle/Split 的内部选中覆盖按其额外状态接入。Avalonia Border 的自身 Render 不能覆写，且
BorderThickness 会改变内容测量，因此描边不能简单移回 `_chrome` 或 Surface 的前置 Render。

`_chrome` 承担 Child 的逻辑继承关系、Padding、测量和排列，Background 保持为空；
Surface.Render 按同一有效圆角绘制 tonal tint 后的背景，对应上游 background(shape).clip(shape) 顺序，
避免背景抗锯齿覆盖再次乘内容裁剪的覆盖。覆盖层仅为视觉子节点。
描边不占布局，不接受输入；它不再次套用背景 clip，避免圆角抗锯齿覆盖被乘两次。
纯视觉节点不继承 Avalonia 的 FlowDirection。描边、交互层和选中覆盖层显式绑定 Surface 的方向，
非均匀描边的原生 Border 子层也绑定其父层方向，使各层沿用 Surface 的同一局部坐标，避免 RTL 二次镜像。
该绑定跟随运行时方向变化；没有交换公开 CornerRadius 的角值，也没有改变内容的逻辑继承关系。
均匀线宽沿固定上游物理像素 ceil 后向内绘制；非均匀 Thickness 保留 Avalonia Border 分支。
普通零线宽无描边，Toggle/Split 描边动画才启用内部 hairline 语义。确认原因、范围和依据见
[Surface 圆角描边决定](../../../../docs/DECISIONS.md)。

Tonal elevation 在 `_chrome` 上传递累积值，当前 Surface 读取祖先值后加上自己的非负值。
着色门槛是纯色画刷的 Color 与当前 Surface Token 精确相等；Background 属性本身不被改写，
只换实际绘制画刷。主题、资源、父级与 elevation 变化需重新解析；阴影高度不参与累加。
着色 alpha 为 `((4.5 * log(elevation + 1)) + 2) / 100`，颜色合成保留画刷 Opacity。

仅当 TemplatedParent 是启用 `MaterialInteraction.IsInteractive` 的 Control 时，Surface 才在
attach 接入 owner 的控制器。独立 Surface 不注册一套输入状态机。detach 释放 attachment，
由控制器清理呈现者动画；不能为各个视觉层分别创建独立交互状态。

按压形状、选中形状、静止形状依次选择目标，形状更新在共享时钟帧合并。取消旧 effect 后再启动
下一目标，保留速度和上游记忆键规则；Button 与 Split 的配置更新策略并不完全相同。
IconButton 通过内部 ButtonSurfaceState.IsPressed 传入独立的 source collector；未提供时仍读取 controller。
其换源重建 indication 时使用现有控制器的保留 Surface motion 选项，不能连带取消记忆中的形变。
IconButton 形状实例重建还会使 ripple 的 focusRingShape 身份改变，须清除旧 indication；首次配置和
每帧形变采样不会触发这次清理，详见该组件的 [生命周期说明](../IconButton/INTERNALS.md#外部交互源与形变生命周期)。
背景、描边、阴影、ripple clip 必须共用实际圆角；尺寸变化与 spring 过冲还要归一化轮廓。
静态目标同样参与归一化，不再依赖是否存在形状动画。内部 CornerShape.Defined 将公开 CornerRadius
中精确的 1000 值按既有 Full 约定解析为 50%，普通数值保持 DIP；Split 的静止/按压 Full 为 100%，
选中 Circle 为 50%，由模板显式提供上下文。普通 1000 DIP 与 Full 仍无法由现有公开类型区分。
动画先按当前尺寸解析端点并插值，再按 CornerBasedShape 的两侧比例限制生成外圆角；后端圆角路径
仍执行 Skia 的重叠半径 fit。第三目标保存带固定进度的 CornerShape.Blend，不冻结为旧尺寸的数值。
反向保留进度/速度翻转，0/1 边界复用端点。Split 光学偏移读取归一化前的角值。
Chip/Menu 的外部形状动画通过 SetAnimatedCorners 提供已解析的 DIP 值，结束托管时清除；该值不会
再次被解释为 Full，也不覆盖 CornerRadius 的配置绑定。
通用输入/激活职责见[共享决定](../../../../docs/DECISIONS.md)，
具体公开形状 API 见 [Button](../Button/INTERNALS.md)、[ToggleButton](../ToggleButton/INTERNALS.md)
及 [SplitButton](../SplitButton/INTERNALS.md)。

可选 InsetRing 通过独立视觉子节点置于普通 outline 与 checked overlay 之上，
跟随 EffectiveCornerRadius；内部 FocusRingCornerRadius 可用于上游焦点形状与背景不同的组件。
该部件持有自己的动画与订阅，默认延迟到启用环后创建，不占内容尺寸；CheckBox / RadioButton / ListItem
的单一 ripple 跨焦点模式保留订阅，因此挂载时即创建呈现部件，在 Opacity 下只收集交互。
内部 `SetAnimatedCorners` 的 shapeStateChanged 输入保留可观察形状进度的绘制失效，即使重定向首帧
仍求得同一轮廓；这不改变纯主题色更新的静止绘制缓存。详情见
[Interaction](../../Foundation/Interaction/README.md)。静态 Surface 不注册输入或显示焦点环。

## 3. 已确认决定与已知差异

共享的轮廓求值、背景裁剪、描边、交互激活和阴影坐标选择见 [DECISIONS.md](../../../../docs/DECISIONS.md)，适用范围随各条决定维护。

Tooltip 的内部 `SetContainerGeometry` 接入背景、内容 clip 与阴影的路径并集，不参与测量；清空后恢复既有圆角路径。
它仅供无独立描边/交互的 Tooltip 模板使用，不代表任意形状的边框、波纹或焦点环已经完成；授权和范围见共享决定 §27。

- 剩余移植任务授权范围内新增三个非破坏性组件入口，不改变静态 MaterialSurface。
  点击/选择/切换各自保留语义：普通 Click/Command、IsSelected + Click、IsChecked + CheckedChangeRequested。
  不建立共享交互控件基类；ClickableSurface 的两个派生类仅表达 Surface 自身重载。
- 三个入口的内容色复用 ContentColorFor；禁用保留颜色和最小交互留白。48 DIP 来自上游
  LocalMinimumInteractiveComponentSize 默认值，继承环境可覆盖；Surface 没有独立的组件 Token 表。
  Surface 的两个 elevation 直接绑定，不引入 Button/Card 按交互改变高度或形状的行为。
- 组件负责 Enter/Space 的按键释放激活，保留原生指针捕获和 Command.CanExecute。
  固定 Core `foundation/Clickable.kt` 按 keyCode 独立记录 press；本地按键集合保留双键重叠、重复抑制、
  未匹配释放不激活，换源/禁用/失焦/卸载清理。共享 controller 继续负责每个键的事件配对和波纹。
  原生输入连到一个私有或外部 MaterialInteractionSource，换源取消旧的自身输入并重建 indication，
  卸载断开源；外部事件不产生业务操作。模板绘制部分继续由 MaterialSurface 自身释放。
- 自定义路径 Shape 已接入，完整 Compose semantics 尚未实现。UIA 通过 Custom 类型和 Invoke/SelectionItem/Toggle
  表达上游未指定 role 的语义；屏幕阅读器实际表现未经验证。
- 原生 ContentPresenter 可以传播 Stretch 分配，但不能令任意显式 Width/Height 子控件服从 Compose
  Box 的最小约束。该边界不是已接受的语义等价：推荐先明确全库父级最小约束传递能力，
  不在 Surface 内改写调用者的尺寸属性。例如 Width=120 的 Surface 内部 Width=32 的子节点，
  Avalonia 保留 32，Compose Modifier.size(32.dp) 在紧约束下可成为 120；变更会影响其他内容容器，待维护者决定。
- 非均匀 Thickness 描边沿用原生绘制。
- 既有阴影坐标与栅格差异按共享决定的特定范围保留。
- 各真实桌面后端、设备 DPI 切换和全部调用方组合的视觉一致性未经验证。
- 2026-11-08 新增 `material-shape.dynamic.{click,card,elevated,outlined}.resize.{ltr,rtl}`
  保留八个未解决的方向切换差异：33ms 切换 FlowDirection 时，固定 Core 的背景/裁剪已经换向，
  通用描边仍保留旧方向；49ms 尺寸变化后描边恢复。本地同帧更新这些层。已对照实际截图，
  尚未确认 Core cache delegate 的失效传播原因；这不是已接受差异，不能用任意延迟本地描边消除。
  同族 Button 快速反向的少量 Core 重复/额外截图也可能不稳定，Avalonia 重复稳定；分别读取 case.yaml，
  不把两端 withinTolerance 等同于参考重复性通过。

## 4. 验证入口

2026-11-08 显式形状交付：`material-shape-final` 305 例为 30 equal、267 withinTolerance、8 different，
零执行错误。八个差异均为上文方向切换描边；9 例参考重复性不稳定（8 个 Button reverse 的 160ms
及 click.hit.zero 的 176ms），均只发生在 Core，Avalonia 两种重复检查全部稳定。保留原始差异与自检门禁。
额外 `shape-{radial,conic}-{cut,offset}` 四例为 2 equal、2 withinTolerance，均稳定且无执行错误；
线性渐变的切角、孔洞与偏移路径已包含在 305 例中。未以这些样本宣称任意自定义画刷和全部 DPI 已验证。
已查看方向切换、非圆角焦点、孔洞、偏移渐变和 RTL 三状态动画的实际双端截图。

最终整仓 Debug/Release 及 Catalog Release 构建通过；受影响的控件行为回归 318/318 通过。
全库最终 Debug 运行 2532/2538：5 项 Headless DefaultRenderLoop.Add 清理线程访问异常，
1 项 NonInteractiveScrollBar 淡出断言失败；这六项所在类隔离重跑 63/63 通过。此结果不能写为全量通过，
也不足以证明失败与本轮改动无关。日志为 `artifacts/shape-full-tests-final.log` 与
`artifacts/shape-full-failures-recheck.log`。固定提交、依赖锁、比较容差均未改变。

- `MaterialSurfaceTests` 覆盖主题、模板优先级、描边布局、tonal gate、嵌套累积及重挂。
- `MaterialShapeTests` 覆盖切角归一化、百分比、RTL、真实模板、孔洞 FillRule、快照、命中及生命周期。
  `material-shape` 双端族覆盖 Surface/Card/Button/Chip/IconButton/ToggleButton 的路径、阴影、裁剪、
  波纹、焦点、按压反向、三目标形变和中途尺寸变化。新增 Catalog 示例位于 SurfaceDemo 与主窗口。
- `InteractiveSurfaceTests` 覆盖三个交互入口的自然尺寸、最小留白、描边、内容色、键盘释放/取消、
  受控绑定、automation、命令禁用、源替换与重挂；Catalog `SurfaceDemo` 提供完整接线。
- `MaterialSurfaceRenderTests` 用 Skia headless 渲染检查阴影、覆盖、圆角和 hairline，`InsetFocusRingTests` 检查反馈缓存与形状重绘。
- 方向回归覆盖 LTR → RTL → LTR 的非对称描边（均匀及原生非均匀分支）、悬停层与选中覆盖层；
  `MaterialSurfaceRenderTests` 17 项通过。`surface` 双端 23 例为 10 equal、13 withinTolerance、
  0 different、零执行错误且重复稳定（`20261105-042829-4912403a`），包含六个新增方向/绘制层用例。
  Button 的 RTL 非对称按压也在现有门禁内且重复稳定（`20261105-042956-3d2cdb73`）。
- 双端 `SurfaceCases.cs` / `SurfaceCases.kt` 的族名为 `surface`。共享修改还应选择 Button、Chip、Toggle/Split、ListItem 等实际调用方回归。
- 2026-11-05 `shadow-contour-current` 在当前实现上新增 24 个静态轮廓用例：6 种尺寸/圆角、
  1/6 DIP 阴影、实色/透明填充。共 47 例为 10 equal、32 withinTolerance、5 different，
  零执行错误且全部重复稳定。失败为透明 circle/large/morph/pill 的 1 DIP，以及透明 pill 的 6 DIP；
  所有几何读数一致。它们直接使用真实 MaterialSurface/Compose Surface，没有动画、输入或模拟阴影。
  Catalog `SurfaceDemo` 增加相应低高度透明样本，便于在桌面后端观察；该展示不等同于 GPU 验证。
- 同轮调查固定 Skiko `0.152.0-alpha02` 的 `ShadowUtils.drawShadow`，独立 ambient-only 探针亦出现
  morph 轮廓内部渐变，本地则没有。物理像素路径替代 DIP+CTM 的实验图像不变；直接采用
  `SkShadowTessellator::handleQuad` 的非 Ganesh 控制点分支虽令该样本相等，却令整个 Surface 族
  出现 8 个不同用例，且使既有圆形 ambient 网格测试失败。这不足以证明固定原生构建采用该分支，
  当时两项实验均撤回；保留 Ganesh 算法及全部门禁。那轮尚未定位的内部渐变不能
  归入 DECISIONS §20 已接受的坐标栅格差异。实验报告 `shadow-quad-surface`、`chip-shadow-quad`
  只用于说明当时被否决的单项修改，不再作为当前实现结论。
- 2026-11-06 本轮依据用户扩大授权重新决定共享阴影接入，详见 DECISIONS §20。
  独立固定 Skiko 的 quad/control-point 全图比较及 circle/blur fallback 比较确认原生曲线分支，
  随后同时保留该分支与 Core 的物理像素路径；不增加 alpha、不按高度特判、不调整门禁。
  修复前 `surface-high-elevation` 的 55 例有 10 different；修复后 `surface-native-curve` 为
  26 equal、29 withinTolerance、0 different，零执行错误且全部重复稳定，结构化读数全部相等。
  新增 8 例是 56×56 DIP、16/28 DIP 圆角、16/40 DIP elevation、实色/透明填充，
  全部在原门禁内；原先 5 个透明低高度失败也全部消除。已查看实际高阴影图像。
  `SurfaceDemo` 提供相应样本；公开 DIP 用法不变。当前是 raster 验证，GPU/DPI 切换仍需独立验证。
- 2026-11-04 `interactive-surface` 最终运行 `20261104-081657-a2237b4f`：111 例执行错误 0，
  92 例像素完全相等、19 例容差内、超差 0；覆盖三个入口的静态颜色/尺寸/描边/阴影与输入/换源/重挂。
  105 例稳定，6 例 Core 波纹重叠时不稳定：click.keys / keys-reverse、select.keys-reverse、toggle.keys
  的额外被动截图，以及 click.source / toggle.source 的普通重复捕获，最大单通道变化均为 1。
  Avalonia 自检均稳定；保留原始截图及不稳定门禁，不以两端比较相等替代重复性通过。
  Windows Catalog 实际点击、Enter、Space、选择与切换回写已冒烟验证；这不扩大为全部辅助技术验证。
- `surface-focus` 运行 `20261104-082659-0562d80c`：30 例均稳定、执行错误 0，27 例完全相等、
  3 例容差内、超差 0；覆盖三个入口的深色/自定义焦点环、反向、禁用、模式切换、换源与重挂。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter 'FullyQualifiedName~MaterialSurface|FullyQualifiedName~InteractiveSurfaceTests|FullyQualifiedName~InsetFocusRingTests'
./tests/VisualParity/parity.ps1 self-test --family surface
./tests/VisualParity/parity.ps1 self-test --family interactive-surface
./tests/VisualParity/parity.ps1 self-test --family surface-focus
```
