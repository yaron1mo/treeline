# Playtest (2026-11-16)

A whole run on one field, played with the mouse in the Unity editor. Flat colours, no art: it tests the battle, not the look.

## How to play

1. Open the project in Unity 6.3, open any scene (SampleScene), press **Play**. The playtest screen opens by itself.
2. Pick a field with `<` `>` (only before wave 1).
3. Click a defender on the left, then click the map to place it. Only the lit lower part of the field takes defenders. Right click stops placing.
4. Click a placed defender to see its range (white ring) and sight (green ring), to **Move** it or **Sell** it (half the price back). An FPV operator can get a **search point**.
5. **Start wave** (20 waves, `waves.csv`). During a wave: pause, speed x1 / x2 / x4. You can still place; new defenders run in from our line.
6. A held wave pays the kill rewards of that wave (a quarter of each enemy's value) plus `wave_income` (100). Lose when the line takes `line_health` (60) damage; win after wave 20.

The numbers come from `unity/Assets/Data` (Sheet tabs `units`, `battle_rules`, new tab `waves`). Edit the Sheet, copy into the CSV, press Play again.

## What is recorded

Every wave writes a line to `playtest-log.csv` in Unity's persistent data folder (the path is shown at the bottom of the panel; on Windows it is under `%USERPROFILE%\AppData\LocalLow\<company>\<product>`): field, wave, held or lost, money after, defenders, line damage, kills, the list of defenders. Send it after playing.

## What to look at

- Is it clear what is happening (who shoots whom, why the line took damage)?
- Which wave is too easy or too hard; do you always buy the same thing?
- Is money too tight or too loose (start 250 = two rifle squads, +100 a wave plus kill rewards)?

Scripted players (`tools/balance/PlaytestRun.cs`) hold 9-11 waves on average, the best runs 15: a careful player should get further.

To get the old debug view back, set `PlaytestView.Enabled` to `false` in `unity/Assets/Scripts/Game/PlaytestView.cs`.
