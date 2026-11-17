# Battlefield renderer: Steppe Patch v0.1

**Status:** experimental visual preview (not a production renderer). No unit sprites, combat effects or game HUD are included in this PR.

## What it draws

- Reads the **existing JSON files** from `unity/Assets/Resources/Fields/`. No duplicate map coordinates.
- Supports all three game test maps: `forest-belts` (Sector 07), `balka-and-railway` (Sector 12), and `river-crossing` (Sector 19), each 200 × 400 m.
- Draws each file's polygon fields, tracks, craters, roads, water, ravines, hills, embankments, groves and forest belts in 2D.
- Bright earthy Steppe Patch palette with subtle point-filtered pixels (2 source pixels per metre).
- Reuses small procedural tree shapes; bakes shadows and terrain details into one texture per map.
- Three precomputed crop-wind overlay frames + one soft drifting cloud shadow, rather than hundreds of animated GameObjects.
- Keeps 90° orthographic camera and the original aspect ratio; no perspective, parallax or 3D models.
- Everything is generated locally from simple code; no paid assets, external textures or asset licences.

## Preview in Unity

Open `unity/` in Unity 6.3 LTS, enter Play in any test scene while using the Editor.
`BattlefieldPreview` starts automatically **in the Editor only**; it hides the temporary `FieldDebugView`.
Use the on-screen **Forest / Balka / River** buttons, or keyboard **1 / 2 / 3**, to switch maps.

This is intentionally separate from `Treeline.Sim` and `Treeline.Game`. The original battle simulation, JSON geometry, navigation, unit tables and balance are unchanged. The preview takes over the Editor scene's test camera and temporary debug display; remove or disable the preview bootstrap to return to that debug mode.

## Limits / next iteration

- Built for comparing landscape art, not for final mobile performance profiling; runtime drawing is done once when a map is loaded (CPU textures), not every frame.
- Wind moves over sunflowers/stubble using four tiny-alpha frames; it is a stylistic approximation, not physical plant bending.
- Shadows under tree crowns are baked (do not sway yet); cloud shadow moves slowly.
- No unit/FX/HUD integration in this branch, and no screenshots are claimed from an actual Unity execution.
- Visual coordinates use map +X to the right, map +Y downward; render texture origin in Unity is centered, so later unit layers must convert to `(x - width/2, height/2 - y)`.
- Open questions for art review: field hue/contrast, crown density, amount of pixel character, cloud shadow opacity and wind pace.

## With the playtest (2026-11-17, Claude)

When `PlaytestView` runs, it picks the map (`ShowField`) and owns the camera and GUI; this layer only draws the ground. Keys 1/2/3 were removed: the project uses the new Input System only, where the old `Input` class throws.
