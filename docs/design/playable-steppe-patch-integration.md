# Treeline — Playable Steppe Patch v0.5 integration

**Branch:** `codex/playable-steppe-patch-integration`  
**Base:** latest `codex/open-field-balance-2026-11-12` (20-wave game, balanced unit and economic tables).  
**Source artwork:** `codex/battlefield-steppe-patch-v01` (the 23 unit sprite atlas, landscape, atmospheric motion and particle effects).

## What works together

- The full 20-wave playtest, budget, placement, sell/move, defenders, enemies and AI remain in `Treeline.Game.PlaytestView`.
- Terrain uses `BattlefieldPreview` (the three original map JSONs); the Game screen keeps control of camera, field selection, input and left HUD.
- `BattlefieldUnitLayer` binds to the **real Battle instance** using Game's existing `SendMessage` bridge. It renders the 23 actual unit ID sprites from the indexed point-filtered atlas. It updates positions and orientation from actual simulation state. No artificial demo formations appear in playtest.
- The playtest's block-shaped debug marks are disabled when the real unit layer is bound. The old marks remain as a fallback if the art layer cannot be found.
- The existing Playtest HUD and playable waves are kept; the standalone Steppe Patch preview HUD is hidden during playtest.
- Muzzle flashes are triggered from actual `ShotTraces`. Projectile disappearance triggers visual blast effects; destroyed enemy vehicles trigger additional wreck smoke and blast. Moving vehicle dust follows their velocity. Small birds, crop wind, moving cloud shadows, water glints and cosmetic props are environmental.
- The bridge ruins on River Crossing are decorative and still impassable. The ford and field navigation are unchanged.
- New `Resources/Art/Units/units_atlas_indexed.txt` is the compact color-indexed source of the 23 sprites. `tools/art/unit_atlas.py` can export and repack PNG tiles for future edits. Includes Unity `.meta` files.
- Removed `UnityEngine.Input.GetKeyDown` from the render bootstrap: the project uses the new Input System. Field switches in the standalone art preview are still available by GUI, and field switches in the real game use the playtest HUD.

## Test in Unity 6.3 LTS

1. **Exit Play mode**. In GitHub Desktop, **Fetch origin** and switch to `codex/playable-steppe-patch-integration` (not the balance-only or art-only branches). Fetch if the branch is not listed yet. Preserve any local uncommitted changes using Stash.
2. Return to Unity and allow it to reimport the assets. Open `SampleScene`, press Play.
3. The left panel should read **TREELINE playtest**, with 20 waves and build menu. There should be no independent enormous `ART PREVIEW` menu covering the map.
4. Place one or more defenders by choosing them on the left and clicking the lower part of the map; watch the new sprite appear.
5. Press **Start wave 1**. Real enemy sprites should spawn and travel through the original terrain. Look for shot effects, projectile blasts, dust and smoke. Test pause, x2/x4 and next wave.
6. Restart or change fields with left-panel arrows. Confirm Forest Belts, Balka and Railway, River Crossing (decorative broken bridge).
7. Check Console for exceptions and verify that the 23 sprite atlas loads. Player Settings > **Active Input Handling: Both** is NOT required by this integrated renderer.

## Constraints and known limits

- No changes to `Treeline.Sim`, unit CSV values, wave JSON/CSV, economy, terrain/nav grid, unit combat or pathfinding.
- The current left HUD remains **temporary IMGUI playtest controls**, not the final UI Toolkit HUD.
- Sprite art is minimal v0.1 and not visually approved. Scale and readability need review in a real Unity screen.
- Destruction is approximated from live projectile disappearance and enemy-death state, not yet a typed Sim visual event stream. Some projectile disappearance may require FX refinement.
- All decorative scenery is visual-only, so pole/house/wreck positions are not currently colliders/cover.
- Unity has not been launched from this assistant environment. Test compilation and behavior in the Editor before merging.
