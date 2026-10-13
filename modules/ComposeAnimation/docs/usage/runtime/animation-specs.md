# 动画规格（Animation Specs）

动画规格（spec）描述「值如何随时间变化」：`Animatable<T>.AnimateToAsync` 的第二个参数、`Transition` 的动画声明、控件层的 `AnimationSpec` 属性，接受的都是这里的类型。规格本身是轻量配置对象；动画创建时引擎调用 `IAnimationSpec<T>.Vectorize(converter)` 把规格按转换器维度向量化一次，之后规格的字段不再被读取。

接口层次：

```text
IAnimationSpec<T>                        // 一切规格（含无限动画）
└── IFiniteAnimationSpec<T>              // 有限动画（弹簧等物理规格也在此列）
    └── IDurationBasedAnimationSpec<T>   // 时长已知的规格，暴露 Delay / Duration
IDecayAnimationSpec<T>                   // 衰减规格，独立的一支
```

> Easing（缓动曲线）在 [类型转换器与缓动](./converters-and-easing.md) 展开，本篇只引用。规格与 `Animatable<T>` 的配合见 [Animatable\<T\>](./animatable.md)；打断、时钟与 `MotionDurationScale` 等前置概念见 [运行时概览](./overview.md)。

## 快速开始

```csharp
using ComposeAnimation.Runtime;

// 时长精确：300ms 补间（默认缓动 FastOutSlowIn）
await animatable.AnimateToAsync(motion, 100,
    new TweenSpec<double>(duration: TimeSpan.FromMilliseconds(300)));

// 物理手感：弹簧，被打断时带着速度接续
await animatable.AnimateToAsync(motion, 100,
    new SpringSpec<double>(Spring.DampingRatioMediumBouncy));
```

## API 要点

### SpringSpec\<T\>：弹簧

基于阻尼比与刚度的物理弹簧；时长由物理决定，被打断时从当前速度接续，手感最自然。`Animatable<T>.AnimateToAsync` 缺省用它。

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| 构造 | `SpringSpec(double dampingRatio = Spring.DampingRatioNoBouncy, double stiffness = Spring.StiffnessMedium, T? visibilityThreshold = default)` | 见左 | `dampingRatio < 0`、`stiffness <= 0` 抛 `ArgumentOutOfRangeException` |
| `DampingRatio` | `double`（get/set） | 1 | 阻尼比：< 1 欠阻尼（回弹过冲），= 1 临界阻尼（不过冲） |
| `Stiffness` | `double`（get/set） | 1500 | 刚度：越大越快 |
| `VisibilityThreshold` | `T?`（get） | `default`（视为未设置） | 各分量距离与速度低于该阈值时判定结束；阈值越松结束越早 |

`Spring` 静态常量：

| 常量 | 值 | 常量 | 值 |
|---|---|---|---|
| `Spring.StiffnessHigh` | 10 000 | `Spring.DampingRatioHighBouncy` | 0.2 |
| `Spring.StiffnessMedium` | 1 500 | `Spring.DampingRatioMediumBouncy` | 0.5 |
| `Spring.StiffnessMediumLow` | 400 | `Spring.DampingRatioLowBouncy` | 0.75 |
| `Spring.StiffnessLow` | 200 | `Spring.DampingRatioNoBouncy` | 1 |
| `Spring.StiffnessVeryLow` | 50 | `Spring.DefaultDisplacementThreshold` | 0.01 |

```csharp
// 中等回弹的弹簧，0.01 的结束阈值
var bouncy = new SpringSpec<double>(
    Spring.DampingRatioMediumBouncy,
    Spring.StiffnessMedium,
    visibilityThreshold: 0.01);
await animatable.AnimateToAsync(motion, 100, bouncy);
```

位移类值建议传类型化阈值（如 `VisibilityThresholds.Point`，每分量 0.5 DIP），否则默认 0.01 会让收尾拖长——见 [VisibilityThresholds](./converters-and-easing.md)。

### TweenSpec\<T\>：补间

在固定时长内按缓动曲线插值，时长精确可控，是 Material 节奏的标准载体。

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| 构造 | `TweenSpec(TimeSpan? duration = null, TimeSpan? delay = null, Easing? easing = null)` | 见下 | 三个属性也可在构造后赋值；`Duration`/`Delay` 赋负值抛 `ArgumentOutOfRangeException` |
| `Duration` | `TimeSpan`（get/set） | 300ms（`AnimationConstants.DefaultDuration`） | 动画时长 |
| `Delay` | `TimeSpan`（get/set） | 0 | 开始前保持初始值的时间 |
| `Easing` | `Easing`（get/set） | `ComposeEasings.FastOutSlowIn` | 可用 `ComposeEasings.*`、`CubicBezierEasing` 或 Avalonia 自带缓动 |

```csharp
// 1 秒、无延迟、Avalonia 内置 CubicEaseInOut
var spec = new TweenSpec<double>(
    TimeSpan.FromMilliseconds(1000),
    TimeSpan.FromMilliseconds(0),
    new CubicEaseInOut());
```

### SnapSpec\<T\>：瞬时跳变

立即跳到目标值，可选延迟。`Duration` 恒为零；对 `TargetBasedAnimation<T>` 而言总时长等于 `Delay`。

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| 构造 | `SnapSpec(TimeSpan? delay = null)` | 0 | 负延迟抛 `ArgumentOutOfRangeException` |
| `Delay` | `TimeSpan`（get） | 0 | 延迟期间保持初始值 |
| `Duration` | `TimeSpan`（get） | `TimeSpan.Zero` | 跳变本身不占时长 |

```csharp
// 50ms 后瞬间到位（取自 SnapSpecTest.cs）
var spec = new SnapSpec<double>(TimeSpan.FromMilliseconds(50));
```

### KeyframesSpec\<T\>：关键帧

在毫秒时间轴上钉关键点，区间默认线性插值；可对单个区间挂缓动或弧线。0 时刻未定义关键帧时从动画初始值插值，末尾未定义时向目标值插值。

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| 构造 | `KeyframesSpec(KeyframesSpecConfig<T> config)` | — | |
| `Delay` / `Duration` | `TimeSpan`（get） | 取自 config | |

`KeyframesSpecConfig<T>`（及样条版的 `KeyframesWithSplineSpecConfig<T>`，二者同基类 `KeyframesSpecBaseConfig<T>`）：

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| `Duration` | `TimeSpan`（get/set） | 300ms | 负值抛异常 |
| `Delay` | `TimeSpan`（get/set） | 0 | 负值抛异常 |
| `At` | `KeyframeEntity<T> At(int timestamp, T value)` | — | 在 `timestamp` **毫秒**处钉一个关键帧；负时间戳抛异常 |
| `AtFraction` | `KeyframeEntity<T> AtFraction(double fraction, T value)` | — | 在 `Duration` 的 `fraction` 比例处钉帧；`fraction` 须在 [0, 1] |

`KeyframeEntity<T>` 的 `Using(Easing)` / `Using(ArcMode)` 塑造**从该帧开始**的区间（默认 `ComposeEasings.Linear` + `ArcMode.ArcLinear`），返回自身可链式调用。

```csharp
// 取自 KeyframesSpecTest.cs：0~200ms 用 CubicEaseIn，其余区间线性
var config = new KeyframesSpecConfig<double> { Duration = TimeSpan.FromMilliseconds(400) };
config.At(0, 0).Using(new CubicEaseIn());
config.At(200, 100);
config.At(400, 200);

await animatable.AnimateToAsync(motion, 200, new KeyframesSpec<double>(config));
```

### KeyframesWithSplineSpec\<T\>：样条关键帧

用单调三次 Hermite 样条穿过所有关键帧，速度全程连续，产生平滑的曲线运动；同样支持按区间挂 `Easing`。

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| 构造 | `KeyframesWithSplineSpec(KeyframesWithSplineSpecConfig<T> config, double? periodicBias = null)` | `null`（非周期） | `periodicBias` 须在 [0, 1]，否则抛 `ArgumentOutOfRangeException` |
| `PeriodicBias` | `double`（get） | `double.NaN`（非周期） | 调整首尾速度使之相等的程度：0 只改首速度，1 只改尾速度，0.5 两者均改 |

```csharp
// 平滑穿过途经点的 2D 路径
var config = new KeyframesWithSplineSpecConfig<Point> { Duration = TimeSpan.FromMilliseconds(1000) };
config.At(0, new Point(0, 0));
config.At(250, new Point(50, 100));
config.At(750, new Point(150, 50));
config.At(1000, new Point(200, 200));

var spec = new KeyframesWithSplineSpec<Point>(config);
```

`periodicBias` 用于首尾相接的循环（如脉动）：起点与终点值相同并给出 bias，首、尾速度会被拉成相等。

### ArcAnimationSpec\<T\>：弧线位移

对 2D 值（`Point`、`Vector` 等）沿四分之一椭圆弧插值，而不是直线；一维值退化为线性。适合位置、偏移类动画。

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| 构造 | `ArcAnimationSpec(ArcMode mode = ArcMode.ArcBelow, TimeSpan? duration = null, TimeSpan? delay = null, Easing? easing = null)` | 见下 | `duration`/`delay` 为负抛异常 |
| `Mode` | `ArcMode`（get） | `ArcBelow` | `ArcLinear = 0`（直线）、`ArcBelow = 4`（弧线在连线下方）、`ArcAbove = 5`（弧线上方） |
| `Duration` | `TimeSpan`（get） | 300ms | |
| `Delay` | `TimeSpan`（get） | 0 | |
| `Easing` | `Easing`（get） | `ComposeEasings.FastOutSlowIn` | |

```csharp
// 从 (0,0) 到 (200,200) 走「上方」弧线：中点 X 超前、Y 滞后（取自 ArcAnimationSpecTest.cs）
var spec = new ArcAnimationSpec<Point>(
    ArcMode.ArcAbove, TimeSpan.FromMilliseconds(1000), easing: new LinearEasing());
await positionAnimatable.AnimateToAsync(new Point(200, 200), spec);
```

关键帧的区间也可以用 `config.At(...).Using(ArcMode.ArcAbove)` 单独弧化。

### RepeatableSpec\<T\> 与 InfiniteRepeatableSpec\<T\>：重复

把一个 `IDurationBasedAnimationSpec<T>`（补间、关键帧、弧线等）重复播放固定次数或无限次。

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| `RepeatableSpec` 构造 | `RepeatableSpec(int iterations, IDurationBasedAnimationSpec<T> animation, RepeatMode repeatMode = RepeatMode.Restart, StartOffset initialStartOffset = default)` | `Restart`、无偏移 | `iterations < 1` 抛 `ArgumentOutOfRangeException`；总时长 ≈ `iterations × (Delay + Duration)` |
| `InfiniteRepeatableSpec` 构造 | `InfiniteRepeatableSpec(IDurationBasedAnimationSpec<T> animation, RepeatMode repeatMode = RepeatMode.Restart, StartOffset initialStartOffset = default)` | 同上 | 永不自然结束（`Duration == TimeSpan.MaxValue`），需外部打断；被重复动画 `Delay` 与 `Duration` 全零时抛 `ArgumentException` |
| `RepeatMode` | `enum { Restart, Reverse }` | `Restart` | `Reverse` 每次反向回放 |
| `StartOffset` 构造 | `StartOffset(int offsetMillis, StartOffsetType offsetType = StartOffsetType.Delay)` | `Delay` | 偏移只在首轮前施加一次；`offsetMillis` 须非负 |
| `StartOffsetType` | `enum { Delay, FastForward }` | `Delay` | `Delay`：先空等 offset 再开始（总时长变长）；`FastForward`：直接从 offset 播放时间点开始（总时长变短） |

```csharp
// 3 次往返
var tween = new TweenSpec<double>(TimeSpan.FromMilliseconds(500), easing: new LinearEasing());
var spec = new RepeatableSpec<double>(3, tween, RepeatMode.Reverse);

// 无限循环 + 快进偏移，像进度指示器
var infinite = new InfiniteRepeatableSpec<double>(
    new TweenSpec<double>(TimeSpan.FromMilliseconds(1000), easing: new LinearEasing()),
    RepeatMode.Restart,
    new StartOffset(250, StartOffsetType.FastForward));
```

### 衰减规格：DecayAnimationSpec

衰减没有目标值：从初速度出发自然减速到静止，终值由物理决定。配合 `Animatable<T>.AnimateDecayAsync`（fling 之后）使用。工厂都挂在静态类 `DecayAnimationSpec` 上：

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| `ExponentialDecay<T>` | `static IDecayAnimationSpec<T> ExponentialDecay<T>(double frictionMultiplier = 1, double absVelocityThreshold = 0.1)` | 见左 | 摩擦与速度成正比的指数衰减；两参数均须 > 0。内部摩擦系数为 -4.2 × multiplier，落点 ≈ `initial - velocity / friction` |
| `SplineBasedDecay<T>` | `static IDecayAnimationSpec<T> SplineBasedDecay<T>(double density)` | — | Android OverScroller 同款 fling 样条；`density` 传 `TopLevel.RenderScaling`，须 > 0，缩放比变化时重建 spec |
| `CalculateTargetValue` | `spec.CalculateTargetValue(converter, initialValue, initialVelocity)`；另有 `float`/`double` 便捷重载 | — | 不跑动画，直接预测自然停下的落点 |
| `ScrollFriction` | `const double = 0.015` | — | 对应 Android `ViewConfiguration.getScrollFriction()`，供 `SplineBasedDecay` 内部使用 |
| `GenerateDecayAnimationSpec<T>` | `static IDecayAnimationSpec<T> GenerateDecayAnimationSpec<T>(this IFloatDecayAnimationSpec spec)` | — | 把自定义的标量衰减（`IFloatDecayAnimationSpec`）逐维应用到任意类型 |

```csharp
// fling 松手后的惯性滚动（取自 AnimatableTest.cs）
var result = await animatable.AnimateDecayAsync(motion, 500, DecayAnimationSpec.ExponentialDecay<double>());

// 预测落点，用于「惯性结束后对齐到最近项」之类的计算（取自 DecayAnimationSpecTest.cs）
var decay = DecayAnimationSpec.ExponentialDecay<double>();
double target = decay.CalculateTargetValue(10, 80);

// Android 手感的 fling 曲线
var fling = DecayAnimationSpec.SplineBasedDecay<double>(topLevel.RenderScaling);
```

## 常见模式/进阶用法

### 如何选择

| 需求 | 推荐规格 |
|---|---|
| 时长精确、Material 节奏（进出场、状态切换） | `TweenSpec<T>` |
| 物理手感、随时被打断也要自然（拖拽跟随、回弹） | `SpringSpec<T>`（也是缺省） |
| 立即跳变，可带延迟 | `SnapSpec<T>` |
| 精确编排中间关键点、各区间节奏不同 | `KeyframesSpec<T>` |
| 平滑途经多个点、速度全程连续（曲线路径） | `KeyframesWithSplineSpec<T>` |
| 2D 位置走弧线而非直线 | `ArcAnimationSpec<T>` |
| 心跳、脉冲、进度指示等循环 | `RepeatableSpec<T>` / `InfiniteRepeatableSpec<T>` |
| fling 之后的惯性衰减 | `DecayAnimationSpec.ExponentialDecay` / `SplineBasedDecay` |

### 离线采样：TargetBasedAnimation\<T\>

不挂时钟也能求值——`TargetBasedAnimation<T>` 把规格 + 转换器 + 起止值变成一个可按播放时间采样的对象，适合写测试、画曲线、预算时长：

```csharp
var animation = new TargetBasedAnimation<double>(
    new TweenSpec<double>(TimeSpan.FromMilliseconds(1000), easing: new CubicEaseInOut()),
    new DoubleConverter(),
    0, 100);

double mid = animation.GetValue(TimeSpan.FromMilliseconds(500));
TimeSpan total = animation.Duration; // 含延迟
double endVelocity = animation.GetEndVelocity();
```

衰减对应 `DecayAnimation<T>`（`TargetValue` 是预测落点）。`IsInfinite` 标记无限规格。

### 组合：先减速、再回弹

`Animatable<T>` 的互斥打断天然支持「衰减 → 弹簧」串接：衰减收尾时（或用户再次触摸时）直接 `AnimateToAsync`，新动画从当前值、当前速度接续，无需手工对齐速度。

## 注意事项

- **规格是启动时快照**：`Vectorize` 在动画创建时调用一次；改动 spec 的可变属性（如 `TweenSpec.Duration`）不影响在途动画。
- **`At` 的时间戳是毫秒 `int`**，不是 `TimeSpan`；超时长的时间戳合法但不推荐。
- **`ArcAnimationSpec` 只对 2D 有意义**；用于 `double` 等一维值时全程线性，直接用 `TweenSpec` 即可。
- **无限动画永不自然完成**：`InfiniteRepeatableSpec` 的 `AnimateToAsync` 任务只有被打断/取消才结束，注意捕获 OCE；`MotionDurationScale = 0` 时无限动画停在 0 播放时间（有限动画则直接跳到终点）。
- **参数校验都在构造时**：负时长、负延迟、负阻尼比、非正刚度、[0,1] 之外的 `periodicBias`、非正的衰减参数都会立即抛异常。
- **衰减的 `density`** 对应 Compose 的 `Density.density`（dpi / 160）；Avalonia 里传 `TopLevel.RenderScaling`，DPI 变化后重建 spec。
- Easing 的全集（`ComposeEasings`、`CubicBezierEasing`、Avalonia 内置缓动）见 [类型转换器与缓动](./converters-and-easing.md)。

## 延伸阅读

- [Animatable\<T\>](./animatable.md)：规格的消费方，互斥与打断语义
- [类型转换器与缓动](./converters-and-easing.md)：`Easing`、`ComposeEasings`、`CubicBezierEasing`
- [Transition](./transitions.md)：以声明方式把规格绑到状态变化上
- [运行时概览](./overview.md)：帧时钟、线程模型、`MotionDurationScale`
- [返回使用文档入口](../README.md)
