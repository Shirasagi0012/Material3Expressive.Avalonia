# Progress Indicator

四个库控件分别提供普通/波浪、线形/圆形进度：`LinearProgressIndicator`、
`CircularProgressIndicator`、`LinearWavyProgressIndicator`、`CircularWavyProgressIndicator`。
每个都支持确定和不确定模式，不使用原生 ProgressBar 的 Theme 切换变体。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)，包括窗口加载内容前的动画初始化。
维护与算法边界见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

以下示例无需 ViewModel，前两项显示确定进度，后两项持续播放：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel Spacing="24">
    <m3:LinearProgressIndicator Progress="0.6" />
    <m3:CircularProgressIndicator Progress="0.6" />
    <m3:LinearWavyProgressIndicator IsIndeterminate="True" />
    <m3:CircularWavyProgressIndicator IsIndeterminate="True" />
  </StackPanel>
</UserControl>
```

后续 XAML 沿用相同命名空间。`IsIndeterminate` 默认 false；也可以把 `Progress` 绑定到
ViewModel 的 0～1 数值属性，启用编译绑定的视图需声明其 `x:DataType`。
进度改变直接绘制，不自动平滑。需要平滑时由应用用 ComposeAnimation 的 Animatable 驱动，
可选择 `ProgressIndicatorDefaults.ProgressAnimationSpec`（spring）或
`WavyProgressIndicatorDefaults.ProgressAnimationSpec`（500ms linear tween）。

## 尺寸与状态

普通线形、普通圆形、波浪线形、波浪圆形的首选尺寸分别为 240×4、40×40、240×10、48×48 DIP。
这些是软默认值，父级可以缩小；用原生 `Width` / `Height` 明确约束。
普通线形用实际高度作为线宽；普通圆形用 `StrokeWidth`（默认 4 DIP）。

绘制/语义把 Progress 按 Float 限制在 0～1，但不改写属性原值；NaN 的 automation 数值为 0。
不确定模式保留属性值，停止提供确定的 RangeValue provider。
所有变体是只读 ProgressBar automation，不接受点击或焦点；进度变化发送 RangeValue 通知。
普通线形跟随方向，线形波浪 RTL 按基线旋转 180°，圆形不因 RTL 反转。

## 颜色、端帽与波浪

共享 `Color`、`TrackColor`、`GapSize` 为颜色与间隙入口。普通版用 `StrokeCap` 控制端帽。
波浪版分别配置 `Stroke` 与 `TrackStroke`，例如：

```xml
<m3:LinearWavyProgressIndicator Progress="0.6" Amplitude="0.5"
                                Wavelength="30" WaveSpeed="15">
  <m3:LinearWavyProgressIndicator.Stroke>
    <m3:ProgressIndicatorStroke Width="6" Cap="Round" />
  </m3:LinearWavyProgressIndicator.Stroke>
  <m3:LinearWavyProgressIndicator.TrackStroke>
    <m3:ProgressIndicatorStroke Width="2" Cap="Flat" />
  </m3:LinearWavyProgressIndicator.TrackStroke>
</m3:LinearWavyProgressIndicator>
```

Stroke Width 使用 DIP；Join/Miter 也可配置。`PathEffect` 使用 Skia 物理像素单位，其生命周期
由调用方持有，不能在控件仍绘制时释放。没有内容槽或业务事件，主要通过属性/绑定更新。

`Stroke` / `TrackStroke` 为 null 使用各自厚度 Token。`Wavelength` 为 null 使用变体默认值；
`WaveSpeed` 的单位是 DIP/秒，null 跟随实际波长，非正值停止波浪位移但保留波形。
`Amplitude` 非 null 覆盖 `AmplitudeSelector`，结果限到 0～1；未指定时确定进度在
<=0.1 或 >=0.95 取 0，其余取 1，不确定模式取 1。函数形式可在 C# 中设置：

```csharp
// indicator 是视图中已有的 LinearWavyProgressIndicator。
indicator.AmplitudeSelector = progress => progress < 0.9 ? 1 : 0;
indicator.WaveSpeed = 0;
```

可空参数赋 null 或清除局部属性可恢复默认值，统一外观见[Token 接入](../../../../docs/GETTING_STARTED.md#主题与定制)。

## 终点标记与使用边界

普通线形和线形波浪可用 `StopSize="0"` 隐藏 stop；普通线形还提供 `DrawStopIndicator`。
该回调在轨道与进度之后执行，参数为 DrawingContext 和 DIP 尺寸；null 表示默认绘制，空回调隐藏：

```csharp
// linear 是视图中已有的 LinearProgressIndicator。
linear.DrawStopIndicator = (_, _) => { };
```

确定圆形波浪的 Morph 按固定基线惰性创建：初始固定非端点振幅在未发生振幅动画时仍可能呈圆形，
不能保证设置任意静态 Amplitude 就立即得到插值波形。振幅运行中更新也保留上游目标门控，
不是任意时刻都可中断的新动画。细节见 [INTERNALS](INTERNALS.md#动画与缓存不变量)。
线形语义矩形上下各扩展 10 DIP，但不改变布局大小。
真实桌面播放、平台屏幕阅读器和自定义 PathEffect 尚未单独验收。
[ProgressIndicatorDemo](../../../Material3Expressive.Catalog/ProgressIndicatorDemo.axaml) 展示全部八种模式及独立线宽。
