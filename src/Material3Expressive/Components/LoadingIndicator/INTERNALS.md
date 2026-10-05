# Loading Indicator 实现说明

使用方法见 [README](README.md)。两个控件共享形状与时间逻辑；普通协程延时和动画帧必须保持
独立，不能为了简化测试把 650ms delay 换成动画 Tween。

## 默认配置数据流

尺寸与圆角的基线只由 TokenSource 提供，主题绑定到已有配置属性，不在属性元数据或 Defaults 重复数字。LoadingIndicatorDefaults 仅保留真实形状序列；Polygons=null 的模式选择、形状工厂与协程/旋转算法不改。

## 固定上游与本地入口

固定版本以 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）是默认逻辑依据，
Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）提供桌面参照。
下面 Material3 路径相对于 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

固定两端 LoadingIndicator 实现相同。

| 上游文件 / 关键符号 | 本地职责 |
| --- | --- |
| `LoadingIndicator.kt`：`LoadingIndicator`、`ContainedLoadingIndicator` 的确定/不确定入口 | [LoadingIndicator.cs](LoadingIndicator.cs)、[ContainedLoadingIndicator.cs](ContainedLoadingIndicator.cs) 的颜色 API；[LoadingIndicatorBase.cs](LoadingIndicatorBase.cs) 的状态、模式和生命周期 |
| `LoadingIndicator.kt`：`LoadingIndicatorDefaults` 与形状序列 | [LoadingIndicatorDefaults.cs](LoadingIndicatorDefaults.cs)、[LoadingIndicatorShapes.cs](LoadingIndicatorShapes.cs)，MaterialShapes 的 normalized polygon / Morph |
| `LoadingIndicator.kt` 的绘制与 `internal/ShapeUtil.kt` | [LoadingIndicatorDrawing.cs](LoadingIndicatorDrawing.cs)、LoadingIndicatorShapes 的缩放、Cubic、旋转和居中 |
| `tokens/LoadingIndicatorTokens.kt` | [LoadingIndicatorTokenSource.cs](LoadingIndicatorTokenSource.cs)、[LoadingIndicator.axaml](LoadingIndicator.axaml) 的软尺寸与颜色 |

## 形状与状态不变量

确定模式构造相邻 normalized polygon 的 Morph，进度选择区间，最终进度 1 保留末 Morph
终点并旋转 `-progress × 180°`。默认 Circle 先旋转 18° 再到 SoftBurst。
不确定模式增加末形状回首形状的 Morph，不能在最后一段突然跳回初始路径。

不确定形状 spring 为 damping .6、stiffness 200、阈值 .1。每次并发启动 spring 与 650ms
普通协程等待，再等待 spring 完成；随后前进索引、snapTo(0)、旋转目标加 90°。
全局旋转用 4666ms linear infiniteRepeatable Animatable，不能换成 InfiniteTransition。

只换 Polygons 时保留 remembered Animatable 值和旋转目标，重置形状索引/重启 effect；
按 FrameRecomposer 顺序先采样重组帧的旧动画，再应用新列表与 effect。
帧到来前绘制仍使用旧序列，多次替换合并最终 key，防止短列表配上旧索引或丢失旧动画末帧。
元素相等的替换不重启；模式切换和 detach/attach 则替换状态。取消作用域要同时清理任务、帧等待和延时。

## 绘制、尺寸与语义

MaterialShapes 提供原形状、normalized、Morph 和 bounds，Skia 保留 Fill 与控制点 bounds 的
缩放、居中及旋转。内部 active shape 保持正方形，非正方形容器内居中，缩放关系保留
`calculateScaleFactor × IndicatorSize / min(ContainerWidth, ContainerHeight)`。
ContainerWidth/ContainerHeight/IndicatorSize 是隐藏主题输入；普通 Width/Height 仍负责应用约束。

ContainerShape 接收实际 DIP 尺寸与 FlowDirection，返回同一背景/裁剪轮廓，优先于物理四角
CornerRadius。确定进度按 Float 限制，保留输入属性原值；NaN 不填充 active path，但 automation
为 0。只读 RangeValue 范围 0～1，不确定模式不提供数值范围，语义边界不扩张。

## 已确认选择与共享依赖

2026-10-04 维护者确认两个公开控件、默认不确定模式、Polygons 和容器轮廓 API，
依据见 [拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/LoadingIndicator/README.md)。允许无容器颜色版本也使用 CornerRadius/
ContainerShape，是明确的本地扩展；固定上游该入口轮廓固定为 Full。

普通协程延时通过共享 TimeProvider/DelayAsync 接入，原因、授权及适用范围见
[普通协程延时与动画帧时钟](../../../../docs/DECISIONS.md#普通协程延时与动画帧时钟)。
DurationScale 或动画帧不能改变这段 delay；确定测试显式推进两条时间轴，Draw/Snapshot 均被动。
旧验证使用的 0.2.7-alpha 是历史背景，当前依赖安装和初始化见
[公共接入](../../../../docs/GETTING_STARTED.md)，不要保留相邻仓库本地包的过期指引。

## 一致性边界与验证

尚未提供 Compose InfiniteAnimationPolicy 的平台协程上下文拦截入口；现有 DurationScale
继续由共享环境处理。实际桌面播放、辅助技术客户端、任意用户 polygon/geometry 未全面验证，
不能把默认路径测试推广到所有输入。

[LoadingIndicatorTests](../../../../tests/Material3Expressive.Tests/LoadingIndicatorTests.cs) 覆盖尺寸、
automation、模式/列表变更、Token、XAML、延时/帧与清理。
[LoadingIndicatorCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/LoadingIndicatorCases.cs)
及 [LoadingIndicatorCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/LoadingIndicatorCases.kt)
使用真实公开组件，覆盖默认/自定义形状、区间端点、主题/颜色、RTL、约束、自定义轮廓、
多轮循环、列表/模式替换、卸载/重挂和稀疏帧。

旧合并记录中结构化事实相等、raster 严格相等或在既定容差内，重复与额外被动截图均稳定。
当时依赖升级还回归圆/线波浪循环、AppBar 吸附中断、SegmentedButton 反向四个代表场景，
并非这些组件的全量重验。详细历史数字及环境见固定提交，未跟踪产物不作为永久链接。
以下为复现入口，实际结果需以新生成报告为准：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter "FullyQualifiedName~LoadingIndicatorTests"
./tests/VisualParity/parity.ps1 self-test --family loading-indicator --require-match
```

参考运行环境与报告阅读见 [VisualParity](../../../../tests/VisualParity/README.md)。
