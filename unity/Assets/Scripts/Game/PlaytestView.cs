using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Treeline.Sim;

namespace Treeline.Game
{
    /// <summary>
    /// Playtest screen (Yaroslav, 2026-11-16): a whole run on one field, played with the mouse in the Unity editor.
    /// Pick a field, place defenders in the lower part of the field, start the waves from waves.csv one by one, get
    /// money after each held wave, sell or move defenders, win after the last wave or lose when the line breaks.
    /// Throw-away tooling like <see cref="FieldDebugView"/> (flat colours, IMGUI); the real drawing and HUD live in
    /// Treeline.Render and Treeline.UI. Press Play in any scene; it creates itself. Set <see cref="Enabled"/> to false
    /// to get the old debug view back.
    /// Every wave is also written to playtest-log.csv in Application.persistentDataPath for the balance work.
    /// The run is a <see cref="Run"/>: fixed steps and a record of the player's actions, so "Back to wave N" replays it
    /// to the start of an earlier wave (2026-11-17).
    /// </summary>
    public sealed class PlaytestView : MonoBehaviour
    {
        /// <summary>True: Play opens this screen; false: the old <see cref="FieldDebugView"/>.</summary>
        public const bool Enabled = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Enabled) return;
            if (FindFirstObjectByType<PlaytestView>() != null) return;
            new GameObject("PlaytestView").AddComponent<PlaytestView>();
        }

        const float PanelWidth = 300f;   // GUI units (scaled with the screen height)
        const float PickRadiusM = 8f;

        // Data
        readonly List<Field> fields = new List<Field>();
        readonly List<string> fieldFiles = new List<string>();   // Resources/Fields names, for the art layer
        GameObject art;            // Treeline.Render.BattlefieldPreview, if present: the ground is drawn by it
        bool overArt;
        bool spritesBound;
        int current;
        UnitTable units;
        ObstacleTable obstacles;
        ArmourTable armour;
        BattleRules rules;
        WavePlan plan;
        readonly List<UnitDef> defenderTypes = new List<UnitDef>();
        string loadError = "";

        // Run
        NavGrid grid;
        VisionMap vision;
        Run run;              // the run: fixed steps, recorded actions, rewind (Treeline.Sim.Run)
        Battle battle => run?.Battle;
        int wavesHeld => run != null ? run.WavesHeld : 0;
        bool runOver => run != null && run.Over;
        int rewindTo = 1;     // wave chosen for "Back to wave"
        float stepClock;      // real time not yet simulated
        string message = "";
        float messageUntil;
        int simSpeed = 1;
        bool paused;

        // Input modes
        string placing;       // unit id to place on the next click, or null
        Defender selected;
        enum ClickMode { Normal, Move, Waypoint }
        ClickMode mode;

        // Drawing
        Texture2D tex;
        SpriteRenderer sr;
        Color32[] terrain;    // the field without units, one pixel per metre
        Color32[] pixels;
        GUIStyle small, title, label, center;
        Vector2 scroll;
        int terrainVersion;
        Vector2? pendingClick;   // map clicks are handled in Update, not in the middle of a GUI event

        // Playtest log
        readonly StringBuilder log = new StringBuilder();
        string logPath;
        float waveStartLine;
        int waveStartKills;

        void Start()
        {
            foreach (var asset in Resources.LoadAll<TextAsset>("Fields"))
            {
                try
                {
                    var f = FieldParser.Parse(asset.text);
                    if (f.Name != "Vision test") { fields.Add(f); fieldFiles.Add(asset.name); }
                }
                catch (System.Exception e) { Debug.LogError($"Field '{asset.name}': {e.Message}"); }
            }
            // Start on the field with named entry points if there is one.
            for (int i = 0; i < fields.Count; i++) if (fields[i].Spawns.Count > 0) { current = i; break; }

            units = Load("units.csv", UnitTable.Parse);
            obstacles = Load("obstacles.csv", ObstacleTable.Parse);
            armour = Load("armour.csv", ArmourTable.Parse);
            rules = Load("battle_rules.csv", BattleRules.Parse) ?? new BattleRules();
            plan = Load("waves.csv", WavePlan.Parse);
            if (units != null)
                foreach (var d in units.All)
                    if (d.IsDefence && d.BuildCost > 0f) defenderTypes.Add(d);
            defenderTypes.Sort((a, b) => a.BuildCost.CompareTo(b.BuildCost));

            logPath = System.IO.Path.Combine(Application.persistentDataPath, "playtest-log.csv");
            if (!System.IO.File.Exists(logPath))
                System.IO.File.AppendAllText(logPath, "time,field,wave,result,budget_after,defenders,line_damage_wave,line_damage_total,kills_wave,defender_list\n");

            sr = new GameObject("Field").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            if (fields.Count == 0) loadError = "No field files in Resources/Fields.";
            else if (units == null || plan == null) loadError = "Could not read Assets/Data (units.csv, waves.csv). See the Console.";
            else NewRun();
        }

        T Load<T>(string file, System.Func<string, T> parse) where T : class
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Data", file);
            try { return System.IO.File.Exists(path) ? parse(System.IO.File.ReadAllText(path)) : null; }
            catch (System.Exception e) { Debug.LogError($"{file}: {e.Message}"); return null; }
        }

        void NewRun()
        {
            var field = fields[current];
            int seed = System.Environment.TickCount;
            float minY = rules.PlacementZoneStart * field.Height;
            // Every rewind builds the battle again from the same field, data and seed (a fresh grid: wrecks go).
            run = new Run(() =>
            {
                grid = new NavGrid(field, obstacles);
                vision = obstacles != null ? new VisionMap(field, obstacles) : null;
                var b = new Battle(grid, units, rules.StartBudget, vision, armour, -1f, seed, rules);
                b.PlacementMinY = minY;
                return b;
            }, plan, rules);
            run.WaveEnded += OnWaveEnded;
            rewindTo = 1;
            stepClock = 0f;
            placing = null;
            selected = null;
            mode = ClickMode.Normal;
            paused = false;
            spritesBound = false; // a new Battle instance needs a new visual binding
            ShowArt();
            BuildTerrain();
            FitCamera();
            Say($"{field.Name}: place defenders anywhere outside the dark side strips, then start wave 1.", 8f);
        }

        // The Steppe Patch ground (Codex, Treeline.Render) under our flat layer. Found by name: Render already depends on
        // Game, so Game cannot reference it directly.
        void ShowArt()
        {
            if (art == null) art = GameObject.Find("Steppe Patch Battlefield Preview");
            bool was = overArt;
            overArt = art != null;
            if (art != null)
            {
                art.SendMessage("ShowField", fieldFiles[current], SendMessageOptions.DontRequireReceiver);
                // Render depends on Game; Game intentionally uses this message bridge.
                GameObject artUnits = GameObject.Find("Unit sprites");
                if (artUnits != null && battle != null)
                {
                    artUnits.SendMessage("BindBattle", battle, SendMessageOptions.DontRequireReceiver);
                    spritesBound = true;
                }
            }
            if (overArt != was && grid != null) BuildTerrain();
        }

        // ---- loop -------------------------------------------------------------------------------------------------

        void Update()
        {
            if (battle == null) return;
            if ((!overArt || !spritesBound) && Time.frameCount % 15 == 0)
                ShowArt(); // art and unit layer may awake after the Game playtest
            if (selected != null && !selected.Alive) { selected = null; mode = ClickMode.Normal; }
            if (selected != null)
            {
                bool still = false;
                foreach (var d in battle.Defenders) if (d == selected) { still = true; break; }
                if (!still) { selected = null; mode = ClickMode.Normal; }
            }
            if (pendingClick.HasValue) { var c = pendingClick.Value; pendingClick = null; OnMapClick(c.x, c.y); }

            if (battle.Phase == BattlePhase.Round && !runOver && !paused)
            {
                // Fixed steps (Run.Dt), so a rewind replays exactly what happened; fast-forward runs more of them.
                stepClock = Mathf.Min(stepClock + Time.deltaTime * simSpeed, 0.25f * simSpeed);
                while (stepClock >= Run.Dt && battle.Phase == BattlePhase.Round && !runOver)
                {
                    run.Tick();
                    stepClock -= Run.Dt;
                }
            }
            else stepClock = 0f;
            Draw();
        }

        void OnWaveEnded(int wave, string result)
        {
            if (run.Replaying) return;
            LogWave(result);
            if (result == "lost")
                Say($"The line is broken on wave {wave}. Go back to an earlier wave or restart.", 999f);
            else if (run.Won)
                Say($"Victory: all {plan.WaveCount} waves held, line {battle.LineDamage:0.#} / {battle.LineHealth:0}.", 999f);
            else
                Say($"Wave {wave} held. +{rules.WaveIncome + run.LastReward:0} money ({run.LastReward:0} for kills). Get ready for wave {wave + 1}.", 6f);
            rewindTo = wave;   // "Back to wave" offers this wave again by default (retry it)
        }

        void Rewind(int wave)
        {
            if (!run.RewindTo(wave)) { Say($"Cannot go back to wave {wave}.", 3f); return; }
            selected = null;
            placing = null;
            mode = ClickMode.Normal;
            paused = false;
            stepClock = 0f;
            spritesBound = false;   // a new Battle instance needs a new visual binding
            ShowArt();
            BuildTerrain();
            Say($"Back before wave {wave}: money {battle.Budget:0}, {battle.Defenders.Count} defenders, as they were.", 6f);
        }

        void StartNextWave()
        {
            int next = battle.WaveNumber + 1;
            if (next > plan.WaveCount || runOver) return;
            run.StartNextWave();
            waveStartLine = battle.LineDamage;
            waveStartKills = Kills();
            placing = null;
            Say($"Wave {next}: {Describe(next)}", 6f);
        }

        int Kills()
        {
            int k = 0;
            foreach (var kv in battle.Stats)
                if (units.TryGet(kv.Key, out var d) && !d.IsDefence) k += kv.Value.Killed;
            return k;
        }

        string Describe(int wave)
        {
            var sb = new StringBuilder();
            foreach (var g in plan.WaveGroups(wave))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(g.Count).Append(' ').Append(units.TryGet(g.UnitId, out var d) ? d.Name : g.UnitId);
            }
            return sb.ToString();
        }

        void LogWave(string result)
        {
            var list = new StringBuilder();
            foreach (var d in battle.Defenders) { if (list.Length > 0) list.Append(' '); list.Append(d.Def.Id); }
            string line = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm},{1},{2},{3},{4:0},{5},{6:0.#},{7:0.#},{8},{9}\n",
                System.DateTime.Now, fields[current].Name, battle.WaveNumber, result, battle.Budget, battle.Defenders.Count,
                battle.LineDamage - waveStartLine, battle.LineDamage, Kills() - waveStartKills, list);
            try { System.IO.File.AppendAllText(logPath, line); } catch (System.Exception e) { Debug.LogWarning(e.Message); }
            Debug.Log("Playtest: " + line.Trim());
        }

        void Say(string text, float seconds)
        {
            message = text;
            messageUntil = Time.time + seconds;
        }

        // ---- clicks -----------------------------------------------------------------------------------------------

        void OnMapClick(float x, float y)
        {
            if (battle == null || runOver) return;
            if (x < 0f || y < 0f || x > grid.Width || y > grid.Height) return;

            if (placing != null)
            {
                if (run.Place(placing, x, y, out var d, out string why))
                {
                    selected = d;
                    if (!units.TryGet(placing, out var def) || battle.Budget < def.BuildCost) placing = null;
                }
                else Say(why + ".", 3f);
                return;
            }
            if (selected != null && mode == ClickMode.Move)
            {
                if (!run.Move(selected.Id, x, y, out string why)) Say(why + ".", 3f);
                mode = ClickMode.Normal;
                return;
            }
            if (selected != null && mode == ClickMode.Waypoint)
            {
                if (!run.SetWaypoint(selected.Id, x, y, out string why)) Say(why + ".", 3f);
                mode = ClickMode.Normal;
                return;
            }
            // Select the nearest defender under the click.
            Defender best = null;
            float bestD = PickRadiusM * PickRadiusM;
            foreach (var d in battle.Defenders)
            {
                float dx = d.X - x, dy = d.Y - y, dd = dx * dx + dy * dy;
                if (dd < bestD) { bestD = dd; best = d; }
            }
            selected = best;
            mode = ClickMode.Normal;
        }

        // ---- drawing ----------------------------------------------------------------------------------------------

        int W => Mathf.RoundToInt(grid.Width);
        int H => Mathf.RoundToInt(grid.Height);

        void BuildTerrain()
        {
            if (tex == null || tex.width != W || tex.height != H)
            {
                tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 1f);   // 1 pixel = 1 metre
            }
            terrainVersion = grid.Version;
            terrain = new Color32[W * H];
            pixels = new Color32[W * H];
            float zone = battle.PlacementMinY;
            for (int py = 0; py < H; py++)
                for (int px = 0; px < W; px++)
                {
                    float x = px + 0.5f, y = py + 0.5f;
                    int cell = grid.CellAt(x, y);
                    if (overArt)
                    {
                        // Only what the art does not show: the no-placing zone and wrecks that appear in battle.
                        Color32 o = new Color32(0, 0, 0, 0);
                        var wreck = vision != null ? vision.ObstacleInCell(cell) : null;
                        if (wreck != null && wreck.Id == "wreck") o = new Color32(70, 50, 40, 255);
                        else if (grid.IsEdge(cell)) o = new Color32(0, 0, 0, 120);   // closed side strips
                        else if (y < zone) o = new Color32(0, 0, 0, 90);
                        terrain[Index(x, y)] = o;
                        continue;
                    }
                    Color c = TerrainColor(grid.Cells[cell]);
                    if (vision != null)
                    {
                        var ob = vision.ObstacleInCell(cell);
                        if (ob != null && !TerrainTable.TryParse(ob.Id, out _)) c = ObjectColor(ob.Id);
                    }
                    if (float.IsPositiveInfinity(grid.Cost(UnitClass.Infantry)[cell])) c = Color.Lerp(c, new Color(0.3f, 0.35f, 0.45f), 0.5f);
                    if (y < zone) c = Color.Lerp(c, Color.black, 0.35f);   // no placing up here
                    terrain[Index(x, y)] = c;
                }
            // Our line at the bottom.
            for (int px = 0; px < W; px++) { terrain[Index(px, H - 0.5f)] = new Color32(60, 130, 235, 255); terrain[Index(px, H - 1.5f)] = new Color32(60, 130, 235, 255); }
        }

        void Draw()
        {
            if (terrain == null) return;
            if (grid.Version != terrainVersion) BuildTerrain();   // a wreck appeared
            System.Array.Copy(terrain, pixels, pixels.Length);

            // Selected defender: its range (white) and sight (pale) rings, and an FPV's search point.
            if (selected != null)
            {
                Ring(selected.X, selected.Y, selected.Def.RangeM, new Color32(235, 235, 235, 255));
                Ring(selected.X, selected.Y, selected.Def.SightRangeM, new Color32(150, 200, 150, 255));
                if (selected.HasDroneWaypoint) Cross(selected.DroneWaypointX, selected.DroneWaypointY, 4, new Color32(255, 255, 255, 255));
            }
            foreach (var tr in battle.ShotTraces)
            {
                float span = tr.ArrivesAt - tr.FiredAt;
                float f = span > 0f ? Mathf.Clamp01((battle.RoundTime - tr.FiredAt) / span) : 1f;
                // Ours orange (hit) / grey (miss); theirs red (hit) / dark red (miss).
                var col = tr.ByAttacker
                    ? (tr.Hit ? new Color32(255, 50, 50, 255) : new Color32(150, 40, 40, 255))
                    : (tr.Hit ? new Color32(255, 190, 90, 255) : new Color32(150, 150, 150, 255));
                Line(tr.FromX, tr.FromY, tr.FromX + (tr.ToX - tr.FromX) * f, tr.FromY + (tr.ToY - tr.FromY) * f, col);
            }
            // Our losses: a red cross where the defender fell, and for a few seconds a line to whoever killed it.
            foreach (var l in battle.Losses)
            {
                float age = battle.RoundTime - l.Time;
                if (age < 0f || age > 10f) continue;
                if (age < 4f) Dashed(l.KillerX, l.KillerY, l.X, l.Y, new Color32(255, 60, 60, 255));
                XMark(l.X, l.Y, 3, new Color32(255, 40, 40, 255));
            }
            if (!spritesBound)
            {
                // Fallback for scenes without the new Render layer.
                foreach (var a in battle.Attackers)
                {
                    Color32 c;
                    int r;
                    if (a.Airborne) { c = a.Def.Id == "fpv" ? new Color32(220, 60, 220, 255) : new Color32(255, 120, 255, 255); r = a.Def.Id == "fpv" ? 1 : 3; }
                    else if (a.Class == UnitClass.Vehicle) { c = a.Dismounting ? new Color32(255, 230, 60, 255) : new Color32(255, 140, 40, 255); r = 3; }
                    else { c = new Color32(235, 60, 50, 255); r = 1; }
                    Block(a.X, a.Y, r, c);
                }
                foreach (var d in battle.Defenders)
                {
                    var c = d == selected ? new Color32(255, 255, 255, 255)
                        : d.State == DefenderState.Running ? new Color32(140, 235, 245, 255) : new Color32(60, 130, 235, 255);
                    Block(d.X, d.Y, 2, c);
                }
                foreach (var dr in battle.Drones) Block(dr.X, dr.Y, 1, new Color32(255, 255, 255, 255));
            }
            foreach (var pr in battle.Projectiles)
            {
                if (pr.Kind == ProjectileKind.Shell) Cross(pr.ToX, pr.ToY, 2, new Color32(140, 20, 20, 255));
                Block(pr.X, pr.Y, 1, new Color32(255, 40, 40, 255));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
        }

        int Index(float x, float y)
        {
            int px = Mathf.Clamp((int)x, 0, W - 1), py = Mathf.Clamp((int)y, 0, H - 1);
            return (H - 1 - py) * W + px;   // field y runs down from the enemy edge; the texture's runs up
        }

        void Block(float x, float y, int r, Color32 c)
        {
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    float px = x + dx, py = y + dy;
                    if (px < 0 || py < 0 || px >= W || py >= H) continue;
                    pixels[Index(px, py)] = c;
                }
        }

        void Cross(float x, float y, int r, Color32 c)
        {
            for (int k = -r; k <= r; k++)
            {
                if (x + k >= 0 && x + k < W && y >= 0 && y < H) pixels[Index(x + k, y)] = c;
                if (y + k >= 0 && y + k < H && x >= 0 && x < W) pixels[Index(x, y + k)] = c;
            }
        }

        void Line(float x0, float y0, float x1, float y1, Color32 c)
        {
            float len = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            int steps = Mathf.Max(1, Mathf.CeilToInt(len));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps, x = x0 + (x1 - x0) * t, y = y0 + (y1 - y0) * t;
                if (x >= 0 && y >= 0 && x < W && y < H) pixels[Index(x, y)] = c;
            }
        }

        void Dashed(float x0, float y0, float x1, float y1, Color32 c)
        {
            float len = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            int steps = Mathf.Max(1, Mathf.CeilToInt(len));
            for (int i = 0; i <= steps; i++)
            {
                if ((i / 3) % 2 == 1) continue;
                float t = (float)i / steps, x = x0 + (x1 - x0) * t, y = y0 + (y1 - y0) * t;
                if (x >= 0 && y >= 0 && x < W && y < H) pixels[Index(x, y)] = c;
            }
        }

        void XMark(float x, float y, int r, Color32 c)
        {
            for (int k = -r; k <= r; k++)
            {
                if (x + k >= 0 && x + k < W && y + k >= 0 && y + k < H) pixels[Index(x + k, y + k)] = c;
                if (x + k >= 0 && x + k < W && y - k >= 0 && y - k < H) pixels[Index(x + k, y - k)] = c;
            }
        }

        void Ring(float cx, float cy, float radius, Color32 c)
        {
            if (radius <= 0f) return;
            int steps = Mathf.Max(24, Mathf.CeilToInt(radius * 6.3f));
            for (int i = 0; i < steps; i++)
            {
                float a = i * Mathf.PI * 2f / steps, x = cx + Mathf.Cos(a) * radius, y = cy + Mathf.Sin(a) * radius;
                if (x >= 0 && y >= 0 && x < W && y < H) pixels[Index(x, y)] = c;
            }
        }

        void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float scale = GuiScale();
            float panel = Mathf.Clamp01(PanelWidth * scale / Mathf.Max(1f, Screen.width));
            cam.rect = new Rect(panel, 0f, 1f - panel, 1f);
            cam.orthographic = true;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.06f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            float aspect = Mathf.Max(0.1f, (Screen.width * (1f - panel)) / Mathf.Max(1f, Screen.height));
            cam.orthographicSize = Mathf.Max(grid.Height / 2f * 1.04f, grid.Width / 2f * 1.04f / aspect);
        }

        static float GuiScale() => Mathf.Max(1f, Screen.height / 720f);

        int lastW, lastH;

        // ---- GUI --------------------------------------------------------------------------------------------------

        void OnGUI()
        {
            if (Screen.width != lastW || Screen.height != lastH)
            {
                lastW = Screen.width; lastH = Screen.height;
                if (grid != null) FitCamera();
            }
            var cam = Camera.main;
            var e = Event.current;

            // Clicks on the map (raw screen pixels, before the GUI is scaled).
            float s = GuiScale();
            if (e.type == EventType.MouseDown && e.button == 0 && grid != null && cam != null && e.mousePosition.x > PanelWidth * s)
            {
                var world = cam.ScreenToWorldPoint(new Vector3(e.mousePosition.x, Screen.height - e.mousePosition.y, 10f));
                pendingClick = new Vector2(world.x + grid.Width / 2f, grid.Height / 2f - world.y);
                e.Use();
            }
            if (e.type == EventType.MouseDown && e.button == 1) { placing = null; mode = ClickMode.Normal; e.Use(); }

            EnsureStyles();

            // Defender labels on the map.
            if (battle != null && cam != null)
                foreach (var d in battle.Defenders)
                {
                    var sp = cam.WorldToScreenPoint(new Vector3(d.X - grid.Width / 2f, grid.Height / 2f - d.Y, 0f));
                    GUI.Label(new Rect(sp.x + 6, Screen.height - sp.y - 10, 120, 20), Short(d.Def.Id) + "  " + Mathf.CeilToInt(d.Hp), small);
                }

            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float panelH = Screen.height / s;
            var old = GUI.color;
            GUI.color = new Color(0.07f, 0.08f, 0.07f, 1f);
            GUI.DrawTexture(new Rect(0, 0, PanelWidth, panelH), Texture2D.whiteTexture);
            GUI.color = old;

            GUILayout.BeginArea(new Rect(8, 8, PanelWidth - 16, panelH - 16));
            scroll = GUILayout.BeginScrollView(scroll);
            if (loadError.Length > 0) { GUILayout.Label(loadError, label); GUILayout.EndScrollView(); GUILayout.EndArea(); return; }
            if (battle == null) { GUILayout.EndScrollView(); GUILayout.EndArea(); return; }

            GUILayout.Label("TREELINE  playtest", title);
            GUILayout.BeginHorizontal();
            GUI.enabled = battle.WaveNumber == 0 || runOver;
            if (GUILayout.Button("<", GUILayout.Width(30))) { current = (current + fields.Count - 1) % fields.Count; NewRun(); }
            GUILayout.Label(fields[current].Name, center);
            if (GUILayout.Button(">", GUILayout.Width(30))) { current = (current + 1) % fields.Count; NewRun(); }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            int shownWave = battle.Phase == BattlePhase.Round ? battle.WaveNumber : battle.WaveNumber + 1;
            GUILayout.Label($"Wave {Mathf.Min(shownWave, plan.WaveCount)} / {plan.WaveCount}     Money {battle.Budget:0}", label);
            GUILayout.Label($"Line {battle.LineDamage:0.#} / {battle.LineHealth:0}     Enemies killed {Kills()}", label);
            if (battle.Rewards > 0f) GUILayout.Label($"Kill rewards this wave: {battle.Rewards:0} (paid when the wave is held)", small);
            if (battle.Phase == BattlePhase.Round && !runOver)
                GUILayout.Label($"On the field {battle.Attackers.Count}, still coming {battle.Pending}", small);

            if (Time.time < messageUntil || runOver) GUILayout.Label(message, label);

            // Who killed our defenders (newest first, last six).
            if (battle.Losses.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label($"Losses: {battle.Losses.Count}", label);
                for (int i = battle.Losses.Count - 1; i >= 0 && i >= battle.Losses.Count - 6; i--)
                {
                    var l = battle.Losses[i];
                    float dist = Mathf.Sqrt((l.X - l.KillerX) * (l.X - l.KillerX) + (l.Y - l.KillerY) * (l.Y - l.KillerY));
                    GUILayout.Label($"w{l.Wave}: {NameOf(l.VictimId)} killed by {NameOf(l.KillerId)} from {dist:0} m", small);
                }
            }

            GUILayout.Space(6);
            if (!runOver)
            {
                bool betweenWaves = battle.Phase == BattlePhase.Preparation;
                if (betweenWaves)
                {
                    int next = battle.WaveNumber + 1;
                    GUILayout.Label("Next: " + Describe(next), small);
                    if (GUILayout.Button($"Start wave {next}", GUILayout.Height(36))) StartNextWave();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(paused ? "Resume" : "Pause", GUILayout.Height(30))) paused = !paused;
                    foreach (int sp in new[] { 1, 2, 4 })
                        if (GUILayout.Toggle(simSpeed == sp, "x" + sp, "Button", GUILayout.Height(30))) simSpeed = sp;
                    GUILayout.EndHorizontal();
                }
            }
            // Back to the start of an earlier wave (or retry the one lost): the run is played again from its record.
            int maxBack = Mathf.Min(wavesHeld + 1, plan.WaveCount);
            if (maxBack >= 1 && (wavesHeld >= 1 || runOver))
            {
                rewindTo = Mathf.Clamp(rewindTo, 1, maxBack);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<", GUILayout.Width(26), GUILayout.Height(26))) rewindTo = Mathf.Max(1, rewindTo - 1);
                if (GUILayout.Button($"Back to wave {rewindTo}", GUILayout.Height(26))) Rewind(rewindTo);
                if (GUILayout.Button(">", GUILayout.Width(26), GUILayout.Height(26))) rewindTo = Mathf.Min(maxBack, rewindTo + 1);
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Restart", GUILayout.Height(26))) NewRun();

            // Build menu.
            GUILayout.Space(6);
            GUILayout.Label(placing != null ? "Click the map to place (right click: stop)" : "Defenders (click, then click the map):", small);
            foreach (var d in defenderTypes)
            {
                GUI.enabled = !runOver && battle.Budget >= d.BuildCost;
                bool on = placing == d.Id;
                if (GUILayout.Toggle(on, $"{d.Name}  —  {d.BuildCost:0}", "Button") != on)
                {
                    placing = on ? null : d.Id;
                    selected = null;
                    mode = ClickMode.Normal;
                }
            }
            GUI.enabled = true;

            // Selected defender.
            if (selected != null)
            {
                GUILayout.Space(6);
                var d = selected.Def;
                GUILayout.Label($"{d.Name}: HP {Mathf.CeilToInt(selected.Hp)} / {d.Hp:0}, range {d.RangeM:0} m, sight {d.SightRangeM:0} m", label);
                if (selected.State == DefenderState.Running) GUILayout.Label("Running in from our line...", small);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(mode == ClickMode.Move ? "Click the map..." : "Move")) mode = ClickMode.Move;
                float refund = d.BuildCost * rules.SellRefund;
                if (GUILayout.Button($"Sell +{refund:0}"))
                {
                    run.Sell(selected.Id, out _);
                    selected = null;
                    mode = ClickMode.Normal;
                }
                GUILayout.EndHorizontal();
                if (selected != null && d.Behaviour == "fpv_launcher" &&
                    GUILayout.Button(mode == ClickMode.Waypoint ? "Click the map..." : "FPV search point"))
                    mode = ClickMode.Waypoint;
            }

            // Legend.
            GUILayout.Space(8);
            if (spritesBound)
                GUILayout.Label("STEPPE PATCH: actual units use natural camouflage; small armbands mark sides. " +
                    "Click a defender to view range; lines are live shots: ours orange (hit) / grey (miss), theirs red / dark red. " +
                    "Red cross: one of ours fell, dashed line: from whom. Dark side strips: closed to everyone. " +
                    "Explosions, smoke and dust follow real battle state.", small);
            else
                GUILayout.Label("Blue: ours (light blue: running in, white: selected). Red dots: infantry. Orange: vehicles " +
                    "(yellow: letting troops out). Pink: aircraft, purple: enemy FPV. White dots: our drones. Lines: our shots " +
                    "orange (hit) / grey (miss), theirs red / dark red. Red cross: one of ours fell, dashed line: from whom. " +
                    "Dark red cross: where a shell lands. Dark side strips: closed to everyone. Blue-grey: impassable.", small);
            GUILayout.Label("Log: " + logPath, small);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        string NameOf(string id) => units != null && units.TryGet(id, out var d) ? d.Name : id;

        static string Short(string id)
        {
            switch (id)
            {
                case "rifle_squad": return "RIF";
                case "machine_gun": return "MG";
                case "sniper": return "SNP";
                case "mortar": return "MOR";
                case "anti_tank": return "ATGM";
                case "air_defence": return "AD";
                case "fpv_operator": return "FPV";
                case "bomber_operator": return "BMB";
                case "recon_drone": return "REC";
                default: return id;
            }
        }

        void EnsureStyles()
        {
            if (small != null) return;
            small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            small.normal.textColor = new Color(0.85f, 0.88f, 0.8f);
            label = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            label.normal.textColor = Color.white;
            title = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.75f, 0.9f, 0.6f);
            center = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            center.normal.textColor = Color.white;
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
    }
}
