# Changelog

## Pre-release 1.0.0-pr2 — NOLoader R48 (2026-06-10)

**Branch:** `prerelease/noloader-r48` · **Tag:** `v1.0.0-pr2` · **Build:** `1.0.0 Build PR-R2SP48` (NOLoader core: `0.1.0 Build RDY1R48`)

### NOLoader.SmoothCamera (orbit R41–R48)

- Position smoother: mod-offset velocity limit, framing + visibility unified path (R45–R47)
- Orbit visibility helper: virtual camera WorldToScreen, maneuver-gated fine tracking
- Anti-jitter R48: dual-stage offset filter, 1.4 cm deadband, base-height smooth, rotation micro-damp
- Default `OrbitHeightMultiplier = 0.68`
- Source: `NOLoader.SmoothCamera/` · deploy pack: `deploy/NOLoader/mods/SmoothCamera/`

### Asset

- **SmoothCamera-NOLoader-1.0.0-pr2.zip** — extract into `NOLoader/mods/SmoothCamera/` (requires NOLoader RDYTU core)

---

## Pre-release 1.0.0-pr1 — NOLoader R40 (2026-06-10)

**Branch:** `prerelease/noloader-r40` · **Tag:** `v1.0.0-pr1` · **Build:** `1.0.0 Build PR-R2SP40` (Engine: `DEV2SP40`)

### NOLoader.SmoothCamera (orbit R37–R40)

- BepInEx-like rotation slerp: `Lerp(cruise,gun,gunWeight)` without hard VPU snap
- Combat gates: axis debounce (2+ frames >0.15), block on pan/tilt deviation >0.35°
- HUD-only boresight latch; soft gun transition (no rotation reset on gun switch)
- Dynamic framing: target/applied drive, pitch-rate dampening, SmoothStep gunWeight blend
- Source: `NOLoader.SmoothCamera/` · deploy pack: `deploy/NOLoader/mods/SmoothCamera/`

### Asset

- **SmoothCamera-NOLoader-1.0.0-pr1.zip** — extract into `NOLoader/mods/SmoothCamera/` (requires NOLoader RDYTU core)

---

## 1.0.0 (release)

- Orbit combat camera: boresight follow (smooth cruise, stiff VPU), dynamic pitch height framing, HUD reticle compensation.
- External full HUD (Flight, Combat, MFD, minimap) in orbit/chase/TV for local aircraft.
- Auto-center pan/tilt for orbit/TV; G-LOC effects in external views.
- Replaces legacy `com.alex.smoothcenter` (`SmaathThirdPersonCamera.dll`).

## 1.0.0 Build DEV1SP28

- Performance: remove duplicate HUD refresh hooks and event subscriptions.
- Performance: cache orbit pan/tilt input; single gun-selected check; dedupe WorldToScreen in HUD compensation.
- Fix: cockpit transition no longer double-runs suppress/release; clear stale framing state when orbit writes skip.

## 1.0.0 Build DEV1S27

- HUD: compensate reticle only for dynamic framing offset (not base height); snap diamond to reticle during maneuvers.

## 1.0.0 Build DEV1S25

- Dynamic framing: height only (no camera rotation); signed pitch compensation.

## 1.0.0 Build DEV1S17–S24

- Orbit camera rewrite: single transform writer, boresight aim, dynamic framing iterations, HUD diamond alignment fixes.

## 1.0.0 Build DEV1S1–S6

- Initial multi-mode architecture, combat attitude iterations, view-switch stability fixes.
