using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ComposeAnimation;

/// <summary>
/// Queries and invalidates an experimental cooperative lookahead domain without exposing its
/// internal coordinator. Queries never run layout and return only fully committed target data.
/// </summary>
public static class Lookahead
{
    /// <summary>
    /// Raised directly on every cooperative participant after a complete target generation commits.
    /// Ordinary descendants and native content below a LookaheadBoundary do not receive it.
    /// </summary>
    public static readonly RoutedEvent<LookaheadTargetLayoutEventArgs> TargetLayoutUpdatedEvent =
        RoutedEvent.Register<Control, LookaheadTargetLayoutEventArgs>(
            "LookaheadTargetLayoutUpdated", RoutingStrategies.Direct);

    public static void AddTargetLayoutUpdatedHandler(
        Control participant, EventHandler<LookaheadTargetLayoutEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(participant);
        ArgumentNullException.ThrowIfNull(handler);
        participant.AddHandler(TargetLayoutUpdatedEvent, handler);
    }

    public static void RemoveTargetLayoutUpdatedHandler(
        Control participant, EventHandler<LookaheadTargetLayoutEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(participant);
        ArgumentNullException.ThrowIfNull(handler);
        participant.RemoveHandler(TargetLayoutUpdatedEvent, handler);
    }
    /// <summary>
    /// Returns the participant's committed target snapshot. Returns <see langword="null"/> when
    /// the target is dirty or uncommitted, or when <paramref name="participant"/> is not a member
    /// of its inherited domain. A domain entry is considered a member even though it is not stored
    /// in its own result table.
    /// </summary>
    public static LookaheadLayoutSnapshot? GetSnapshot(Control participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        participant.VerifyAccess();
        var context = LookaheadScope.FindContext(participant);
        var snapshot = context?.Snapshot;
        if (context is null || context.IsTargetDirty || snapshot is null) return null;
        return ReferenceEquals(context.Owner, participant) || snapshot.Results.ContainsKey(participant)
            ? snapshot : null;
    }

    /// <summary>Returns this participant's committed target result, or null when none is available.</summary>
    public static LookaheadLayoutResult? TargetFor(Control participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        var snapshot = GetSnapshot(participant);
        return snapshot?.Results.GetValueOrDefault(participant);
    }

    /// <summary>Invalidates the complete target layout of the participant's current domain.</summary>
    public static void InvalidateTargetLayout(Control participant)
        => RequireContext(participant).InvalidateTargetLayout();

    /// <summary>
    /// Invalidates real approach measurement without discarding the committed target snapshot.
    /// </summary>
    public static void InvalidateApproachMeasure(Control participant)
    {
        var context = RequireContext(participant);
        participant.InvalidateMeasure();
        context.InvalidateApproachPlacement();
    }

    /// <summary>
    /// Invalidates real approach placement without discarding the committed target snapshot.
    /// </summary>
    public static void InvalidateApproachArrange(Control participant)
        => RequireContext(participant).InvalidateApproachPlacement();

    /// <summary>
    /// Runs a synchronous move inside the content's current domain while preserving eligible
    /// in-flight motion. The move must finish in the same attached domain and coordinate reference.
    /// </summary>
    public static void MoveContent(Control content, Action move)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(move);
        var context = RequireContext(content);
        if (ReferenceEquals(context.Owner, content))
            throw new InvalidOperationException("MoveContent requires content inside a lookahead domain, not the domain entry itself.");
        context.MoveContent(content, move);
    }

    private static LookaheadContext RequireContext(Control participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        participant.VerifyAccess();
        var context = LookaheadScope.FindContext(participant);
        if (context is null)
            throw new InvalidOperationException("The control is not in a lookahead domain.");
        if (context.Contains(participant)) return context;
        throw new InvalidOperationException(
            "The control is not a participant in its inherited lookahead domain.");
    }
}

/// <summary>Data for a committed target-layout generation.</summary>
public sealed class LookaheadTargetLayoutEventArgs : RoutedEventArgs
{
    internal LookaheadTargetLayoutEventArgs(LookaheadLayoutSnapshot snapshot)
        : base(Lookahead.TargetLayoutUpdatedEvent)
    {
        Snapshot = snapshot;
    }

    public LookaheadLayoutSnapshot Snapshot { get; }
}
