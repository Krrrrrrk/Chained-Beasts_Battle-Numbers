using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace ChainedBeasts.BattleNumbers;

internal sealed class PixelNumberRenderer : IDisposable
{
    // Two colors x ten digits. One small native draw per digit, no per-hit textures.
    private readonly Texture2D[][] _glyphs = { new Texture2D[10], new Texture2D[10] };
    internal PixelNumberRenderer(DisplaySettings settings)
    {
        try
        {
            for (var direction = 0; direction < 2; direction++)
            for (var digit = 0; digit < 10; digit++)
            {
                var texture = new Texture2D(PixelDigits.Width, PixelDigits.Height, TextureFormat.RGBA32, false);
                _glyphs[direction][digit] = texture; // Own it before any further native call can fail.
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                var mask = PixelDigits.Mask(digit);
                var pixels = new Il2CppStructArray<Color32>(mask.Length);
                var tint = direction == 0 ? settings.OutgoingColor : settings.IncomingColor;
                for (var y = 0; y < PixelDigits.Height; y++)
                for (var x = 0; x < PixelDigits.Width; x++)
                {
                    // Unity texture rows begin at the bottom; font masks begin at the top.
                    var layer = mask[(PixelDigits.Height - 1 - y) * PixelDigits.Width + x];
                    var color = layer == 3 ? new Color32 { r = (byte)Math.Round(tint.R * 255), g = (byte)Math.Round(tint.G * 255), b = (byte)Math.Round(tint.B * 255), a = 255 }
                        : layer == 2 ? new Color32 { r = 255, g = 255, b = 255, a = 255 }
                        : layer == 1 ? new Color32 { a = 255 } : default;
                    pixels[y * PixelDigits.Width + x] = color;
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
            }
        }
        catch { Dispose(); throw; }
    }
    internal bool IsValid
    {
        get
        {
            foreach (var direction in _glyphs)
            foreach (var texture in direction) if (texture == null) return false;
            return true;
        }
    }
    internal void Draw(string text, float x, float y, float scale, PopupFrame frame, Relationship relationship)
    {
        var unit = PixelDigits.Unit(scale, frame.Pop);
        var left = (float)Math.Round(x - PixelDigits.TextWidth(text.Length, unit) / 2f);
        var top = (float)Math.Round(Screen.height - y - PixelDigits.Height * unit / 2f);
        var glyphs = _glyphs[relationship == Relationship.LocalToHostile ? 0 : 1];
        // White tint preserves both baked outline colors and the configured face color.
        GUI.color = new Color(1, 1, 1, frame.Alpha);
        for (var i = 0; i < text.Length; i++)
        {
            var digit = text[i] - '0';
            if (digit < 0 || digit > 9) continue;
            GUI.DrawTexture(new Rect(left + i * PixelDigits.Advance * unit, top, PixelDigits.Width * unit, PixelDigits.Height * unit), glyphs[digit]);
        }
    }
    public void Dispose()
    {
        foreach (var direction in _glyphs)
        for (var i = 0; i < direction.Length; i++)
        {
            if (direction[i] != null) UnityEngine.Object.Destroy(direction[i]);
            direction[i] = null;
        }
    }
}
