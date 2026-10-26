# Battle Numbers

Floating damage numbers for **Chained Beasts**. I made this to show actual HP lost: pale yellow when I hurt an enemy, red when an enemy hurts me.

The numbers use larger pixel digits with a **white inner outline and black outer outline**, plus a short pop, rise and fade. I've tested version **0.1.1** in Solo play.

The current **0.2.0 source** adds experimental host/client support. The host
measures committed HP loss and sends cosmetic receipts to each player owner;
clients show their own outgoing/incoming hits. Both host and client need 0.2.0
for client numbers. This version is build checked but still awaits a manual
multiplayer test. [Download the experimental 0.2.0 DLL](https://github.com/Krrrrrrk/Chained-Beasts_Battle-Numbers/releases/download/v0.2.0/ChainedBeasts.BattleNumbers.dll) from its [prerelease](https://github.com/Krrrrrrk/Chained-Beasts_Battle-Numbers/releases/tag/v0.2.0). The stable-install instructions below remain 0.1.1.

## Screenshots

**Outgoing damage** — pale yellow numbers show HP lost by an enemy.

![Pale yellow pixel damage number above an enemy](assets/screenshots/outgoing-damage.png)

**Incoming damage** — red numbers show HP lost by my player.

![Red pixel damage number above the player](assets/screenshots/incoming-damage.png)

## Download and install

1. Install [BepInEx 6 build 791 — Windows x64 Unity IL2CPP](https://builds.bepinex.dev/projects/bepinex_be/791/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791%2Bb782da4.zip) in your Chained Beasts game folder if you don't already have it.
2. Download [ChainedBeasts.BattleNumbers.dll](https://github.com/Krrrrrrk/Chained-Beasts_Battle-Numbers/releases/download/v0.1.1/ChainedBeasts.BattleNumbers.dll) from the [latest release](https://github.com/Krrrrrrk/Chained-Beasts_Battle-Numbers/releases/latest).
3. With the game closed, put the DLL in `BepInEx/plugins/BattleNumbers/`. Create that folder if needed, and keep only one copy of the DLL.
4. Launch the game and start a Solo run.

This is standalone: Mod Foundation and Terms & Contusions are optional. It is cosmetic and does not change damage, health, combat, saves or network authority.

Tested with Chained Beasts 1.0.1 (Steam build 26041777), Unity 6000.3.21f1 and BepInEx 6.0.0-be.791+b782da4. The mod disables itself on an uninspected GameAssembly build.

## Settings

First launch creates `BepInEx/config/local.chainedbeasts.battlenumbers.cfg`. Close the game before editing; restart to apply.

```ini
[Display]
Enabled = true
ShowOutgoing = true
ShowIncoming = true
TextScale = 1
Lifetime = 1
OutgoingColor = #FFF0A0
IncomingColor = #FF5555
```

Use `TextScale` to resize the numbers (0.5–2.5), and `Lifetime` to change their duration in seconds (0.25–3). Colors use `#RRGGBB`. Set `Enabled = false` to disable the mod. Optional diagnostic logging is off by default.

## Known issues and limits

- Some hits on bosses do not show damage numbers. I'm leaving that as a known issue for now; I'm not tackling boss-specific coverage.
- Published **0.1.1 is Solo-only**. Experimental 0.2.0 supports host/client receipts, awaiting a manual two-PC test. A 0.2.0 client needs a 0.2.0 host. Foundation/Terms & Contusions remain optional.
- Friendly, environmental, internal/self and unattributed damage is excluded. Some status or area effects may not retain a usable attacker identity.
- Up to 96 numbers can be shown at once; extreme bursts replace the oldest numbers.

## Bug reports

If you submit a bug report, please attach your **`Player.log` from the session where the problem happened**. It helps me figure out what went wrong.

You can find it here on Windows:

```text
%USERPROFILE%\AppData\LocalLow\Featherweight Games\ChainedBeasts\Player.log
```

Please also mention what you were doing when it happened and which other mods you have installed.

## Build from source

Requires Windows, a .NET SDK that supports **net6.0**, and the installed game's BepInEx loader and generated `BepInEx/interop` assemblies.

```powershell
dotnet build src/ChainedBeasts.BattleNumbers/ChainedBeasts.BattleNumbers.csproj -c Release
```

For a nondefault game location, set `CHAINED_BEASTS_GAME_DIR` before building. The DLL is written to `src/ChainedBeasts.BattleNumbers/bin/Release/net6.0/`. Game binaries, loader dependencies and generated bindings are not included in this repository or release.

## Uninstall

With the game closed, remove `BepInEx/plugins/BattleNumbers/ChainedBeasts.BattleNumbers.dll`. Optionally remove `BepInEx/config/local.chainedbeasts.battlenumbers.cfg`. No save restoration is needed; leave other mods and the loader in place.

## License

[MIT](LICENSE). Small BepInEx/IL2CPP hook and component patterns were adapted from [Chained Beasts Mod Foundation](https://github.com/Krrrrrrk/chained-beasts-mod-foundation), also MIT licensed, copyright (c) 2026 Krrrrrrk. The pixel digit designs are original; no game fonts or assets are bundled. This is an unofficial mod.
