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

        void Start()
        {
            foreach (var asset in Resources.LoadAll<TextAsset>("Fields"))
            {
                try { fields.Add(FieldParser.Parse(asset.text)); }
                catch (System.Exception e) { Debug.LogError($"Field '{asset.name}': {e.Message}"); }
            }
            if (fields.Count == 0) { status = "No field files found in Resources/Fields"; return; }

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

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        void OnGUI()
        {
            float s = Mathf.Max(1f, Screen.height / 540f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            GUILayout.BeginArea(new Rect(8, 8, Screen.width / s - 16, 120));
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
            }
            GUILayout.EndArea();
        }
    }
}
