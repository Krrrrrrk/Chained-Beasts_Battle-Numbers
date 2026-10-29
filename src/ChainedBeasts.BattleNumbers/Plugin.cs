using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace ChainedBeasts.BattleNumbers;

[BepInPlugin(Guid, Name, Version)]
public sealed class Plugin : BasePlugin
{
    public const string Guid = "local.chainedbeasts.battlenumbers";
    public const string Name = "Chained Beasts Battle Numbers";
    public const string Version = "1.0.0";
    private Harmony _harmony;
    private NumbersController _controller;
    public override void Load()
    {
        RuntimeState.Logger = Log;
        RuntimeState.UseGameFont = Config.Bind("Display", "UseGameFont", true,
            "Reuse the game's loaded Skranji HUD font. Pixel digits are used while unavailable. Restart to apply.").Value;
        RuntimeState.Settings = DisplaySettings.Create(
            Config.Bind("Display", "Enabled", true, "Enable Battle Numbers on next launch.").Value,
            Config.Bind("Display", "ShowOutgoing", true, "Player damage to hostile NPCs; includes friends when ShowOtherPlayers is enabled.").Value,
            Config.Bind("Display", "ShowIncoming", true, "Hostile NPC damage to players; includes friends when ShowOtherPlayers is enabled.").Value,
            Config.Bind("Display", "TextScale", 1f, "Text scale: 0.5 to 2.5. Nonfinite values use 1.").Value,
            Config.Bind("Display", "Lifetime", 1f, "Seconds: 0.25 to 3. Nonfinite values use 1.").Value,
            Config.Bind("Display", "OutgoingColor", "#FFF0A0", "Six-digit #RRGGBB; invalid values use pale yellow.").Value,
            Config.Bind("Display", "IncomingColor", "#FF5555", "Six-digit #RRGGBB; invalid values use red.").Value,
            Config.Bind("Diagnostics", "Enabled", false, "Optional bounded aggregate diagnostics, every ten seconds.").Value,
            Config.Bind("Display", "ShowOtherPlayers", true, "Also show friends dealing/receiving damage. Requires Battle Numbers 0.3.0+ on the host and this viewer. Restart to apply.").Value);
        Log.LogInfo($"Battle Numbers {Version}; Enabled={RuntimeState.Settings.Enabled}; server-confirmed HP receipts; ShowOtherPlayers={RuntimeState.Settings.OtherPlayers} on host and clients");
        if (!RuntimeState.Settings.Enabled) return;
        _harmony = new Harmony(Guid + ".observer");
        try
        {
            NativeSafety.VerifyFingerprint();
            DamageObserver.Install(_harmony);
            _controller = AddComponent<NumbersController>();
            Log.LogInfo($"Observer installed; awaiting first-frame patch audit. UseGameFont={RuntimeState.UseGameFont}; appearance requires manual verification.");
        }
        catch (Exception e)
        {
            RuntimeState.Fail(e);
            _harmony.UnpatchSelf();
        }
    }
    public override bool Unload()
    {
        RuntimeState.Ready = false;
        RuntimeState.Pending.Clear();
        _harmony?.UnpatchSelf();
        if (_controller != null) UnityEngine.Object.Destroy(_controller);
        return true;
    }
}
