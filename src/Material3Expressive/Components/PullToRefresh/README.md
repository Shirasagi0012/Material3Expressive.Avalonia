# Pull to refresh

自动连接正文滚动的下拉刷新容器、圆箭头、Expressive 形状指示器和自定义内容容器。
`PullToRefreshBox` 处理拖动及释放，应用负责实际刷新工作与 `IsRefreshing`。

## 基础用法

加载 MaterialTheme 并初始化窗口动画作用域后，在有限高度中包裹一个纵向 ScrollViewer：

```xml
<m3:PullToRefreshBox xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
                    x:Name="RefreshBox" Height="360"
                    IsRefreshing="{Binding IsRefreshing}"
                    RefreshRequested="RefreshRequested">
  <ScrollViewer HorizontalScrollBarVisibility="Disabled"
                VerticalScrollBarVisibility="Hidden">
    <!-- 真实可滚动正文 -->
  </ScrollViewer>
</m3:PullToRefreshBox>
```

```csharp
private async void RefreshRequested(object? sender, EventArgs e)
{
    ViewModel.IsRefreshing = true;
    try { await ViewModel.ReloadAsync(); }
    finally { ViewModel.IsRefreshing = false; }
}
```

到正文顶部后继续向下拉，严格越过阈值并释放才触发一次请求。`IsRefreshing=false` 自动隐藏。
系统取消已开始的拖动同样以零速度结束：未越阈值回弹，越阈值仍请求刷新。
`RefreshEnabled=false` 仅阻止新的拉动消费，不禁用正文；按上游规则，已累积的拉动仍处理释放。
不指定 `State` 时，`ActualState` 提供容器拥有的内置状态。`IsScrollConnected` 可诊断实际接入状态。
容器默认拉伸正文，以便 Avalonia ScrollViewer 得到有限视口；自定义布局可设置内容对齐。

已有 nested-scroll 宿主也可以只使用指示器，将它放在内容上方：

```xml
<Grid xmlns="https://github.com/avaloniaui"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <Grid.Resources><m3:PullToRefreshState x:Key="RefreshState" /></Grid.Resources>
  <ScrollViewer><!-- 页面内容 --></ScrollViewer>
  <m3:PullToRefreshIndicator x:Name="Indicator" State="{StaticResource RefreshState}" />
</Grid>
```

在宿主已决定刷新时调用：

```csharp
Indicator.IsRefreshing = true;
await Indicator.State!.AnimateToThresholdAsync();
// 完成业务刷新后：
Indicator.IsRefreshing = false;
await Indicator.State.AnimateToHiddenAsync();
```

## 变体与属性

自定义内容容器支持固定上游 IndicatorBox 的形状入口。`Shape` 使用共享 MaterialShape，null 保留
CornerRadius；形状同时用于背景、阴影和内容裁剪，不改变距离、布局大小或顶部裁剪。

```xml
<m3:PullToRefreshIndicatorBox State="{Binding RefreshState}" Background="#EADDFF">
  <m3:PullToRefreshIndicatorBox.Shape><m3:CutCornerShape TopStart="12" TopEnd="4" BottomEnd="12" BottomStart="4" /></m3:PullToRefreshIndicatorBox.Shape>
  <m3:MaterialSymbol Symbol="Refresh" Width="24" Height="24" />
</m3:PullToRefreshIndicatorBox>
```

两个内置指示器继承该容器属性，方便本地定制；固定 Compose 的 Indicator/LoadingIndicator 快捷函数
没有独立 shape 参数，直接对应的参考入口是 IndicatorBox 自定义内容组合。Catalog 的下拉刷新窗口
同时显示圆形、Expressive 和切角自定义容器，共用距离和刷新状态。

- `PullToRefreshIndicator`：40 DIP 容器，拉动时显示箭头，刷新时显示旋转进度。
- `PullToRefreshLoadingIndicator`：48 DIP 容器，拉动时形变，刷新时循环形变；超过阈值继续旋转。
- `State.DistanceFraction` 为只读距离比例，1 表示阈值，允许大于 1。`SnapToAsync(fraction)` 设置拉动距离，两个 `AnimateTo…Async` 方法使用可中断弹簧。
- `MaxDistance` 默认 80 DIP，决定距离比例对应的屏幕位移；它不注册刷新手势或触发回调。
- `IsRefreshing` 控制刷新内容与阴影显隐；未连接 `ScrollBehavior` 时不自动写入 State。`Background`、`Color`、`Elevation`、`CornerRadius` 可覆盖外观。

## 内容槽与定制

`PullToRefreshBox.Indicator` 可替换默认圆形指示器。自定义槽负责绑定相同状态与刷新标志：

```xml
<m3:PullToRefreshBox x:Name="ExpressiveRefresh" IsRefreshing="{Binding IsRefreshing}">
  <m3:PullToRefreshBox.Indicator>
    <m3:PullToRefreshLoadingIndicator State="{Binding #ExpressiveRefresh.ActualState}"
                                    IsRefreshing="{Binding #ExpressiveRefresh.IsRefreshing}" />
  </m3:PullToRefreshBox.Indicator>
  <ScrollViewer HorizontalScrollBarVisibility="Disabled"><!-- 正文 --></ScrollViewer>
</m3:PullToRefreshBox>
```

容器的 `RefreshThreshold` 控制实际手势阈值；指示器的 `MaxDistance` 独立控制视觉位移，均默认 80 DIP。
容器不自动重写自定义槽的绑定、大小或对齐。普通外部状态和派生状态均可用于 `State`。

自定义内容使用 `PullToRefreshIndicatorBox` 的 `Content` / `ContentTemplate`，保留同一位移、顶部裁剪和阴影规则：

```xml
<m3:PullToRefreshIndicatorBox State="{StaticResource RefreshState}" Background="Gold">
  <Border Width="16" Height="16" Background="DarkBlue" />
</m3:PullToRefreshIndicatorBox>
```

容器默认在顶部居中。位移不改变其布局占位，顶部之外裁掉，底部与两侧可溢出；父容器需要为实际显示区域留出空间。使用局部组件 Token 可动态调整默认距离、尺寸、颜色和动效。

可派生 `PullToRefreshState` 为阈值和隐藏操作选择不同动画，既有 `State` 绑定不变。例如高回弹进入、默认弹簧隐藏：

```csharp
using ComposeAnimation.Runtime;
using Material3Expressive.Components;

public sealed class BouncyRefreshState : PullToRefreshState
{
    public override Task AnimateToThresholdAsync(CancellationToken cancellationToken = default) =>
        AnimateToAsync(1, new SpringSpec<float>(dampingRatio: .2f), cancellationToken);
}

// 设置给任一指示器；ScrollBehavior 也会调用派生状态的操作。
Indicator.State = new BouncyRefreshState();
```

覆写 `AnimateToHiddenAsync` 可单独定制隐藏；覆写操作调用受保护的 `AnimateToAsync`，继续使用同一距离值、速度和取消生命周期。
`SnapToAsync` 也可覆写；调用基类方法发布距离并取消已有动画。异步覆写必须传递取消令牌，不应另起未受控的后台动画。
Catalog 下方的显式滚动示例使用这一高回弹状态。
“预览越阈值回弹”按钮让右侧圆箭头保持拉动模式，展示距离动画反复跨越阈值时的透明度过渡；再次点击可中断重播。

**外部状态**

`State` 也接受实现 `IPullToRefreshState` 的普通 ViewModel：发布 `DistanceFraction`、`IsAnimating`
的 `INotifyPropertyChanged` 通知，并实现 `SnapToAsync`、`AnimateToThresholdAsync`、`AnimateToHiddenAsync`。
通知必须在 UI 线程发出；运行状态为 true 时，滚动策略不消费新的拖动输入。
距离不额外限制到 0–1，超阈值的箭头张力与 Expressive 旋转保持原规则。

外部实现负责自己的动画资源和生命周期，异步操作应遵守调用方取消令牌。指示器换源或卸载只退订外部状态，
不会销毁应用共享的 ViewModel；滚动策略卸载仍取消由它发起的操作。内置 `PullToRefreshState`
继续共享同窗口的动画作用域，最后一个消费者卸载时取消内置动画。
Catalog 的“外部可观察状态”展示普通 `ObservableObject` 和 partial observable property 驱动两个指示器。

迁移：`State` / `StateProperty` 的声明类型由 `PullToRefreshState` 改为 `IPullToRefreshState`。
已有 `new PullToRefreshState(...)`、派生类和 XAML 资源无需修改。目标类型推断的 `State = new(1)`
改为 `State = new PullToRefreshState(1)`；需要存储任意状态时使用接口类型；依赖内置 DirectProperty
的代码保留明确的内置实例引用。仓内此类调用已迁移。

## 使用限制与注意事项

有 nested-scroll 协议的宿主可以设置 `ScrollBehavior`，由它处理拖动张力、阈值和回弹：

```csharp
var behavior = new PullToRefreshScrollBehavior();
Indicator.ScrollBehavior = behavior;
behavior.RefreshRequested += (_, _) => Indicator.IsRefreshing = true;

// 在宿主的对应回调中调用。正 Y 向下，距离为 DIP，速度为 DIP/s。
var preConsumed = behavior.OnPreScroll(availableY, isUserInput);
// 将 availableY - preConsumed 交给滚动子项；把其剩余量送入 post：
var postConsumed = behavior.OnPostScroll(childRemainingY, isUserInput);
var consumedVelocity = await behavior.OnPreFlingAsync(availableVelocityY);
// 将 availableVelocityY - consumedVelocity 交给后续参与者。
// 业务刷新完成后设为 false，behavior 自动执行隐藏弹簧：
Indicator.IsRefreshing = false;
```

`RefreshThreshold` 默认 80 DIP，与视觉 `MaxDistance` 分别配置。原始拖动距离乘以 .5，超过阈值后使用递减张力。
只有严格超过阈值才请求刷新；释放只消费向下速度。`behavior.IsEnabled=false` 阻止滚动消费，但按上游语义不跳过已经拉动后的释放处理。
behavior 只能连接一个挂载的指示器；挂载时根据 `IsRefreshing` 初始化距离，换 behavior 或卸载会取消它拥有的任务。
刷新请求不会自动完成业务刷新。回弹被刷新状态、其他 State 操作或卸载中断时，`OnPreFlingAsync` 可能抛出 `OperationCanceledException`。

自动容器只接入内容中的唯一纵向物理 Material ScrollViewer，横轴须为 Disabled；多个并列视口、
逻辑滚动内容和其他呈现器不会自动连接。嵌套 ScrollViewer 不作为第二个候选；它自己的滚动仍由原生 chaining 规则处理。
它可与标准 Scaffold 的普通/搜索 AppBar 共用正文视口，按祖先次序进行 pre、正文、反序 post。
触摸/笔采用固定 Core 的 18 DIP 纵向阈值，首笔越阈值输入只扣除这段距离。
普通桌面滚轮按 Core 默认行为不产生 pre-fling，不能把滚轮 idle 当作刷新释放。
鼠标与触控板仍遵循现有滚动呈现器输入能力，不声明未测试设备事件等价。

显式 `PullToRefreshScrollBehavior` 入口仍由宿主提供实际消费顺序与释放速度，不能仅把 `ScrollChanged` 当成 nested-scroll。
Catalog 同时提供真实触摸自动容器、距离滑块和显式 pre/post/release 按钮示例。

动画需在指示器挂载后调用；构造初值或 `SnapToAsync` 可在挂载前使用。操作被新动画、Snap、取消令牌或最后一个消费者卸载中断时，任务取消，保留当前距离。同一 State 可供同窗口的多个指示器使用，不能跨窗口同时使用。

圆箭头需要 Skia 后端。内部映射、未实现范围和实际双端结果见 [INTERNALS.md](INTERNALS.md)。
