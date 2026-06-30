**Developer:** Mursisru

# Smooth Camera AutoCenter

[![Nuclear Option](https://img.shields.io/badge/Game-Nuclear%20Option-blue)](https://store.steampowered.com/app/2168680/Nuclear_Option/) [![NOLoader](https://img.shields.io/badge/Loader-NOLoader-purple)](https://github.com/Mursisru/NOLoader) [![Version](https://img.shields.io/badge/Version-1.0.0-green)]() [![License](https://img.shields.io/badge/License-MIT-lightgrey)](LICENSE)


Multi-mode smooth external camera for **Nuclear Option** (BepInEx). Replaces legacy `com.alex.smoothcenter`.

**Version:** 1.0.0 Build DEV2SP1  
**BepInEx GUID:** `com.at747.smoothcamera`

## Features

- **Orbit:** boresight combat camera, dynamic pitch height framing, auto-center pan/tilt
- **Chase / TV:** external HUD + auto-center (TV)
- **HUD:** full flight HUD on local aircraft in external views; reticle compensates dynamic camera offset
- **G-LOC:** blackout/vignette in external views

## Install

> [!IMPORTANT]
> **NOLoader required** - install [NOLoader](https://github.com/Mursisru/NOLoader/releases) before this mod.

1. Remove old plugins: `SmaathThirdPersonCamera.dll`, `com.alex.smoothcenter`
2. Copy `SmoothCamera_Engine.dll` to `BepInEx\plugins\`

## Config

`BepInEx\config\com.at747.smoothcamera.cfg`

Key sections: `General`, `Combat`, `Orbit`, `Chase`, `TV`.

| Key | Description |
|-----|-------------|
| `OrbitDynamicFramingStrength` | Dynamic camera height by pitch (0=off) |
| `CombatFollowEnabled` | Boresight orbit camera |
| `AlignHudToBoresight` | HUD reticle compensation for framing |

## Build

```powershell
msbuild SmoothCamera_Engine\SmoothCamera_Engine.csproj /p:Configuration=Release
```

Output: `SmoothCamera_Engine\bin\Release\SmoothCamera_Engine.dll`

## Migration

Disable `com.alex.smoothcenter` before enabling this mod. Both plugins conflict on orbit camera hooks.

---

## Keywords

nuclear-option, noloader, mod, smoothcamera, csharp, unity
