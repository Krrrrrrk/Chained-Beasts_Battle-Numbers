# Chained Beasts: Battle Numbers

I wanted floating damage numbers in **Chained Beasts**, so I made some.

Hits on enemies show up in pale yellow, and damage taken by players shows up in red. The numbers pop up, float a little, then fade away. Nothing fancy, just a quick way to see how hard something actually hit.

By default, it uses the game's **Skranji HUD font**, with a white inner outline and black outer outline. If the font isn't available, it falls back to the original pixel-style digits.

**[Download Battle Numbers v1.0.0](https://github.com/Krrrrrrk/Chained-Beasts_Battle-Numbers/releases/tag/v1.0.0)**

Made for **Chained Beasts 1.0.2 (Steam build 26231777)**.

## Screenshots

These are from an earlier version with the pixel-style numbers. The current version uses the game's HUD font by default.

**Dealing damage** - pale yellow numbers above enemies.

![Pale yellow pixel damage number above an enemy](assets/screenshots/outgoing-damage.png)

**Taking damage** - red numbers above your character.

![Red pixel damage number above the player](assets/screenshots/incoming-damage.png)

## Installation

This mod runs on its own. You **don't** need Mod Foundation or Terms & Contusions.

1. Install [BepInEx 6 (IL2CPP, Windows x64, build 791)](https://builds.bepinex.dev/projects/bepinex_be/791/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791%2Bb782da4.zip) if you haven't already. Extract it into the game folder, alongside `ChainedBeasts.exe`, and launch the game once to let BepInEx set itself up.
2. Download [ChainedBeasts.BattleNumbers.dll](https://github.com/Krrrrrrk/Chained-Beasts_Battle-Numbers/releases/download/v1.0.0/ChainedBeasts.BattleNumbers.dll).
3. Close the game and put the DLL in `BepInEx/plugins/BattleNumbers/`. Create the folder if it doesn't exist, and make sure you don't have a second copy of the DLL somewhere else in `plugins`.
4. Start the game and play!

Use **v1.0.0 for game version 1.0.2**. Releases up through v0.2.0 were made for game version 1.0.1.

The mod checks the game's GameAssembly build and disables itself if it doesn't recognize it. That way a game update doesn't leave it hooking into something it shouldn't.

## Multiplayer

Yep, you can see your friends' damage too.

With `ShowOtherPlayers = true` (the default), the host checks how much HP a hit actually removed and sends cosmetic number updates to players running the mod. So you'll see both damage your friends deal and damage they take, as long as it's in your camera view.

For the full multiplayer display, install **v1.0.0 on the host and on each PC that wants to see everyone's damage**. Your friends don't all need Battle Numbers just to have *their* hits show up on your screen, but the host does need it. Other gameplay mods may have their own matching-install requirements.

Older versions have more limited compatibility:

- A viewer on v1.0.0 with a host older than v0.3.0 gets a warning and only sees their own hits.
- Older viewers connecting to a newer host also keep their original own-hits view.
- A host's personal display preferences don't limit what upgraded viewers can choose to show.

Want just your own hits? Change `ShowOtherPlayers = false` in the config and restart.

Friendly/PvP damage isn't displayed, and numbers outside the camera view are hidden. Multiplayer has worked well in my testing so far, but I haven't covered every possible edge case.

## Settings

After the first launch, you'll find the config here:

`BepInEx/config/local.chainedbeasts.battlenumbers.cfg`

Close the game before editing it, then restart to apply changes.

```ini
[Display]
Enabled = true
ShowOutgoing = true
ShowIncoming = true
ShowOtherPlayers = true
UseGameFont = true
TextScale = 1
Lifetime = 1
OutgoingColor = #FFF0A0
IncomingColor = #FF5555
```

Most of these are pretty self-explanatory:

- `ShowOutgoing` / `ShowIncoming`: choose which direction of damage to display.
- `ShowOtherPlayers`: show friends' damage as well as your own.
- `UseGameFont`: use the game's HUD font; turn it off for the older pixel digits. The log will tell you if the game font couldn't be loaded.
- `TextScale`: change number size, from `0.5` to `2.5`.
- `Lifetime`: how long numbers last, from `0.25` to `3` seconds.
- `OutgoingColor` / `IncomingColor`: colors in `#RRGGBB` format.
- `Enabled`: turn the mod off without removing the DLL.

There's optional diagnostic logging too, but it's off by default.

## Known issues

Two things I know about, and at this point don't plan to fix:

- Some boss hits don't show numbers.
- A number can appear when another player attacks a hallucination.

A few other limitations worth mentioning:

- Friendly/PvP, environmental, self-inflicted, internal, and unattributed damage aren't shown. Some status or area effects also don't have a clear attacker to associate with the hit.
- The display holds up to **96 numbers at once**. If things get *really* hectic, the oldest numbers get replaced.
- Battle Numbers is cosmetic. It doesn't change actual damage, HP, combat rules, saves, or who controls the game state over the network.

## Having problems?

If something breaks, please send me your `Player.log` from the session where it happened:

```text
%USERPROFILE%\AppData\LocalLow\Featherweight Games\ChainedBeasts\Player.log
```

For multiplayer bugs, logs from **both the host and the client** are especially useful. Let me know which other mods you had installed and what was happening at the time.

## Building from source

If you'd rather build it yourself, you'll need Windows, a .NET SDK that supports **net6.0**, BepInEx installed for the game, and its generated `BepInEx/interop` assemblies.

```powershell
dotnet build src/ChainedBeasts.BattleNumbers/ChainedBeasts.BattleNumbers.csproj -c Release
```

The DLL ends up in `src/ChainedBeasts.BattleNumbers/bin/Release/net6.0/`.

If your game is installed somewhere unusual, set `CHAINED_BEASTS_GAME_DIR` before building. Game files, BepInEx dependencies, and generated interop assemblies aren't included in the repo.

For reference, this was built against **Chained Beasts 1.0.2**, **Unity 6000.3.21f1**, and **BepInEx 6.0.0-be.791+b782da4**.

## Uninstalling

Close the game and delete `BepInEx/plugins/BattleNumbers/ChainedBeasts.BattleNumbers.dll`. You can also remove `BepInEx/config/local.chainedbeasts.battlenumbers.cfg` if you don't want to keep your settings.

No save restoration needed. You can leave BepInEx and any other mods alone.

## License

[MIT](LICENSE). Some BepInEx/IL2CPP hook and component patterns were adapted from my [Chained Beasts Mod Foundation](https://github.com/Krrrrrrk/chained-beasts-mod-foundation) project (also MIT, copyright © 2026 Krrrrrrk).

The fallback pixel digits are my own. No game fonts or other game assets are included. This is an unofficial mod and isn't affiliated with the game developers.
