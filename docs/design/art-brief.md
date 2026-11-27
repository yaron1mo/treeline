# Art brief

Written by Claude (CTO) from Yaroslav's decisions, 2026-11-27. Every visual task (Codex) starts from this file. Do not propose a style that breaks section 1.

## 1. Fixed (decided)

- **Style base (Yaroslav, 2026-11-27): the PR #8 unit art is the base.** Round 3 (PR #31) looked too childish ("a game for five-year-olds"); its pixel variant has some good points and is a secondary source. Combine, improve and complete; do not start a new style.
- **Genre and tone: a satire.** Yaroslav (2026-11-27): the game is a satire in which the invaders are pigs. It is not a shooter or a simulator. **No hyper-realism and no horror.** Stylised, readable, with dark humour.
- **Attackers: zombie pigs in helmets** in the gear of a modern invading army (the PR #8 pig is the reference): angry, aggressive, they push forward to kill you; gross and dirty (snot from the snout, drool). The satire comes from the grossness and the stupidity, not from cute cartoon shapes. Not a realistic horror monster either.
- **Defenders: soldiers in modern uniform. No zombies on the defence side** (Yaroslav, 2026-11-27): no rotting flesh, no decay stage.
  - Low level: a **human** soldier.
  - Middle level: a human with more experience (more gear, scars, beard, more patches), still a human.
  - **Veteran: a skeleton in uniform**, calm and smiling: "they do not die". **Black humour, sarcasm and irony live on the veterans**: sarcastic patches, a coffee mug, a cigarette, sunglasses, a relaxed pose under fire.
- **Authentic gear, stylised bodies.** The gear (camouflage, plate carriers, radios, drones) is recognisable and accurate. The bodies and faces are stylised for satire.
- **In-game sprites are not key art.** On a phone a unit is about 1 cm. A detailed concept is key art (store page, menus, loading screens). In-game sprites are simpler: a clear silhouette, a few colours, one or two signature details (pig snout, skull, helmet).
- **Camera:** top-down, phone screen.
- **No real flags, national symbols, unit insignia, letters such as Z, or real people.** Fictional patches only.

## 2. References: modern military culture (especially Ukrainian)

Study these and take the feeling, not the content:

- **Gear:** pixel camouflage (MM-14), multicam, olive; plate carriers, helmets with covers, ear protection, knee pads, tourniquets on the vest, radios, drone controllers, thermal capes.
- **Patches (chevrons):** a strong culture of humorous and sarcastic patches. Our defenders wear fictional patches with dark humour (call sign, blood type, a joke). Patches are a natural cosmetic reward.
- **Drone footage:** grainy FPV camera view, black-and-white and thermal views, the "drop" from a bomber drone, the last frames of an FPV hit. Use this look for recon views, the forecast screen and kill replays.
- **Viral front-line videos:** short clips with phonk music, fast cuts, slow-motion hits, kill-feed text. This is where "triple kill" and the kill-streak text come from.
- **Artillery and rocket launches at night:** HIMARS-style launches, long light trails, a flash on the horizon. Use this for support calls.
- **Trench life:** dugouts, sandbags, pallets, camouflage nets, a cat in the trench, energy drinks, power banks.

## 3. Open (Yaroslav decides from concept art)

| Axis | Options |
| --- | --- |
| **Rendering** | (a) improved pixel art: one pixel density for units, effects and UI; (b) stylised hand-painted 2D (not realistic); (c) mixed: realistic terrain from real maps, stylised units. Realistic rendering is excluded (Yaroslav, 2026-11-27) |
| **Palette** | (a) muted military base (olive, mud, dry steppe, multicam) with a few bright signal colours for gameplay (pig skin, undead glow, fire); (b) drone-footage look (desaturated, grain, thermal accents); (c) darker, high-contrast night palette |
| **Violence** | (a) 16+: stylised blood, no dismemberment; undead lose bones in a funny way; pigs fall with helmets flying; (b) 18+ (Hotline Miami level): changes the store rating, ads and featuring |

## 4. Claude's proposal (for discussion)

- Rendering (a), improved pixel art, because it keeps the authentic look that Yaroslav likes and is the cheapest to animate in many directions.
- Palette (a) with (b) for special screens: a muted military base, bright colours only where the player must look. The "candy" feeling comes from feedback (hit flash, shake, kill-streak text, sound, phonk-like music), not from a bright palette.
- Violence (a) 16+.

## 5. Animation budget (how to keep it cheap)

A unit needs 2 views (straight and diagonal, the other diagonal is a mirror) × walk (6–8 frames), shoot (3–4), death (6) = about 30–40 frames. 20 attacker types and 10 defender types at 3 levels give about 2 000 frames if each frame is drawn separately. This is too much for consistent quality.

Rules:
- **Modular units:** one body, separate parts (head: pig / human / skull; helmet; vest; weapon; patches). The parts are drawn once and combined. A new level or type changes parts, not the whole animation.
- **Animate the parts in code** (simple 2D bones: bob, swing, recoil, fall), not frame by frame, where it looks good enough at phone size.
- **Draw frame by frame only the special moments** (deaths, the veteran's ironic idle, big vehicles).
- **Test at real size first:** every style test shows the unit at in-game size on a phone crop before any detail work.
