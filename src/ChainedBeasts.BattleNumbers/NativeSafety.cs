using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BepInEx;
using HarmonyLib;
using Il2CppInterop.Common;

namespace ChainedBeasts.BattleNumbers;

internal static class NativeSafety
{
    internal const string GameHash = "ba3ef02fa87da4c41bb27d7610539c9578132204f0fb5713833e2ef93415ce29";
    private static readonly Dictionary<MethodBase, long> Selected = new();
    internal static void VerifyFingerprint()
    {
        using var input = File.OpenRead(Path.Combine(Paths.GameRootPath, "GameAssembly.dll"));
        using var algorithm = SHA256.Create();
        var actual = Convert.ToHexString(algorithm.ComputeHash(input)).ToLowerInvariant();
        if (actual != GameHash) throw new NotSupportedException("Uninspected GameAssembly build; reinspect before enabling native hooks. SHA256=" + actual);
    }
    internal static long Entry(MethodBase method)
    {
        var field = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method);
        if (field is null) return 0;
        var info = (IntPtr)field.GetValue(null);
        return info == IntPtr.Zero ? 0 : Marshal.ReadIntPtr(info).ToInt64();
    }
    internal static void Install(Harmony harmony, string name, Type[] signature, int expectedRva, string prefix, string finalizer = null)
    {
        var target = AccessTools.DeclaredMethod(typeof(NetworkHealthObject), name, signature)
            ?? throw new MissingMethodException(typeof(NetworkHealthObject).FullName, name);
        var entry = Entry(target);
        using var process = Process.GetCurrentProcess();
        var module = process.Modules.Cast<ProcessModule>().Single(m => string.Equals(m.ModuleName, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase));
        if (entry != module.BaseAddress.ToInt64() + expectedRva) throw new InvalidOperationException("Unexpected native entry: " + target);
        foreach (var old in Selected) if (old.Key != target && old.Value == entry) throw new InvalidOperationException("Aliased own hook: " + target);
        RejectForeignAliases(target, entry);
        var before = new HarmonyMethod(typeof(DamageObserver), prefix) { priority = Priority.First, before = new[] { DamageObserver.FoundationOwner } };
        var cleanup = finalizer is null ? null : new HarmonyMethod(typeof(DamageObserver), finalizer) { priority = Priority.Last };
        harmony.Patch(target, prefix: before, finalizer: cleanup);
        Selected[target] = entry;
        Report(target);
    }
    private static void RejectForeignAliases(MethodBase target, long entry)
    {
        foreach (var other in Harmony.GetAllPatchedMethods())
            if (other != target && Entry(other) == entry) throw new InvalidOperationException("Another patched wrapper aliases " + target + ": " + other);
    }
    internal static void Audit()
    {
        foreach (var selected in Selected)
        { RejectForeignAliases(selected.Key, selected.Value); Report(selected.Key); }
    }
    private static void Report(MethodBase target)
    {
        var info = Harmony.GetPatchInfo(target);
        var parts = info is null ? "none" : string.Join("; ", info.Prefixes.Select(p => $"prefix {p.owner}:{p.priority}")
            .Concat(info.Postfixes.Select(p => $"postfix {p.owner}:{p.priority}"))
            .Concat(info.Finalizers.Select(p => $"finalizer {p.owner}:{p.priority}")));
        RuntimeState.Logger?.LogInfo(target.DeclaringType.Name + "." + target.Name + " patches: " + parts);
    }
}
