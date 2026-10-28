using System;
using System.Collections.Generic;
using System.Linq;

namespace ChainedBeasts.BattleNumbers;

// Pure routing/receive state used by the native adapter and nongame checks.
public sealed class NumberSubscriptions
{
    private readonly Dictionary<ulong, bool> _peers = new();
    private readonly int _capacity;
    public NumberSubscriptions(int capacity = 4)
    { if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity)); _capacity = capacity; }
    public int Count => _peers.Count;
    public bool Subscribe(ulong peer, bool otherPlayers, ulong local, IEnumerable<ulong> connected)
    {
        Retain(connected);
        if (peer == local || !connected.Contains(peer) || !_peers.ContainsKey(peer) && _peers.Count >= _capacity) return false;
        _peers[peer] = otherPlayers; return true;
    }
    public IEnumerable<ulong> Recipients(NumberPacket packet)
    {
        foreach (var peer in _peers)
            if (peer.Value || packet.SourcePlayer && packet.SourceOwner == peer.Key || packet.VictimPlayer && packet.VictimOwner == peer.Key)
                yield return peer.Key;
    }
    public void Retain(IEnumerable<ulong> connected)
    { foreach (var id in _peers.Keys.Where(id => !connected.Contains(id)).ToArray()) _peers.Remove(id); }
    public void Clear() => _peers.Clear();
}

public sealed class NumberReceiptStream
{
    public string Session { get; private set; }
    public bool HostSupportsAllPlayers { get; private set; }
    public long LastSequence { get; private set; }
    public bool AcceptHandshake(NumberPacket packet)
    {
        if (packet is null || packet.Protocol != 1 || packet.Kind != "accepted" || !Guid.TryParseExact(packet.Session, "N", out _)) return false;
        if (Session != packet.Session) LastSequence = 0;
        Session = packet.Session; HostSupportsAllPlayers = packet.AllPlayers; return true;
    }
    public bool TryAccept(NumberPacket packet, string battle, ulong local, double now, bool showOtherPlayers, out DamageReceipt receipt)
    {
        receipt = default;
        if (packet is null || packet.Protocol != 1 || packet.Kind != "number" || Session is null || packet.Session != Session ||
            battle is null || packet.Battle != battle || packet.Sequence <= LastSequence || packet.Loss <= 0 || !double.IsFinite(now)) return false;
        var value = packet.ForClient(local, now, showOtherPlayers);
        if (!value.Position.IsFinite || packet.Source == packet.Victim || packet.SourcePlayer && packet.SourceEnemy || packet.VictimPlayer && packet.VictimEnemy) return false;
        // Sequence is an event identity, never an amount/victim/time key. Advance even for a locally hidden direction.
        LastSequence = packet.Sequence; receipt = value; return true;
    }
    public void Reset() { Session = null; LastSequence = 0; HostSupportsAllPlayers = false; }
}
