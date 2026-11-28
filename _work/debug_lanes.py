import json, os, sys
HERE = os.path.abspath('tools/maps')
ROOT = os.path.abspath('.')
sys.path.insert(0, HERE)
import sim_routes

name = sys.argv[1]
field = json.load(open(os.path.join(ROOT, 'unity/Assets/Resources/Fields', name + '.json'), encoding='utf-8'))
obstacles = sim_routes.load_obstacles(ROOT)
grid = sim_routes.NavGrid(field, obstacles, edge_margin=10.0)
h = field['size'][1]
for cls, cost in (('inf', grid.cost_i), ('veh', grid.cost_v)):
    dist, nxt = sim_routes.flow_field(grid, cost)
    xs = []
    for i in range(15):
        sx = grid.w * (i + 0.5) / 15
        path = sim_routes.trace(grid, dist, nxt, grid.cell_at(sx, 1.0))
        if not path:
            print(cls, 'x=%.0f no path' % sx)
            continue
        cross = None
        for cell in path:
            y = grid.cy(cell)
            if h * 0.25 <= y <= h * 0.75 and (cross is None or y >= h * 0.5):
                cross = grid.cx(cell)
        xs.append((round(sx), round(cross) if cross else None, len(path)))
    print(cls, xs)
