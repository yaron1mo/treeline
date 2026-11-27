#!/usr/bin/env python3
"""Map grammar for the designed maps (issue #46).

Implements the elements and rules of section "map grammar" in docs/design/maps.md
(branch design/maps) as parameters:

  field size 200 x 400, portrait, attackers from the top (y = 0), base at the bottom;
  belts across the field with 1-2 gaps each, gap shift >= 50 m between belts;
  gap width 16-20 m (30 m = a deliberate wide gap); belt spacing 60-120 m;
  a pocket (a belt along the route), a road, tracks through the gaps;
  groves (also scattered "islands"), pond, craters; field patchwork in 2-3 tones (look only);
  rivers (edge to edge, so they force a crossing), fords, pipes, ruined bridges;
  hills (blobs; a ridge is a row of hills with saddles; a slope is a long ellipse),
  gullies (balka), embankments;
  rocks, wrecks (also a wreck column), bushes, hedgehogs, heaps, trench lines, wire;
  a sunflower field (bushes until the Sim has a crop rule);
  a burned strip is a normal belt until the Sim has a burned-belt rule.

Same seed + same parameters -> same map. Every random number comes from one
random.Random(seed) instance, so a map is fully reproducible.

Used by tools/maps/generate.py (writes the field JSON) and tools/maps/check.py.
"""

import math
import random

# Rule values from docs/design/maps.md section 4.2 (defaults; a concept may override).
RULES = {
    "field_size": [200, 400],
    "belt_spacing": [60, 120],      # min, max mean spacing between belts
    "belts_per_map": [2, 4],
    "gap_width": [16, 20],           # a "wide gap" is 30, only on purpose
    "wide_gap_width": 30,
    "gaps_per_belt": [1, 2],
    "gap_shift_min": 50,             # next belt's gap at least this far from the one above
    "gap_x_range": [20, 180],        # gaps stay inside the closed 10 m edge strips
    "lanes": [2, 3],
    "belt_width": [12, 16],
    "edge_margin": 10,               # battle_rules edge_margin_m: closed side strips
    "craters_radius": [0.6, 2.2],
    "spawns": [1, 2],
}

# Field tones used by the reference map (look only, no gameplay effect).
TONES = ["stubble", "plowed", "sunflower", "rape", "fallow"]


def _rng(concept):
    return random.Random(concept.get("seed", 1))


def _j(rng, lo, hi):
    return lo + rng.random() * (hi - lo)


def _clamp(v, lo, hi):
    return max(lo, min(hi, v))


# ---------------------------------------------------------------- fields

def field_strips(concept, rng, belt_ys, belt_widths):
    """Horizontal field strips between the belt rows, with wavy boundaries (look only).

    patchwork=True splits every strip into 2-3 vertical patches of different tones.
    """
    w, h = concept["size"]
    spec = concept.get("fields", {})
    tones = spec.get("tones", TONES[:3])
    wavy = spec.get("wavy", 6)
    patchwork = spec.get("patchwork", False)
    angles = spec.get("angle", [2, 88])
    bounds = [0.0] + [y for y in belt_ys] + [float(h)]
    feats = []
    ti = 0
    for i in range(len(bounds) - 1):
        y0, y1 = bounds[i], bounds[i + 1]
        # wavy boundary lines shared between neighbouring strips
        def edge(y):
            j = _j(rng, -wavy, wavy)
            k = _j(rng, -wavy, wavy)
            return (0.0, y + j), (w, y + k)
        (ax, ay), (bx, by) = edge(y0)
        (cx, cy), (dx, dy) = edge(y1)
        if not patchwork:
            poly = [[ax, ay], [bx, by], [dx, dy], [cx, cy]]
            feats.append({"type": "field", "poly": poly, "tone": tones[ti % len(tones)],
                          "angle": _j(rng, *angles) if angles[0] == angles[1] else rng.choice(angles) + _j(rng, -6, 6)})
            ti += 1
        else:
            splits = sorted(rng.sample([w * 0.33, w * 0.5, w * 0.66], rng.randint(1, 2)))
            xs = [0.0] + splits + [w]
            for j in range(len(xs) - 1):
                x0, x1 = xs[j], xs[j + 1]
                pad0 = _j(rng, -wavy, wavy)
                pad1 = _j(rng, -wavy, wavy)
                poly = [[x0, y0 + pad0], [x1, y1 + pad1 * 0], [x1, y1 + _j(rng, -wavy, wavy)], [x0, y0 + _j(rng, -wavy, wavy)]]
                # keep the polygon simple: left edge on y0 line, right edge on y1 line
                poly = [[x0, y0 + _j(rng, -wavy, wavy)], [x1, y0 + _j(rng, -wavy, wavy)],
                        [x1, y1 + _j(rng, -wavy, wavy)], [x0, y1 + _j(rng, -wavy, wavy)]]
                feats.append({"type": "field", "poly": poly, "tone": tones[ti % len(tones)],
                              "angle": rng.choice(angles) + _j(rng, -6, 6)})
                ti += 1
    return feats


# ---------------------------------------------------------------- belts

def belt_rows(concept, rng):
    """Belt row y positions and per-belt widths from the belt parameters.
    A concept without "belts" has no belt rows (river, hill, forest and steppe maps)."""
    b = concept.get("belts") or {}
    count = b.get("count", 0)
    y0 = b.get("y0", 74)
    spacing = b.get("spacing", 82)
    jit = b.get("spacing_jitter", 8)
    wspec = b.get("width", RULES["belt_width"])
    wlo, whi = (wspec, wspec) if isinstance(wspec, (int, float)) else wspec
    ys, ws = [], []
    y = float(y0)
    for i in range(count):
        if i > 0:
            y += max(30.0, spacing + _j(rng, -jit, jit))
        ys.append(round(y, 1))
        ws.append(round(_j(rng, wlo, whi), 1))
    return ys, ws


def belt_gaps(concept, rng, belt_ys):
    """Gap centres per belt row. Explicit gap_x wins; otherwise the rule generator:
    1-2 gaps per belt, at least gap_shift_min from every gap of the row above,
    two gaps in one row at least 60 m apart, inside gap_x_range."""
    b = concept.get("belts") or {}
    explicit = b.get("gap_x")
    if explicit:
        assert len(explicit) == len(belt_ys), "gap_x must give one entry per belt"
        return [[float(x) for x in row] for row in explicit]
    mode = b.get("gap_pattern", "shift")
    per = b.get("gaps_per_belt", 1)
    gw = b.get("gap_width", RULES["gap_width"])
    gw = gw if isinstance(gw, list) else [gw, gw]
    shift = b.get("gap_shift_min", RULES["gap_shift_min"])
    xlo, xhi = b.get("gap_x_range", RULES["gap_x_range"])
    rows = []
    prev = []
    for i in range(len(belt_ys)):
        n = per[i] if isinstance(per, list) else per
        if mode == "aligned":            # Needle: one line of gaps
            x = prev[0] if prev else _j(rng, 90, 110)
            row = [x]
        elif mode == "edges":            # Switchback: far edges, alternating
            x = (xhi if i % 2 == 0 else xlo) + _j(rng, 0, 12) * (1 if i % 2 == 0 else -1)
            row = [_clamp(x, xlo, xhi)]
        else:                            # shift: bend the route
            row = []
            tries = 0
            while len(row) < n and tries < 200:
                tries += 1
                x = _j(rng, xlo, xhi)
                if all(abs(x - p) >= shift for p in prev) and all(abs(x - q) >= 60 for q in row):
                    row.append(x)
            if not row:
                row = [_clamp((prev[0] + 120) % (xhi - xlo) + xlo, xlo, xhi)]
        rows.append(sorted(row))
        prev = rows[-1]
    return rows


def gap_widths(concept, rng, rows):
    b = concept.get("belts") or {}
    gw = b.get("gap_width", RULES["gap_width"])
    if not rows:
        return []
    if isinstance(gw, list) and len(gw) == 2 and isinstance(gw[0], (int, float)):
        lo, hi = gw
        wide = b.get("wide_gap_belt")  # belt index that gets the deliberate 30 m gap (map 20, group B)
        out = []
        for i, row in enumerate(rows):
            out.append([RULES["wide_gap_width"] if i == wide else round(_j(rng, lo, hi), 1) for _ in row])
        return out
    return [[gw for _ in row] for row in rows]


def belt_features(concept, rng, belt_ys, belt_ws, rows, widths):
    """Belt segments: a row is split at its gaps; every segment is a slightly wavy line."""
    w, h = concept["size"]
    shape = (concept.get("belts") or {}).get("shape", "straight")
    feats = []
    drop = (concept.get("belts") or {}).get("chevron_drop", 30)
    for i, y in enumerate(belt_ys):
        bw = belt_ws[i]
        gaps = sorted(zip(rows[i], widths[i]))
        if shape == "chevron":
            # Funnel: the row is a V; the gap sits at the bottom vertex.
            gx = gaps[0][0]
            gw = gaps[0][1]
            lx, rx = 18.0, w - 18.0
            feats.append({"type": "belt", "width": bw,
                          "line": [[lx, y + _j(rng, -2, 2)],
                                   [_clamp(gx - gw / 2 - bw, lx + 8, gx - gw / 2), y + drop],
                                   [gx - gw / 2 - bw * 0.5, y + drop + _j(rng, -2, 2)]]})
            feats.append({"type": "belt", "width": bw,
                          "line": [[gx + gw / 2 + bw * 0.5, y + drop + _j(rng, -2, 2)],
                                   [_clamp(gx + gw / 2 + bw, gx + gw / 2, rx - 8), y + drop],
                                   [rx, y + _j(rng, -2, 2)]]})
            continue
        # straight row: segments between gaps
        edges = [-6.0]
        for gx, gw in gaps:
            edges += [gx - gw / 2, gx + gw / 2]
        edges.append(w + 6.0)
        for k in range(0, len(edges), 2):
            x0, x1 = edges[k], edges[k + 1]
            if x1 - x0 < 4:
                continue
            pts = []
            steps = max(2, int((x1 - x0) / 45) + 1)
            for s in range(steps):
                x = x0 + (x1 - x0) * s / (steps - 1)
                pts.append([round(x, 1), round(y + _j(rng, -2.5, 2.5), 1)])
            feats.append({"type": "belt", "width": bw, "line": pts})
    return feats


def pocket_feature(concept, rng, belt_ys, belt_ws, rows, widths):
    """A belt along the route next to a gap: a strip closed on 3 sides (map 4 Pocket)."""
    p = concept.get("pocket")
    if not p:
        return []
    i = p.get("belt", len(belt_ys) - 1)
    side = p.get("side", 1)
    ln = p.get("len", 70)
    gx = min(rows[i], key=lambda g: abs(g - p.get("gap_x", rows[i][0])))
    gw = widths[i][rows[i].index(gx)]
    bw = belt_ws[i]
    x = gx + side * (gw / 2 + bw / 2 + 5)
    y = belt_ys[i]
    top = y - ln if p.get("toward", "up") == "up" else y - 8
    bot = y + 12 if p.get("toward", "up") == "up" else y + ln
    pts = [[round(x + _j(rng, -2, 2), 1), round(top, 1)],
           [round(x + _j(rng, -2, 2), 1), round((top + bot) / 2, 1)],
           [round(x + _j(rng, -2, 2), 1), round(bot, 1)]]
    return [{"type": "belt", "width": bw, "line": pts}]


def fork_feature(concept, rng, belt_ys, belt_ws):
    """A central stub belt between two rows splits the corridor into two lanes (map 8 Fork)."""
    f = concept.get("fork")
    if not f:
        return []
    x = f.get("stub_x", 100)
    i0, i1 = f.get("between", [0, 1])
    y0 = belt_ys[i0] + 12
    y1 = belt_ys[i1] - 12
    bw = min(belt_ws[i0], 13)
    pts = [[round(x + _j(rng, -2, 2), 1), round(y0, 1)],
           [round(x + _j(rng, -2, 2), 1), round((y0 + y1) / 2, 1)],
           [round(x + _j(rng, -2, 2), 1), round(y1, 1)]]
    return [{"type": "belt", "width": bw, "line": pts}]


# ---------------------------------------------------------------- road and tracks

def road_feature(concept, rng):
    specs = concept.get("roads") or ([concept["road"]] if concept.get("road") else [])
    return [line_feature(rng, r, "road", 5) for r in specs]


def track_features(concept, rng, belt_ys, rows):
    """One track per lane: from the top edge through a gap of every belt row down to
    the base. In each lower row the track takes the gap nearest to its current x.
    Extra tracks (Highway) run at random x."""
    w, h = concept["size"]
    t = concept.get("tracks", {})
    mode = t.get("mode", "gaps") if isinstance(t, dict) else t
    feats = []
    if isinstance(t, dict) and t.get("lines"):
        return [{"type": "tracks", "line": wavy(rng, ln, 3)} for ln in t["lines"]]
    if mode in ("gaps", "gap") and rows:
        for g0 in rows[0]:
            x = g0
            pts = [[round(x + _j(rng, -6, 6), 1), -4.0]]
            for i, y in enumerate(belt_ys):
                gx = min(rows[i], key=lambda g: abs(g - x))
                pts.append([round(x + _j(rng, -8, 8), 1), round(y - 14 - _j(rng, 0, 8), 1)])
                pts.append([round(gx + _j(rng, -4, 4), 1), round(y + _j(rng, -3, 3), 1)])
                x = gx
            pts.append([round(x + _j(rng, -10, 10), 1), h + 4.0])
            feats.append({"type": "tracks", "line": pts})
    extra = t.get("extra", 0) if isinstance(t, dict) else 0
    for _ in range(extra):
        x = _j(rng, 30, w - 30)
        pts = [[round(x, 1), -4.0],
               [round(x + _j(rng, -25, 25), 1), round(h * 0.33, 1)],
               [round(x + _j(rng, -25, 25), 1), round(h * 0.66, 1)],
               [round(x + _j(rng, -15, 15), 1), h + 4.0]]
        feats.append({"type": "tracks", "line": pts})
    return feats


# ---------------------------------------------------------------- small cover

def blob(rng, x, y, r, n=18, ry=None):
    """A closed blob around (x, y): radius r in x, ry (default r * 0.82) in y."""
    ry = r * 0.82 if ry is None else ry
    pts = []
    for k in range(n):
        a = 2 * math.pi * k / n
        rr = r * _j(rng, 0.72, 1.18)
        pts.append([round(x + rr * math.cos(a), 1), round(y + ry * (rr / r) * math.sin(a), 1)])
    return pts


def ellipse(rng, x, y, rx, ry, n=18, jit=(0.85, 1.12)):
    """A closed ellipse around (x, y) with radii rx and ry."""
    pts = []
    for k in range(n):
        a = 2 * math.pi * k / n
        rr = _j(rng, *jit)
        pts.append([round(x + rx * rr * math.cos(a), 1), round(y + ry * rr * math.sin(a), 1)])
    return pts


def wavy(rng, pts, wiggle):
    """Jitter the interior points of a polyline (the ends stay put)."""
    if wiggle <= 0:
        return [[float(x), float(y)] for x, y in pts]
    out = [[float(pts[0][0]), float(pts[0][1])]]
    for x, y in pts[1:-1]:
        out.append([round(x + _j(rng, -wiggle, wiggle), 1), round(y + _j(rng, -wiggle, wiggle), 1)])
    out.append([float(pts[-1][0]), float(pts[-1][1])])
    return out


def line_feature(rng, spec, ftype, default_width):
    """One line feature from a spec: an explicit "line" (optionally jittered) or a
    generated shape ("across": a wavy line edge to edge at y; "vertical": top to bottom at x)."""
    width = spec.get("width", default_width)
    if spec.get("line"):
        return {"type": ftype, "line": wavy(rng, spec["line"], spec.get("wiggle", 0)), "width": width}
    shape = spec.get("shape", "across")
    w, h = RULES["field_size"]
    if shape == "across":
        y = spec.get("y", h / 2)
        wig = spec.get("wiggle", 10)
        pts = [[-6.0, y]]
        for i in range(1, 5):
            pts.append([round(w * i / 5, 1), round(y + _j(rng, -wig, wig), 1)])
        pts.append([w + 6.0, y])
        return {"type": ftype, "line": pts, "width": width}
    if shape == "vertical":
        x = spec.get("x", w / 2)
        wig = spec.get("wiggle", 8)
        pts = [[x, -6.0]]
        for i in range(1, 5):
            pts.append([round(x + _j(rng, -wig, wig), 1), round(h * i / 5, 1)])
        pts.append([x, h + 6.0])
        return {"type": ftype, "line": pts, "width": width}
    raise ValueError(f"unknown line shape {shape!r} for {ftype}")


def grove_features(concept, rng):
    feats = []
    for g in concept.get("groves", []):
        if "rx" in g:
            feats.append({"type": "grove", "poly": ellipse(rng, g["x"], g["y"], g["rx"], g.get("ry", g["rx"] * 0.8))})
        else:
            feats.append({"type": "grove", "poly": blob(rng, g["x"], g["y"], g.get("r", 12))})
    sc = concept.get("grove_scatter")
    if sc:
        x0, y0, x1, y1 = sc["zone"]
        rlo, rhi = sc.get("r", [6, 16])
        for _ in range(sc.get("count", 10)):
            feats.append({"type": "grove", "poly": blob(rng, _j(rng, x0, x1), _j(rng, y0, y1), _j(rng, rlo, rhi))})
    return feats


# ---------------------------------------------------------------- rivers and crossings


def river_features(concept, rng):
    """Rivers: line + width. Vehicles are blocked, infantry pays 4 (2 on a ford or in a pipe).
    A river only forces a crossing when it spans the field from edge to edge."""
    return [line_feature(rng, r, "river", 16) for r in concept.get("rivers", [])]


def ford_features(concept, rng):
    """Fords: blobs on a river where vehicles cross (move cost 2 for both classes).
    Elongated along the crossing (ry) and with tight jitter, so the ford cells always
    reach both banks through the vehicle clearance rule."""
    feats = []
    for f in concept.get("fords", []):
        feats.append({"type": "ford", "poly": ellipse(rng, f["x"], f["y"], f.get("r", 9), f.get("ry", 15),
                                                     n=16, jit=(0.95, 1.05))})
    return feats


def pipe_features(concept, rng):
    """Pipes: a short line + width across a river. Infantry crosses at cost 2, vehicles cannot."""
    return [line_feature(rng, p, "pipe", 6) for p in concept.get("pipes", [])]


def bridge_ruins_features(concept, rng):
    """Ruined bridges: decoration only (the Sim ignores the type); the road is split at
    the banks, so vehicles cannot use a ruined bridge."""
    return [line_feature(rng, b, "bridge_ruins", 7) for b in concept.get("bridge_ruins", [])]


# ---------------------------------------------------------------- hills, gullies, banks


def hill_features(concept, rng):
    """Hills: blobs. On top: more range and sight, enemy fire less accurate; behind: cover.
    "ridges" places a row of hills (the saddles are the gaps between them);
    "slopes" are long ellipses from the attacker side down the field."""
    feats = []
    for hh in concept.get("hills", []):
        if "rx" in hh:
            feats.append({"type": "hill", "poly": ellipse(rng, hh["x"], hh["y"], hh["rx"], hh.get("ry", hh["rx"] * 0.8))})
        else:
            feats.append({"type": "hill", "poly": blob(rng, hh["x"], hh["y"], hh.get("r", 18))})
    for rg in concept.get("ridges", []):
        y = rg["y"]
        for x in rg["xs"]:
            feats.append({"type": "hill", "poly": blob(rng, x, y + _j(rng, -6, 6), rg.get("r", 15))})
    for sl in concept.get("slopes", []):
        feats.append({"type": "hill", "poly": ellipse(rng, sl["x"], sl["y"], sl["rx"], sl.get("ry", sl["rx"]))})
    return feats


def balka_features(concept, rng):
    """Gullies (balka): line + width. Infantry 1.2 (a hidden route), vehicles blocked."""
    return [line_feature(rng, b, "balka", 8) for b in concept.get("balkas", [])]


def embankment_features(concept, rng):
    """Embankments: line + width. Infantry 2, vehicles blocked."""
    return [line_feature(rng, e, "embankment", 5) for e in concept.get("embankments", [])]


# ---------------------------------------------------------------- objects and scatter


def _blob_list(rng, items, ftype):
    feats = []
    for it in items:
        if "line" in it:
            feats.append({"type": ftype, "line": wavy(rng, it["line"], it.get("wiggle", 2)),
                          "width": it.get("width", 4)})
        else:
            feats.append({"type": ftype, "poly": blob(rng, it["x"], it["y"], it.get("r", 4))})
    return feats


def object_features(concept, rng):
    """Rocks, wrecks, bushes, hedgehogs (objects from obstacles.csv) and heaps (terrain).
    A "wreck_column" places a row of wrecks along a line: a destroyed column of vehicles."""
    feats = []
    for key, ftype in (("rocks", "rock"), ("wrecks", "wreck"), ("bushes", "bush"),
                       ("hedgehogs", "hedgehog"), ("heaps", "heap")):
        feats += _blob_list(rng, concept.get(key, []), ftype)
    for col in concept.get("wreck_columns", []):
        x0, y0 = col["from"]
        x1, y1 = col["to"]
        n = col.get("count", 8)
        r = col.get("r", 4)
        for i in range(n):
            t = i / (n - 1) if n > 1 else 0.5
            x = x0 + (x1 - x0) * t + _j(rng, -2, 2)
            y = y0 + (y1 - y0) * t + _j(rng, -2, 2)
            feats.append({"type": "wreck", "poly": blob(rng, x, y, r)})
    return feats


def trench_features(concept, rng):
    """Trench lines: cover inside, move cost 1 for both classes (old trench lines on a map)."""
    return [line_feature(rng, t, "trench", 4) for t in concept.get("trenches", [])]


def wire_features(concept, rng):
    """Barbed wire: infantry 6, vehicles 2."""
    return [line_feature(rng, w, "wire", 2) for w in concept.get("wires", [])]


def scatter_features(concept, rng):
    """Bush zones: "sunflower" is the big crop field of map 17 - a zone full of bushes.
    Bushes hide what is behind them but give no cover; the Sim rule this map needs is a
    crop that blocks sight without cover, until Claude adds it bushes stand in (issue #46).
    "bush_zones" scatters single bushes over a zone (open steppe)."""
    zones = list(concept.get("bush_zones", []))
    if concept.get("sunflower"):
        zones.append(concept["sunflower"])
    feats = []
    for s in zones:
        x0, y0, x1, y1 = s["zone"]
        rlo, rhi = s.get("r", [2.5, 4.5])
        ftype = s.get("type", "bush")
        for _ in range(s.get("count", 40)):
            feats.append({"type": ftype, "poly": blob(rng, _j(rng, x0, x1), _j(rng, y0, y1), _j(rng, rlo, rhi), n=10)})
    return feats


def pond_feature(concept, rng):
    p = concept.get("pond")
    if not p:
        return []
    return [{"type": "pond", "poly": blob(rng, p["x"], p["y"], p.get("r", 10), n=16)}]


def crater_features(concept, rng):
    w, h = concept["size"]
    n = concept.get("craters", 60)
    lo, hi = RULES["craters_radius"]
    pts = []
    for _ in range(n):
        pts.append([round(_j(rng, 4, w - 4), 1), round(_j(rng, 4, h - 4), 1), round(_j(rng, lo, hi), 1)])
    return [{"type": "craters", "points": pts}]


# ---------------------------------------------------------------- assembly

def generate(concept):
    """Build one field dict (the Forest belts format) from a concept parameter set."""
    rng = _rng(concept)
    size = concept.get("size", RULES["field_size"])
    concept = dict(concept)
    concept["size"] = size
    w, h = size

    belt_ys, belt_ws = belt_rows(concept, rng)
    rows = belt_gaps(concept, rng, belt_ys)
    widths = gap_widths(concept, rng, rows)
    # a belt may not eat its gaps: effective opening = gap width - belt width >= 4 m
    # (in Forest belts the openings are 3-4 m; the Sim cell is 2 m)
    for i, ws in enumerate(belt_ws):
        belt_ws[i] = round(min(ws, min(widths[i]) - 4, 16), 1)

    features = []
    features += field_strips(concept, rng, belt_ys, belt_ws)
    features += river_features(concept, rng)
    features += track_features(concept, rng, belt_ys, rows)
    features += crater_features(concept, rng)
    features += hill_features(concept, rng)
    features += balka_features(concept, rng)
    features += embankment_features(concept, rng)
    features += object_features(concept, rng)
    features += scatter_features(concept, rng)
    features += grove_features(concept, rng)
    features += pond_feature(concept, rng)
    features += belt_features(concept, rng, belt_ys, belt_ws, rows, widths)
    features += [line_feature(rng, b, "belt", 14) for b in concept.get("belt_lines", [])]
    features += pocket_feature(concept, rng, belt_ys, belt_ws, rows, widths)
    features += fork_feature(concept, rng, belt_ys, belt_ws)
    features += road_feature(concept, rng)
    features += ford_features(concept, rng)
    features += pipe_features(concept, rng)
    features += bridge_ruins_features(concept, rng)
    features += trench_features(concept, rng)
    features += wire_features(concept, rng)

    spawns = concept.get("spawns")
    if not spawns:
        if rows:
            spawns = [round(g, 1) for g in rows[0]]
            if len(spawns) > 2:
                spawns = spawns[:2]
        else:
            spawns = [round(w * 0.33, 1), round(w * 0.66, 1)]

    field = {
        "name": concept["name"],
        "sector": concept.get("sector", "Sector 07"),
        "size": [w, h],
        "spawns": spawns,
        "subtitle": concept.get("subtitle", ""),
        "features": features,
    }
    # keep the layout for the checks and the preview
    field["_layout"] = {"belt_ys": belt_ys, "belt_ws": belt_ws, "gaps": rows, "gap_widths": widths,
                        "rivers": [f["line"] for f in features if f["type"] == "river"]}
    return field


def strip_layout(field):
    """Field dict without the generator-only _layout key (what goes into the JSON file)."""
    return {k: v for k, v in field.items() if not k.startswith("_")}
