

def sheet_spawns(grid, w):
    """Attacker entries: where a road or a field track meets the top edge."""
    xs = set()
    for r in range(2):
        for c in range(SHEET_COLS):
            if grid[r][c] in "=:" and grid[0][c] not in "RFpQ":
                xs.add((c + 0.5) * SHEET_CELL)
    xs = sorted(xs)
    if len(xs) > 3:
        step = (len(xs) - 1) / 2.0
        xs = [xs[round(i * step)] for i in range(3)]
    margin = RULES["edge_margin"]
    return [round(min(max(x, margin), w - margin), 1) for x in xs]


def sheet_field(concept):
    """Build one field dict from the concept sheet named by concept['sheet']."""
    path = os.path.join(ROOT, concept["sheet"])
    grid = resolve_markers(parse_sheet(path))
    rng = random.Random(concept.get("seed", 1))
    w, h = concept.get("size", RULES["field_size"])
    features = sheet_features(grid, rng)
    spawns = concept.get("spawns") or sheet_spawns(grid, w)
    field = {
        "name": concept["name"],
        "sector": concept.get("sector", "Sector 07"),
        "size": [w, h],
        "subtitle": concept.get("subtitle", ""),
        "features": features,
    }
    if spawns:
        field["spawns"] = spawns
    field["_layout"] = {"sheet": concept["sheet"], "grid": grid, "spawns": spawns}
    return field
