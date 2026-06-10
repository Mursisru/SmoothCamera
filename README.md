# Smooth Camera AutoCenter

Multi-mode smooth external camera for **Nuclear Option**. Supports **BepInEx** and **NOLoader**. Replaces legacy `com.alex.smoothcenter`.

**Version:** 1.0.0 Build PR-R2SP90 (pre-release **v1.0.0-pr5** — NOLoader R90)  
**BepInEx GUID:** `com.at747.smoothcamera` · **NOLoader mod id:** `com.at747.smoothcamera`

## Features

- **Orbit:** boresight combat camera, dynamic pitch height framing, auto-center pan/tilt
- **Chase / TV:** external HUD + auto-center (TV)
- **HUD:** full flight HUD on local aircraft in external views; reticle compensates dynamic camera offset
- **G-LOC:** blackout/vignette in external views

## Install (BepInEx)

1. Remove old plugins: `SmaathThirdPersonCamera.dll`, `com.alex.smoothcenter`
2. Copy `SmoothCamera_Engine.dll` to `BepInEx\plugins\`

## Install (NOLoader)

1. Install [NOLoader](https://github.com/Mursisru/NOLoader) RDYTU core first
2. Download **SmoothCamera-NOLoader-1.0.0-pr3.zip** from [Releases](https://github.com/Mursisru/SmoothCamera/releases)
3. Extract into `NOLoader/mods/SmoothCamera/` (game closed)
4. Config: `NOLoader/mods/SmoothCamera/mod_config.ini`

## Config

`BepInEx\config\com.at747.smoothcamera.cfg`

Key sections: `General`, `Combat`, `Orbit`, `Chase`, `TV`.

| Key | Description |
|-----|-------------|
| `OrbitDynamicFramingStrength` | Dynamic camera height by pitch (0=off) |
| `CombatFollowEnabled` | Boresight orbit camera |
| `AlignHudToBoresight` | HUD reticle compensation for framing |

## Build

**BepInEx:**

```powershell
msbuild SmoothCamera_Engine\SmoothCamera_Engine.csproj /p:Configuration=Release
```

**NOLoader mod** (requires sibling `NOLoader_Engine` repo):

```powershell
dotnet build NOLoader.SmoothCamera\NOLoader.SmoothCamera.csproj -c RDYTU
```

## Migration

Disable `com.alex.smoothcenter` before enabling this mod. Both plugins conflict on orbit camera hooks.
