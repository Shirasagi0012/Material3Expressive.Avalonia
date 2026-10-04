# Loading Indicator

`LoadingIndicator` 与 `ContainedLoadingIndicator` 分别移植固定 Compose 的无容器颜色与有容器颜色入口，
每个控件支持确定和不确定两种模式。依据为 `docs/upstream-baseline.json` 的
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 中 `LoadingIndicator.kt`、
`tokens/LoadingIndicatorTokens.kt` 和 `internal/ShapeUtil.kt`；固定桌面 Core 的 LoadingIndicator 实现相同。

## 用法与 API

应用 TopLevel 在加载 XAML 或设置内容前初始化 ComposeAnimation 环境。两个控件默认
`IsIndeterminate=True`；确定进度需显式关闭该属性。进度变化直接改变形状，不自动插值。

```xml
<m3:LoadingIndicator />
<m3:LoadingIndicator IsIndeterminate="False" Progress="{Binding Progress}" />
<m3:ContainedLoadingIndicator CornerRadius="12"
    IndicatorColor="White" ContainerColor="#6750A4" />
```

- 共享 `Progress`、`IsIndeterminate`、`Polygons`、`CornerRadius` 和 `ContainerShape` 的实现位于 `LoadingIndicatorBase`。
- `LoadingIndicator.Color` 对应上游 color；Contained 的 `IndicatorColor` / `ContainerColor` 对应两个颜色参数。
- `Polygons` 是至少两个 `RoundedPolygon` 的 `IReadOnlyList`；null 根据模式选择上游默认序列。
  通过替换列表触发更新，不监听可变列表的原地修改。元素相等的列表替换保留 effect。
- `ContainerShape` 是可选的 `Func<Size, FlowDirection, Geometry>`，接收实际 DIP 尺寸和方向；
  返回同一条背景及裁剪轮廓，覆盖 `CornerRadius`。CornerRadius 是 Avalonia 的物理四角顺序。
- `LoadingIndicatorDefaults` 保留两个默认形状序列，以及 48×48 容器、38 active size 的上游值。
  控件的 ContainerWidth、ContainerHeight、IndicatorSize 是供动态 Token 绑定的隐藏主题输入；
  容器尺寸是首选值，可被外部 Width/Height 或父约束改变。

```csharp
VisualAnimationScope.Initialize(window);
var indicator = new ContainedLoadingIndicator
{
    Polygons = [MaterialShapeCatalog.Pill, MaterialShapeCatalog.SoftBurst],
    ContainerShape = (size, direction) => new EllipseGeometry(new Rect(size))
};
```

上述 API、默认不确定模式和容器轮廓适配已于 2026-10-04 由维护者确认。
CornerRadius / ContainerShape 也能用于无容器颜色版本，这是已确认的本地接口扩展；
上游该版本的容器轮廓固定为 Full。

## 算法与平台适配

- 确定模式构造相邻 normalized polygon 的 Morph；进度选择区间，最终进度 1 保留末 Morph 的终点，
  并按 `-progress × 180°` 旋转。默认序列为旋转 18° 的 Circle → SoftBurst。
- 不确定模式添加末形状回到首形状的 Morph。弹簧使用 damping .6、stiffness 200、阈值 .1；
  每次并发开始弹簧与 650ms 协程等待，再等待弹簧完成。完成时前进索引、snapTo(0)、旋转目标增加 90°。
  全局旋转是 4666ms linear infiniteRepeatable Animatable；这里不使用 InfiniteTransition。
- 只替换 Polygons 时保留 remembered Animatable 值与旋转目标，重置形状索引并重启 effect；
  沿 FrameRecomposer 顺序先采样重组帧的旧动画，再应用列表与 effect。帧到来前保留旧绘制序列，
  多次替换合并为最终 key，避免较短列表遇到旧索引，也不丢失最后一帧的动画值。
  切换模式及 detach/重新 attach 替换对应状态。作用域取消同时释放任务、帧等待和延时。
- MaterialShapes 提供原形状、normalized、Morph 与 bounds；Skia 保留 Fill、控制点 bounds 缩放和居中，
  以及旋转。内部 active shape 保持正方形，在非正方形容器内居中；其缩放关系保留上游的
  `calculateScaleFactor × IndicatorSize / min(ContainerWidth, ContainerHeight)`。
- 普通协程 delay 来自 kotlinx.coroutines，不属于 animation-core 求解器。已授权的
  ComposeAnimation 0.2.7-alpha 用独立 TimeProvider/DelayAsync 适配此职责，仍通过本地 NuGet 消费。
  动画帧与 DurationScale 不改变 delay；测试显式推进延时和帧两条时间轴。
- RangeValue automation 只读，范围 0～1；不确定模式不提供数值范围。输入保留原值，绘制按 Float
  限制进度，NaN 绘制不填充 active path，而 automation 映射为 0；语义边界不额外扩张。

## 验证入口与边界

`tests/Material3Expressive.Tests/LoadingIndicatorTests.cs` 覆盖布局、automation、模式/列表更新、
Token、XAML 与清理。双端用例位于 `tests/VisualParity/.../Cases/LoadingIndicatorCases.cs` 和
`reference/src/main/kotlin/cases/LoadingIndicatorCases.kt`，Compose 端调用固定依赖的真实 API。
场景含默认/自定义形状、进度端点和区间、颜色/主题、RTL、自动与受限尺寸、任意容器裁剪、
多轮循环、模式与列表变更、detach/重新 attach、稀疏帧。Draw/Snapshot 不推进延时或动画帧。

Compose 的 InfiniteAnimationPolicy 属于平台协程上下文；Avalonia 环境没有该上下文入口，
未提供其自定义拦截接口。常规动画及现有 DurationScale 仍由 ComposeAnimation 管理。
真实桌面播放、辅助技术客户端及任意用户提供的 polygon/geometry 未据此宣称全面验证。
本轮具体结果及原始差异保留在 artifacts 的运行产物，不以编译通过替代视觉对比。
