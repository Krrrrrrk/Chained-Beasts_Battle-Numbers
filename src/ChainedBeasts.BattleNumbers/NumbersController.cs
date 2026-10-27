using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace ChainedBeasts.BattleNumbers;

public sealed class NumbersController : MonoBehaviour
{
    private readonly DamageReceipt[] _popups = new DamageReceipt[NumberTypography.Capacity];
    private readonly string[] _text = new string[NumberTypography.Capacity];
    private int _next;
    private bool _audited;
    private Camera _camera;
    private double _cameraCheck, _diagnosticAt;
    private IntPtr _session;
    private int _scene;
    private PixelNumberRenderer _renderer;
    private GameFontRenderer _gameRenderer;
    private double _fontCheck;
    private bool _fontWaitLogged, _fontErrorLogged;
    public NumbersController(IntPtr pointer) : base(pointer) { }
    public void LateUpdate()
    {
        if (RuntimeState.Failed) { _gameRenderer?.Clear(); return; }
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
            if (!supported) { RuntimeState.Pending.Clear(); _gameRenderer?.Clear(); return; }
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
            RenderGameFont(now);
        }
        catch (Exception e) { RuntimeState.Fail(e); Clear(); }
    }
    private bool CanProject => _camera != null && _camera.enabled && _camera.gameObject.activeInHierarchy && _camera.targetDisplay == 0;
    private void RenderGameFont(double now)
    {
        if (!RuntimeState.UseGameFont) return;
        try
        {
            if (_gameRenderer is not null && !_gameRenderer.IsValid) { _gameRenderer.Dispose(); _gameRenderer = null; }
            if (_gameRenderer is null && now >= _fontCheck)
            {
                _fontCheck = now + 2;
                _gameRenderer = GameFontRenderer.TryCreate(RuntimeState.Settings, transform);
                if (_gameRenderer is not null)
                    RuntimeState.Logger.LogInfo("Battle Numbers font: " + NumberTypography.HudFontName + " (borrowed from the game's loaded HUD assets)");
                else if (!_fontWaitLogged)
                {
                    _fontWaitLogged = true;
                    RuntimeState.Logger.LogInfo("HUD font is not loaded yet; using pixel digits and retrying every two seconds.");
                }
            }
            if (_gameRenderer is null) return;
            if (!CanProject) { _gameRenderer.Clear(); return; }
            var screenScale = NumberTypography.ScreenScale(Screen.height, RuntimeState.Settings.Scale);
            var view = _camera.pixelRect;
            for (var i = 0; i < _popups.Length; i++)
            {
                if (_text[i] is null) { _gameRenderer.Hide(i); continue; }
                var r = _popups[i];
                var frame = PopupMath.Frame(now, r.CapturedAt, RuntimeState.Settings.Lifetime, r.Sequence);
                if (!frame.Alive) { _gameRenderer.Hide(i); continue; }
                var p = _camera.WorldToScreenPoint(new Vector3(r.Position.X, r.Position.Y, r.Position.Z));
                if (!PopupMath.InViewport(p.x, p.y, p.z, view.x, view.y, view.width, view.height)) { _gameRenderer.Hide(i); continue; }
                var x = p.x + frame.X * screenScale;
                var y = p.y + (frame.Y + frame.Rise) * screenScale;
                if (!PopupMath.InViewport(x, y, p.z, view.x, view.y, view.width, view.height)) { _gameRenderer.Hide(i); continue; }
                _gameRenderer.Draw(i, _text[i], x, y, screenScale, frame, r.Relationship);
            }
        }
        catch (Exception e)
        {
            _gameRenderer?.Dispose(); _gameRenderer = null; _fontCheck = now + 10;
            if (!_fontErrorLogged)
            {
                _fontErrorLogged = true;
                RuntimeState.Logger.LogWarning("HUD font rendering unavailable; using pixel digits: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
    public void OnGUI()
    {
        if (!RuntimeState.Ready || RuntimeState.Failed || _session == IntPtr.Zero || _camera == null ||
            _gameRenderer?.IsValid == true || Event.current.type != EventType.Repaint) return;
        var oldColor = GUI.color;
        var oldDepth = GUI.depth;
        try
        {
            if (!CanProject) return;
            if (_renderer is null || !_renderer.IsValid)
            { _renderer?.Dispose(); _renderer = new PixelNumberRenderer(RuntimeState.Settings); }
            GUI.depth = -900;
            var now = Time.unscaledTimeAsDouble;
            var screenScale = NumberTypography.ScreenScale(Screen.height, RuntimeState.Settings.Scale);
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
        _gameRenderer?.Clear();
    }
    public void OnDestroy() { RuntimeState.Ready = false; NetworkNumbers.Reset(); Clear(); _renderer?.Dispose(); _renderer = null; _gameRenderer?.Dispose(); _gameRenderer = null; }
}
