# Changelog

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
