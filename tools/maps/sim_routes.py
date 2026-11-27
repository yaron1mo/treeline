#!/usr/bin/env python3
"""Python port of the Sim routing used by the Balance run (tools/balance/Routes.cs).

NavGrid (2 m cells, terrain costs, vehicle clearance, 10 m closed edge strips),
FlowField (Dijkstra to the rear edge) and Routes.Of (lanes where traced infantry and
vehicle routes cross the placing zone, grouped within 25 m, at most 3).

Used by tools/maps/check.py to verify locally that a generated map gives the number
of lanes the design asks for, without waiting for a Balance run. The port follows
unity/Assets/Scripts/Sim/NavGrid.cs, FlowField.cs and tools/balance/Routes.cs line by
line; the numbers it prints are the same the C# tools compute.
"""

import csv
import heapq
import math
import os

CELL = 2.0
INF = float("inf")

# TerrainKind order in NavGrid.cs: later kinds paint over earlier ones.
TERRAIN_ORDER = ["open", "hill", "heap", "grove", "belt", "pond", "river",
                 "embankment", "balka", "pipe", "road", "ford"]
# TerrainTable fallback costs (NavGrid.cs), used when obstacles.csv has no row.
FALLBACK_INF = {"open": 1, "hill": 1.6, "heap": 3, "grove": 2, "belt": 2, "pond": INF,
                "river": 4, "embankment": 2, "balka": 1.2, "pipe": 2, "road": 0.8, "ford": 2}
FALLBACK_VEH = {"open": 1, "hill": 1.8, "heap": INF, "grove": INF, "belt": 3, "pond": INF,
                "river": INF, "embankment": INF, "balka": INF, "pipe": INF, "road": 0.5, "ford": 2}


def load_obstacles(root):
    """move costs from unity/Assets/Data/obstacles.csv: id -> (infantry, vehicle)."""
    path = os.path.join(root, "unity", "Assets", "Data", "obstacles.csv")
    out = {}
    with open(path, newline="", encoding="utf-8") as fh:
        for r in csv.DictReader(fh):
            def num(s):
                s = (s or "").strip()
                return INF if s.lower() == "blocked" else (float(s) if s else None)
            out[r["id"]] = (num(r["move_cost_infantry"]), num(r["move_cost_vehicle"]))
    return out


def _dist_seg(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    l2 = dx * dx + dy * dy
    t = 0.0 if l2 == 0 else max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / l2))
    qx, qy = ax + t * dx - px, ay + t * dy - py
    return math.hypot(qx, qy)


def _dist_line(px, py, pts):
    return min(_dist_seg(px, py, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1])
               for i in range(len(pts) - 1))


def _in_poly(px, py, pts):
    c = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi, xj, yj = pts[i][0], pts[i][1], pts[j][0], pts[j][1]
        if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi) + xi:
            c = not c
        j = i
    return c


class NavGrid:
    def __init__(self, field, obstacles, edge_margin=10.0):
        self.w = field["size"][0]
        self.h = field["size"][1]
        self.cols = int(round(self.w / CELL))
        self.rows = int(round(self.h / CELL))
        self.spawns = field.get("spawns") or []
        self.cells = ["open"] * (self.cols * self.rows)
        raw_i = [0.0] * (self.cols * self.rows)
        raw_v = [0.0] * (self.cols * self.rows)
        shapes = []
        for f in field["features"]:
            t = f["type"]
            if t not in TERRAIN_ORDER or t == "open":
                continue
            pts = f.get("poly") or f.get("line")
            if not pts:
                continue
            pad = 0.0 if f.get("poly") else max(f.get("width", 0) / 2, CELL * 0.75)
            x0 = min(p[0] for p in pts) - pad
            x1 = max(p[0] for p in pts) + pad
            y0 = min(p[1] for p in pts) - pad
            y1 = max(p[1] for p in pts) + pad
            shapes.append((TERRAIN_ORDER.index(t), f, pts, x0, y0, x1, y1))
        shapes.sort(key=lambda s: s[0])
        for r in range(self.rows):
            for c in range(self.cols):
                x = (c + 0.5) * CELL
                y = (r + 0.5) * CELL
                t = "open"
                for _, f, pts, x0, y0, x1, y1 in shapes:
                    if not (x0 <= x <= x1 and y0 <= y <= y1):
                        continue
                    if f.get("poly"):
                        if _in_poly(x, y, pts):
                            t = f["type"]
                    elif _dist_line(x, y, pts) <= max(f.get("width", 0) / 2, CELL * 0.75):
                        t = f["type"]
                i = r * self.cols + c
                self.cells[i] = t
                oi, ov = obstacles.get(t, (None, None))
                raw_i[i] = FALLBACK_INF[t] if oi is None else oi
                raw_v[i] = FALLBACK_VEH[t] if ov is None else ov
        # objects (rock, wreck, ...) paint over the terrain; none of the designed
        # group A features are objects, so this is a no-op for them
        for f in field["features"]:
            t = f["type"]
            if t in TERRAIN_ORDER:
                continue
            if t not in obstacles:
                continue
            oi, ov = obstacles[t]
            if oi is None and ov is None:
                continue
            pts = f.get("poly") or f.get("line")
            if not pts:
                continue
            pad = 0.0 if f.get("poly") else max(f.get("width", 0) / 2, CELL * 0.75)
            for r in range(self.rows):
                for c in range(self.cols):
                    x = (c + 0.5) * CELL
                    y = (r + 0.5) * CELL
                    if not (min(p[0] for p in pts) - pad <= x <= max(p[0] for p in pts) + pad
                            and min(p[1] for p in pts) - pad <= y <= max(p[1] for p in pts) + pad):
                        continue
                    covered = _in_poly(x, y, pts) if f.get("poly") else _dist_line(x, y, pts) <= pad
                    if covered:
                        i = r * self.cols + c
                        if oi is not None:
                            raw_i[i] = oi
                        if ov is not None:
                            raw_v[i] = ov
        # closed side strips (battle_rules edge_margin_m)
        if edge_margin > 0:
            for r in range(self.rows):
                for c in range(self.cols):
                    x = (c + 0.5) * CELL
                    if x < edge_margin or x > self.w - edge_margin:
                        i = r * self.cols + c
                        raw_i[i] = INF
                        raw_v[i] = INF
        self.cost_i = list(raw_i)
        self.cost_v = self._clearance(raw_v)

    def _clearance(self, src):
        out = list(src)
        for r in range(self.rows):
            for c in range(self.cols):
                i = r * self.cols + c
                v = src[i]
                if v != INF and self.cells[i] not in ("road", "ford"):
                    if ((c > 0 and src[i - 1] == INF) or (c < self.cols - 1 and src[i + 1] == INF)
                            or (r > 0 and src[i - self.cols] == INF)
                            or (r < self.rows - 1 and src[i + self.cols] == INF)):
                        out[i] = INF
        return out

    def cell_at(self, x, y):
        c = min(self.cols - 1, max(0, int(x // CELL)))
        r = min(self.rows - 1, max(0, int(y // CELL)))
        return r * self.cols + c

    def cx(self, cell):
        return (cell % self.cols + 0.5) * CELL

    def cy(self, cell):
        return (cell // self.cols + 0.5) * CELL


def flow_field(grid, cost):
    """FlowField.cs: Dist/Next to the rear edge (bottom row)."""
    n = grid.cols * grid.rows
    dist = [INF] * n
    nxt = [-1] * n
    heap = []
    for c in range(grid.cols):
        i = (grid.rows - 1) * grid.cols + c
        if cost[i] == INF:
            continue
        dist[i] = cost[i] / 2.0
        nxt[i] = -2
        heapq.heappush(heap, (dist[i], i))
    dx = [1, -1, 0, 0, 1, 1, -1, -1]
    dy = [0, 0, 1, -1, 1, -1, 1, -1]
    while heap:
        d, i = heapq.heappop(heap)
        if d > dist[i]:
            continue
        c0, r0 = i % grid.cols, i // grid.cols
        for m in range(8):
            nc, nr = c0 + dx[m], r0 + dy[m]
            if nc < 0 or nc >= grid.cols or nr < 0 or nr >= grid.rows:
                continue
            j = nr * grid.cols + nc
            if cost[j] == INF:
                continue
            diag = dx[m] != 0 and dy[m] != 0
            if diag and (cost[r0 * grid.cols + nc] == INF or cost[nr * grid.cols + c0] == INF):
                continue
            nd = d + (cost[i] + cost[j]) / 2.0 * (math.sqrt(2.0) if diag else 1.0)
            if nd < dist[j]:
                dist[j] = nd
                nxt[j] = i
                heapq.heappush(heap, (nd, j))
    return dist, nxt


def trace(grid, dist, nxt, start):
    path = []
    if dist[start] == INF:
        return path
    cur = start
    guard = 0
    while cur >= 0 and guard < len(dist):
        path.append(cur)
        cur = nxt[cur]
        guard += 1
    return path


def lanes(grid, zone_top, zone_bottom):
    """Routes.Of: lanes where traced routes cross the placing zone, busiest first."""
    starts = grid.spawns if grid.spawns else [grid.w * (i + 0.5) / 15 for i in range(15)]
    mid_y = (zone_top + zone_bottom) / 2
    out = []
    for cost in (grid.cost_i, grid.cost_v):
        dist, nxt = flow_field(grid, cost)
        for sx in starts:
            path = trace(grid, dist, nxt, grid.cell_at(sx, CELL * 0.5))
            if not path:
                continue
            cross_x = None
            pts = []
            for cell in path:
                x, y = grid.cx(cell), grid.cy(cell)
                if y < zone_top or y > zone_bottom:
                    continue
                pts.append((x, y))
                if cross_x is None and y >= mid_y:
                    cross_x = x
            if cross_x is None:
                continue
            lane = None
            for l in out:
                if abs(l["x"] - cross_x) < 25:
                    lane = l
                    break
            if lane is None:
                lane = {"x": cross_x, "weight": 0, "points": []}
                out.append(lane)
            lane["weight"] += 1
            lane["points"] += pts
    out.sort(key=lambda l: -l["weight"])
    return out[:3] if len(out) <= 3 else out[:3]


def analyse(root, field):
    """Lanes of one field file: as the Balance run's WaveCost sees them (placing zone
    0.6-0.9 h) and mid-field (0.25-0.75 h), where the design's lanes are visible."""
    obstacles = load_obstacles(root)
    grid = NavGrid(field, obstacles, edge_margin=10.0)
    h = field["size"][1]
    zone = lanes(grid, h * 0.6, h * 0.9)
    mid = lanes(grid, h * 0.25, h * 0.75)
    return {
        "placing": [{"x": round(l["x"], 1), "weight": l["weight"]} for l in zone],
        "mid": [{"x": round(l["x"], 1), "weight": l["weight"]} for l in mid],
    }
