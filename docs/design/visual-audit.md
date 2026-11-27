# Visual audit: Treeline

2026-11-27 · Codex · Issue #27

## Decision

I recommend **A: clean cartoon**, with a top-down camera and modern fictional equipment. It gives the pigs clear shapes at phone size. It also gives the field and effects one visual language. Keep the open terrain and tree belts. Do not copy the fantasy weapons, flags, buildings, or camera angle in the concepts.

The boards are image-model studies, not production assets or exact game screens. Each board shows a similar attack, a belt, defenders, and an explosion. The generated poses and camera angles differ. The phone images are 360 × 780 crops of the boards, not screenshots from a phone. The model does not keep a strict pixel grid in B or C. C shows blood and fallen pigs, but does not fully show the requested extreme violence. Do not use the images as a rating test.

| Option | Board and phone crop | Art cost | Assessment |
| --- | --- | --- | --- |
| A. Clean cartoon | [Board](visual/a-cartoon.png) · [phone](visual/a-cartoon-phone.png) | High initial cost: redraw units, terrain, effects, and UI. Moderate cost per new unit after the rules are set. | Best readability and best fit for the candy goal. The board uses a side view; production must use top-down art. |
| B. Pixel, 16+ target | [Board](visual/b-pixel-16.png) · [phone](visual/b-pixel-16-phone.png) | High: rebuild all layers on one pixel grid. Each direction, effect, and UI state needs pixel review. | Strong mood. Small pixels and neon effects can hide units. The board has fantasy equipment and mixed pixel sizes. |
| C. Pixel, adult target | [Board](visual/c-pixel-18.png) · [phone](visual/c-pixel-18-phone.png) | Highest: B cost plus death states, blood, content controls, and separate store art. | The violence draws attention away from the line. This board is less graphic than the requested comparison. |

These costs are relative estimates, not schedules. A needs a production test before a schedule is possible. Age labels are targets, not ratings.

## Current game

[Start](visual/current-start.png) · [Wave 1](visual/current-wave.png) · [Later in wave](visual/current-late.png) ? [Staged explosion](visual/current-explosion.png) ? [Wave at 120 s](visual/current-120s.png). These are Unity camera captures at 1280 × 720. The camera capture omits the IMGUI panel. It cannot prove how the full HUD reads or responds to touch. The capture setup places 2 rifle squads and starts wave 1. The third placement fails because the budget is too low. The wave contains 5 recruits, so it is not a dense late wave. The editor triggers the explosion effect at an attacker for the staged image. It is difficult to see at this scale. The 120 s capture does not show a clearly identifiable death at this scale. A fixed-time capture can miss short effects.

**Keep:** the open field, tree belts, paths, and changes in crop colour give the map a distinct place. The top-down view supports route reading. The belt gaps suggest useful movement routes.

**Change:** the field has more visual detail than the units. In the wave image, attackers merge into the trees and are hard to count. The small defenders near the bottom are difficult to find. The large empty side area reduces the space available for the field. The blue line at the bottom is thin and has little contrast. The current art mixes textured terrain with pixel units and smooth overlays. The result does not yet feel like one game.

### Phone readability

At the full-map scale, the current capture does not support reliable identification of each unit type at 1 cm. The wave image contains only recruits, so it cannot test every type. The line is hard to find and the route to danger is not clear enough. Test on a physical 360 × 780 phone at actual size, in sunlight and with colour vision filters. Ask players to identify each type, count a group, find the line, and point to the nearest danger within 2 seconds. Record errors and adjust before production.

## Candy direction

Use a small palette. Keep most of the field quiet so the active units stand out.

| Role | Colour | Use |
| --- | --- | --- |
| Field | wheat `#E7C86A`, leaf `#7EA354` | Large flat areas and belts; lower saturation behind units. |
| Attacker | pig pink `#F47F80` | Main attacker body; use shape as well as colour. |
| Defender | teal `#288D91` | Defender clothing and selection marks. |
| Effect | warm yellow `#FFD34E` | Short hits and explosion core. |
| Outline | deep plum `#30283A` | Unit edges, line, and UI text. |
| UI light | cream `#FFF2D2` | Button faces and high-contrast labels. |

Give each attacker a distinct outer shape: small round recruit, narrow fast attacker, wide armoured attacker. Keep the snout visible from above. Give defenders a separate silhouette and teal accent. Use a consistent dark outline; start at 2 screen pixels at 360 px width and test on devices. Target a 24–36 px visible unit height at the normal phone view. Keep visual size separate from simulation range and collision.

Make the line a continuous high-contrast band with an arrow or pulse at a threatened section. Use shape, motion, and colour together for danger. Do not let an explosion cover the line for more than a short moment.

Give hits a short light flash. Add a small, optional screen shake for large impacts; never move the UI. Show short damage numbers only for important hits, with a cap on simultaneous numbers. Show kill streak text away from the line and provide an option to turn it off. Use distinct short sounds for hits, large impacts, and line damage. Add volume controls. Test all feedback together in a crowded wave.

Use large rounded buttons with clear pressed and disabled states. Start with a 48 × 48 dp touch target, safe-area padding, and a short label. Place the primary wave button where a thumb can reach it without covering the line. Test actual touch and text sizes on Android and iOS.

## References

- **Kingdom Rush:** study the clear unit shapes and impact timing. Keep Treeline's top-down field and its own characters.
- **Plants vs. Zombies:** study the strong colour roles and readable attacker groups. Do not copy its characters or lane layout.
- **Hotline Miami:** study colour contrast and immediate impact feedback for B and C. Do not copy its sprites, scenes, or violence.

## Adult content and release

A PEGI 18 or Apple 17+ outcome is not guaranteed. PEGI and Apple assess the actual game. Apple now also lists regional 18+ ratings, and prolonged graphic or sadistic realistic violence can make an app unrated for the App Store. See [Apple age definitions](https://developer.apple.com/help/app-store-connect/reference/app-information/age-ratings-values-and-definitions/) and [PEGI ratings](https://pegi.info/what-do-the-labels-mean).

Ad demand can fall for restricted content, but do not assume every 18+ game loses ads. Google has an exception for ordinary gameplay imagery. Check the final content with each ad network. See [Google publisher restrictions](https://support.google.com/admob/answer/10437795?hl=en-GB).

An adult rating limits the potential audience. Store featuring is an editorial decision; a rating alone does not prove exclusion. Apple requires store assets to meet a 4+ standard and forbids gore in those assets. See [Apple asset guidance](https://developer.apple.com/app-store/asset-best-practices/) and [featuring considerations](https://developer.apple.com/app-store/getting-featured/).

## Work in value order

1. Approve A, B, or C and the violence limit. Confirm a modern top-down setting.
2. Make 3 production pig silhouettes and 1 defender at actual phone scale. Test identification.
3. Redraw the line and danger signal. Test them over every field colour.
4. Make one coherent field tile and belt set. Reduce detail behind units.
5. Build one crowded wave with hit, death, and explosion states. Test contrast and performance.
6. Build a touch HUD with large buttons and safe areas. Test it on phones.
7. Make the remaining unit, effect, sound, and UI states. Check the asset licences.
