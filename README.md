# RunHoard

A mobile hyper-casual runner/battler built with Unity 6. Run a stickman down a procedurally generated track, grow your crowd through gates, dodge hazards, fight enemy squads, and storm a castle at the end of each level. Collect coins along the way and spend them in the upgrade shop.

## Gameplay

- **Run and steer** a stickman down a generated track.
- **Gates** add to or take away from your crowd. Later levels mix in penalties and harsher multipliers.
- **Hazards**: spiked rollers, swinging pendulum hammers, and boxing gloves.
- **Enemies** scale up from single stickmen to moving squads to a castle boss with a health bar.
- **Coins and upgrades**: collect coins, then buy permanent upgrades (starting army size and level, hats) in the shop. Purchases persist between launches.
- **Level progression**: track length, castle health, gate difficulty, obstacle density, and squad movement all ramp up as you advance.

## Tech

- Unity **6000.4.11f1** (Unity 6)
- Universal Render Pipeline (URP), with separate mobile and PC pipeline assets
- New Input System
- Target platform: Android

## Project layout

| Path | Contents |
| --- | --- |
| `Assets/Scripts/` | Gameplay code: track generation, player and enemy controllers, obstacles, economy, UI feedback |
| `Assets/Prefabs/` | Stickman, enemies, castle, coins, gates, and hazards |
| `Assets/Scenes/SampleScene.unity` | The game scene |
| `Assets/Editor/` | Editor tooling, including Android signing setup |
| `Assets/Settings/` | URP pipeline assets and profiles |
| `Packages/manifest.json` | Package dependencies |

## Getting started

1. Install Unity **6000.4.11f1** via Unity Hub (with the **Android Build Support** module for mobile builds).
2. Clone this repository and open the folder as a project in Unity Hub.
3. Open `Assets/Scenes/SampleScene.unity` and press **Play**.

## Building for Android

The repository does not contain any signing keys. To produce a signed build you need your own Android keystore.

- `Assets/Editor/AndroidSigningSetup.cs` applies signing settings at build time and reads the keystore passwords from the macOS Keychain (or from environment variables) instead of storing them in project files. See the comment at the top of that file for the one-time setup, and adjust the keystore path to match your machine.
- Never commit keystores, passwords, or build outputs (`*.keystore`, `*.jks`, `*.apk`, `*.aab`). They are listed in `.gitignore`.

## Notes

- Difficulty ramps through castle health, obstacle density, penalty severity, and enemy movement. Run speed stays constant.
- There are currently no automated tests, even though the Unity Test Framework package is installed.
