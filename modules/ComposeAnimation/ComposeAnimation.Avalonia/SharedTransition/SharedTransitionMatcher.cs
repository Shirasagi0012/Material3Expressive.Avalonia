using Avalonia;

namespace ComposeAnimation;

internal sealed record SharedTransitionPair(SharedTransitionParticipant Source, SharedTransitionParticipant Target,
    Rect SourceBounds, Rect TargetBounds);
internal enum SharedMatchFailure { MissingSource, MissingTarget, AmbiguousKey, SourceLayoutUnavailable, TargetLayoutUnavailable }
internal sealed record SharedMatchDiagnostic(object Key, SharedMatchFailure Reason, bool CanWait);
internal sealed record SharedTransitionMatchResult(long Generation, IReadOnlyList<SharedTransitionPair> Pairs,
    IReadOnlyList<SharedMatchDiagnostic> Diagnostics)
{
    // MissingTarget keeps CanWait so a still-empty batch can wait for a late template.
    // Once any pair is ready, unmatched keys fall back instead of blocking the rest.
    internal bool NeedsLayout =>
        Diagnostics.Any(d => d.Reason == SharedMatchFailure.TargetLayoutUnavailable) ||
        (Pairs.Count == 0 && Diagnostics.Any(d => d.CanWait));
}

internal static class SharedTransitionMatcher
{
    internal static SharedTransitionMatchResult Resolve(SharedTransitionPreparation request,
        IReadOnlyList<SharedTransitionMotion> motions, Func<SharedTransitionParticipant, Rect?> readTargetLayout)
    {
        var before = request.Before.ToDictionary(p => p.Id);
        var pairs = new List<SharedTransitionPair>();
        var diagnostics = new List<SharedMatchDiagnostic>();
        foreach (var group in request.Participants.Where(p => p.IsActive).GroupBy(p => p.Key))
        {
            var running = motions.Where(m => !m.Session.IsClosed && Equals(m.Pair.Source.Key, group.Key)).ToArray();
            var outgoing = group.Where(p => p.Visibility.TargetState != EnterExitState.Visible &&
                ((before.TryGetValue(p.Id, out var old) && old.TargetState == EnterExitState.Visible) ||
                 running.Any(m => m.Session.Uses(p)))).ToArray();
            var incoming = group.Where(p => p.Visibility.TargetState == EnterExitState.Visible).ToArray();
            // A stable registration unrelated to this intent requires no shared work.
            if (outgoing.Length == 0 && running.Length == 0 &&
                incoming.All(p => before.TryGetValue(p.Id, out var old) && old.TargetState == EnterExitState.Visible)) continue;
            if (outgoing.Length > 1 || incoming.Length > 1)
            {
                diagnostics.Add(new(group.Key, SharedMatchFailure.AmbiguousKey, false));
                continue;
            }
            if (outgoing.Length == 0) { diagnostics.Add(new(group.Key, SharedMatchFailure.MissingSource, false)); continue; }
            if (incoming.Length == 0) { diagnostics.Add(new(group.Key, SharedMatchFailure.MissingTarget, true)); continue; }
            var source = outgoing[0];
            var target = incoming[0];
            // A reversal starts at the running presentation, never at an original endpoint.
            var existing = running.SingleOrDefault(m => m.Session.Uses(source) && m.Session.Uses(target));
            var origin = existing?.Bounds.Value ?? (before.TryGetValue(source.Id, out var snapshot) ? snapshot.LayoutBounds : null);
            if (origin is null) { diagnostics.Add(new(group.Key, SharedMatchFailure.SourceLayoutUnavailable, false)); continue; }
            // A reversed endpoint can be inside a remeasuring shared ancestor. Its live visual
            // rectangle then belongs to the current flight, not to its resting layout. Preserve
            // the last committed resting rectangle per endpoint across repeated reversals.
            var reversed = existing is not null && existing.Pair.Source == target && existing.Pair.Target == source;
            var destination = reversed && target.IsLayoutAffectedByAncestorFlight &&
                existing!.TryGetRestingBounds(target, out var resting)
                ? resting : readTargetLayout(target);
            if (destination is null) { diagnostics.Add(new(group.Key, SharedMatchFailure.TargetLayoutUnavailable, true)); continue; }
            pairs.Add(new(source, target, origin.Value, destination.Value));
        }
        return new(request.Generation, pairs, diagnostics);
    }
}
