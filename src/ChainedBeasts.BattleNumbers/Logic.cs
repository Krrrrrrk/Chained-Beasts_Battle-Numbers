using System;
using System.Globalization;

namespace ChainedBeasts.BattleNumbers;

public enum Relationship { Excluded, LocalToHostile, HostileToLocal }
public readonly record struct Actor(ulong NetworkId, bool LocalPlayer, bool HostileNpc);
public readonly record struct WorldPoint(float X, float Y, float Z)
{
    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z);
}
// Deliberately contains no Unity object or victim reference. Valid after death/despawn.
public readonly record struct DamageReceipt(long Sequence, Actor Source, Actor Victim, Relationship Relationship,
    int HpLost, WorldPoint Position, double CapturedAt);
public readonly record struct Tint(float R, float G, float B);
public readonly record struct DisplaySettings(bool Enabled, bool Outgoing, bool Incoming, float Scale, float Lifetime,
    Tint OutgoingColor, Tint IncomingColor, bool Diagnostics)
{
    public static DisplaySettings Create(bool enabled, bool outgoing, bool incoming, float scale, float lifetime,
        string outgoingColor, string incomingColor, bool diagnostics) => new(enabled, outgoing, incoming,
        Clamp(scale, .5f, 2.5f, 1), Clamp(lifetime, .25f, 3, 1),
        Parse(outgoingColor, new Tint(1, 240f / 255, 160f / 255)), Parse(incomingColor, new Tint(1, 85f / 255, 85f / 255)), diagnostics);
    private static float Clamp(float n, float min, float max, float fallback) => float.IsFinite(n) ? Math.Clamp(n, min, max) : fallback;
    public static Tint Parse(string value, Tint fallback)
    {
        if (value is null || value.Length != 7 || value[0] != '#' ||
            !uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb)) return fallback;
        return new Tint((rgb >> 16) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    }
}
public static class ReceiptRules
{
    public static Relationship Classify(Actor source, Actor victim) => source.NetworkId == victim.NetworkId ? Relationship.Excluded :
        source.LocalPlayer && victim.HostileNpc ? Relationship.LocalToHostile :
        source.HostileNpc && victim.LocalPlayer ? Relationship.HostileToLocal : Relationship.Excluded;
    public static int PositiveLoss(int oldHealth, int newHealth) => (int)Math.Clamp((long)Math.Max(0, oldHealth) - Math.Max(0, newHealth), 0, int.MaxValue);
    public static bool Visible(DamageReceipt r, DisplaySettings s) => s.Enabled && r.HpLost > 0 && r.Sequence > 0 && r.Position.IsFinite &&
        double.IsFinite(r.CapturedAt) && (r.Relationship == Relationship.LocalToHostile && s.Outgoing || r.Relationship == Relationship.HostileToLocal && s.Incoming);
}
// One instance per executed RPC, never a time/amount deduplication key.
public sealed class CommitLatch
{
    public bool Consumed { get; private set; }
    public bool TryCommit(int before, int after)
    {
        if (Consumed || ReceiptRules.PositiveLoss(before, after) == 0) return false;
        Consumed = true; return true;
    }
}
// The production RPC/callback bridge is testable without loading Unity or calling combat handlers.
public sealed class ReceiptCapture
{
    public sealed class Frame
    {
        internal Frame Parent;
        public long VictimHandle;
        public Actor Source, Victim;
        public WorldPoint Position;
        public bool Eligible;
        public ulong SourceOwner, VictimOwner;
        public bool SourcePlayer, VictimPlayer;
        internal readonly CommitLatch Latch = new();
    }
    private Frame _current;
    public Frame ActiveFrame => _current;
    private long _sequence;
    public Frame Enter() { var frame = new Frame { Parent = _current }; _current = frame; return frame; }
    public void Exit(Frame frame) { if (frame is not null) _current = frame.Parent; }
    public bool Commit(long victimHandle, int before, int after, double time, out DamageReceipt receipt)
    {
        receipt = default;
        var frame = _current;
        if (frame is null || !frame.Eligible || frame.VictimHandle != victimHandle || !frame.Latch.TryCommit(before, after)) return false;
        receipt = new DamageReceipt(++_sequence, frame.Source, frame.Victim, ReceiptRules.Classify(frame.Source, frame.Victim),
            ReceiptRules.PositiveLoss(before, after), frame.Position, time);
        return true;
    }
}
public sealed class ReceiptBuffer
{
    private readonly DamageReceipt[] _items;
    private int _head, _count;
    public int Count { get { lock (_items) return _count; } }
    public ReceiptBuffer(int capacity) { if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity)); _items = new DamageReceipt[capacity]; }
    public void Add(DamageReceipt r)
    {
        lock (_items)
        {
            if (_count == _items.Length) { _head = (_head + 1) % _items.Length; _count--; }
            _items[(_head + _count++) % _items.Length] = r;
        }
    }
    public bool Take(out DamageReceipt r)
    {
        lock (_items)
        {
            if (_count == 0) { r = default; return false; }
            r = _items[_head]; _items[_head] = default; _head = (_head + 1) % _items.Length; _count--; return true;
        }
    }
    public void Clear() { lock (_items) { Array.Clear(_items, 0, _items.Length); _head = _count = 0; } }
}
public readonly record struct PopupFrame(float Alpha, float Pop, float Rise, float X, float Y, bool Alive);
public static class PopupMath
{
    public static PopupFrame Frame(double now, double started, float lifetime, long sequence)
    {
        var age = now - started;
        if (!double.IsFinite(age) || !float.IsFinite(lifetime) || lifetime <= 0 || age < 0 || age >= lifetime) return default;
        var t = (float)(age / lifetime);
        var fade = Math.Clamp((1 - t) / .55f, 0, 1);
        var alpha = fade * fade * (3 - 2 * fade);
        var pop = 1 + .22f * Math.Max(0, 1 - t / .18f);
        // Stable lanes preserve distinct close hits and multi-target events without random state.
        var lane = (int)((ulong)sequence % 7);
        return new PopupFrame(alpha, pop, 42 * t, (lane - 3) * 13, (lane % 3) * 12, true);
    }
    public static bool InViewport(float x, float y, float z, float left, float bottom, float width, float height) =>
        float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(z) && z > 0 && width > 0 && height > 0 &&
        x >= left && x <= left + width && y >= bottom && y <= bottom + height;
}
