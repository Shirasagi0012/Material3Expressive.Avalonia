# Progress Indicator

四个独立控件提供确定与不确定模式，共八种 Compose 入口对应的行为：
`LinearProgressIndicator`、`CircularProgressIndicator`、`LinearWavyProgressIndicator`、
`CircularWavyProgressIndicator`。2026-10-04 维护者确认采用上述独立控件。

依据 `docs/upstream-baseline.json` 的 AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`：

- `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ProgressIndicator.kt`
- 同目录 `WavyProgressIndicator.kt`、`internal/LinearWavyProgressModifiers.kt`、`internal/CircularWavyProgressModifiers.kt`
- 同目录 `tokens/{ProgressIndicator,LinearProgressIndicator,CircularProgressIndicator,Motion}Tokens.kt`
- `internal/ShapeUtil.kt`、`internal/AccessibilityUtil.kt`

固定 Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的上述四个主实现文件与 AndroidX 内容一致。
圆形波浪的 `RoundedPolygon` 与 `Morph` 使用维护者授权添加的 `modules/MaterialShapes`，
固定在 `f4bfeaefbdb6c77e0c8c9b8cb83178727369e0c7`。

## 使用

按项目动画接入约定，在窗口构造器里、内容挂载前调用 `VisualAnimationScope.Initialize(this)`；
应用加载 `MaterialTheme.axaml`。

```xml
<StackPanel xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia" Spacing="24">
    <m3:LinearProgressIndicator Progress="{Binding Progress}" />
    <m3:CircularProgressIndicator Progress="{Binding Progress}" />
    <m3:LinearWavyProgressIndicator IsIndeterminate="True" />
    <m3:CircularWavyProgressIndicator IsIndeterminate="True" />
</StackPanel>
```

`Progress` 使用 0～1；绘制与语义按 Compose Float 范围截取，保留原始属性值。
NaN 的确定语义值为 0。不确定模式保留属性中的进度，但移除确定的 RangeValue provider。
进度改变默认立即绘制；不会隐式平滑。应用可用 ComposeAnimation 的 Animatable 与
`ProgressIndicatorDefaults.ProgressAnimationSpec`（spring）或
`WavyProgressIndicatorDefaults.ProgressAnimationSpec`（500ms linear tween）自行驱动。

```csharp
var indicator = new LinearWavyProgressIndicator
{
    Progress = .6,
    Stroke = new ProgressIndicatorStroke(6),
    TrackStroke = new ProgressIndicatorStroke(2, PenLineCap.Flat),
    Wavelength = 30,
    WaveSpeed = 0, // 停止波浪运动，保留波形
    AmplitudeSelector = p => p < .9 ? 1 : 0
};
```

```xml
<m3:LinearWavyProgressIndicator Progress="0.6" Amplitude="0.5" Wavelength="30" WaveSpeed="15">
    <m3:LinearWavyProgressIndicator.Stroke>
        <m3:ProgressIndicatorStroke Width="6" Cap="Round" />
    </m3:LinearWavyProgressIndicator.Stroke>
    <m3:LinearWavyProgressIndicator.TrackStroke>
        <m3:ProgressIndicatorStroke Width="2" Cap="Flat" />
    </m3:LinearWavyProgressIndicator.TrackStroke>
</m3:LinearWavyProgressIndicator>
```

共享参数为 `Color`、`TrackColor`、`GapSize`。普通线形用高度作为线宽，提供 `StrokeCap`、
`StopSize` 和 `DrawStopIndicator`；普通圆形另外提供 `StrokeWidth`。
波浪版用独立 `Stroke`、`TrackStroke`，其 Width 使用 DIP；Join、Miter、PathEffect 对应 Compose Stroke。
PathEffect 使用 Skia 物理像素单位且由调用者持有其生命周期。

`Stroke`/`TrackStroke` 为 null 时使用各自厚度 Token。
`Wavelength` 为 null 时采用变体默认值；`WaveSpeed` 为 null 时跟随实际波长，非正值停止运动。
`Amplitude` 非 null 时覆盖 `AmplitudeSelector`；否则确定模式默认在进度 `<= .1f` 或 `>= .95f`
时使用 0，其余使用 1，不确定模式使用 1。振幅截取到 0～1。
恢复主题默认值可使用 `ClearValue`，或为上述可空参数赋 null。

普通线形的 `DrawStopIndicator` 默认 null，表示按上游画 stop；空回调或 `StopSize = 0` 可隐藏。
回调在轨道和进度之后调用，参数是 DrawingContext 与控件 DIP 大小：

```csharp
linear.DrawStopIndicator = (context, size) =>
    context.DrawEllipse(Brushes.Red, null, new Point(size.Width - 2, size.Height / 2), 2, 2);
```

## 适配与行为

- Compose Canvas/Path/PathMeasure 直接对应 Skia；路径在 UI 线程计算，绘制操作持有独立的路径快照。
  长度、间隙、端帽补偿、波浪切片和旋转使用物理像素 Float 运算；ShapeUtil 的 Cubic 投影保留 Float 转换。
- `Width`/`Height` 保留 Avalonia 的标准使用方式。Token 输入提供软默认尺寸，允许父级缩小。
  普通线形 240×4、普通圆形 40×40、波浪线形 240×10、波浪圆形 48×48 DIP。
  ContainerWidth/ContainerHeight/DefaultWavelength/ActiveThickness/TrackThickness 是主题接入属性，不是额外的几何模型。
- 线形普通版保留方向判断；波浪版 RTL 按上游旋转 180°。圆形不根据 RTL 反转。
- 无障碍为只读 ProgressBar；线形语义矩形上下各扩展 10 DIP，不改变测量与布局尺寸。
  无焦点和点击行为。确定进度更新发送 RangeValue 变更通知。
- 不确定普通版和线形波浪版使用 ComposeAnimation InfiniteTransition；圆形不确定波浪版按上游
  使用三个并发 Animatable。所有动画使用控件自身子作用域，卸载只结束该控件的动画。
- 保留上游振幅动画的运行中目标门控，以及圆形绘制缓存经由 isDrawingWave 观察振幅的条件。
  不把后来的进度更新简单改成可中断的振幅动画。
- 保留基线的惰性 Morph：初始确定圆形的固定非端点振幅在没有振幅动画时仍使用圆形；
  Morph 创建后再使用插值路径。这是基线行为，不自行改成每次都创建 Morph。
- Avalonia 不允许 Render 内调用 InvalidateVisual。订阅发布的初始值由当前绘制消费，后续动画帧正常触发重绘。
  圆形缓存对振幅最终值的失效保留至异步任务完成，避免在完成门控打开前消费掉待重建状态。

## 验证入口

`tests/Material3Expressive.Tests/ProgressIndicatorTests.cs` 覆盖尺寸约束、语义、主题 Token、
显式参数优先级、模式默认值、卸载/重新挂载和兄弟动画隔离。

双端用例分别位于 `tests/VisualParity/Material3Expressive.Parity/Cases/ProgressIndicatorCases.cs`
与 `tests/VisualParity/reference/src/main/kotlin/cases/ProgressIndicatorCases.kt`，参考端直接调用八个 Compose API。
覆盖进度边界和振幅阈值、端帽、间隙、独立线宽、stop、透明颜色、尺寸、RTL、主题变化、
模式切换、运行中的进度变化、调速/停波，以及 1750ms/6000ms 循环边界。
动态属性变更后两端均在同一逻辑时刻提交两个 waiter batch，分别完成 Compose 参数重组与
该批次新创建动画的起始帧；不增加经过时间。常驻动画保持连续 16ms 帧网格，循环边界采样不额外发帧。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release
./tests/VisualParity/parity.ps1 self-test --family progress-indicator
```

Catalog 的 `ProgressIndicatorDemo.axaml` 展示八种变体和自定义线宽。2026-10-04 在 Windows 上执行：
控件库 744 项测试通过（本组件 15 项）；121 个双端用例中 93 个完全相等、28 个在既有容差内，
没有执行错误、未匹配用例或重复性差异，`self-test --require-match` 通过。原始 4× Raster 差异保留在报告中。
产物位于 `artifacts/visual-parity/runs/20261004-091748-6d372fac`，日志为 `artifacts/progress-*.log`。
桌面实时显示、平台屏幕阅读器与自定义 PathEffect 尚需单独验证。

2026-10-04 合入主线后，使用 ComposeAnimation `0.2.6-alpha` 重新验证：Release 全解决方案构建、
744 项控件测试和 123 项 Parity 框架测试通过。121 项全量 `self-test --require-match` 实际重新采集双端，
包含重复捕获与额外被动截图检查，全部稳定；93 项严格一致、28 项在既有容差内，无执行错误或未比较项。
395 个采样的 790 项结构化事实全部相等；391 张请求的 Raster 中 323 张严格一致、68 张在容差内。
逐样本比较状态与分支交付报告一致，原始 4× Raster 差异仍保留。
本次报告见 [progress-indicator-merge-20261004](../../../../artifacts/visual-parity/runs/progress-indicator-merge-20261004/summary.md)。
上述桌面、屏幕阅读器与自定义 PathEffect 的验证范围没有扩展。
