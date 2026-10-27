using System;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;

namespace ChainedBeasts.BattleNumbers;

internal sealed class GameFontRenderer : IDisposable
{
    private readonly TextMeshProUGUI[] _labels = new TextMeshProUGUI[NumberTypography.Capacity];
    private readonly DisplaySettings _settings;
    private readonly TMP_FontAsset _font; // Borrowed. Never modify/destroy the game's font or material.
    private GameObject _root;
    private Material _material;
    internal bool IsValid => _root != null && _font != null && _material != null;

    internal static GameFontRenderer TryCreate(DisplaySettings settings, Transform parent)
    {
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var font in fonts)
        {
            if (font == null || !NumberTypography.IsHudFont(font.name) || font.material == null) continue;
            var digits = true;
            for (var digit = '0'; digit <= '9'; digit++)
                if (!font.HasCharacter(digit, false, false)) { digits = false; break; }
            if (digits) return new GameFontRenderer(settings, parent, font);
        }
        return null;
    }

    private GameFontRenderer(DisplaySettings settings, Transform parent, TMP_FontAsset font)
    {
        _settings = settings; _font = font;
        try
        {
            _root = new GameObject("Battle Numbers (HUD font)", Il2CppType.Of<RectTransform>());
            _root.hideFlags = HideFlags.DontSave;
            _root.transform.SetParent(parent, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29900;
            canvas.targetDisplay = 0;
            // No GraphicRaycaster or input components: popups cannot intercept clicks.
            _material = new Material(font.material);
            _material.hideFlags = HideFlags.DontSave;
            _material.name = "Battle Numbers HUD font (private)";
            SetColor("_FaceColor", Color.white);
            SetColor("_OutlineColor", Color.white);
            SetFloat("_OutlineWidth", .12f);
            SetFloat("_OutlineSoftness", 0);
            if (_material.HasProperty("_UnderlayColor"))
            {
                _material.DisableKeyword("UNDERLAY_INNER");
                _material.EnableKeyword("UNDERLAY_ON");
                SetColor("_UnderlayColor", Color.black);
                SetFloat("_UnderlayOffsetX", 0); SetFloat("_UnderlayOffsetY", 0);
                SetFloat("_UnderlayDilate", .55f); SetFloat("_UnderlaySoftness", 0);
            }
        }
        catch { Dispose(); throw; }
    }
    private void SetColor(string property, Color value) { if (_material.HasProperty(property)) _material.SetColor(property, value); }
    private void SetFloat(string property, float value) { if (_material.HasProperty(property)) _material.SetFloat(property, value); }

    internal void Draw(int slot, string text, float x, float y, float scale, PopupFrame frame, Relationship relationship)
    {
        var label = _labels[slot];
        if (label == null)
        {
            var go = new GameObject("Damage " + slot, Il2CppType.Of<RectTransform>());
            go.transform.SetParent(_root.transform, false); // Root owns cleanup even if setup fails.
            label = go.AddComponent<TextMeshProUGUI>();
            _labels[slot] = label;
            label.raycastTarget = false;
            label.font = _font;
            label.fontSharedMaterial = _material;
            label.fontSize = NumberTypography.FontSize;
            label.enableAutoSizing = false;
            label.fontStyle = FontStyles.Normal;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.richText = false;
            label.extraPadding = true;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(1000, 140);
        }
        if (label.text != text) label.text = text;
        var tint = relationship == Relationship.LocalToHostile ? _settings.OutgoingColor : _settings.IncomingColor;
        // TMP alpha fades face, outline and underlay together.
        label.color = new Color(tint.R, tint.G, tint.B, frame.Alpha);
        label.rectTransform.anchoredPosition = new Vector2(x, y);
        var size = scale * frame.Pop;
        label.rectTransform.localScale = new Vector3(size, size, 1);
        if (!label.enabled) label.enabled = true;
    }
    internal void Hide(int slot) { if (_labels[slot] != null && _labels[slot].enabled) _labels[slot].enabled = false; }
    internal void Clear() { for (var i = 0; i < _labels.Length; i++) Hide(i); }
    public void Dispose()
    {
        if (_root != null) UnityEngine.Object.Destroy(_root);
        if (_material != null) UnityEngine.Object.Destroy(_material);
        _root = null; _material = null;
        Array.Clear(_labels, 0, _labels.Length);
    }
}
