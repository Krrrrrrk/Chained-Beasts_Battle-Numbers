using System;

namespace ChainedBeasts.BattleNumbers;

// Original 5x7 digit designs. Two texels per source pixel leave room for thin,
// distinct white/black outlines without importing a game font or another asset.
public static class PixelDigits
{
    public const int Width = 16, Height = 20, Advance = 14;
    public const int DefaultUnit = 3; // 14 fill texels * 3 = 42px at 1080p, up from 28px text.
    private static readonly string[] Digits =
    {
        "01110/10001/10011/10101/11001/10001/01110",
        "00100/01100/00100/00100/00100/00100/01110",
        "01110/10001/00001/00010/00100/01000/11111",
        "11110/00001/00001/01110/00001/00001/11110",
        "00010/00110/01010/10010/11111/00010/00010",
        "11111/10000/10000/11110/00001/00001/11110",
        "01110/10000/10000/11110/10001/10001/01110",
        "11111/00001/00010/00100/01000/01000/01000",
        "01110/10001/10001/01110/10001/10001/01110",
        "01110/10001/10001/01111/00001/00001/01110"
    };
    // Top-to-bottom rows: 0 transparent, 1 black outer, 2 white inner, 3 colored face.
    public static byte[] Mask(int digit)
    {
        if (digit < 0 || digit > 9) throw new ArgumentOutOfRangeException(nameof(digit));
        var mask = new byte[Width * Height];
        var rows = Digits[digit].Split('/');
        bool Face(int x, int y) => x >= 3 && x < 13 && y >= 3 && y < 17 && rows[(y - 3) / 2][(x - 3) / 2] == '1';
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            if (Face(x, y)) { mask[y * Width + x] = 3; continue; }
            var nearest = 3;
            for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++)
                if (Face(x + dx, y + dy)) nearest = Math.Min(nearest, Math.Max(Math.Abs(dx), Math.Abs(dy)));
            mask[y * Width + x] = nearest == 1 ? (byte)2 : nearest == 2 ? (byte)1 : (byte)0;
        }
        return mask;
    }
    public static int Unit(float screenScale, float pop) =>
        Math.Max(1, (int)Math.Round(DefaultUnit * screenScale * pop, MidpointRounding.AwayFromZero));
    public static int TextWidth(int digits, int unit) => digits <= 0 ? 0 : ((digits - 1) * Advance + Width) * unit;
}
