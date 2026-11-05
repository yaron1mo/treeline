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

            sr = new GameObject("Terrain").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            Load(0);
        }

        void Load(int index)
        {
            current = (index + fields.Count) % fields.Count;
            var field = fields[current];

            var sw = System.Diagnostics.Stopwatch.StartNew();
            grid = new NavGrid(field);
            infantryFlow = new FlowField(grid, UnitClass.Infantry);
            vehicleFlow = new FlowField(grid, UnitClass.Vehicle);
            vision = obstacles != null ? new VisionMap(field, obstacles) : null;
            obsX = field.Width / 2f;
            obsY = field.Height / 2f;
            sw.Stop();
            Debug.Log($"Field '{field.Name}': {grid.Cols}x{grid.Rows} cells, nav built in {sw.ElapsedMilliseconds} ms");

            Redraw();
            FitCamera(field);
        }

        void Redraw()
        {
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

            // Routes from the enemy edge, one every ~24 m.
            int shown = 0, blocked = 0;
            double best = double.PositiveInfinity;
            for (int c = 3; c < grid.Cols; c += 12)
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
            GUILayout.BeginArea(new Rect(8, 8, Screen.width / s - 16, 190));
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
                    if (showVision)
                    {
                        if (GUILayout.Button("Left", GUILayout.Height(34))) MoveObserver(-ObserverStep, 0);
                        if (GUILayout.Button("Right", GUILayout.Height(34))) MoveObserver(ObserverStep, 0);
                        if (GUILayout.Button("Up", GUILayout.Height(34))) MoveObserver(0, -ObserverStep);
                        if (GUILayout.Button("Down", GUILayout.Height(34))) MoveObserver(0, ObserverStep);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndArea();
        }
    }
}
