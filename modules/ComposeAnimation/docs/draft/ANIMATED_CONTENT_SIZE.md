> **历史设计稿**：本文早于 2026-10 的动画基础重构，仅作历史参考，名称与行为可能已过时。现行用法见 [`docs/usage`](../usage/README.md)；迁移见 [迁移指南](../usage/migration-from-visual-animation-scope.md)。主要改名：`VisualAnimationScope` → `AnimationScope`（`AnimationScope.Get`），`SharedAnimationFrameClock` → 内部 `FrameClock`，`IAnimationFrameClock` → `IFrameSource`，`AnimationScope<T>` → `AnimationRun<T>`。

# AnimatedContentSize

`AnimatedContentSize` 对应 Compose `Modifier.animateContentSize`：内容按当前目标约束正常布局，
控件向父级报告动画中的尺寸。兄弟元素因父级重新布局而随之移动，默认裁剪超出动画区域的内容。
它不会把内容截图拉伸，也不会默认按每一帧的动画尺寸重新换行。

普通 Avalonia 布局路径已完成本轮行为收尾；协作式 Lookahead 支持单独标为实验性。

## 普通使用

```xml
<ca:AnimatedContentSize xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
                        x:Name="SizeHost" HorizontalAlignment="Left"
                        ContentAlignment="TopStart"
                        Padding="12" BorderThickness="1" BorderBrush="SlateGray">
    <TextBlock x:Name="Message" Width="240" TextWrapping="Wrap" Text="Short text" />
</ca:AnimatedContentSize>
```

改变 `Message.Text`，或替换 `SizeHost.Content` 后，测量得到的新内容尺寸会成为动画目标。
也支持普通 Grid、TextBlock、数据绑定、ContentTemplate 和自定义 ControlTemplate，不要求使用 LookaheadRoot。

```csharp
SizeHost.AnimationSpec = new TweenSpec<Size>(
    TimeSpan.FromMilliseconds(250), easing: new LinearEasing());
SizeHost.AnimationCompleted += (_, e) =>
    Debug.WriteLine($"{e.InitialSize} -> {e.TargetSize}");
```

默认规格为 `SpringSpec<Size>`，StiffnessMediumLow（400），非弹跳阻尼，阈值为 0.5 DIP。
`AnimationSpec` 不接受 null。改变规格影响后续目标变化，不重新计时当前动画；不应原地修改正在使用的规格对象。
`MotionDurationScale.ScaleFactor = 0` 和零时长规格在收到首个动画帧时完成，不额外保留一个完整过渡。

## 尺寸、对齐与模板

- `ContentAlignment` 控制完整模板内容在动画区域中的位置，支持九种对齐和 FlowDirection。
  单独改变对齐或 FlowDirection 会立即使排列失效，不必等待另一次尺寸变化。
- Horizontal/VerticalContentAlignment 是模板内部的内容对齐，与外层 ContentAlignment 是两层不同的布局。
- 默认模板转发 Background、BackgroundSizing、BorderBrush、BorderThickness、CornerRadius、Padding，
  以及 Content、ContentTemplate 和内部内容对齐。字体/前景等沿 Avalonia 的属性继承机制传递。
- Padding 和边框位于被动画的模板区域内，计入动画目标；Margin 位于外部。
  希望装饰不参与该区域的测量时，将装饰控件放在 AnimatedContentSize 外部。
- 自定义模板按**完整的直接视觉根节点**测量和排列，内嵌的 presenter、Border、Grid 不会被跳过。
  自定义模板负责自己的属性绑定，遵循 Avalonia ContentControl 的 presenter 注册约定。
- 显式 ContentTemplate 可以为 null 内容构造控件，遵循 Avalonia 原有语义。
  没有生成内容和装饰时，空内容可以过渡至零尺寸，再从零尺寸恢复。
- 默认 ClipToBounds=true；调用方可以显式关闭。开启时，以当前动画控件的边界裁剪。

动画目标应用控件自身的 Min/Max/固定尺寸，报告给父级的尺寸还受当前可用空间限制；
布局舍入由 Avalonia 执行。动画内部值与最终 Bounds 不必相同：弹簧可以过冲，
负的尺寸样本会被限制为零，父级空间不足时也可以截断可见尺寸。
修改外部约束可能同时改变子内容的实际测量目标，从而触发重定向。

## 生命周期与完成事件

首次**挂载后的测量**直接初始化到目标，不产生动画或完成事件。
首次挂载前的离树测量不会成为首次显示动画的起点。

动画中更新目标，会从 Animatable 当前值/速度重定向；被替代的运行不触发 AnimationCompleted。
事件 InitialSize 是本次运行开始时的内部动画尺寸，TargetSize 是完成结果，而不是受父约束截断后的 Bounds。
事件处理器可以再次更换内容、替换时钟、重置或移除控件；旧运行不会在这些操作后覆盖新运行状态。

同一实例临时离开视觉树时取消帧等待并保留当前尺寸。重挂后使用新树的约束和时钟，从保留尺寸重新趋近目标，
即使目标未变也会恢复。离树时间不计入新运行；重挂不保留旧速度或剩余时长。
从 LookaheadRoot 移到普通布局时，不继续使用旧作用域的预测结果。

回收实例用于不同业务项时显式调用：

```csharp
SizeHost.ResetAnimation();
SizeHost.Content = nextItem;
```

这会取消当前运行、忘记旧尺寸；下一次测量直接初始化到目标，不报告被取消运行的完成。
不要在普通 Content 变化时自动重置，否则会失去内容尺寸过渡。
Avalonia 没有直接对应 Compose Modifier.Node.onReset 的通用回收回调，回收宿主负责调用此方法。

## 时钟与错误

加载内容前调用 `VisualAnimationScope.Initialize(window, clock?)`；省略 clock 使用 UI 帧源。同根自动共享按需请求，运行期间帧源固定。

根 Scope 的 `AnimationFailed` 提供失败来源，源和根的 `LastError` 保留最近异常。规格失败或非有限尺寸停止该运行并使用目标尺寸，更换目标或规格后可以继续。帧源失败停止该根环境的源请求，不因 ResetAnimation 或重新挂载而重试。完成事件处理器失败记录诊断，已完成布局不回退；正常取消不触发成功完成事件。

普通模板的测量/排列异常仍按 Avalonia 布局异常处理。

## 实验性 Lookahead：显式作用域即启用

普通使用不启用协作式 Lookahead。显式添加 `LookaheadRoot` 即表示选择启用，没有全局开关或第二个逐控件开关：

```xml
<ca:LookaheadRoot xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation" Width="350" Height="300">
    <ca:LookaheadStackPanel>
        <ca:AnimatedContentSize HorizontalAlignment="Left">
            <ca:LookaheadTextBlock MaxWidth="240" TextWrapping="Wrap" Text="Independent target measurement" />
        </ca:AnimatedContentSize>
    </ca:LookaheadStackPanel>
</ca:LookaheadRoot>
```

该路径使用独立预测的内容目标尺寸，并用目标约束测量真实内容；内容仍不按动画宽度逐帧重排。
目前限定默认模板、直接 Control 内容、内部 Stretch 对齐、无 Padding/BorderThickness/ContentTemplate。
后代须是协作节点或固定尺寸整体叶节点，不支持的结构明确报错。协议和支持范围仍可能变化。
这些限制只属于实验路径，不能外推为普通 AnimatedContentSize 的限制。
完整说明见 [协作式 Lookahead 实验](LOOKAHEAD_EXPERIMENT.md)。

## 与 Compose 的对应与偏差

对照本机 AndroidX `AnimationModifier.kt` 和 `AnimationModifierTest.kt`：

| 项目 | 本实现与上游关系 |
| --- | --- |
| 自然尺寸动画、重定向、对齐、默认裁剪、完成通知 | 保留核心行为；含 LTR/RTL 和中断完成值的回归验证 |
| 无 Lookahead 路径 | 上游同样支持，不要求先实现通用 Lookahead |
| 有 Lookahead 路径 | 保留目标约束测量语义，覆盖范围受协作树限制 |
| 尺寸单位 | double DIP、0.5 DIP 阈值；上游是 IntSize、1px 阈值，不声称逐像素/逐帧完全相同 |
| 最小约束 | Avalonia Measure(Size) 没有四向 Constraints；本实现将容器目标规范化到自己的 Min/Max/固定尺寸，未向任意子控件强行注入 Compose 的最小约束 |
| API 载体 | ContentControl 和嵌套关系代替 Modifier 链；装饰顺序由控件树表达 |
| intrinsic 测量 | Avalonia 无对应的通用 intrinsic 查询协议，未移植四个透传入口 |
| 重挂 | 保留尺寸并重新启动，确保最终目标正确；上游也保留数据、处理未完成恢复，但其测试明确不保证重挂后剩余轨迹，不能声称完全相同 |
| 回收重置 | 显式 ResetAnimation 对应 onReset 意图；回收宿主负责触发 |
| 固定根帧环境与错误诊断 | .NET/Avalonia 附加契约，上游没有一一对应的控件 API |
| 扩展能力 | 可关闭裁剪；ContentAlignment 为九种预设，不提供 Compose 任意自定义 Alignment 对象 |

默认模板现在可桥接嵌套 LookaheadPosition 的真实位置 approach；本组件仍保留目标约束测量语义。
按中间尺寸重测内容由独立实验性 LookaheadSize 承担。统一 bounds 动画与 SharedTransition 尚未接入。

## 验证

`AnimatedContentSizeTest` 覆盖多次变更、长时间空闲后再次动画和兄弟重排。
`AnimatedContentSizeBehaviorTest` 覆盖九种 LTR/RTL 对齐、动态对齐、装饰/模板/空内容、重挂/重置、
离树预测量、移出 Lookahead、Scope 结束、回调重入、规格切换、零时长/零缩放、约束、弹簧过冲/速度和错误恢复。
`LookaheadTest` / `LookaheadTextTest` 保留目标测量和动画快照的专项验证。
测试平台为 Avalonia 12.1.2 / .NET 10 headless；桌面 Demo 的构建与自动测试不等于所有平台/DPI 的视觉验收。
