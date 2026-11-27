# Round 3: phone sprite test

## Decision

Choose the pixel or painted rendering. Both need a camera-angle revision before production. The faces are more frontal than the top-down view in the art brief. The pixel rendering keeps stronger edges at phone size. The painted rendering has softer edges and loses more detail at phone size.

[Compare the sprites at 1x and 4x](visual/round3/size-comparison.png). The 1x sprite is 32 pixels on this sheet. The same PR #8 rifle-squad sprite appears under all 3 defender levels. The current game has no separate art for those levels. The pig comparison uses the PR #8 regular infantry sprite. This comparison shows a change in camera angle as well as style.

## Battle captures

The captures show the actual Forest belts battle. The test adds budget to place 3 defenders. It runs the simulation at 3x speed. The game uses its usual art when the `-agentStyle` flag is absent. The test maps the 3 defender images to the rifle squad, machine gunner and sniper. This mapping is only for visual review. It does not add defender levels to the game.

| Rendering | Phone image, 360 x 780 | Start | Mid-wave | First attacker death |
| --- | --- | --- | --- | --- |
| Pixel | [Phone](visual/round3/pixel-phone.png) | [Start](visual/round3/pixel-start.png) | [Mid](visual/round3/pixel-mid.png) | [Death](visual/round3/pixel-death.png) |
| Painted | [Phone](visual/round3/painted-phone.png) | [Start](visual/round3/painted-start.png) | [Mid](visual/round3/painted-mid.png) | [Death](visual/round3/painted-death.png) |

[View the 2x unit crop](visual/round3/pixel-units-2x.png). The phone images are reductions of the 1080 x 2340 camera captures. Unity captures the camera output, so these images do not include the editor GUI. Each battle uses a new random seed, so positions differ between captures. The death capture triggers when the simulation records the first kill. The test uses a simple rotation for that fall. It does not test a finished death animation.

To repeat a capture from PowerShell:

```powershell
& ./tools/unity/unity.ps1 -Method AgentTools.CapturePlay -Extra @('-agentStyle','pixel','-agentBattle','1','-agentSeries','1','-agentOut','../build/shots/pixel.png','-agentWait','28','-agentWidth','1080','-agentHeight','2340')
```

Change `pixel` to `painted` for the other rendering. The editor tool saves start, mid-wave and death images. The test uses 64 x 64 texture files for both styles. The pixel style uses point filtering and 32 colours per sprite. The painted style uses bilinear filtering. Effects and terrain still use the current game art. The full game does not yet use one pixel density across all effects and UI.

## Modular test

[View the 8-frame strip](visual/round3/modular-walk-strip.png). [View the generated parts](visual/round3/modular-source.png). `tools/art/round3_walk.py` assembles the head, helmet, body and vest, weapon and patch. It moves the head, body and weapon with a small bob and swing. The strip is a separate test. The Unity battle uses the complete sprites, not these parts. The strip has no leg articulation and does not prove a production walk cycle.

A new unit is an estimated 2-4 hours after the shared parts and workflow are approved. This estimate includes new parts, cleanup, assembly and a phone-size check. It does not include a new direction, shoot or death animation. No production timing has been measured.

## Veteran patches

[View 6 fictional patch designs](visual/round3/veteran-patches.png): coffee, hourglass, folding chair, bandage, thumbs-up and umbrella. These are large design studies. The details need simplification for a small sprite.

## References

These are design observations, not assets for reuse.

- [Door Kickers 2](https://inthekillhouse.com/doorkickers2/): use clear overhead gear silhouettes and terrain separation.
- [Into the Breach](https://subsetgames.com/itb.html): use strong unit outlines and clear threat cues at small size.
- [Kingdom Rush](https://play.kingdomrush.com/kingdom-rush): use distinct character silhouettes and readable action in a crowded defence game.
- [Hotline Miami](https://www.devolverdigital.com/games/hotline-miami): use brief high-contrast feedback to show an action. Keep this game's violence outside the test.
- [Worms Armageddon](https://www.team17.com/games/worms-armageddon): use absurd character details to make conflict satirical.

## Source and limits

The built-in image model made `pixel-source.png`, `painted-source.png`, `modular-source.png` and `veteran-patches.png` for this test. The prompts asked for a pig, 3 defender levels, modern gear, a muted military palette and no real insignia. The patch prompt asked for 6 fictional skeleton jokes. The source sheets were reduced to 64 x 64 unit textures. The pixel version was quantized and its alpha edge was cleaned with a threshold. This is a style test, not a finished animation set.
