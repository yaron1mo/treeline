import json, os, sys
HERE = os.path.abspath('tools/maps')
ROOT = os.path.abspath('.')
sys.path.insert(0, HERE)
import sim_routes

for name in sys.argv[1:]:
    field = json.load(open(os.path.join(ROOT, 'unity/Assets/Resources/Fields', name + '.json'), encoding='utf-8'))
    obstacles = sim_routes.load_obstacles(ROOT)
    grid = sim_routes.NavGrid(field, obstacles, edge_margin=10.0)
    dist, _ = sim_routes.flow_field(grid, grid.cost_v)
    print('===', name, 'veh-reachable (.) vs not (#), 10 m cells; top = enemy edge')
    for r in range(0, grid.rows, 5):
        line = ''
        for c in range(0, grid.cols, 5):
            reach = any(dist[(r + rr) * grid.cols + (c + cc)] != sim_routes.INF
                        for rr in range(5) for cc in range(5))
            line += '.' if reach else '#'
        print(line)
