using System.Collections.Generic;
using UnityEngine;
using Treeline.Sim;

namespace Treeline.Game
{
    /// <summary>
    /// Debug view of the Sim: loads the field files, builds the hidden nav grid and flow fields,
    /// and shows terrain plus the routes units would take. Press Play in any scene; it creates itself.
    /// Throw-away tooling: the real drawing lives in Treeline.Render.
    /// </summary>
    public sealed class FieldDebugView : MonoBehaviour
    {
        const float GameSpeedMps = 6f; // for the time estimate only

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<FieldDebugView>() != null) return;
            new GameObject("FieldDebugView").AddComponent<FieldDebugView>();
        }

        readonly List<Field> fields = new List<Field>();
        int current;
        UnitClass cls = UnitClass.Infantry;

        NavGrid grid;
        FlowField infantryFlow, vehicleFlow;
        Texture2D tex;
        SpriteRenderer sr;
        string status = "";

        // Vision test: an observer you move with the buttons; shows what it sees (needs Data/obstacles.csv, editor only).
        const float BaseSight = 30f;
        const float ObserverStep = 4f;
        ObstacleTable obstacles;
        VisionMap vision;
        bool showVision;

        // Battle test: deploy defenders at the cursor (the same red dot), start the round, move them.
        UnitTable unitTable;
        Battle battle;
        ArmourTable armourTable;
        BattleRules battleRules;
        int flowVersion;
        int simSpeed = 1;
        bool battleMode;
        int lastDefenderId;
        string battleMessage = "";
        float obsX, obsY;

        void Start()
        {
            foreach (var asset in Resources.LoadAll<TextAsset>("Fields"))
            {
                try { fields.Add(FieldParser.Parse(asset.text)); }
                catch (System.Exception e) { Debug.LogError($"Field '{asset.name}': {e.Message}"); }
            }
            if (fields.Count == 0) { status = "No field files found in Resources/Fields"; return; }

            string obstaclePath = System.IO.Path.Combine(Application.dataPath, "Data", "obstacles.csv");
            try { if (System.IO.File.Exists(obstaclePath)) obstacles = ObstacleTable.Parse(System.IO.File.ReadAllText(obstaclePath)); }
            catch (System.Exception e) { Debug.LogError($"obstacles.csv: {e.Message}"); }

            string unitPath = System.IO.Path.Combine(Application.dataPath, "Data", "units.csv");
            try { if (System.IO.File.Exists(unitPath)) unitTable = UnitTable.Parse(System.IO.File.ReadAllText(unitPath)); }
            catch (System.Exception e) { Debug.LogError($"units.csv: {e.Message}"); }

            string armourPath = System.IO.Path.Combine(Application.dataPath, "Data", "armour.csv");
            try { if (System.IO.File.Exists(armourPath)) armourTable = ArmourTable.Parse(System.IO.File.ReadAllText(armourPath)); }
            catch (System.Exception e) { Debug.LogError($"armour.csv: {e.Message}"); }

            string rulesPath = System.IO.Path.Combine(Application.dataPath, "Data", "battle_rules.csv");
            try { if (System.IO.File.Exists(rulesPath)) battleRules = BattleRules.Parse(System.IO.File.ReadAllText(rulesPath)); }
            catch (System.Exception e) { Debug.LogError($"battle_rules.csv: {e.Message}"); }

            sr = new GameObject("Terrain").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            Load(0);
        }

        void Load(int index)
        {
            current = (index + fields.Count) % fields.Count;
            var field = fields[current];

            var sw = System.Diagnostics.Stopwatch.StartNew();
            grid = new NavGrid(field, obstacles);
            infantryFlow = new FlowField(grid, UnitClass.Infantry);
            vehicleFlow = new FlowField(grid, UnitClass.Vehicle);
            flowVersion = grid.Version;
            vision = obstacles != null ? new VisionMap(field, obstacles) : null;
            battle = unitTable != null ? new Battle(grid, unitTable, 3000f, vision, armourTable, -1f, 1, battleRules) : null;
            lastDefenderId = 0;
            battleMessage = "";
            obsX = field.Width / 2f;
            obsY = field.Height / 2f;
            sw.Stop();
            Debug.Log($"Field '{field.Name}': {grid.Cols}x{grid.Rows} cells, nav built in {sw.ElapsedMilliseconds} ms");

            Redraw();
            FitCamera(field);
        }

        void Redraw()
        {
            if (grid.Version != flowVersion)
            {
                // A wreck changed the ground: rebuild the routes shown.
                infantryFlow = new FlowField(grid, UnitClass.Infantry);
                vehicleFlow = new FlowField(grid, UnitClass.Vehicle);
                flowVersion = grid.Version;
            }
            var flow = cls == UnitClass.Infantry ? infantryFlow : vehicleFlow;
            var cost = grid.Cost(cls);

            if (tex == null || tex.width != grid.Cols || tex.height != grid.Rows)
            {
                tex = new Texture2D(grid.Cols, grid.Rows, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                // 1 pixel = one 2 m cell, so 1 world unit = 1 metre.
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, grid.Cols, grid.Rows), new Vector2(0.5f, 0.5f), 1f / NavGrid.CellSize);
            }

            var pixels = new Color32[grid.Count];
            for (int i = 0; i < grid.Count; i++)
            {
                Color c = TerrainColor(grid.Cells[i]);
                if (float.IsPositiveInfinity(cost[i])) c = Color.Lerp(c, new Color(0.85f, 0.2f, 0.15f), 0.45f);
                pixels[Index(i)] = c;
            }

            // Obstacles that are not nav terrain (rocks, bushes, wrecks) and the observer's view.
            if (vision != null)
            {
                for (int i = 0; i < grid.Count; i++)
                {
                    var ob = vision.ObstacleInCell(i);
                    if (ob != null && !TerrainTable.TryParse(ob.Id, out _)) pixels[Index(i)] = ObjectColor(ob.Id);
                }

                if (showVision)
                {
                    var seen = vision.Compute(obsX, obsY, BaseSight);
                    float radius = vision.SightRadius(obsX, obsY, BaseSight);
                    var shade = new Color(0.04f, 0.05f, 0.03f);
                    var lit = new Color(0.75f, 1f, 0.55f);
                    for (int i = 0; i < grid.Count; i++)
                    {
                        float dx = grid.CenterX(i) - obsX, dy = grid.CenterY(i) - obsY;
                        if (dx * dx + dy * dy > radius * radius) continue;
                        Color baseColor = pixels[Index(i)];
                        pixels[Index(i)] = Color.Lerp(Color.Lerp(baseColor, shade, 0.6f), Color.Lerp(baseColor, lit, 0.3f), seen[i]);
                    }
                    int oc = grid.CellAt(obsX, obsY);
                    for (int dr = -1; dr <= 1; dr++)
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            int cell = oc + dr * grid.Cols + dc;
                            if (cell >= 0 && cell < grid.Count) pixels[Index(cell)] = new Color32(235, 70, 60, 255);
                        }
                }
            }

            // Defenders (blue = standing, light blue = running in) and the cursor.
            if (battle != null && battleMode)
            {
                if (!showVision) PaintBlock(pixels, obsX, obsY, new Color32(235, 70, 60, 255));
                // Each attacker's planned way: yellow = fastest, green = evading known defenders, violet = safest.
                foreach (var a in battle.Attackers)
                {
                    var path = a.PathCells;
                    if (path == null) continue;
                    var col = a.Mode == RouteMode.Evade ? new Color32(120, 220, 120, 255)
                        : a.Mode == RouteMode.Safest ? new Color32(190, 120, 235, 255) : new Color32(240, 214, 90, 255);
                    for (int k = a.PathPosition; k < path.Count; k++) pixels[Index(path[k])] = col;
                }
                foreach (var a in battle.Attackers)
                    PaintBlock(pixels, a.X, a.Y, a.Class == UnitClass.Vehicle
                        ? (a.Dismounting ? new Color32(255, 230, 60, 255) : new Color32(255, 150, 40, 255))
                        : new Color32(235, 70, 60, 255));
                foreach (var d in battle.Defenders)
                    PaintBlock(pixels, d.X, d.Y, d.State == DefenderState.Standing ? new Color32(60, 130, 235, 255) : new Color32(140, 235, 245, 255));
                // Our drones in the air (white).
                foreach (var dr in battle.Drones) PaintBlock(pixels, dr.X, dr.Y, new Color32(255, 255, 255, 255));
            }

            // Routes from the enemy edge, one every ~24 m.
            int shown = 0, blocked = 0;
            double best = double.PositiveInfinity;
            // Where the field names entry points, show exactly those routes; otherwise a sample along the edge.
            var starts = new System.Collections.Generic.List<int>();
            if (grid.SpawnXs.Length > 0) foreach (float sx in grid.SpawnXs) starts.Add(grid.CellAt(sx, 0f));
            else for (int c = 3; c < grid.Cols; c += 12) starts.Add(c);
            bool roundRunning = battle != null && battleMode && battle.Phase == BattlePhase.Round;
            if (roundRunning) starts.Clear();   // during a round the attackers' own routes are drawn instead
            foreach (int c in starts)
            {
                var path = flow.Trace(c);
                if (path.Count == 0) { blocked++; continue; }
                shown++;
                best = System.Math.Min(best, flow.Dist[c]);
                foreach (int cell in path) pixels[Index(cell)] = new Color32(240, 214, 90, 255);
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            string klass = cls == UnitClass.Infantry ? "Infantry" : "Vehicles";
            status = $"{fields[current].Name}  |  {klass}  |  routes: {shown} found, {blocked} blocked  |  fastest ~ {best * NavGrid.CellSize / GameSpeedMps:0} s";
            if (battle != null && battleMode)
                status += $"\nBattle: {battle.Phase}, budget {battle.Budget:0}, defenders {battle.Defenders.Count}, drones {battle.Drones.Count}, run speed {battle.RunSpeedMps:0} m/s. {battleMessage}";
            if (battle != null && battleMode && battle.Phase == BattlePhase.Round)
            {
                int fast = 0, evade = 0, safe = 0;
                foreach (var a in battle.Attackers)
                {
                    if (a.Mode == RouteMode.Evade) evade++;
                    else if (a.Mode == RouteMode.Safest) safe++;
                    else fast++;
                }
                status += $"\nWave {battle.WaveNumber}, {battle.RoundTime:0} s, attackers {battle.Attackers.Count} (+{battle.Pending} to come), line {battle.LineDamage:0.#}/{battle.LineHealth:0}, {battle.Outcome}. Routes: fastest {fast}, evade {evade}, safest {safe}";
            }
            if (showVision && vision != null)
                status += $"\nVision: observer at ({obsX:0}, {obsY:0}), sight radius {vision.SightRadius(obsX, obsY, BaseSight):0} m (dark = hidden, bright = seen)";
        }

        // Field y runs down from the enemy edge; texture y runs up. Flip so the enemy is at the top.
        int Index(int cell)
        {
            int c = cell % grid.Cols, r = cell / grid.Cols;
            return (grid.Rows - 1 - r) * grid.Cols + c;
        }

        void FitCamera(Field field)
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.06f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            float aspect = Mathf.Max(0.1f, (float)Screen.width / Screen.height);
            float needH = field.Height / 2f * 1.06f;
            float needW = field.Width / 2f * 1.06f / aspect;
            cam.orthographicSize = Mathf.Max(needH, needW);
        }

        static Color TerrainColor(TerrainKind t)
        {
            switch (t)
            {
                case TerrainKind.Hill: return Hex(0x5d5c3d);
                case TerrainKind.Heap: return Hex(0x34332d);
                case TerrainKind.Grove: return Hex(0x22301b);
                case TerrainKind.Belt: return Hex(0x2b3a22);
                case TerrainKind.Pond: return Hex(0x3a5058);
                case TerrainKind.River: return Hex(0x3a525c);
                case TerrainKind.Embankment: return Hex(0x7a7458);
                case TerrainKind.Balka: return Hex(0x3d442d);
                case TerrainKind.Pipe: return Hex(0xb9b8aa);
                case TerrainKind.Road: return Hex(0x8a7f5c);
                case TerrainKind.Ford: return Hex(0x5a7178);
                default: return Hex(0x535a3a);
            }
        }

        void PaintBlock(Color32[] pixels, float x, float y, Color32 color)
        {
            int center = grid.CellAt(x, y);
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    int cell = center + dr * grid.Cols + dc;
                    if (cell >= 0 && cell < grid.Count) pixels[Index(cell)] = color;
                }
        }

        void Update()
        {
            if (battle == null || battle.Phase != BattlePhase.Round) return;
            if (battle.Outcome != BattleOutcome.Ongoing) return;
            // Fixed small steps so a fast-forward gives the same result as real time.
            for (int i = 0; i < simSpeed * 3; i++) battle.Step(Time.deltaTime / 3f);
            Redraw();
        }

        void StartWave(params (string id, int count, float delay, float interval)[] waves)
        {
            battle.BeginWave();
            foreach (var w in waves) battle.QueueWave(w.id, w.count, w.delay, w.interval);
            battle.StartRound();
            Redraw();
        }

        void Deploy(string unitId)
        {
            if (battle.TryPlace(unitId, obsX, obsY, out var d, out string why)) { lastDefenderId = d.Id; battleMessage = ""; }
            else battleMessage = why + ".";
            Redraw();
        }

        static Color ObjectColor(string id)
        {
            switch (id)
            {
                case "rock": return Hex(0xa8a8a0);
                case "wreck": return Hex(0x6b4a3a);
                case "bush": return Hex(0x4f8a3a);
                case "trench": return Hex(0x2a2418);
                default: return Hex(0xc080c0);
            }
        }

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        void MoveObserver(float dx, float dy)
        {
            var field = fields[current];
            obsX = Mathf.Clamp(obsX + dx, 1f, field.Width - 1f);
            obsY = Mathf.Clamp(obsY + dy, 1f, field.Height - 1f);
            Redraw();
        }

        void OnGUI()
        {
            float s = Mathf.Max(1f, Screen.height / 540f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            GUILayout.BeginArea(new Rect(8, 8, Screen.width / s - 16, 310));
            GUILayout.Label(status);
            if (fields.Count > 0)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("< Field", GUILayout.Height(34))) Load(current - 1);
                if (GUILayout.Button("Field >", GUILayout.Height(34))) Load(current + 1);
                if (GUILayout.Button(cls == UnitClass.Infantry ? "Show vehicles" : "Show infantry", GUILayout.Height(34)))
                {
                    cls = cls == UnitClass.Infantry ? UnitClass.Vehicle : UnitClass.Infantry;
                    Redraw();
                }
                GUILayout.EndHorizontal();

                if (vision != null)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(showVision ? "Vision off" : "Vision on", GUILayout.Height(34))) { showVision = !showVision; Redraw(); }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                if (battle != null && GUILayout.Button(battleMode ? "Battle off" : "Battle on", GUILayout.Height(34))) { battleMode = !battleMode; Redraw(); }
                if (showVision || battleMode)
                {
                    if (GUILayout.Button("Left", GUILayout.Height(34))) MoveObserver(-ObserverStep, 0);
                    if (GUILayout.Button("Right", GUILayout.Height(34))) MoveObserver(ObserverStep, 0);
                    if (GUILayout.Button("Up", GUILayout.Height(34))) MoveObserver(0, -ObserverStep);
                    if (GUILayout.Button("Down", GUILayout.Height(34))) MoveObserver(0, ObserverStep);
                }
                GUILayout.EndHorizontal();

                if (battle != null && battleMode)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Rifles 100", GUILayout.Height(34))) Deploy("rifle_squad");
                    if (GUILayout.Button("Gunner 200", GUILayout.Height(34))) Deploy("machine_gun");
                    if (GUILayout.Button("Sniper 300", GUILayout.Height(34))) Deploy("sniper");
                    if (GUILayout.Button("Mortar 350", GUILayout.Height(34))) Deploy("mortar");
                    if (GUILayout.Button("Anti-tank 450", GUILayout.Height(34))) Deploy("anti_tank");
                    if (GUILayout.Button("FPV op. 400", GUILayout.Height(34))) Deploy("fpv_operator");
                    if (GUILayout.Button("Bomber op. 450", GUILayout.Height(34))) Deploy("bomber_operator");
                    if (GUILayout.Button("Recon 250", GUILayout.Height(34))) Deploy("recon_drone");
                    if (GUILayout.Button("Move last here", GUILayout.Height(34)))
                    {
                        if (lastDefenderId == 0) battleMessage = "Deploy someone first.";
                        else if (!battle.TryMove(lastDefenderId, obsX, obsY, out string why)) battleMessage = why + ".";
                        else battleMessage = "";
                        Redraw();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.enabled = battle.Outcome != BattleOutcome.Lost;
                    if (GUILayout.Button("Wave: infantry", GUILayout.Height(34)))
                        StartWave(("recruit", 10, 0f, 1.5f), ("regular", 4, 4f, 2f));
                    if (GUILayout.Button("Wave: armour", GUILayout.Height(34)))
                        StartWave(("old_tank", 2, 0f, 6f), ("ifv", 2, 3f, 6f), ("modern_tank", 1, 10f, 1f));
                    if (GUILayout.Button("Wave: transport", GUILayout.Height(34)))
                        StartWave(("motorbike", 4, 0f, 1f), ("buggy", 3, 2f, 1.5f), ("van", 2, 4f, 4f), ("mrap", 2, 6f, 4f));
                    if (GUILayout.Button("Wave: mixed", GUILayout.Height(34)))
                        StartWave(("regular", 6, 0f, 1.5f), ("old_tank", 1, 6f, 1f), ("ifv", 1, 9f, 1f), ("mrap", 2, 3f, 5f), ("van", 1, 12f, 1f));
                    GUI.enabled = true;
                    if (GUILayout.Button("Reset", GUILayout.Height(34))) Load(current);
                    if (GUILayout.Button($"Speed x{simSpeed}", GUILayout.Height(34))) simSpeed = simSpeed == 1 ? 4 : 1;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndArea();
        }
    }
}
