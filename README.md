**Developer:** Mursisru

# Smooth Camera AutoCenter

[![Nuclear Option](https://img.shields.io/badge/Game-Nuclear%20Option-blue)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![BepInEx 5](https://img.shields.io/badge/Loader-BepInEx%205-orange)](https://docs.bepinex.dev/)
[![Version](https://img.shields.io/badge/Version-1.0.0-green)](https://github.com/Mursisru/SmoothCamera/releases/tag/v1.0.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow)](https://github.com/Mursisru/SmoothCamera/blob/main/LICENSE)

---

## Critical warnings

> [!IMPORTANT]
> **BepInEx 5 (x64) required** - install [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) before this mod.

> [!WARNING]
> **Remove legacy SmoothCenter plugins** - delete `SmaathThirdPersonCamera.dll` and `com.alex.smoothcenter`; both hook the same orbit camera pipeline and will conflict.

> [!TIP]
> **Configuration Manager recommended** for in-game tuning of `com.at747.smoothcamera.cfg`.

Multi-mode smooth external camera for **[Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/)** (BepInEx 5). Replaces legacy `com.alex.smoothcenter` / `SmaathThirdPersonCamera.dll`.

**Plugin GUID:** `com.at747.smoothcamera`  
**Version:** `1.0.0` (BepInEx semver) · dev build string in `AppVersion.DisplayVersion`

## Table of contents

- [Critical warnings](#critical-warnings)
- [Features](#features)
- [Requirements](#requirements)
- [Install](#install)
- [Configuration](#configuration)
- [Migration from legacy mods](#migration-from-legacy-mods)
- [Build](#build)
- [Troubleshooting](#troubleshooting)
- [License](#license)

## Features

| Mode | Behavior |
|------|----------|
| **Orbit** | Boresight combat camera, dynamic pitch-height framing, auto-center pan/tilt |
| **Chase / TV** | External HUD + auto-center (TV view) |
| **HUD** | Full flight HUD on local aircraft in external views; reticle compensates dynamic camera offset |
| **G-LOC** | Blackout / vignette in external views |

---

## Requirements

- **[Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/)** (Steam), Windows x64
- **[BepInEx 5](https://docs.bepinex.dev/)** x64 in the game root
- **[Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager)** (recommended) for in-game tuning

---

## Install

> [!IMPORTANT]
> **BepInEx 5 (x64) required** — install [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) in the Nuclear Option folder before this mod.

> [!WARNING]
> **Remove legacy SmoothCenter plugins** before enabling this mod: `SmaathThirdPersonCamera.dll`, `com.alex.smoothcenter`. Both hook the same orbit camera pipeline and will conflict.

1. Download **`SmoothCamera_Engine.dll`** from [Releases](https://github.com/Mursisru/SmoothCamera/releases) or build Release locally.
2. Copy to:

   ```text
   Nuclear Option\BepInEx\plugins\SmoothCamera_Engine.dll
   ```

3. Launch the game. Config auto-creates at `BepInEx\config\com.at747.smoothcamera.cfg`.

---

## Configuration

File: `BepInEx\config\com.at747.smoothcamera.cfg` (or edit via Configuration Manager).

| Section | Key | Description |
|---------|-----|-------------|
| `General` | `Enabled` | Master toggle |
| `Combat` | `CombatFollowEnabled` | Boresight orbit camera |
| `Orbit` | `OrbitDynamicFramingStrength` | Dynamic camera height by pitch (`0` = off) |
| `Chase` | — | Chase view smoothing |
| `TV` | — | TV view auto-center |
| `HUD` | `AlignHudToBoresight` | HUD reticle compensation for framing offset |

---

## Migration from legacy mods

Disable or delete **`com.alex.smoothcenter`** and **`SmaathThirdPersonCamera.dll`** before enabling Smooth Camera. Do not run both orbit-camera hook sets in the same session.

---

## Build

```powershell
msbuild SmoothCamera_Engine\SmoothCamera_Engine.csproj /p:Configuration=Release
```

Output: `SmoothCamera_Engine\bin\Release\SmoothCamera_Engine.dll`

Set `NuclearOptionRoot` in `Directory.Build.props` or `Directory.Build.user.props` if the game is not in the default Steam path.

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Camera fights itself / jitter | Remove legacy SmoothCenter DLLs |
| Plugin not listed | Verify DLL path under `BepInEx\plugins\` |
| Hooks odd after update | Delete `BepInEx\cache\harmony_interop_cache.dat`, restart |

**Logs:** `BepInEx\LogOutput.log` — search for `com.at747.smoothcamera`.

---

## License

MIT — see [LICENSE](LICENSE).

---

## Keywords

nuclear-option, bepinex, harmony, mod, smooth-camera, external-camera, csharp, unity
