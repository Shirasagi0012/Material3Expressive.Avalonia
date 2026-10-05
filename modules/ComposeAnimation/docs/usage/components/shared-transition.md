# SharedTransition 共享元素过渡

> [!WARNING]
> SharedTransition 是**实验性功能**：API 表面、匹配行为和布局契约仍可能在不预告的情况下变更。`SharedBounds` 的 `RemeasureToBounds` 模式尤其不稳定，升级版本前请重新核对本文与 `docs/SHARED_TRANSITION.md`。

共享元素过渡（shared element transition）让两个页面上的控件在导航时表现为"同一个元素飞过去"：列表里的卡片展开成详情页大图，而不是各自淡入淡出。ComposeAnimation 的 SharedTransition 将 Jetpack Compose 的共享元素模型移植到 Avalonia，建立在两层既有设施之上：

- **[`AnimatedVisibility`](./animated-visibility.md)**：每个端点（共享内容）必须放在一个 `AnimatedVisibility` 之下，由它提供进入/退出的可见性作用域；
- **调用方持有的 [`Transition<TState>`](../runtime/transitions.md)**：页面状态机由调用方创建、驱动帧循环（`RunAsync()`）并负责 `Dispose()`；`SharedTransitionHost` 只在其上协调共享过渡，不接管它的生命周期。

配对、准备、会话生命周期和覆盖层绘制由 host 内部的协调器完成；应用代码只需要声明端点的 `Key`、接线页面可见性，并在导航时调用 `BeginUpdate()`。

可运行的列表页 ↔ 详情页示例见 `Catalog/Demos/Shared/SharedListDetailDemo.axaml`（使用 `ScaleToBounds`）。

## 快速开始

下面是列表页 → 详情页的共享过渡示例：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
        x:Class="Demo.MainWindow">
    <ca:SharedTransitionHost x:Name="Host">
        <Grid>
            <!-- 列表页：每个列表项是一个共享端点，Key 绑定到条目 Id -->
            <ca:AnimatedVisibility x:Name="ListPage"
                                   Enter="{x:Static Demo.MainWindow.PageEnter}"
                                   Exit="{x:Static Demo.MainWindow.PageExit}">
                <ItemsControl ItemsSource="{Binding Items}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <ca:SharedBounds Key="{Binding Id}">
                                <!-- 卡片内容 -->
                            </ca:SharedBounds>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </ca:AnimatedVisibility>

            <!-- 详情页：Key 绑定到当前选中条目的同一个 Id -->
            <ca:AnimatedVisibility x:Name="DetailsPage"
                                   Enter="{x:Static Demo.MainWindow.PageEnter}"
                                   Exit="{x:Static Demo.MainWindow.PageExit}">
                <ca:SharedBounds Key="{Binding SelectedItem.Id}">
                    <!-- 详情内容 -->
                </ca:SharedBounds>
            </ca:AnimatedVisibility>
        </Grid>
    </ca:SharedTransitionHost>
</Window>
```

代码后置（页面过渡用 120ms 淡入淡出，只作用于共享卡片之外的页面内容）：

```csharp
using ComposeAnimation;
using ComposeAnimation.Runtime;

public partial class MainWindow : Window
{
    public static readonly EnterTransition PageEnter =
        new FadeIn { Spec = new TweenSpec<double>(TimeSpan.FromMilliseconds(120)) };
    public static readonly ExitTransition PageExit =
        new FadeOut { Spec = new TweenSpec<double>(TimeSpan.FromMilliseconds(120)) };

    private readonly Transition<bool> _page;

    public MainWindow()
    {
        var motion = VisualAnimationScope.Initialize(this);
        InitializeComponent();
        // true 表示"当前在列表页"；根环境负责帧源与生命周期。
        _page = motion.CreateTransition(true);
        Host.Transition = _page;
        ListPage.Source = VisibilitySource.From(_page, list => list);
        DetailsPage.Source = VisibilitySource.From(_page, list => !list);
        Opened += (_, _) => _ = motion.Launch(ct => _page.RunAsync(ct));
    }

    private void Navigate(DemoItem? item)
    {
        // Key 交换（SelectedItem 变化）与页面翻转必须落在同一个准备生成里。
        using (Host.BeginUpdate())
        {
            ViewModel.Show(item);              // 更新 SelectedItem / IsListPage
            _page.UpdateTarget(ViewModel.IsListPage);
        }
    }
}
```

要点：

1. 两个页面的 `AnimatedVisibility` 必须通过 `VisibilitySource.From(_page, ...)` 挂到**同一个**父 `Transition<bool>` 上；用各自独立的状态创建页面会产生互不相干的根 transition，端点永远无法配对。
2. 两侧端点的 `Key` 使用相等的值（这里是条目 `Id`），类型一致（同为 `SharedBounds`）。
3. 导航时把"换 key 的内容绑定"和"翻转页面"一起包进 `Host.BeginUpdate()`，否则它们会被拆成两个准备，看到两张卡片同时起飞或没有配对。

## 核心概念

### Host 坐标空间

`SharedTransitionHost` 是一个 `ContentControl`，定义一个共享过渡坐标空间。覆盖层（overlay）就画在 host 模板内的 `SharedTransitionOverlay` 上，所有飞行矩形都以 host 坐标表示。规则：

- **不同 host 里的端点永不配对**；端点还必须与 host 在同一个 `TopLevel`（窗口）里，跨窗口的 key 会回退为普通进入/退出。
- 一个 host 绑定**一个**父 `Transition`：开始共享准备之后再更换或清空 `Transition` 会抛出 `InvalidOperationException`；调用 `BeginUpdate()` 之前必须先设置 `Transition`。
- host 不启动、不停止也不 `Dispose` 父 transition 的帧循环——那始终是调用方的责任。

### 端点与注册时机

`SharedElement` 和 `SharedBounds` 都继承自抽象类 `SharedTransitionContent`，称为**端点**（endpoint）。端点不需要手工注册：

- 当它某个祖先 `AnimatedVisibility` 的可见性作用域（scope）**被创建时**，端点自动向最近的 `SharedTransitionHost` 注册。祖先没有 `AnimatedVisibility` 的共享内容不会注册，也就不参与任何配对。
- `Key` 为 `null` 的端点不注册。注册跟随可视化树生命周期；`AnimatedVisibility` 在换页时会短暂移除旧的呈现再创建新呈现，注册在此期间保留，由 host 统一管理。

### Key 跨页面匹配

一次导航中，host 收集所有端点的快照并按 `Key` 匹配：一个**离开方**（当前可见、目标隐藏）与一个**进入方**（当前隐藏、目标可见）配上同一 key，就形成一次共享会话。匹配失败的 key 回退为普通的 `Enter`/`Exit` 动画：

- 同一方向（同侧）出现重复 key 是**二义**（ambiguous），该 key 整体回退，matcher 不会按注册顺序挑选。
- `SharedElement` 与 `SharedBounds` 即使 key 相同也**不配对**（模式不匹配），两侧都按普通内容显示。
- 目标端缺少有效布局矩形（未完成布局、零尺寸、跨 `TopLevel`、含不可逆的用户 `RenderTransform` 等）时，该 key 回退。

### 准备（preparation）与生成（generation）

每次父 transition 的目标将要变化，scope 会先创建一次**准备**：为所有已注册端点拍摄快照（key、当前/目标可见状态、布局矩形），并持有父 transition 与各可见性 transition 的准备租约，阻止它们提前完成。每次准备带一个单调递增的**生成号**（`Generation`）。

准备等待布局完成后由协调器解析：配对成功的 key 安装共享会话并开始覆盖层飞行；未就绪的 key 各自回退。若在 `PreparationTimeout`（默认 1200ms）内布局一直没有就绪，准备**过期**，所有 key 回退为普通动画——界面不会卡在等待状态。取消或过期只放弃协调，**不会改变应用自己的导航状态**。

`Host.BeginUpdate()` 把一批 key/内容/可见性变更合并成**一个**生成；没有它，对 `UpdateTarget` 的每次调用、每次内容绑定变化都可能各自触发一次准备。

### 覆盖层绘制

飞行期间，配对端点的原地在位绘制被抑制，改由 host 的覆盖层绘制（通过 `VisualBrush` 捕获表面）。覆盖层 `IsHitTestVisible = false`，不会成为隐形的点击障碍；会话结束的同一回调里恢复进入方树的绘制并移除覆盖层。离开方端点保持抑制，直到它的可见性作用域把它卸载——即使共享会话先于普通退出动画完成。

## API 要点

### `SharedTransitionHost : ContentControl`

| 成员 | 类型 | 说明 |
| --- | --- | --- |
| `Transition` | `Transition?`（styled） | 调用方持有的父 `Transition<TState>`。使用前必须设置；开始准备后不可更换 |
| `PreparationTimeout` | `TimeSpan`（styled） | 准备等待布局的超时，默认 1200ms；设为负值禁用超时 |
| `BeginUpdate()` | 返回 `SharedTransitionUpdate` | 开启一个变更批，`Dispose` 时提交为一个准备生成。未设置 `Transition` 时抛出 `InvalidOperationException` |
| `Scope` | `SharedTransitionScope?` | 当前共享作用域（低层入口，见下） |
| `IsPreparing` | `bool` | 是否有准备正在等待布局/提交 |
| `IsOverlayActive` | `bool` | 覆盖层当前是否在绘制飞行内容 |
| `RegisteredEndpointCount` | `int` | 当前已注册端点数 |
| `ActiveSessionCount` | `int` | 未关闭的共享会话数 |
| `EndpointStates` | `string` | 各端点 `key: 当前状态->目标状态` 的汇总（如 `photo: Visible->PostExit`），以 ` \| ` 连接 |
| `LastPreparationParticipants` | `string` | 最近一次准备请求的参与者摘要，无请求时为 `-` |
| `Diagnostics` | `IReadOnlyList<string>` | 最近一次布局/准备解析产生的诊断（未匹配、二义、模式不符等 key），每次布局通过前清空重建 |
| `LastPreparationError` / `LastSessionError` | `Exception?` | 最近一次准备回调 / 会话运行与交接的异常；失败只取消该次协调，不影响导航 |

### `SharedTransitionContent : ContentControl`（abstract）

| 成员 | 类型 | 说明 |
| --- | --- | --- |
| `Key` | `object?`（styled） | 跨页面匹配键；`null` 不注册 |
| `BoundsTransform` | `SharedBoundsTransform?`（styled） | 边界动画规约来源；为 `null` 时使用 `new SpringSpec<Rect>()` |
| `ZIndexInOverlay` | `double`（styled） | 该端点在覆盖层中的 Z 顺序，用于多个并发共享会话的叠放 |

### `SharedElement : SharedTransitionContent`

一个视觉标识整体飞过去：过渡期间只有**进入方**内容画在覆盖层，源端点只提供起始矩形并保持在位不绘制。适合图片、图标等两侧内容视觉一致的场合。

### `SharedBounds : SharedTransitionContent`

两侧各自保留自己的内容：源淡出、目标淡入，经过同一个动画矩形。适合卡片、文字等在移动中内容也会变化的场合。额外成员：

| 成员 | 类型 | 说明 |
| --- | --- | --- |
| `FadeSpec` | `IFiniteAnimationSpec<double>`（styled） | 内容交叉淡入淡出规约，默认 `new SpringSpec<double>()`。边界与淡变可用不同规约，会话等两者都完成 |
| `ResizeMode` | `SharedBoundsResizeMode`（styled） | `ScaleToBounds`（默认，缩放目标内容到共享矩形）或 `RemeasureToBounds`（飞行中对真实内容实时重测，见下文） |

### `SharedBoundsTransform`

| 成员 | 类型 | 说明 |
| --- | --- | --- |
| `Spec` | `IFiniteAnimationSpec<Rect>` | 边界动画规约，默认 `new SpringSpec<Rect>()` |
| `Create(Rect from, Rect to)` | `virtual` 方法 | 按本次飞行的起点/终点矩形选择规约；默认返回 `Spec`。可派生后按距离、方向等返回不同规约 |

### `SharedTransitionScope`（低层）

`SharedTransitionHost` 的正常入口已足够；只有绕过 host 自建协调时才直接使用它。abstract、`IDisposable`；`static SharedTransitionScope Create<TState>(Transition<TState> parent)` 创建；公开 `ParentTransition`、`Generation`、`IsUpdating`、`IsPreparing`、`LastPreparationError`/`LastSessionError`、`BeginUpdate()`。它不启动/停止父 transition 的帧循环，自身也不负责渲染。

### `SharedTransitionUpdate : IDisposable`

`BeginUpdate()` 返回的同步变更批。`Dispose` 提交最外层批次；`Cancel()` 放弃整批协调（不回滚已经写入的应用状态）。**不要跨 `await` 持有**；嵌套批次必须按获取的反序 `Dispose`。

## 常见模式 / 进阶用法

### 选择 `SharedElement` 还是 `SharedBounds`

| 控件 | 覆盖层内容 | 典型用途 |
| --- | --- | --- |
| `SharedElement` | 仅进入方捕获 | 图片、图标等两侧视觉一致的内容 |
| `SharedBounds` | 同一矩形内源淡出 + 目标淡入 | 卡片、文字等移动中内容也变化的场合 |

两者都保留逻辑父级、`DataContext`、绑定以及用户设置的 `Opacity`/`RenderTransform`。

### `ResizeMode`：缩放还是重测

- `ScaleToBounds`（默认）：目标内容按共享矩形**缩放**呈现。对内容零要求，代价是飞行中内容是拉伸的位图。
- `RemeasureToBounds`：飞行中对**真实内容**按当前共享矩形实时重测、重排——文字会随宽度重新折行。这是协作式 lookahead 布局：端点的直接内容必须是实现 `ILookaheadConstrainedLayoutNode` 的协作式控件（如派生自 `LookaheadControl`，或用 `LookaheadBoundary` 包一层普通子树），参见 [AnimatedBounds 与 lookahead](./animated-bounds.md)。没有外层 lookahead 域时 `SharedBounds` 会自动创建一个本地域，无需手工放置 `LookaheadRoot`。

```xml
<ca:SharedBounds Key="{Binding Id}" ResizeMode="RemeasureToBounds">
    <demo:SharedCardLayout Title="{Binding Title}" Color="{Binding Color}" />
</ca:SharedBounds>
```

`RemeasureToBounds` 的硬约束（违反会在布局时抛出 `NotSupportedException`，而不是静默缩放）：保留默认模板；直接内容是全约束 `Control`；内容对齐为 Stretch；不设 `ContentTemplate`/`Padding`/`BorderThickness`（装饰放进协作式内容里）；端点到其坐标参考之间不允许用户 `RenderTransform` 或 RTL 镜像。

### 为什么需要 `BeginUpdate()`

共享配对发生在"换 key 的内容"与"页面翻转"**同时生效**的那一次布局上。分开执行会产生两个生成：第一个生成里详情页还没有选中项（key 为空），第二个生成里列表项已经消失——两次都匹配失败。包进一个批次后两者属于同一准备：

```csharp
using (Host.BeginUpdate())
{
    _vm.Show(item);                       // 详情端点的 Key 换到选中项
    _page.UpdateTarget(_vm.IsListPage);   // 页面翻转
}
```

返回导航同理；demo 里返程时保留 `SelectedItem`，直到共享会话把表面交还。不要跨 `await` 持有批次。

### 定制边界与淡变规约

demo 用 600ms 的补间替代默认弹性边界，用瞬时切换替代默认淡变（两端点内容在任意中间尺寸完全一致时，交叉淡变会让不透明背景半透明）：

```csharp
public static readonly SharedBoundsTransform CardMotion = new()
{
    Spec = new TweenSpec<Rect>(TimeSpan.FromMilliseconds(600), easing: new CubicEaseInOut())
};
public static readonly IFiniteAnimationSpec<double> ContentHandoff = new SnapSpec<double>();
```

```xml
<ca:SharedBounds Key="{Binding Id}"
                 BoundsTransform="{x:Static Demo.MainWindow.CardMotion}"
                 FadeSpec="{x:Static Demo.MainWindow.ContentHandoff}" />
```

注意 `FadeSpec = SnapSpec<double>` 只让内容所有权瞬时切换，**并不**关闭边界动画。需要按飞行距离/方向选择规约时派生 `SharedBoundsTransform` 并覆盖 `Create(Rect from, Rect to)`。

### 多个并发会话的叠放

同一批导航中可以有多个 key 同时飞行（例如整组卡片）。用 `ZIndexInOverlay` 控制它们在覆盖层中的叠放顺序：

```xml
<ca:SharedElement Key="{Binding Id}" ZIndexInOverlay="1" />
```

### 与页面 `Enter`/`Exit` 配合

共享会话只接管配对端点；页面的其余部分仍由 `AnimatedVisibility` 的 `Enter`/`Exit` 负责。demo 的做法是给页面配一个 120ms 的 `FadeIn`/`FadeOut`（上文 `PageEnter`/`PageExit`），只淡入淡出共享卡片之外的集合，视觉上卡片"持续存在"，其余内容让位。`Enter`/`Exit` 的呈现变换（淡入/滑动/缩放）会被共享布局读取自动剥离，不会污染飞行矩形。

### 运行中的反转与忙状态

飞行中反向导航是支持的：当前会话原地反转（边界动画掉转目标，淡变取反并保留速度）。demo 用 host 状态驱动"过渡中"标志，阻止飞行中打开另一张卡片：

```csharp
_vm.IsTransitioning = _page.IsRunning || Host.IsPreparing || Host.IsOverlayActive;
```

## 注意事项与诊断

- **调用方拥有父 transition**：创建 `Transition<TState>`、设置 `Host.Transition`、`RunAsync()` 启动帧循环、窗口关闭时 `Dispose()`，都是应用的责任。host 只协调，不接管生命周期。
- **先设 `Transition` 再 `BeginUpdate()`**，且开始共享准备后不得更换 `Transition`（抛 `InvalidOperationException`）。
- **端点必须在某个祖先 `AnimatedVisibility` 下**，且两侧页面的 `Source` 必须派生自同一个父 transition，否则永不配对。
- **Key 规则**：`null` 不注册；同侧重复 key 按二义回退；`SharedElement` 与 `SharedBounds` 同 key 不配对。
- **准备超时**：`PreparationTimeout` 默认 1200ms，布局长期不就绪时准备过期、所有 key 回退为普通动画；设为负值可禁用超时（不建议在真实界面使用）。
- **诊断入口**：
  - `Diagnostics`——每次布局通过时重建，列出未匹配、二义、模式不符、跨 `TopLevel` 等 key 的原因；
  - `LastPreparationError` / `LastSessionError`——准备回调与会话运行/交接的最近异常；这些失败只取消当次协调，**不会改动应用的导航状态**；
  - `EndpointStates`、`LastPreparationParticipants`、`RegisteredEndpointCount`、`ActiveSessionCount`——快速核对注册与配对是否符合预期。
- **主题按压态的坑**：Fluent 主题按钮的按下缩放会在松手后残留约 75ms，若以缩放后的位图作为源捕获会得到错误起点。demo 在卡片按钮上显式 `RenderTransform="{x:Null}" Transitions="{x:Null}"` 规避；`RemeasureToBounds` 的坐标链本来就只支持布局平移。
- **覆盖层不接收输入**（`IsHitTestVisible = false`）；共享中的端点外层同样不命中测试，原位不会成为隐形点击目标。
- **嵌套共享内容**：子级 `SharedElement`/`SharedBounds` 有自己的会话时会从父级捕获中剔除；若子级始终未配对则不会被抑制，父级不会留下空洞。
- **运行中的 `ResizeMode` 变更**会取消当前飞行，而不是连续切换模式。
- **测试参考**：`ComposeAnimation.Test/SharedTransitionHostTest.cs`（注册、配对、反转、二义回退、覆盖层命中测试）、`ComposeAnimation.Test/SharedBoundsLookaheadTest.cs`（`RemeasureToBounds` 的重测与交接）覆盖了本文描述的大部分行为。

### 演示与验证入口

Catalog 当前提供 `ScaleToBounds` 的列表 ↔ 详情演示：

```powershell
dotnet run --project Catalog/Catalog.csproj -- --demo shared/list-detail
dotnet run --project Catalog/Catalog.csproj -- --run shared/list-detail --out artifacts/shared-transition
```

`RemeasureToBounds` 的自动化验证见 `ComposeAnimation.Test/SharedBoundsLookaheadTest.cs`。

## 延伸阅读

- [AnimatedVisibility](./animated-visibility.md)：端点所依赖的进入/退出作用域与 `VisibilitySource`
- [AnimatedBounds](./animated-bounds.md)：lookahead 布局与 `RemeasureToBounds` 要求的协作式内容
- [Transition（运行时）](../runtime/transitions.md)：`Transition<TState>` 的创建、目标更新与帧循环
- [文档入口](../README.md)
- `docs/SHARED_TRANSITION.md`（即 [../../SHARED_TRANSITION.md](../../draft/SHARED_TRANSITION.md)）：英文参考，含布局契约细节与支持矩阵
- 可运行示例：`Catalog/Demos/Shared/SharedListDetailDemo.axaml`
