# 类型转换器与缓动（Converters & Easing）

引擎内部始终在 `double` 向量上运算：值、速度、边界、弹簧阈值都是逐分量的 `double`。`ITwoWayConverter<T>` 负责 `T` 与向量之间的双向转换，是把任意类型接入动画系统的关口；`Easing` 则决定时长型动画的节奏曲线。二者都是 `ComposeAnimation.Runtime` 里所有规格与持有者的共用基础。

> 转换器在哪里被消费、弹簧如何用阈值判定结束，见 [Animatable\<T\>](./animatable.md) 与 [动画规格](./animation-specs.md)；时钟与线程等前置概念见 [运行时概览](./overview.md)。

## 快速开始

```csharp
using Avalonia;                      // Point
using Avalonia.Animation.Easings;    // CubicEaseIn
using ComposeAnimation.Runtime;

// 为值类型挑选对应的转换器与类型化阈值（取自 LookaheadPosition.cs 的生产用法）
var position = new Animatable<Point>(
    new PointConverter(), new Point(0, 0), clock,
    visibilityThreshold: VisibilityThresholds.Point);

await position.AnimateToAsync(new Point(100, 200), new SpringSpec<Point>());

// 为时长型规格挑缓动：Compose 曲线或 Avalonia 内置曲线均可
var tween = new TweenSpec<double>(
    TimeSpan.FromMilliseconds(300), easing: ComposeEasings.FastOutSlowIn);
var tween2 = new TweenSpec<double>(
    TimeSpan.FromMilliseconds(300), easing: new CubicEaseIn());
```

## API 要点

### ITwoWayConverter\<T\>

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| `Dimension` | `int`（get） | — | 向量维度，须 ≥ 1；决定动画的向量化维度 |
| `To` | `void To(T value, Span<double> destination)` | — | 把值写入向量；`destination` 长度等于 `Dimension` |
| `From` | `T From(ReadOnlySpan<double> vector)` | — | 从向量读回值 |

内置实现均为 `sealed` 无参类，直接 `new` 即可（无单例）：

| 转换器 | `T` | 维度 | 向量布局 |
|---|---|---|---|
| `DoubleConverter` | `double` | 1 | `[value]` |
| `FloatConverter` | `float` | 1 | `[value]` |
| `Int32Converter` | `int` | 1 | `[value]`（`From` 截断取整） |
| `PointConverter` | `Avalonia.Point` | 2 | `[X, Y]` |
| `VectorConverter` | `Avalonia.Vector` | 2 | `[X, Y]` |
| `SizeConverter` | `Avalonia.Size` | 2 | `[Width, Height]` |
| `RectConverter` | `Avalonia.Rect` | 4 | `[Left, Top, Right, Bottom]`（**边**，不是宽高） |
| `ThicknessConverter` | `Avalonia.Thickness` | 4 | `[Left, Top, Right, Bottom]` |
| `CornerRadiusConverter` | `Avalonia.CornerRadius` | 4 | `[TopLeft, TopRight, BottomRight, BottomLeft]` |
| `ColorConverter` | `Avalonia.Media.Color` | 4 | `[Alpha, Oklab L, Oklab a, Oklab b]` |

### AnimationVector

向量化后的值/速度容器，一般只在自定义规格或读取 `AnimationState<T>.VelocityVector` 时直接接触。

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| 构造 | `AnimationVector(int dimension)` | — | `dimension <= 0` 抛 `ArgumentOutOfRangeException` |
| `Dimension` | `int`（get） | — | 分量个数 |
| `this[int]` | `double`（get/set） | — | 索引器 |
| `AsSpan()` / `AsReadOnlySpan()` | `Span<double>` / `ReadOnlySpan<double>` | — | 零拷贝访问底层存储 |
| `Clear()` | `void` | — | 全部分量归零 |
| `CopyFrom` | `void CopyFrom(ReadOnlySpan<double> source)` | — | 维度不一致抛 `ArgumentException` |
| `Clone()` | `AnimationVector` | — | 深拷贝 |
| `CreateZero()` | `AnimationVector`（实例方法） | — | 同维度的零向量 |
| `From` | `static AnimationVector From(ReadOnlySpan<double> components)` | — | 从分量数组新建 |

### VisibilityThresholds

弹簧结束判定的逐分量阈值表。弹簧在「某分量离目标的距离与速度都低于阈值」时认为该分量到位；阈值过紧（如对 DIP 几何值用默认 0.01）会让收尾明显拖长，过松则会提前截停。

| 成员 | 类型/签名 | 值 | 适用 |
|---|---|---|---|
| `Default` | `const double` | 0.01（= `Spring.DefaultDisplacementThreshold`） | `float`/`double`/Color（Oklab）的每分量缺省 |
| `Dip` | `const double` | 0.5 | Avalonia DIP 几何类型的每分量阈值 |
| `Float` / `Double` | `const float` / `const double` | 0.01 | 标量 |
| `Int32` | `const int` | 1 | 整数（小于 1 的位移取整后即消失） |
| `Point` / `Vector` / `Size` | `static readonly` | 各分量 0.5 | 对应 2D 类型 |
| `Rect` | `static readonly Rect` | `new Rect(0.5, 0.5, 0, 0)` | 零宽高使右/下边等于左/上边，四条边各得 0.5 |
| `Thickness` / `CornerRadius` | `static readonly` | `new Thickness(0.5)` / `new CornerRadius(0.5)` | 四分量各 0.5 |
| `For<T>` | `static T? For<T>(ITwoWayConverter<T> converter)` | — | 按**转换器类型**（非实例）查表；`ColorConverter` 与未知类型返回 `default` |

### Easing 与 CubicBezierEasing

缓动的基类是 Avalonia 的 `Avalonia.Animation.Easings.Easing`（`double Ease(double progress)`），因此 Avalonia 自带缓动（`CubicEaseIn`、`CubicEaseInOut`、`LinearEasing` 等）可直接用于 `TweenSpec`、`ArcAnimationSpec` 与关键帧区间。

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| `CubicBezierEasing` 构造 | `CubicBezierEasing(double a, double b, double c, double d)` | — | 三次贝塞尔缓动，(a,b)、(c,d) 为两个控制点；任一参数为 `NaN` 抛 `ArgumentException` |
| `A` / `B` / `C` / `D` | `double`（get） | — | 四个控制点坐标 |
| — | 实现 `IEquatable<CubicBezierEasing>` | — | 按控制点比较相等性与哈希 |

### ComposeEasings

Material/Compose 标准曲线，静态只读实例：

| 成员 | 类型 | 控制点 | 说明 |
|---|---|---|---|
| `FastOutSlowIn` | `CubicBezierEasing` | (0.4, 0, 0.2, 1) | 快进慢出；`TweenSpec`、`ArcAnimationSpec` 的默认缓动 |
| `LinearOutSlowIn` | `CubicBezierEasing` | (0, 0, 0.2, 1) | 匀速入场、减速出场 |
| `FastOutLinearIn` | `CubicBezierEasing` | (0.4, 0, 1, 1) | 加速入场、匀速出场 |
| `Linear` | `LinearEasing`（Avalonia） | — | 恒等曲线；关键帧区间的默认缓动 |

## 常见模式/进阶用法

### 自定义转换器

实现 `ITwoWayConverter<T>` 即可让任意类型参与动画。要点：`Dimension` 与向量布局保持一致、`To`/`From` 互为逆操作、实现保持无状态。下面为一个二维进度类型写转换器（模式与内置 `PointConverter` 相同）：

```csharp
using ComposeAnimation.Runtime;

/// <summary>演示用的二维进度（X、Y 两个通道）。</summary>
public readonly record struct Progress2D(double X, double Y);

public sealed class Progress2DConverter : ITwoWayConverter<Progress2D>
{
    public int Dimension => 2;

    public void To(Progress2D value, Span<double> destination)
    {
        destination[0] = value.X;
        destination[1] = value.Y;
    }

    public Progress2D From(ReadOnlySpan<double> vector) => new(vector[0], vector[1]);
}
```

之后 `new Animatable<Progress2D>(new Progress2DConverter(), new Progress2D(0, 0), clock)` 即可使用全部规格与边界能力。

### 为类型选阈值

```csharp
// 手工给弹簧指定类型化阈值（取自 VisibilityThresholdsTest.cs）
var spec = new SpringSpec<Point>(visibilityThreshold: VisibilityThresholds.Point);

// 或按转换器查表：Color/未知类型得到 default（弹簧退回每分量 0.01）
var threshold = VisibilityThresholds.For(new PointConverter()); // VisibilityThresholds.Point
```

经验法则：DIP 几何值用 `Dip`（0.5）级别，纯标量用 0.01，整数用 1。

### 自定义缓动

多数场景 `CubicBezierEasing` 够用（含过冲曲线，如 `new CubicBezierEasing(0.34, 1.56, 0.64, 1.0)`）；更特殊的曲线可继承 `Easing` 并重写 `Ease(double progress)`。`Ease` 可能被并行求值，实现应保持无状态（`ComposeEasings` 实例即并发安全）。

## 注意事项

- **维度即一切**：`Dimension` 决定速度向量、边界、弹簧阈值的形状。`To` 写入的分量数必须与 `Dimension` 一致，且 `To`/`From` 布局必须自洽，否则动画行为错误但不一定抛异常。
- **`RectConverter` 向量化的四条边**是 `[Left, Top, Right, Bottom]`：边界与阈值按边理解，用 `Rect` 表达边值时请写 `new Rect(left, top, right - left, bottom - top)`。
- **`Int32Converter.From` 截断取整**：值在 delay 或低速段可能逐帧相等，但内部速度向量仍以全精度推进（这也是 `Animatable<T>` 不按等值抑制通知的原因之一）。
- **`ColorConverter` 走 Oklab**：以固定 Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的 `Color.VectorConverter` 和 ui-graphics 转换为依据。sRGB / XYZ / Oklab 的矩阵和 Float 运算边界保留；输入与输出转换都包含上游 Oklab Color 的 Half 分量及 10 位 alpha 打包。向量接口仍为 double，写入的是拆包后的 Float 值；`From` 先投影为 Float 并裁到合法域（L∈[0,1]，a/b∈[-0.5,0.5]，alpha∈[0,1]）。输出仍为 byte sRGB，API 不变。黑到白的中点是 `(99,99,99)`。
- **颜色往返可能有损**：`From(To(color))` 与原字节颜色不保证完全相同，例如 `#FF00FE01` 经固定上游路径得到 `#FF00FE00`。`TargetBasedAnimation` 保留原始 InitialValue / TargetValue，结束时直接返回原目标；`Animatable.SnapToAsync` 也保留精确目标。固定 Compose 的输入向量、颜色投影和弹簧采样预期见 `ComposeAnimation.Test/Fixtures/ColorConverter/`；本次对齐不改变 core 现有 double 计算及时间表示。
- **`VisibilityThresholds.For` 按转换器类型查表**：两个 `PointConverter` 实例得到同一阈值；`ColorConverter`（无法把 Oklab 阈值无损编码回 `Color`）与未知类型返回 `default`，此时弹簧逐分量退回 0.01。
- **转换器无单例**：内置转换器都是无状态 `sealed` 类，随用随 `new`，不要假设引用相等有任何语义。

## 延伸阅读

- [Animatable\<T\>](./animatable.md)：转换器与阈值的消费方
- [动画规格](./animation-specs.md)：缓动在 `TweenSpec`/关键帧/弧线中的使用
- [Transition](./transitions.md)：声明式动画如何复用这些基础类型
- [运行时概览](./overview.md)：帧时钟与线程模型
- [返回使用文档入口](../README.md)
