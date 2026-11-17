# Battlefield renderer: Steppe Patch v0.3

**Status:** experimental visual preview (not a production renderer). Unit sprites and the final battle HUD are not included. Visual combat FX exist in a separate demonstration mode but are not connected to the combat simulation.

## What it draws

- Reads the **existing JSON files** from `unity/Assets/Resources/Fields/`. No duplicate map coordinates.
- Supports all three game test maps: `forest-belts` (Sector 07), `balka-and-railway` (Sector 12), and `river-crossing` (Sector 19), each 200 × 400 m.
- Draws each file's polygon fields, tracks, craters, roads, water, ravines, hills, embankments, groves and forest belts in 2D.
- Bright earthy Steppe Patch palette with subtle point-filtered pixels (2 source pixels per metre).
- Reuses small procedural tree shapes; bakes shadows and terrain details into one texture per map.
- Six precomputed crop-wind overlay frames + three soft drifting cloud shadows, rather than hundreds of animated GameObjects.
- Water has lightweight moving shimmer highlights using the same six-frame overlay.
- Small flock of ambient birds (eight shared-sprite objects), taking flight when a nearby demo explosion occurs. One pooled FX system (96 sprites) provides muzzle flashes, explosions, dust, temporary smoke and recurring smoke above the decorative wreck.
- Field edges are irregular and feathered on screen (the JSON polygon outlines and physics do not change).
- Stronger field-row shading: dark furrows for plowed earth, regular sunflower rows, lighter stubble bands; sparse stalk detail.
- Decorative double wheel ruts follow pre-existing `tracks` field features.
- Craters now vary in visible diameter and have scattered earth, dark centres and scorch.
- One sunlight direction (towards bottom-right of Field) for crown, pole, bale, ruin and wreck shadows.
- Sparse map-specific props: power poles and cables, hay bales, a roof-damaged house, a burned-out vehicle. **All are visual-only** and do not alter navigation, colliders or cover.
- River banks get damp-earth strips and visual smoothing of the original centreline.
- `bridge_ruins` is a **decorative-only** feature on River Crossing: two broken deck stubs with a gap over the river. Navigation still uses the existing ford; the broken bridge does not add a path or change movement rules.
- Keeps 90° orthographic camera and the original aspect ratio; no perspective, parallax or 3D models.
- Everything is generated locally from simple code; no paid assets, external textures or asset licences.

## Preview in Unity

Open `unity/` in Unity 6.3 LTS, enter Play in any test scene while using the Editor.
`BattlefieldPreview` starts automatically **in the Editor only**; it hides the temporary `FieldDebugView`.
Use the on-screen **Forest / Balka / River** buttons, or keyboard **1 / 2 / 3**, to switch maps.
Toggle **FX DEMO** to preview flashes, blasts, smoke and vehicle dust. The FX demonstration is **not a real battle** and does not create units or apply damage. The FX component exposes `MuzzleFlash`, `Explosion`, `VehicleDust`, `WreckSmoke` and `FrightenBirds` methods that accept map coordinates (metres); later the Game bridge can call them from real events.

This is intentionally separate from `Treeline.Sim` and `Treeline.Game`. The original battle simulation, JSON geometry, navigation, unit tables and balance are unchanged. The preview takes over the Editor scene's test camera and temporary debug display; remove or disable the preview bootstrap to return to that debug mode.

## Limits / next iteration

- Built for comparing landscape art, not for final mobile performance profiling; runtime drawing is done once when a map is loaded (CPU textures), not every frame.
- Wind moves over sunflowers/stubble using six tiny-alpha frames; it is a stylistic approximation, not physical plant bending.
- Shadows under tree crowns are baked (do not sway yet); three cloud shadows drift across the field; water glints animate with wind.
- FX are preview-only, not linked to Sim combat events; unit/HUD integration is still pending. No screenshots are claimed from an actual Unity execution.
- Visual coordinates use map +X to the right, map +Y downward; render texture origin in Unity is centered, so later unit layers must convert to `(x - width/2, height/2 - y)`.
- Open questions for art review: field hue/contrast, crown density, amount of pixel character, cloud shadow opacity and wind pace.

## Test checklist (Unity Editor)

- All three maps still follow the same geometry; the broken bridge is visibly split and remains untraversable.
- Crop rows can be distinguished at normal fit-to-screen zoom; field borders no longer look like hard rectangular tiles.
- Wind, moving water glints and **three** cloud shadows are visible while the preview is running.
- Power poles, bales, ruins, crater ejecta and wrecks are sparse enough not to mask routes.
- Birds fly continuously and react to nearby demo explosions.
- Toggle FX DEMO and observe pooled muzzle flash, fire, dusty blast, smoke and vehicle dust. This must not affect Sim state.
- Switch maps repeatedly to check resource cleanup and initial camera fit.
- Check Android/phone performance later in real render integration; this Editor preview is not a 60-fps guarantee.
