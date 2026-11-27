# Pig density study

The approved PR #8 regular pig supplies the reference. The source has 4 walk frames and 6 fall frames. The original image uses a 64 px cell. The visible pig is about 50 px tall, before the game scales it.

The 1.5x and 2x studies use the painted study at 96 px and 128 px per cell. These are sampled studies. They need manual pixel cleanup and the requested snot and tusks before production. The painted study uses image generation. It is not a hand-painted production asset. Each study has 4 walk poses and 6 fall poses. The generated poses need animation cleanup.

The phone capture uses a live Forest belts wave. The 4 comparison pigs are animated visual overlays. They do not represent 4 simulated attackers. The capture uses the game camera and game terrain. It does not test combat interactions or death playback. The game camera renders the capture without the debug panel.

## Estimated art cost

These are planning estimates for 1 artist. They are not measured times. A unit includes 1 view and its base art. An animation includes 1 view and cleanup. Walk has 4 frames. Death has 6 frames. A second view, effects, integration and review are extra.

| Study | Unit | Walk | Death |
| --- | ---: | ---: | ---: |
| Current | 2–4 h | 2–3 h | 3–5 h |
| 1.5x | 4–6 h | 4–6 h | 5–8 h |
| 2x | 6–10 h | 6–10 h | 8–12 h |
| Painted | 10–16 h | 10–16 h | 12–20 h |

The samples use the approved source and generated art. The estimates assume an artist redraws and cleans the production frames.

## Capture

Run from the repo root in PowerShell:

```powershell
& ./tools/unity/unity.ps1 -Method AgentTools.CapturePlay -Extra @('-agentOut','../docs/design/visual/density/forest-phone.png','-agentWait','8','-agentWidth','1080','-agentHeight','2340','-agentDensity','1')
```

The output path is relative to the Unity project folder. The order from left to right is current, 1.5x, 2x, painted.
