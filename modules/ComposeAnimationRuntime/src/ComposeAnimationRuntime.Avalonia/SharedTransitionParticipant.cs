using Avalonia.Controls;

namespace ComposeAnimationRuntime.Avalonia;

public enum SharedParticipantRole { Source, Destination, Active }

public sealed record SharedTransitionParticipant(object Key, Control Control, SharedParticipantRole Role, LayoutSnapshot Snapshot);

public sealed class SharedParticipantRegistry
{
    private readonly Dictionary<object, List<SharedTransitionParticipant>> _items = [];
    public int Count => _items.Values.Sum(items => items.Count);
    public IReadOnlyCollection<SharedTransitionParticipant> Participants => _items.Values.SelectMany(items => items).ToArray();

    public void Register(object key, Control control, SharedParticipantRole role = SharedParticipantRole.Active)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(control);
        var items = _items.GetValueOrDefault(key) ?? [];
        var existing = items.FirstOrDefault(item => ReferenceEquals(item.Control, control));
        if (existing is not null)
        {
            if (existing.Role == role)
                return;
        }
        if (items.Any(item => item.Role == role && !ReferenceEquals(item.Control, control)))
            throw new InvalidOperationException($"Duplicate shared transition role '{role}' for key: {key}");
        if (existing is not null)
            items.Remove(existing);
        items.Add(new SharedTransitionParticipant(key, control, role, LayoutSnapshot.Capture(control)));
        _items[key] = items;
    }

    public bool Unregister(object key, Control control)
        => _items.TryGetValue(key, out var items) && Remove(items, key, control);

    public bool TryGet(object key, out SharedTransitionParticipant participant)
    {
        if (_items.TryGetValue(key, out var items) && items.Count > 0) { participant = items[^1]; return true; }
        participant = null!;
        return false;
    }
    public bool TryPair(object key, out SharedTransitionParticipant? source, out SharedTransitionParticipant? destination)
    {
        source = destination = null;
        if (!_items.TryGetValue(key, out var items)) return false;
        source = items.FirstOrDefault(item => item.Role == SharedParticipantRole.Source);
        destination = items.FirstOrDefault(item => item.Role == SharedParticipantRole.Destination);
        return source is not null || destination is not null;
    }
    public void Clear() => _items.Clear();

    private bool Remove(List<SharedTransitionParticipant> items, object key, Control control)
    {
        var removed = items.RemoveAll(item => ReferenceEquals(item.Control, control)) > 0;
        if (items.Count == 0) _items.Remove(key);
        return removed;
    }
}
