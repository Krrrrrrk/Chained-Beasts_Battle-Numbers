using System;

namespace ChainedBeasts.BattleNumbers;

public static class NumberTypography
{
    // Game 1.0.2 resources.assets: TMP_FontAsset 34255, used by native HealthValue labels.
    public const string HudFontName = "Skranji-Regular SDF";
    public const int Capacity = 96;
    public const float FontSize = 56;
    public static bool IsHudFont(string name) => string.Equals(name, HudFontName, StringComparison.Ordinal);
    public static float ScreenScale(int screenHeight, float configuredScale) =>
        Math.Clamp(screenHeight / 1080f, .5f, 2.5f) * configuredScale;
}
