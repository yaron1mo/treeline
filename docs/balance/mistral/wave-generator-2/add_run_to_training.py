#!/usr/bin/env python3
"""Add the waves measured in a Balance run of wave-generator-2 to the training set.

Usage: add_run_to_training.py <run waves.csv> <run wave-cost.csv> <start wave> <tag>

- Appends the rows of the run waves.csv to `training_waves.csv` next to this script, with
  the wave numbers remapped to start..start+n-1 and the notes set to <tag> (a note with a
  shape, `tag:shape`, keeps the shape: `<tag>:shape`).
- Appends the matching rows of the run wave-cost.csv (all fields, NaN rows included) to
  `training_wave_cost.csv` with the same remap.

Example (run 1 of this task): the generated waves 1-30 and the armour probes 31-93 of one
run are added in two calls, the probes as tag mixed-train4 from 20001 and the generator
waves as tag mixed-train3 from 10121.
"""

import csv
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
waves_path, wc_path, start, tag = sys.argv[1], sys.argv[2], int(sys.argv[3]), sys.argv[4]

waves = list(csv.DictReader(open(waves_path, newline="", encoding="utf-8")))
old_nums = sorted({int(r["wave"]) for r in waves})
remap = {w: start + i for i, w in enumerate(old_nums)}

with open(os.path.join(HERE, "training_waves.csv"), "a", newline="", encoding="utf-8") as fh:
    wr = csv.writer(fh, lineterminator="\n")
    for r in waves:
        note = r["notes"]
        if ":" in note:
            note = f"{tag}:{note.split(':', 1)[1]}"
        else:
            note = tag
        wr.writerow([remap[int(r["wave"])], r["unit"], r["count"], r["delay_s"], r["interval_s"], note])

n = 0
with open(os.path.join(HERE, "training_wave_cost.csv"), "a", newline="", encoding="utf-8") as fh:
    wr = csv.writer(fh, lineterminator="\n")
    for r in csv.DictReader(open(wc_path, newline="", encoding="utf-8")):
        w = int(r["wave"])
        if w in remap:
            wr.writerow([r["field"], remap[w], r["threat"], r["cost"], r["held_at_cost"]])
            n += 1

print(f"added {len(old_nums)} waves (numbers {start}..{start + len(old_nums) - 1}, tag {tag}) "
      f"and {n} cost rows to the training set")
