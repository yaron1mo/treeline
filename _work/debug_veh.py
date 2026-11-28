import json, os, sys
HERE = os.path.abspath('tools/maps')
ROOT = os.path.abspath('.')
sys.path.insert(0, HERE)
import sim_routes

for name in sys.argv[1:]:
    field = json.load(open(os.path.join(ROOT, 'unity/Assets/Resources/Fields', name + '.json'), encoding='utf-8'))
    obstacles = sim_routes.load_obstacles(ROOT)
    grid = sim_routes.NavGrid(field, obstacles, edge_margin=10.0)
    print('===', name, 'spawns', field.get('spawns'))
    # 20x40 ascii: 10m cells, V = vehicle-passable, x = vehicle-blocked, i = inf-only, . = open
    for r in range(0, grid.rows, 5):
        line = ''
        for c in range(0, grid.cols, 5):
            i = r * grid.cols + c
            vi = grid.cost_v[i]
            ii = grid.cost_i[i]
            if vi == sim_routes.INF and ii == sim_routes.INF:
                line += '#'
            elif vi == sim_routes.INF:
                line += 'i'
            elif ii == sim_routes.INF:
                line += 'V'  # vehicles only (should not happen)
            else:
                line += '.'
        print(line)
    dist, _ = sim_routes.flow_field(grid, grid.cost_v)
    starts = field.get('spawns') or [grid.w * (k + 0.5) / 15 for k in range(15)]
    for s in starts:
        d = dist[grid.cell_at(s, 1.0)]
        print('  spawn x=%.0f veh dist=%s' % (s, 'INF' if d == sim_routes.INF else round(d, 1)))
