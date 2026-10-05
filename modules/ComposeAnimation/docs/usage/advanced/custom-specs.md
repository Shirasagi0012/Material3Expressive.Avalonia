# 自定义动画规格

本文面向扩展作者：当内置规格（`SpringSpec<T>`、`TweenSpec<T>`、`KeyframesSpec<T>` 等，见 [内置动画规格](../runtime/animation-specs.md)）不能满足需求时，如何实现自己的动画规格。

ComposeAnimation 的规格体系分三层，写一个自定义规格通常只需实现最底层的**标量接口**，再借助现成的桥接类型接到任意 `T` 上——无需自己处理向量与类型转换。

帧时钟、线程模型与 `Task` 异步模型等前置概念见 [引擎总览](../runtime/overview.md)。

## 三层抽象

### 类型化外观层：`IAnimationSpec<T>`

动画宿主（`Animatable<T>`、`Transition<TState>` 等）看到的都是这一层。整个接口只有一个成员：

```csharp
public interface IAnimationSpec<T>
{
    IVectorizedAnimationSpec Vectorize(ITwoWayConverter<T> converter);
}
```

`Vectorize` 把类型化的配置外观转换为向量层实现：转换器负责 `T` 与 `AnimationVector`（`double[]`）之间的往返，规格只面向 `double` 分量工作。每次（重）配置动画时宿主都会调用一次 `Vectorize`（`TargetBasedAnimation<T>` 构造时），因此外观层只存配置、不存每次动画的状态。

两个子接口收窄了 `Vectorize` 的返回类型：

| 接口 | 额外成员 | `Vectorize` 返回 | 语义 |
| --- | --- | --- | --- |
| `IFiniteAnimationSpec<T>` | 无 | `IVectorizedFiniteAnimationSpec` | 有限时长；`Transition` 子动画只接受这一类 |
| `IDurationBasedAnimationSpec<T>` | `TimeSpan Delay { get; }`、`TimeSpan Duration { get; }` | `IVectorizedDurationBasedAnimationSpec` | 时长预先可知；`RepeatableSpec<T>` / `InfiniteRepeatableSpec<T>` 只能包装这一类 |

### 向量层：`IVectorizedAnimationSpec`

向量层以 `ReadOnlySpan<double>` 为输入、`Span<double>` 为输出目的地，方法签名即真实契约（`VectorizedAnimationSpec.cs`）：

```csharp
public interface IVectorizedAnimationSpec
{
    bool IsInfinite { get; }

    TimeSpan GetDuration(
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity);

    void GetValue(TimeSpan playTime,
        ReadOnlySpan<double> initial, ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity, Span<double> destination);

    void GetVelocity(TimeSpan playTime,
        ReadOnlySpan<double> initial, ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity, Span<double> destination);

    // 默认实现：终点时刻的速度
    void GetEndVelocity(ReadOnlySpan<double> initial, ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity, Span<double> destination)
        => GetVelocity(GetDuration(initial, target, initialVelocity), initial, target, initialVelocity, destination);
}
```

- 结果一律写入 `destination`，不要在堆上分配返回数组。
- `GetEndVelocity` 决定动画结束时刻报告的速度——中断接力时它会作为下一支动画的起始速度。时长型规格用默认实现即可；物理类规格应覆写为 0（参考 `VectorizedSpringSpec.GetEndVelocity`：`destination.Clear()`）。
- 两个子接口提供默认实现：`IVectorizedFiniteAnimationSpec` 把 `IsInfinite` 固定为 `false`；`IVectorizedDurationBasedAnimationSpec` 暴露 `Delay`/`Duration` 并把 `GetDuration` 默认实现为 `Delay + Duration`。

### 标量层：`IFloatAnimationSpec`

最易于实现的一层：单维浮点物理模型，每个维度独立动画（`FloatAnimationSpec.cs`）：

```csharp
public interface IFloatAnimationSpec
{
    double GetValue(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity);

    double GetVelocity(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity);

    // 默认实现：终点时刻的速度；物理类规格应覆写为 0
    double GetEndVelocity(double initialValue, double targetValue, double initialVelocity)
        => GetVelocity(GetDuration(initialValue, targetValue, initialVelocity), initialValue, targetValue, initialVelocity);

    TimeSpan GetDuration(double initialValue, double targetValue, double initialVelocity);
}
```

约定：

- `playTime` 从动画启动起算，可能略小于 0 或超出时长，实现应自行钳制（内置 `FloatTweenSpec` 用 `ClampPlayTime` 处理）。
- 速度单位为 **值/秒**（内置 tween 用"当前值与 1ms 前之差 × 1000"的数值微分；物理模型可给出解析解）。
- `initialVelocity` 对曲线类规格通常可忽略（`FloatTweenSpec` 仅在 `playTime == 0` 时原样返回它）；物理模型必须尊重它以保证接力连续。
- `GetDuration` 对时长型规格忽略入参、返回固定时长；对物理模型按初始条件估算（`FloatSpringSpec` 委托给 `SpringEstimation.EstimateDuration`）。

### 平行的衰减体系

衰减动画（惯性滚动）没有目标值——终点是算出来的，因此有独立的一套接口：

| 层 | 接口 | 关键成员 |
| --- | --- | --- |
| 类型化 | `IDecayAnimationSpec<T>` | `IVectorizedDecayAnimationSpec Vectorize(ITwoWayConverter<T> converter)` |
| 向量 | `IVectorizedDecayAnimationSpec` | `AbsVelocityThreshold`、`GetDuration(initial, initialVelocity)`、`GetValue`、`GetVelocity`、`GetTargetValue(initial, initialVelocity, destination)` |
| 标量 | `IFloatDecayAnimationSpec` | `AbsVelocityThreshold`、`GetValue(TimeSpan, double, double)`、`GetVelocity(...)`、`GetDuration(double, double)`、`GetTargetValue(double initialValue, double initialVelocity)` |

## 内置实现与桥接件速查

| 类型 | 所在层 | 说明 |
| --- | --- | --- |
| `FloatTweenSpec(TimeSpan duration, TimeSpan delay = default, Easing? easing = null)` | 标量 | 缓动曲线插值，默认 `ComposeEasings.FastOutSlowIn` |
| `FloatSpringSpec(double dampingRatio = Spring.DampingRatioNoBouncy, double stiffness = Spring.StiffnessMedium, double visibilityThreshold = Spring.DefaultDisplacementThreshold)` | 标量 | 弹簧物理；`GetEndVelocity` 返回 0 |
| `FloatExponentialDecaySpec(double frictionMultiplier = 1, double absVelocityThreshold = 0.1)` | 标量衰减 | 指数衰减 |
| `SplineBasedFloatDecayAnimationSpec(double density)` | 标量衰减 | Android fling 曲线；`density` 传 `TopLevel.RenderScaling` |
| `VectorizedFloatAnimationSpec(IFloatAnimationSpec anim)` | 桥接 | 把标量规格套用到**所有维度**；实现 `IVectorizedFiniteAnimationSpec` |
| `VectorizedFloatAnimationSpec(IFloatAnimationSpec[] anims)` | 桥接 | 逐维度规格；数组长度必须等于转换器维度，否则采样时抛 `ArgumentException` |
| `VectorizedTweenSpec(TimeSpan duration, TimeSpan delay = default, Easing? easing = null)` | 向量 | 参考实现：包装 `VectorizedFloatAnimationSpec` 并补 `Delay`/`Duration` |
| `VectorizedSpringSpec(double dampingRatio, double stiffness, ReadOnlySpan<double> visibilityThreshold, int dimension)` | 向量 | 每维度一个 `FloatSpringSpec` |
| `VectorizedSnapSpec(TimeSpan delay)` | 向量 | 跳切 |
| `DecayAnimationSpec.GenerateDecayAnimationSpec<T>(this IFloatDecayAnimationSpec)` | 桥接 | 标量衰减 → `IDecayAnimationSpec<T>`（逐维度套用） |
| `DecayAnimationSpec.ExponentialDecay<T>(...)` / `DecayAnimationSpec.SplineBasedDecay<T>(double density)` | 工厂 | 上面两个标量衰减的现成 `IDecayAnimationSpec<T>` |
| `DecayAnimationSpec.CalculateTargetValue<T>(this IDecayAnimationSpec<T>, ITwoWayConverter<T>, T, T)` | 工具 | 不运行动画，直接算自然停止点（另有 `float`/`double` 便捷重载） |

类型化外观层的内置规格——`SpringSpec<T>`、`TweenSpec<T>`、`SnapSpec<T>`、`KeyframesSpec<T>`、`ArcAnimationSpec<T>`、`RepeatableSpec<T>`、`InfiniteRepeatableSpec<T>`——都是"存配置 + `Vectorize` 桥接"的范例，写自己的外观层时可以对照源码。

## 教程：实现一个带回超（overshoot）的时长型规格

目标：一个冲过终点再回弹的曲线规格，可用于任意可向量化的 `T`。

### 第 1 步：实现 `IFloatAnimationSpec`

曲线取回超函数 `f(t) = (tension + 1)·(t − 1)³ + tension·(t − 1)² + 1`：`f(0) = 0`、`f(1) = 1`，中段大于 1（回超量随 `tension` 增大）。逐成员说明见代码注释：

```csharp
using ComposeAnimation.Runtime;

/// <summary>回超曲线：冲过目标值后回弹。tension 越大回超越明显。</summary>
public sealed class FloatOvershootSpec : IFloatAnimationSpec
{
    private readonly double _tension;

    public FloatOvershootSpec(TimeSpan duration, double tension = 2.0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegative(tension);
        Duration = duration;
        _tension = tension;
    }

    public TimeSpan Duration { get; }

    // 当前值 = initial + (target − initial) · f(t)。曲线类规格忽略 initialVelocity
    public double GetValue(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity)
    {
        var t = NormalizedTime(playTime);
        var u = t - 1.0;
        var fraction = (_tension + 1.0) * u * u * u + _tension * u * u + 1.0;
        return initialValue + (targetValue - initialValue) * fraction;
    }

    // 速度：数值微分（与内置 FloatTweenSpec 相同的近似方案），单位 值/秒
    public double GetVelocity(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity)
    {
        var clamped = ClampPlayTime(playTime);
        if (clamped == TimeSpan.Zero)
            return initialVelocity;
        var start = GetValue(clamped - TimeSpan.FromMilliseconds(1), initialValue, targetValue, initialVelocity);
        var end = GetValue(clamped, initialValue, targetValue, initialVelocity);
        return (end - start) * 1000.0;
    }

    // 时长型规格：时长与初始条件无关
    public TimeSpan GetDuration(double initialValue, double targetValue, double initialVelocity)
        => Duration;

    // GetEndVelocity 使用接口默认实现（终点时刻的速度）；本曲线在 t = 1 处斜率为 0，无需覆写

    private double NormalizedTime(TimeSpan playTime)
    {
        if (Duration == TimeSpan.Zero) return 1.0;
        return Math.Clamp(ClampPlayTime(playTime).TotalSeconds / Duration.TotalSeconds, 0, 1);
    }

    private TimeSpan ClampPlayTime(TimeSpan playTime)
        => playTime < TimeSpan.Zero ? TimeSpan.Zero : playTime > Duration ? Duration : playTime;
}
```

### 第 2 步：`Vectorize` 桥接（最小路径）

实现 `IFiniteAnimationSpec<T>`，在 `Vectorize` 里把标量规格交给 `VectorizedFloatAnimationSpec`（所有维度共用同一标量规格）：

```csharp
public sealed class OvershootSpec<T> : IFiniteAnimationSpec<T>
{
    public OvershootSpec(TimeSpan? duration = null, double tension = 2.0)
    {
        Duration = duration ?? AnimationConstants.DefaultDuration;
        Tension = tension;
    }

    public TimeSpan Duration { get; }
    public double Tension { get; }

    public IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<T> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        return new VectorizedFloatAnimationSpec(new FloatOvershootSpec(Duration, Tension));
    }
}
```

到此即可用于 `Animatable<T>` 与 `Transition<TState>`（见第 5 步）。

### 第 3 步（可选）：实现 `IDurationBasedAnimationSpec<T>`

想被 `RepeatableSpec<T>` / `InfiniteRepeatableSpec<T>` 包装（例如喂给 `InfiniteTransition`）就必须实现时长型接口。`VectorizedFloatAnimationSpec` 只实现了 `IVectorizedFiniteAnimationSpec`，需要像内置 `VectorizedTweenSpec` 那样再包一层，补上 `Delay`/`Duration`：

```csharp
public sealed class VectorizedOvershootSpec : IVectorizedDurationBasedAnimationSpec
{
    private readonly VectorizedFloatAnimationSpec _anim;

    public VectorizedOvershootSpec(TimeSpan duration, double tension)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        Duration = duration;
        _anim = new VectorizedFloatAnimationSpec(new FloatOvershootSpec(duration, tension));
    }

    public TimeSpan Delay => TimeSpan.Zero;
    public TimeSpan Duration { get; }

    public void GetValue(TimeSpan playTime, ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target, ReadOnlySpan<double> initialVelocity, Span<double> destination)
        => _anim.GetValue(playTime, initial, target, initialVelocity, destination);

    public void GetVelocity(TimeSpan playTime, ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target, ReadOnlySpan<double> initialVelocity, Span<double> destination)
        => _anim.GetVelocity(playTime, initial, target, initialVelocity, destination);

    // IsInfinite（false）、GetDuration（Delay + Duration）、GetEndVelocity 均用接口默认实现
}
```

外观层相应改为：

```csharp
public sealed class OvershootSpec<T> : IDurationBasedAnimationSpec<T>
{
    // …属性同第 2 步…

    public TimeSpan Delay => TimeSpan.Zero;
    public TimeSpan Duration { get; }
    public double Tension { get; }

    public IVectorizedDurationBasedAnimationSpec Vectorize(ITwoWayConverter<T> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        return new VectorizedOvershootSpec(Duration, Tension);
    }
}
```

### 第 4 步（可选）：逐维度规格

若不同维度要用不同参数（例如 `Point` 的 X/Y 回超量不同），用数组构造的 `VectorizedFloatAnimationSpec`；数组长度必须等于 `converter.Dimension`：

```csharp
public IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<T> converter)
{
    ArgumentNullException.ThrowIfNull(converter);
    var perDimension = new IFloatAnimationSpec[converter.Dimension];
    for (var i = 0; i < perDimension.Length; i++)
        perDimension[i] = new FloatOvershootSpec(Duration, Tension + i);
    return new VectorizedFloatAnimationSpec(perDimension);
}
```

### 第 5 步：在动画宿主上使用

```csharp
// Animatable<T>（时钟见 ../runtime/overview.md）
var animatable = new Animatable<double>(new DoubleConverter(), 0, clock);
var result = await animatable.AnimateToAsync(100, new OvershootSpec<double>(TimeSpan.FromMilliseconds(300)));

// Transition<TState>：作为某段的规格
var alpha = transition.AnimateDouble(
    s => s ? 1.0 : 0.0,
    _ => new OvershootSpec<double>(TimeSpan.FromMilliseconds(300)));

// InfiniteTransition：需要第 3 步的 IDurationBasedAnimationSpec<T> 版本
var pulse = infinite.AnimateDouble(0, 100,
    new InfiniteRepeatableSpec<double>(
        new OvershootSpec<double>(TimeSpan.FromMilliseconds(800)), RepeatMode.Reverse));
```

## 注意事项

- **`Vectorize` 的调用时机**：每次（重）配置动画都会调用（`TargetBasedAnimation<T>` 构造时），不是一次性缓存。外观层只存配置；每次动画的状态放进向量化实例。
- **可重入性**：同一规格实例会被多个动画、多个维度共享（`VectorizedFloatAnimationSpec(IFloatAnimationSpec)` 就是全维度共用同一标量实例）。内置规格在调用之间不保留任何每动画状态——每次调用的结果完全由入参决定（`FloatSpringSpec` 内部虽有可变字段，但每次调用都被完整重写）。你的实现也应如此。采样始终发生在帧时钟所属线程，顺序调用是安全的；自行跨线程使用需自行同步。
- **性能**：`GetValue`/`GetVelocity` 每帧每维度都会调用。结果写入 `destination`，需要临时缓冲时用 `stackalloc`（参考 `VectorizedRepeatableSpec`）；`Span<double>` 参数不能被捕获到 lambda 或跨 `await` 使用。向量维度很小（1～4 维），逐维度循环即可。
- **有限性约束**：`Transition` 的子动画必须有限——向量化规格 `IsInfinite == true` 会在注册时抛 `ArgumentException`；`InfiniteTransition` 则只接受 `InfiniteRepeatableSpec<T>`（其内层动画必须是 `IDurationBasedAnimationSpec<T>`，且时长与延迟不能同时为 0）。
- **`GetEndVelocity` 语义**：它决定动画正常结束时交给下一支动画的起始速度。曲线类规格用默认实现（终点瞬时速度）即可；物理类规格应覆写为 0，与 `FloatSpringSpec`/`VectorizedSpringSpec` 保持一致，否则结束瞬间会多出一段残余速度。
- **维度校验**：内置实现用程序集内部的 `VectorValidation` 校验输入/目的地维度；程序集外的实现若直接写向量层接口，请自行检查 `destination.Length` 与各输入长度一致（`VectorizedFloatAnimationSpec` 桥接层已代做）。
- **精度**：`int`、`Color` 等转换器有舍入，速度的权威形式是 `double` 向量；需要保真接力时读取 `VelocityVector`/`GetVelocityVector` 而非转换后的 `Velocity`。转换器的维度约定见 [转换器与缓动](../runtime/converters-and-easing.md)。

## 延伸阅读

- [内置动画规格](../runtime/animation-specs.md)
- [转换器与缓动](../runtime/converters-and-easing.md)
- [引擎总览：帧时钟、线程与异步模型](../runtime/overview.md)
- [`Animatable`：单值动画](../runtime/animatable.md)
- [状态过渡：`Transition<TState>` 与 `InfiniteTransition`](../runtime/transitions.md)
- [返回文档入口](../README.md)
