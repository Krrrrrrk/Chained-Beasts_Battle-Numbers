using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Unity.Netcode;
using UnityEngine;

namespace ChainedBeasts.BattleNumbers;

internal static class NetworkNumbers
{
    private const string Channel = "local.chainedbeasts.battlenumbers/v1";
    private static NetworkManager _manager;
    private static CustomMessagingManager.HandleNamedMessageDelegate _handler;
    private static readonly NumberSubscriptions Subscribers = new();
    private static readonly NumberReceiptStream Stream = new();
    private static bool _oldHostWarning;
    private static string _session;
    private static double _subscribeAt;
    private static string Battle => NetworkSession.InstanceExists && NetworkSession.Instance.runDataManager is { } run
        ? run.runStartTime + ":" + run.RunSeed + ":" + run.CompletedLevels : null;
    internal static void Tick(NetworkManager manager)
    {
        if (!RuntimeState.Supported(manager)) { if (_manager is not null) Reset(); return; }
        if (_manager?.Pointer != manager.Pointer)
        {
            if (manager.CustomMessagingManager is null) return;
            Reset(); _manager = manager; _session = manager.IsServer ? System.Guid.NewGuid().ToString("N") : null;
            _handler = (Action<ulong, FastBufferReader>)Receive;
            manager.CustomMessagingManager.RegisterNamedMessageHandler(Channel, _handler);
        }
        if (!manager.IsServer && _session is null && Time.unscaledTimeAsDouble >= _subscribeAt)
        { Send(NetworkManager.ServerClientId, new() { Kind = "subscribe", AllPlayers = RuntimeState.Settings.OtherPlayers }); _subscribeAt = Time.unscaledTimeAsDouble + 1; }
    }
    private static void Send(ulong id, NumberPacket packet)
    {
        packet.Session = _session;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(packet);
        if (bytes.Length > 2048) return;
        var writer = new FastBufferWriter(bytes.Length, Unity.Collections.Allocator.Temp, bytes.Length);
        try { writer.WriteBytesSafe((Il2CppStructArray<byte>)bytes); _manager.CustomMessagingManager.SendNamedMessage(Channel, id, writer, NetworkDelivery.ReliableSequenced); }
        finally { writer.Dispose(); }
    }
    private static void Receive(ulong sender, FastBufferReader reader)
    {
        try
        {
            var count = reader.Length - reader.Position; if (count < 1 || count > 2048 || !reader.TryBeginRead(count)) return;
            var buffer = new Il2CppStructArray<byte>(count); reader.ReadBytesSafe(ref buffer, count);
            var packet = JsonSerializer.Deserialize<NumberPacket>(buffer.ToArray(), new JsonSerializerOptions { MaxDepth = 4 });
            if (packet is null || packet.Protocol != 1) return;
            if (_manager.IsServer)
            {
                if (packet.Kind == "subscribe" && Subscribers.Subscribe(sender, packet.AllPlayers, _manager.LocalClientId, ConnectedClients()))
                    Send(sender, new() { Kind = "accepted", AllPlayers = true });
                return;
            }
            if (sender != NetworkManager.ServerClientId) return;
            if (packet.Kind == "accepted")
            {
                if (Stream.AcceptHandshake(packet))
                {
                    _session = Stream.Session;
                    if (RuntimeState.Settings.OtherPlayers && !Stream.HostSupportsAllPlayers && !_oldHostWarning)
                    {
                        _oldHostWarning = true;
                        RuntimeState.Logger?.LogWarning("Host supports only owner-specific numbers. Update the host to Battle Numbers 0.3.0+ to see friends' damage.");
                    }
                }
                return;
            }
            if (!Stream.TryAccept(packet, Battle, _manager.LocalClientId, Time.unscaledTimeAsDouble, RuntimeState.Settings.OtherPlayers, out var receipt)) return;
            if (ReceiptRules.Visible(receipt, RuntimeState.Settings))
            { RuntimeState.Pending.Add(receipt); System.Threading.Interlocked.Increment(ref RuntimeState.Captured); }
        }
        catch (Exception e) { RuntimeState.Fail(e); }
    }
    internal static void Publish(DamageReceipt receipt, ReceiptCapture.Frame frame)
    {
        if (_manager?.IsServer != true) return;
        Subscribers.Retain(ConnectedClients());
        var packet = new NumberPacket { Kind = "number", Battle = Battle, Sequence = receipt.Sequence, Source = receipt.Source.NetworkId, Victim = receipt.Victim.NetworkId,
            SourceOwner = frame.SourceOwner, VictimOwner = frame.VictimOwner, SourcePlayer = frame.SourcePlayer, VictimPlayer = frame.VictimPlayer,
            SourceEnemy = receipt.Source.HostileNpc, VictimEnemy = receipt.Victim.HostileNpc,
            Loss = receipt.HpLost, X = receipt.Position.X, Y = receipt.Position.Y, Z = receipt.Position.Z };
        // Routing uses each viewer's preference, never the host's local display switches.
        foreach (var id in Subscribers.Recipients(packet)) Send(id, packet);
    }
    private static HashSet<ulong> ConnectedClients()
    {
        var clients = _manager.ConnectedClientsIds;
        var count = clients.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<ulong>>().Count;
        var live = new HashSet<ulong>(); for (var i = 0; i < count; i++) live.Add(clients[i]);
        return live;
    }
    internal static void Reset()
    {
        try { _manager?.CustomMessagingManager?.UnregisterNamedMessageHandler(Channel); } catch { }
        _manager = null; _handler = null; Subscribers.Clear(); Stream.Reset(); _session = null; _subscribeAt = 0; _oldHostWarning = false;
    }
}
