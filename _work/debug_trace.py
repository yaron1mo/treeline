import json, os, sys
HERE = os.path.abspath('tools/maps')
ROOT = os.path.abspath('.')
sys.path.insert(0, HERE)
import sim_routes

name = sys.argv[1]
x = float(sys.argv[2]) if len(sys.argv) > 2 else None
field = json.load(open(os.path.join(ROOT, 'unity/Assets/Resources/Fields', name + '.json'), encoding='utf-8'))
obstacles = sim_routes.load_obstacles(ROOT)
grid = sim_routes.NavGrid(field, obstacles, edge_margin=10.0)
cost = grid.cost_v
if x is None:
    x = (field.get('spawns') or [100])[0]
dist, nxt = sim_routes.flow_field(grid, cost)
path = sim_routes.trace(grid, dist, nxt, grid.cell_at(x, 1.0))
print(name, 'veh path from x=%.0f, cells=%d' % (x, len(path)))
prev = None
for cell in path:
    cx, cy = grid.cx(cell), grid.cy(cell)
    t = grid.cells[cell]
    if t != prev:
        print('  y=%6.1f x=%6.1f terrain=%s cost=%s' % (cy, cx, t, 'INF' if cost[cell] == sim_routes.INF else round(cost[cell], 2)))
        prev = t
