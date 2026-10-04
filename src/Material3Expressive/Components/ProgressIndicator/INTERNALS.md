# Progress Indicator 实现说明

使用入口见 [README](README.md)。四种控件共享属性、绘制与生命周期，分别连接固定上游的
确定/不确定重载；软默认尺寸与绘制几何不可混成另一套硬尺寸模型。

## 固定上游与本地入口

固定版本以 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）是默认逻辑依据，
Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）提供桌面参照。
下面 Material3 路径相对于 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

固定两端四个主实现文件内容相同：ProgressIndicator.kt、WavyProgressIndicator.kt 及
internal/LinearWavyProgressModifiers.kt、internal/CircularWavyProgressModifiers.kt。
圆形波浪的 RoundedPolygon/Morph 使用 MaterialShapes，授权时固定提交为
`f4bfeaefbdb6c77e0c8c9b8cb83178727369e0c7`；当前依赖取仓库实际配置，不从旧验证记录推断版本。

| 上游文件 / 关键符号 | 本地职责 |
| --- | --- |
| `ProgressIndicator.kt`：`LinearProgressIndicator` / `CircularProgressIndicator` 的确定与不确定重载 | [LinearProgressIndicator.cs](LinearProgressIndicator.cs)、[CircularProgressIndicator.cs](CircularProgressIndicator.cs)、[ProgressIndicator.cs](ProgressIndicator.cs) |
| `WavyProgressIndicator.kt`：`LinearWavyProgressIndicator`、`CircularWavyProgressIndicator`、默认 Stroke / 振幅 | [WavyProgressIndicator.cs](WavyProgressIndicator.cs)、[ProgressIndicatorStroke.cs](ProgressIndicatorStroke.cs) |
| `internal/LinearWavyProgressModifiers.kt`、`internal/CircularWavyProgressModifiers.kt` 的缓存与绘制 | [LinearProgressDrawingCache.cs](LinearProgressDrawingCache.cs)、[CircularProgressDrawingCache.cs](CircularProgressDrawingCache.cs)、[ProgressIndicatorDrawing.cs](ProgressIndicatorDrawing.cs) |
| 两个主文件中的动画规格/默认进度动画 | [ProgressIndicatorMotion.cs](ProgressIndicatorMotion.cs)，ProgressIndicator 的子作用域与门控 |
| `internal/ShapeUtil.kt`、`internal/AccessibilityUtil.kt` | CircularProgressShapes 的 Cubic/Morph 投影；ProgressIndicator automation 与扩展语义边界 |
| `tokens/ProgressIndicatorTokens.kt`、`LinearProgressIndicatorTokens.kt`、`CircularProgressIndicatorTokens.kt`、`MotionTokens.kt` | [ProgressIndicatorTokenSource.cs](ProgressIndicatorTokenSource.cs)、[ProgressIndicator.axaml](ProgressIndicator.axaml) |

## 绘制、布局与语义

Canvas/Path/PathMeasure 对应 Skia。UI 线程计算路径，绘制操作持有独立快照；不要让后续缓存修改
改变已经提交的绘制对象。长度、间隙、端帽补偿、波浪切片和旋转按物理像素 Float 计算，
ShapeUtil 的 Cubic 投影保留 Float 转换。PathEffect 是调用方持有的物理像素资源。

ContainerWidth/ContainerHeight/DefaultWavelength/ActiveThickness/TrackThickness 是 Token 主题输入，
不是鼓励调用方另行维护的几何模型。首选尺寸按 density 取整再服从父约束，显式 Width/Height
仍使用原生路径。普通线形用高度作线宽；两种波浪 Stroke 独立。
普通线形保留方向判断，线形波浪在 RTL 旋转 180°，圆形不随 RTL 反转。

Progress 属性不改写原值，绘制/语义按 Float 夹取 0～1。确定进度更新发 RangeValue 通知，
不确定时移除该 provider；所有变体只读、无焦点/点击。线形语义上下各扩展 10 DIP，
不改变测量尺寸；NaN 的确定 automation 值为 0。

## 动画与缓存不变量

普通不确定版与线形波浪用 InfiniteTransition，圆形不确定波浪按上游使用三个并发 Animatable。
动画均属于控件的子作用域，detach 结束本控件任务与缓存，不能终止兄弟动画；重挂从新状态开始。
1750ms 线形循环、6000ms 圆形循环和 keyframes 的 easing 区间关系保存在 ProgressIndicatorMotion，
`using` 作用于该关键帧开始的区间，不能移动到结束帧。

振幅变化保留运行中目标门控，圆形缓存只在 isDrawingWave 观察振幅时失效；不能把后续进度改变
一概改成可中断振幅动画。固定上游惰性 Morph 也保留：初始确定圆形的固定非端点振幅，
在没有振幅动画时仍使用圆形；Morph 创建后才使用插值路径。

Avalonia 不允许 Render 内 InvalidateVisual。订阅初始值由当前绘制消费，后续帧正常重绘；
圆形缓存对最终振幅的失效保留到异步任务完成，避免完成门控开启之前消耗待重建状态。
这些是有具体上游路径的实现不变量，不代表已批准任意近似绘制。

## 确认依据

[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/ProgressIndicator/README.md)记录 2026-10-04 维护者批准四个独立控件表达八种入口，
并授权添加 MaterialShapes 支撑圆形形状算法。该确认不包含隐式进度平滑、改变振幅门控或
重新设计上游波形。当前依赖及动画初始化统一见[公共接入](../../../../docs/GETTING_STARTED.md)，
历史本地 NuGet 版本不作为当前配置。

## 一致性边界与验证

[ProgressIndicatorTests](../../../../tests/Material3Expressive.Tests/ProgressIndicatorTests.cs) 覆盖
尺寸、automation、Token/显式值优先级、模式默认值、卸载/重挂和兄弟隔离。
[ProgressIndicatorCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ProgressIndicatorCases.cs)
与 [ProgressIndicatorCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ProgressIndicatorCases.kt)
直接调用八个真实 API，覆盖端点/阈值、端帽、间隙、线宽、stop、透明色、RTL、主题/模式变化、
运行中进度、调速/停波与循环边界。

动态属性更新在同一逻辑时刻提交两个 waiter batch，分别完成参数重组与新动画起始帧，
不增加经过时间；常驻动画维持连续 16ms 网格，边界采样不额外发帧。
旧记录的 Windows 双端结果中结构化事实相等、raster 严格相等或在既有容差内、重复稳定；
不能外推到真实桌面播放、其他平台、屏幕阅读器或自定义 PathEffect，这些仍未经验证。
历史统计及环境见固定提交；没有把原始 4× raster 差异消除或重分类。
以下为复现入口，实际结果需以新生成报告为准：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter "FullyQualifiedName~ProgressIndicatorTests"
./tests/VisualParity/parity.ps1 self-test --family progress-indicator --require-match
```

参考环境与证据格式见 [VisualParity](../../../../tests/VisualParity/README.md)。
