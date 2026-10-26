using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace ChainedBeasts.BattleNumbers;

public sealed class NumbersController : MonoBehaviour
{
    private readonly DamageReceipt[] _popups = new DamageReceipt[96];
    private readonly string[] _text = new string[96];
    private int _next;
    private bool _audited;
    private Camera _camera;
    private double _cameraCheck, _diagnosticAt;
    private IntPtr _session;
    private int _scene;
    private PixelNumberRenderer _renderer;
    public NumbersController(IntPtr pointer) : base(pointer) { }
    public void LateUpdate()
    {
        if (RuntimeState.Failed) return;
        try
        {
            if (!_audited) { NativeSafety.Audit(); _audited = true; RuntimeState.Ready = true; }
            var manager = NetworkManager.Singleton;
            NetworkNumbers.Tick(manager);
            var supported = RuntimeState.Supported(manager);
            var session = supported ? manager.Pointer : IntPtr.Zero;
            var scene = SceneManager.GetActiveScene().GetHashCode();
            if (_session != session || _scene != scene)
            { Clear(); _session = session; _scene = scene; }
            if (!supported) { RuntimeState.Pending.Clear(); return; }
            var now = Time.unscaledTimeAsDouble;
            if (_camera == null || now >= _cameraCheck) { _camera = Camera.main; _cameraCheck = now + .2; }
            while (RuntimeState.Pending.Take(out var receipt))
            {
                if (!PopupMath.Frame(now, receipt.CapturedAt, RuntimeState.Settings.Lifetime, receipt.Sequence).Alive) continue;
                _popups[_next] = receipt;
                _text[_next] = receipt.HpLost.ToString(CultureInfo.InvariantCulture);
                _next = (_next + 1) % _popups.Length;
            }
            for (var i = 0; i < _popups.Length; i++)
                if (_text[i] is not null && !PopupMath.Frame(now, _popups[i].CapturedAt, RuntimeState.Settings.Lifetime, _popups[i].Sequence).Alive)
                { _text[i] = null; _popups[i] = default; }
            if (RuntimeState.Settings.Diagnostics && now >= _diagnosticAt)
            { RuntimeState.Logger.LogInfo("Confirmed visible receipts captured=" + RuntimeState.Captured + "; bounded capacity=96"); _diagnosticAt = now + 10; }
        }
        catch (Exception e) { RuntimeState.Fail(e); Clear(); }
    }
    public void OnGUI()
    {
        if (!RuntimeState.Ready || RuntimeState.Failed || _session == IntPtr.Zero || _camera == null ||
            Event.current.type != EventType.Repaint) return;
        var oldColor = GUI.color;
        var oldDepth = GUI.depth;
        try
        {
            if (!_camera.enabled || !_camera.gameObject.activeInHierarchy || _camera.targetDisplay != 0) return;
            if (_renderer is null || !_renderer.IsValid)
            { _renderer?.Dispose(); _renderer = new PixelNumberRenderer(RuntimeState.Settings); }
            GUI.depth = -900;
            var now = Time.unscaledTimeAsDouble;
            var screenScale = Math.Clamp(Screen.height / 1080f, .5f, 2.5f) * RuntimeState.Settings.Scale;
            var view = _camera.pixelRect;
            for (var i = 0; i < _popups.Length; i++)
            {
                if (_text[i] is null) continue;
                var r = _popups[i];
                var frame = PopupMath.Frame(now, r.CapturedAt, RuntimeState.Settings.Lifetime, r.Sequence);
                if (!frame.Alive) continue;
                var p = _camera.WorldToScreenPoint(new Vector3(r.Position.X, r.Position.Y, r.Position.Z));
                if (!PopupMath.InViewport(p.x, p.y, p.z, view.x, view.y, view.width, view.height)) continue;
                var x = p.x + frame.X * screenScale;
                var y = p.y + (frame.Y + frame.Rise) * screenScale;
                if (!PopupMath.InViewport(x, y, p.z, view.x, view.y, view.width, view.height)) continue;
                _renderer.Draw(_text[i], x, y, screenScale, frame, r.Relationship);
            }
        }
        catch (Exception e) { RuntimeState.Fail(e); Clear(); }
        finally { GUI.color = oldColor; GUI.depth = oldDepth; }
    }
    private void Clear()
    {
        Array.Clear(_popups, 0, _popups.Length); Array.Clear(_text, 0, _text.Length);
        RuntimeState.Pending.Clear(); _next = 0; _camera = null;
    }
    public void OnDestroy() { RuntimeState.Ready = false; NetworkNumbers.Reset(); Clear(); _renderer?.Dispose(); _renderer = null; }
}
