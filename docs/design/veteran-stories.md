# Veteran stories and call signs

Proposal by the designer agent, 2026-11-27, for issue #19. Part of [squad-selection.md](squad-selection.md), section 7.5. Later this becomes a CSV in `unity/Assets/Data`. Written in ASD-STE100. Humour is dark. All names are fictional: no real people, units or places.

## 1. How the game uses it

1. At promotion, the game reads the battle statistics of the unit.
2. It picks the reason with the highest priority that is true (priority = order in the table, top first). If none is true, it uses `most_kills`.
3. It picks 1 of the 5 templates for that reason, at random, and fills the variables.

| Variable | Source |
| --- | --- |
| `{callsign}` | The call sign of the unit |
| `{sector}` | Map sector name (a fictional label, for example "Belt 3") |
| `{kills}` | Kills of the unit in the battle |
| `{enemy}` | Enemy type most killed or named by the reason (for example "tank pig") |
| `{wave}` | Number of the wave of the promotion |

## 2. Reasons and templates

| # | Reason | True when |
| --- | --- | --- |
| 1 | `tank_kill` | The unit killed a vehicle |
| 2 | `drone_survivor` | The unit lived through a drone strike |
| 3 | `triple_kill` | 3 kills within a short time |
| 4 | `lane_alone` | The unit held a lane with no other unit near |
| 5 | `last_standing` | The unit was the last one alive on its line |
| 6 | `comeback` | The unit was below 20% HP and still killed an enemy |
| 7 | `night_watch` | The unit made most kills in a night or fog mission |
| 8 | `long_shot` | The unit killed an enemy at the maximum range |
| 9 | `wave_breaker` | The unit made the last kill of a wave |
| 10 | `most_kills` | Fallback: the most kills of the battle |

### 1. tank_kill

1. {callsign} stopped a {enemy} in {sector}. The {enemy} did not plan to stop.
2. In wave {wave}, {callsign} met a {enemy}. Now the {enemy} is scrap and {callsign} has a rank.
3. A {enemy} came to {sector} with a lot of armour. {callsign} had a lot of patience.
4. {callsign} hit the {enemy} where the paint was thin. The report says: "very thin".
5. Wave {wave}: one {enemy}, one {callsign}. The {enemy} lost the count.

### 2. drone_survivor

1. A drone hit next to {callsign} in {sector}. {callsign} is still here. The drone is not.
2. In wave {wave}, the sky fell on {callsign}. {callsign} stood up and asked who is next.
3. {callsign} saw the drone, heard the drone, and lived. {kills} kills followed, out of spite.
4. The drone had a plan for {callsign}. The plan did not include a helmet.
5. {callsign} was in the blast of wave {wave}. The medics call it "luck". {callsign} calls it "Tuesday".

### 3. triple_kill

1. {callsign} took 3 {enemy} in one breath. The third one did not finish its sentence.
2. Wave {wave}, {sector}: three {enemy}, one {callsign}, a very short fight.
3. {callsign} counted to three. The {enemy} did not get to three.
4. The {enemy} came as a group. They left as a list. {callsign} wrote the list.
5. Three in a row in {sector}. {callsign} says it was an accident. It was not.

### 4. lane_alone

1. No one was near {callsign} in {sector}. {kills} {enemy} came. None went through.
2. {callsign} held the lane alone in wave {wave}. The lane is now called "{callsign}'s lane".
3. The line had a hole. {callsign} stood in it and became the line.
4. {callsign} asked for help in {sector}. The help was a bad joke. {callsign} did it alone.
5. Alone in {sector}, {kills} kills, no complaints. A few complaints.

### 5. last_standing

1. In wave {wave}, {callsign} was the last one on the line. {callsign} did not notice. Or did not care.
2. All others left {sector}. {callsign} stayed and finished the job.
3. The line in {sector} fell. One post did not: {callsign}.
4. {callsign} looked left, looked right, saw no one. Then {callsign} made {kills} kills.
5. When wave {wave} ended, {callsign} was alone and in charge of everything. It was a good day.

### 6. comeback

1. {callsign} was nearly done in {sector}. Nearly. {callsign} got {kills} kills after that.
2. At wave {wave}, {callsign} had a few HP left. A few was enough for one {enemy}.
3. The medics wrote "no chance". {callsign} wrote "not now".
4. {callsign} fell, stood up, and hit the {enemy}. The {enemy} did not know the rule about falling.
5. Almost a skull on the wall in {sector}. {callsign} said no, and the {enemy} listened.

### 7. night_watch

1. At night in {sector}, {callsign} saw what others did not. {kills} {enemy} did not see {callsign}.
2. The dark came in wave {wave}. {callsign} had a good eye and a bad mood.
3. {callsign} does not fear the dark. The dark is full of {enemy}, and {callsign} counts.
4. Fog in {sector}, {kills} kills. {callsign} says it was "just listening".
5. When the lights went out, {callsign} got to work. The {enemy} did not get a say.

### 8. long_shot

1. {callsign} hit a {enemy} from so far that the {enemy} had time to think about it.
2. In {sector}, one shot, wave {wave}, a very surprised {enemy}.
3. The {enemy} was a dot on the map. {callsign} made the dot disappear.
4. The range in {sector} is long. The patience of {callsign} is longer.
5. {callsign} says the shot was easy. Others measured it. They stopped talking.

### 9. wave_breaker

1. {callsign} made the last kill of wave {wave}. The rest of the {enemy} lost interest.
2. The wave in {sector} was nearly through. Then {callsign} took the last {enemy}.
3. Wave {wave} ended with one shot from {callsign}. The silence was loud.
4. The last {enemy} of the wave looked at {callsign}. That was its last look.
5. {callsign} closes waves. Wave {wave} was just a door.

### 10. most_kills

1. {callsign} made {kills} kills, the most of the battle. Others made fewer. {callsign} reminds them.
2. In {sector}, {callsign} led the count: {kills}. The {enemy} keep the second place.
3. {kills} kills by wave {wave}. {callsign} did not stop to talk about it.
4. The battle in {sector} had one best unit. The unit knew.
5. {callsign} counted {kills} {enemy}. The {enemy} did not count.

## 3. Call signs (160 words)

Fictional words only. Random pick, no repeat in the same roster. The player can still rename a unit (decided).

Anvil, Badger, Bagel, Banjo, Barnacle, Basil, Beacon, Biscuit, Bishop, Blizzard, Bolt, Bramble, Bucket, Buckle, Bumper, Button, Cactus, Caper, Cashew, Cedar, Chalk, Cheddar, Chimney, Cinder, Clover, Cobweb, Cog, Comet, Copper, Cork, Cricket, Crumb, Dagger, Daisy, Dingo, Domino, Dumpling, Dune, Ember, Fable, Falcon, Fennel, Fidget, Flint, Fog, Fox, Fudge, Gadget, Garlic, Ginger, Gizmo, Goblin, Gravel, Grumble, Gull, Hammer, Harbor, Hazel, Hedgehog, Hiccup, Hinge, Hobnail, Husk, Igloo, Ink, Ivy, Jackal, Jam, Jester, Jigsaw, Juniper, Kettle, Kestrel, Kindle, Kipper, Knot, Ladle, Lantern, Latch, Lemon, Lichen, Locket, Lumber, Magnet, Maple, Marble, Marmot, Mitten, Mole, Moss, Muffin, Mustard, Nail, Nettle, Noodle, Nugget, Oak, Olive, Onion, Orbit, Otter, Paddle, Pancake, Pebble, Pepper, Pickle, Pigeon, Pixel, Plank, Pogo, Poppy, Pretzel, Pudding, Quill, Quiver, Rabbit, Raven, Ribbon, Rivet, Rook, Rust, Saddle, Saffron, Sage, Sardine, Scone, Shovel, Sparrow, Spruce, Sprocket, Stitch, Sundial, Tackle, Thimble, Thistle, Toast, Toggle, Trowel, Tumbler, Turnip, Umber, Velcro, Walnut, Whistle, Willow, Wrench, Yarrow, Zephyr, Zipper

Claude checks the list against real call signs and brands before release (legal check, as for weapon names).
