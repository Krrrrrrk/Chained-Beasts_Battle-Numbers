using System;
using System.Threading;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ChainedBeasts.BattleNumbers;

internal static class RuntimeState
{
    internal static DisplaySettings Settings;
    internal static ManualLogSource Logger;
    internal static bool Ready, Failed, UseGameFont;
    internal static readonly ReceiptBuffer Pending = new(96);
    internal static long Captured;
    internal static bool Supported(NetworkManager manager) => manager != null && manager.IsListening && (manager.IsServer || manager.IsConnectedClient) &&
        GameManager.InstanceExists && GameManager.Instance.CurrentGameMode is GameMode.Solo or GameMode.Local or GameMode.Online;
    internal static void Fail(Exception e)
    {
        Ready = false; Pending.Clear();
        if (!Failed) Logger?.LogError("Cosmetic observer disabled: " + e.GetType().Name + ": " + e.Message);
        Failed = true;
    }
}
internal static class DamageObserver
{
    internal const string FoundationOwner = "local.chainedbeasts.modfoundation.runtime";
    [ThreadStatic] private static ReceiptCapture _capture;
    internal static void Install(Harmony harmony)
    {
        NativeSafety.Install(harmony, "TakeDamageRpc", new[] { typeof(int), typeof(ulong), typeof(bool), typeof(DamageType),
            typeof(bool), typeof(bool), typeof(bool), typeof(RpcParams) }, 0x6df900, nameof(RpcBefore), nameof(RpcFinally));
        NativeSafety.Install(harmony, "OnHealthChanged", new[] { typeof(int), typeof(int) }, 0x6df310, nameof(HealthCommitted));
    }
    private static void RpcBefore(NetworkHealthObject __instance, ulong damageSourceID, bool isInternalDamageSource, out ReceiptCapture.Frame __state)
    {
        // Mask even a dispatch nested inside another execution; finalizer always restores it.
        _capture ??= new ReceiptCapture();
        __state = _capture.Enter();
        if (!RuntimeState.Ready) return;
        try
        {
            if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute ||
                isInternalDamageSource || !RuntimeState.Supported(__instance.NetworkManager) || !__instance.IsServer) return;
            var victim = __instance.TryCast<NetworkAgent>();
            var objects = __instance.NetworkManager.SpawnManager?.SpawnedObjects;
            if (victim == null || objects is null || !objects.ContainsKey(damageSourceID)) return;
            var source = objects[damageSourceID]?.GetComponent<NetworkAgent>();
            if (source == null) return;
            __state.Source = Identify(source);
            __state.Victim = Identify(victim);
            var attacker = source.TryCast<Player>(); var defender = victim.TryCast<Player>();
            __state.SourcePlayer = attacker is not null; __state.VictimPlayer = defender is not null;
            __state.SourceOwner = attacker?.OwnerClientId ?? ulong.MaxValue; __state.VictimOwner = defender?.OwnerClientId ?? ulong.MaxValue;
            if (!(attacker is not null && __state.Victim.HostileNpc || defender is not null && __state.Source.HostileNpc)) return;
            var p = victim.agentCenter + Vector3.up * .55f;
            __state.Position = new WorldPoint(p.x, p.y, p.z);
            __state.VictimHandle = __instance.Pointer.ToInt64();
            __state.Eligible = __state.Position.IsFinite;
        }
        catch (Exception e) { RuntimeState.Fail(e); }
    }
    private static Actor Identify(NetworkAgent agent)
    {
        var player = agent.TryCast<Player>();
        var local = player != null && player.IsOwner && player.OwnerClientId == player.NetworkManager.LocalClientId;
        return new Actor(agent.NetworkObjectId, local, agent.TryCast<Enemy>() != null && !agent.IsPlayerTeam);
    }
    private static void HealthCommitted(NetworkHealthObject __instance, int oldHealth, int newHealth)
    {
        if (!RuntimeState.Ready) return;
        try
        {
            // The native NetworkVariable has committed. These are old/new HP, NOT attempted damage.
            // Capture before base health listeners and before Foundation ReceiptAfter.
            if (_capture is null || !_capture.Commit(__instance.Pointer.ToInt64(), oldHealth, newHealth, Time.unscaledTimeAsDouble, out var receipt)) return;
            NetworkNumbers.Publish(receipt, _capture.ActiveFrame);
            if (ReceiptRules.Visible(receipt, RuntimeState.Settings))
            { RuntimeState.Pending.Add(receipt); Interlocked.Increment(ref RuntimeState.Captured); }
        }
        catch (Exception e) { RuntimeState.Fail(e); }
    }
    private static Exception RpcFinally(Exception __exception, ReceiptCapture.Frame __state)
    {
        _capture?.Exit(__state);
        return __exception; // Never swallow or replace a game/foreign patch exception.
    }
}
