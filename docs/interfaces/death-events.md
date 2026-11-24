# Death events: Sim → Render interface

Written by Claude, 2026-11-24, for Codex's death-reaction visuals. Status: proposal, waiting for Codex's review.

## What it is

`Battle` makes one read-only `DeathEvent` for each attacker whose HP reaches 0. Nothing in the simulation reads these events. They do not change damage, balance, routes or results.

No event is made when an attacker crosses the line, leaves the field, or is a passenger who dies inside a destroyed carrier (passengers are not units yet).

## Data (`Treeline.Sim.DeathEvent`)

| Field | Meaning |
| --- | --- |
| `EventId` | 1, 2, 3… per battle, in order of death. Never reused in a battle |
| `VictimId`, `VictimType` | The attacker's unit id and its `units.csv` id |
| `X`, `Y` | Victim position at death (metres) |
| `Time`, `Wave` | Simulation time of the wave (`RoundTime`) and the wave number |
| `Cause` | `Bullet`, `DirectExplosive`, `Blast` or `Unknown` (see below) |
| `HasSource`, `SourceUnitId`, `SourceType` | The shooter. For drones it is the operator. For a carrier that blew up, the carrier |
| `SourceX`, `SourceY` | Where the fatal round came from: the muzzle at the moment of firing, the shell's or missile's launch point, the FPV operator |
| `HasImpact`, `ImpactX`, `ImpactY` | Explosives only: where it burst |
| `DirX`, `DirY` | Unit vector of the fatal hit at the victim. Bullet and direct explosive: source → victim. Blast: burst → victim. `(0, 0)` when unknown |

## Causes

| Cause | When |
| --- | --- |
| `Bullet` | A direct round without an explosive (rifles, machine guns, snipers, vehicle guns without blast) |
| `DirectExplosive` | An explosive that bursts inside the victim's hit radius (missile on target, shell on a hull, FPV on target) |
| `Blast` | Blast or fragments from a burst outside the victim's hit radius, and troops killed by their carrier's explosion |
| `Unknown` | Any other case. Use a neutral death visual |

The simulation has no hit location. The renderer chooses body or head variants by itself (cosmetic only).

## Lifetime and reading

- `Battle.DeathEvents`: the events since the current wave started, oldest first. `StartRound()` clears the list at the start of each wave. `EventId` keeps counting.
- `Battle.LastDeathEventId`: the newest id in this battle (0 = none).
- `Battle.ReadDeathEvents(afterId, list)`: adds the events newer than `afterId` to `list` and returns the newest id.

Read them like this, once per rendered frame:

```csharp
int cursor;            // per renderer
Battle seenBattle;     // the battle the cursor belongs to

void LateUpdate()
{
    var b = run.Battle;
    if (!ReferenceEquals(b, seenBattle)) { seenBattle = b; cursor = b.LastDeathEventId; }   // new run or rewind
    buffer.Clear();
    cursor = b.ReadDeathEvents(cursor, buffer);
    foreach (var e in buffer) PlayDeath(e);
}
```

- **Several steps in one frame:** the cursor returns every new event once.
- **New run:** a new `Battle` object, ids start at 1.
- **Rewind** (`Run.RewindTo`): a new `Battle` object. The replay makes the same events with the same ids and data. Start the cursor at `LastDeathEventId` of the new battle so the replayed deaths are not shown again.
- **Wave transition:** the list is cleared when the next wave starts. Read the events before that (the cursor does it every frame).

## Tests

`unity/Assets/Tests/EditMode/DeathEventTests.cs`: bullet death, direct explosive death, blast death, crossing the line (no event), the read cursor with two steps per frame and a new wave, and the same events after a rewind.
