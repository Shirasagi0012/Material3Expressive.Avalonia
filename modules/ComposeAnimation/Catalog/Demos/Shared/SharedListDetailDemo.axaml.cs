using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Catalog.Framework;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Catalog.Demos.Shared;

/// <summary>调色板卡片条目。静态卡片没有条目级 DataContext，导航时按 Key 反查。</summary>
public sealed record SharedCardItem(string Id, string Title, Color Color)
{
    public string Description { get; } =
        $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";
}

/// <summary>
/// 移植自 Scratchpad/SharedTransitionDemo 的列表 → 详情共享元素过渡（ScaleToBounds 简化版）。
/// 与原 demo 的关键差异：调用方持有的 Transition&lt;bool&gt; 改用基类 <see cref="DemoControlBase.Clock"/>
/// 接线（自动化模式 = ProbeClock，交互模式 = 懒创建的环境时钟），因此创建推迟到附加可视树之后。
/// </summary>
[Demo("shared/list-detail", Title = "卡片展开", Page = "共享过渡", PageOrder = 4, Order = 0, Experimental = true)]
public partial class SharedListDetailDemo : DemoControlBase
{
    /// <summary>边界动画：弹性矩形。默认本就是 SpringSpec&lt;Rect&gt;，这里显式给出稍慢的参数便于观察飞行。</summary>
    public static readonly SharedBoundsTransform CardMotion = new()
    {
        Spec = new SpringSpec<Rect>(dampingRatio: 0.9, stiffness: 300)
    };

    /// <summary>内容交接：两侧内容布局不同（列表卡片 ↔ 详情头图），用短补间交叉淡入淡出。</summary>
    public static readonly IFiniteAnimationSpec<double> ContentCrossfade =
        new TweenSpec<double>(TimeSpan.FromMilliseconds(240), easing: new CubicEaseInOut());

    private static readonly IFiniteAnimationSpec<double> PageFade =
        new TweenSpec<double>(TimeSpan.FromMilliseconds(120), easing: new CubicEaseInOut());

    /// <summary>页面进出过渡：只作用于共享卡片之外的页面内容。</summary>
    public static readonly EnterTransition PageEnter = new FadeIn { Spec = PageFade };
    public static readonly ExitTransition PageExit = new FadeOut { Spec = PageFade };

    private static readonly SharedCardItem[] Items =
    [
        Item("lake", "Lake", 70, 130, 180),
        Item("grove", "Grove", 46, 139, 87),
        Item("dusk", "Dusk", 178, 87, 80),
        Item("coral", "Coral", 205, 92, 92),
        Item("sand", "Sand", 194, 163, 107),
        Item("mist", "Mist", 122, 158, 159)
    ];

    private static readonly IReadOnlyDictionary<string, SharedCardItem> ItemsById =
        Items.ToDictionary(item => item.Id);

    private Transition<bool>? _page;
    private SharedCardItem? _selected;
    private bool _isListPage = true;

    public SharedListDetailDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddDiagnostic("共享会话", () => Host.ActiveSessionCount, () => 0);
        AddHandler(InputElement.PointerPressedEvent, OnCardPointerPressed);
    }

    private bool IsTransitioning =>
        _page is { IsRunning: true } || Host.IsPreparing || Host.IsOverlayActive;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_page is not null) return;
        // 时钟接线：自动化模式 Clock 为 ProbeClock；交互模式为懒创建的环境时钟，需附加后才能访问。
        _page = Motion.CreateTransition(true, "shared/list-detail");
        Host.Transition = _page;
        ListPage.Source = VisibilitySource.From(_page, list => list);
        DetailsPage.Source = VisibilitySource.From(_page, list => !list);
        _ = Motion.Launch(ct => _page.RunAsync(ct));
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _page?.Dispose(); // Dispose 会同时取消 RunAsync 帧循环并级联释放子 transition
        _page = null;
    }

    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 交互模式下的真实点击路径；自动化动作直接调用 Open（导航方法）。
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.Source is not Control source) return;
        // 点击落在卡片内部的 Border/TextBlock 上，向上找共享内容本身，再按 Key 反查条目。
        var shared = source as SharedTransitionContent
            ?? source.GetVisualAncestors().OfType<SharedTransitionContent>().FirstOrDefault();
        if (shared?.Key is string key && ItemsById.TryGetValue(key, out var item))
            Open(item);
    }

    private void BackButton_OnClick(object? sender, RoutedEventArgs e) => Back();

    private void Open(SharedCardItem item)
    {
        if (_page is null || !_isListPage || IsTransitioning) return;
        Navigate(item);
    }

    private void Back()
    {
        // 打开飞行中返回同样支持：当前会话原地反转。
        if (_page is null || _isListPage) return;
        Navigate(null);
    }

    private void Navigate(SharedCardItem? item)
    {
        if (_page is null) return;
        // 换 key（详情端点切到选中卡片）与页面翻转必须落在同一个准备生成里，
        // 否则会被拆成两次准备，两个方向都匹配不到对端。
        using (Host.BeginUpdate())
        {
            if (item is not null)
            {
                _selected = item;
                DetailHero.Background = new SolidColorBrush(item.Color);
                DetailTitle.Text = item.Title;
                DetailDescription.Text = item.Description;
                DetailsCard.Key = item.Id;
            }
            _isListPage = item is null;
            _page.UpdateTarget(_isListPage);
        }
        // 返程时保留 _selected 与 DetailsCard.Key，直到共享会话把表面交还给列表卡片。
        BackButton.IsEnabled = !_isListPage;
    }

    [DemoAction("打开 dusk 卡片再返回：验证共享元素飞行、页面交接与回程配对（实验性功能）")]
    public async Task OpenAndClose()
    {
        await WaitForSettledAsync("初始");
        await CaptureAsync("initial");
        Assert(ListPage.IsContentPresent, "初始应显示列表页");
        // 精确计数：注册端点应恰好是 6 张列表卡片。若多出来，说明有脱离可视树的旧实例
        // 还揣着同名 Key 的陈旧注册（共享内容在 Detach 时按设计保留注册），所有 key 会按二义回退。
        var liveShared = this.GetVisualDescendants().OfType<SharedTransitionContent>().Count(x => x.Key is not null);
        Assert(Host.RegisteredEndpointCount == Items.Length,
            $"初始应恰好注册 {Items.Length} 个列表端点（注册 {Host.RegisteredEndpointCount}，" +
            $"可视树中带 Key 的共享内容 {liveShared}）：[{Host.EndpointStates}] " +
            $"diag=[{string.Join(";", Host.Diagnostics)}]");
        AssertNoErrors("初始");

        // 直接调用导航方法（与界面点击同一条路径）；真实指针点击走 OnCardPointerPressed。
        Open(Items.First(item => item.Id == "dusk"));

        await AdvanceAsync(2);
        Assert(Host.IsPreparing || Host.IsOverlayActive || Host.ActiveSessionCount > 0,
            "打开后应进入共享准备或飞行" +
            $"（IsPreparing={Host.IsPreparing}, IsOverlayActive={Host.IsOverlayActive}, Sessions={Host.ActiveSessionCount}, " +
            $"page={_page?.CurrentState}->{_page?.TargetState}, detailKey={DetailsCard.Key}, " +
            $"participants={Host.LastPreparationParticipants}, endpoints=[{Host.EndpointStates}], " +
            $"diag=[{string.Join(";", Host.Diagnostics)}]）");

        await AdvanceAsync(8);
        await CaptureAsync("flight");
        Assert(Host.IsOverlayActive, "去程飞行中覆盖层应处于活动状态");
        Assert(Host.ActiveSessionCount == 1, $"去程飞行中应恰好一个共享会话（当前 {Host.ActiveSessionCount}）");

        await WaitForSettledAsync("去程");
        await CaptureAsync("detail");
        Assert(DetailsPage.IsContentPresent && DetailsPage.IsIdle, "去程收敛后详情页应完全呈现");
        Assert(!ListPage.IsContentPresent, "去程收敛后列表页内容应已卸载");
        AssertNoErrors("去程收敛后");

        Click(BackButton);
        await AdvanceAsync(6);
        await CaptureAsync("return-flight");
        Assert(Host.IsOverlayActive, "回程飞行中覆盖层应处于活动状态");

        await WaitForSettledAsync("回程");
        await CaptureAsync("list");
        Assert(ListPage.IsContentPresent && ListPage.IsIdle, "回程收敛后列表页应完全呈现");
        Assert(!DetailsPage.IsContentPresent, "回程收敛后详情页内容应已卸载");
        AssertNoErrors("回程收敛后");
    }

    private bool IsSettled =>
        _page is { IsRunning: false } && !Host.IsPreparing && !Host.IsOverlayActive &&
        Host.ActiveSessionCount == 0 && ListPage.IsIdle && DetailsPage.IsIdle;

    private async Task WaitForSettledAsync(string phase)
    {
        for (var i = 0; i < 900 && !IsSettled; i++)
            await AdvanceAsync(1);
        Assert(IsSettled,
            $"{phase}应在限定帧数内收敛（IsRunning={_page?.IsRunning}, IsPreparing={Host.IsPreparing}, " +
            $"IsOverlayActive={Host.IsOverlayActive}, Sessions={Host.ActiveSessionCount}, {Host.EndpointStates}）");
    }

    private void AssertNoErrors(string phase)
    {
        Assert(Host.LastPreparationError is null, $"{phase}：LastPreparationError 应为 null（{Host.LastPreparationError}）");
        Assert(Host.LastSessionError is null, $"{phase}：LastSessionError 应为 null（{Host.LastSessionError}）");
    }

    private static SharedCardItem Item(string id, string title, byte r, byte g, byte b)
        => new(id, title, Color.FromRgb(r, g, b));
}
