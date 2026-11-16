using System;
using System.Collections.Generic;
using UnityEngine;
using Treeline.Sim;

namespace Treeline.Render
{
    /// <summary>
    /// Visual-only Steppe Patch battlefield preview. All geometry comes from the existing Field JSON files.
    /// No navigation, unit parameters, collision, or balance data are modified.
    /// In the Editor, press Play to preview; buttons or 1/2/3 switch the three test fields.
    /// </summary>
    public sealed class BattlefieldPreview : MonoBehaviour
    {
        const int PixelsPerMetre = 2;
        const float WindFrameSeconds = 0.20f;
        static readonly string[] MapFiles = { "forest-belts", "balka-and-railway", "river-crossing" };

        SpriteRenderer ground, wind, cloud;
        Texture2D groundTexture, cloudTexture;
        Sprite[] windFrames;
        Texture2D[] windTextures;
        Sprite cloudSprite;
        int mapIndex, pixelWidth, pixelHeight, windFrame;
        float windClock, cloudClock;
        string mapTitle = "";
        readonly List<Sprite> spritesToDestroy = new List<Sprite>();

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EditorBoot()
        {
            if (FindFirstObjectByType<BattlefieldPreview>() == null)
                new GameObject("Steppe Patch Battlefield Preview").AddComponent<BattlefieldPreview>();
        }
#endif

        void Start()
        {
            // The existing FieldDebugView is a testing-only renderer, not the new presentation layer.
            var debugView = FindFirstObjectByType<Treeline.Game.FieldDebugView>();
            if (debugView != null) Destroy(debugView.gameObject);

            ground = Layer("Terrain", -100);
            wind = Layer("Moving crops", -98);
            cloud = Layer("Cloud shadow", -95);
            LoadMap(0);
        }

        SpriteRenderer Layer(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        void LoadMap(int number)
        {
            mapIndex = (number + MapFiles.Length) % MapFiles.Length;
            TextAsset source = Resources.Load<TextAsset>("Fields/" + MapFiles[mapIndex]);
            if (source == null)
            {
                Debug.LogError("Missing field: " + MapFiles[mapIndex]);
                return;
            }

            var document = Json.Parse(source.text) as Dictionary<string, object>;
            if (document == null) return;
            var size = (List<object>)document["size"];
            float width = Convert.ToSingle(size[0]);
            float height = Convert.ToSingle(size[1]);
            pixelWidth = Mathf.RoundToInt(width * PixelsPerMetre);
            pixelHeight = Mathf.RoundToInt(height * PixelsPerMetre);
            mapTitle = document.TryGetValue("name", out var n) ? n.ToString() : MapFiles[mapIndex];

            ClearGenerated();
            var features = (List<object>)document["features"];
            var terrain = new Color32[pixelWidth * pixelHeight];
            var crops = new byte[terrain.Length];
            DrawBase(terrain);
            DrawFeatures(features, terrain, crops);

            groundTexture = Texture(terrain);
            ground.sprite = MakeSprite(groundTexture);
            ground.transform.position = Vector3.zero;
            MakeWind(crops);
            MakeCloud();
            FitCamera(width, height);
        }

        void DrawBase(Color32[] pixels)
        {
            for (int y = 0; y < pixelHeight; y++)
                for (int x = 0; x < pixelWidth; x++)
                {
                    float v = Hash(x / 3, y / 3, 11) * 0.12f + Hash(x, y, 13) * 0.055f;
                    pixels[Idx(x, y)] = Tint(new Color32(153, 166, 104, 255), v);
                }
        }

        void DrawFeatures(List<object> raw, Color32[] pixels, byte[] crops)
        {
            var entries = new List<Dictionary<string, object>>();
            foreach (var r in raw)
                if (r is Dictionary<string, object> entry) entries.Add(entry);

            // Paint layers explicitly: landscape below paths, cover and tree crowns above them.
            foreach (var f in entries)
                if (Kind(f) == "field")
                {
                    string tone = Str(f, "tone");
                    int angle = Mathf.RoundToInt(Num(f, "angle", 0));
                    Color32 c = tone == "plowed" ? new Color32(150, 104, 67, 255)
                        : tone == "sunflower" ? new Color32(130, 154, 76, 255)
                        : new Color32(201, 169, 105, 255);
                    PaintPolygon(Points(f, "poly"), pixels, c, tone, angle, crops);
                }

            foreach (var f in entries)
            {
                string type = Kind(f);
                if (type == "field" || type == "tracks" || type == "craters" ||
                    type == "grove" || type == "belt") continue;

                Color32 tint;
                switch (type)
                {
                    case "pond": tint = new Color32(64, 111, 117, 255); break;
                    case "river": tint = new Color32(70, 119, 129, 255); break;
                    case "ford": tint = new Color32(122, 147, 130, 255); break;
                    case "hill": tint = new Color32(144, 143, 91, 255); break;
                    case "heap": tint = new Color32(103, 90, 72, 255); break;
                    case "balka": tint = new Color32(95, 120, 75, 255); break;
                    case "embankment": tint = new Color32(180, 150, 102, 255); break;
                    case "pipe": tint = new Color32(155, 154, 139, 255); break;
                    case "road":
                        PaintPath(Points(f, "line"), Num(f, "width", 5f) + 1f, pixels,
                            new Color32(97, 78, 50, 255));
                        PaintPath(Points(f, "line"), Num(f, "width", 5f) * 0.8f, pixels,
                            new Color32(189, 161, 109, 255));
                        continue;
                    default: tint = new Color32(122, 121, 99, 255); break;
                }

                Vector2[] polygon = Points(f, "poly");
                if (polygon.Length >= 3) PaintPolygon(polygon, pixels, tint, type, 0, null);
                else PaintPath(Points(f, "line"), Num(f, "width", 4f), pixels, tint);
            }

            foreach (var f in entries)
                if (Kind(f) == "tracks")
                {
                    var line = Points(f, "line");
                    PaintPath(line, 1.8f, pixels, new Color32(115, 88, 59, 255));
                    PaintPath(line, 0.8f, pixels, new Color32(199, 167, 109, 255));
                }

            foreach (var f in entries)
                if (Kind(f) == "craters" && f.TryGetValue("points", out var value) && value is List<object> spots)
                    foreach (var point in spots)
                    {
                        var coords = (List<object>)point;
                        float x = Convert.ToSingle(coords[0]), y = Convert.ToSingle(coords[1]);
                        float r = coords.Count > 2 ? Convert.ToSingle(coords[2]) : 1f;
                        Blob(pixels, x - 0.45f, y - 0.35f, r * 1.55f, r * 1.2f,
                            new Color32(224, 185, 123, 255), 185);
                        Blob(pixels, x, y, r * 1.15f, r, new Color32(65, 54, 43, 255), 235);
                        Blob(pixels, x + 0.20f, y + 0.25f, r * 0.7f, r * 0.65f,
                            new Color32(39, 38, 32, 255), 220);
                    }

            foreach (var f in entries)
            {
                if (Kind(f) == "belt") DrawBelt(Points(f, "line"), Num(f, "width", 14f), pixels);
                if (Kind(f) == "grove") DrawGrove(Points(f, "poly"), pixels);
            }
        }

        void PaintPolygon(Vector2[] poly, Color32[] pixels, Color32 color,
            string tone, int angle, byte[] crops)
        {
            if (poly.Length < 3) return;
            Bounds(poly, out int left, out int top, out int right, out int bottom);
            float degrees = angle * Mathf.Deg2Rad;
            float dx = Mathf.Cos(degrees), dy = Mathf.Sin(degrees);
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
                {
                    float wx = (x + 0.5f) / PixelsPerMetre;
                    float wy = (y + 0.5f) / PixelsPerMetre;
                    if (!Inside(poly, wx, wy)) continue;
                    float line = Mathf.Sin((wx * dx + wy * dy) * (tone == "plowed" ? 2.5f : 3.4f));
                    float grain = Hash(x / 3, y / 3, 7) * 0.17f + Hash(x, y, 19) * 0.06f;
                    float stripe = tone == "river" || tone == "pond" ? line * 0.045f : line * 0.075f;
                    int i = Idx(x, y);
                    pixels[i] = Tint(color, grain + stripe);
                    if (crops != null)
                        crops[i] = tone == "sunflower" ? (byte)2 : tone == "stubble" ? (byte)1 : (byte)0;
                }
        }

        void PaintPath(Vector2[] points, float width, Color32[] pixels, Color32 color)
        {
            for (int i = 1; i < points.Length; i++)
            {
                Vector2 a = points[i - 1], b = points[i];
                float len = Vector2.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(len * 1.4f));
                for (int k = 0; k <= steps; k++)
                {
                    Vector2 p = Vector2.Lerp(a, b, k / (float)steps);
                    Blob(pixels, p.x, p.y, width * 0.5f, width * 0.5f, color, 255);
                }
            }
        }

        void DrawBelt(Vector2[] points, float width, Color32[] pixels)
        {
            for (int i = 1; i < points.Length; i++)
            {
                Vector2 a = points[i - 1], b = points[i];
                Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized;
                float len = Vector2.Distance(a, b);
                int trees = Mathf.Max(1, Mathf.FloorToInt(len / 4.2f));
                for (int j = 0; j <= trees; j++)
                {
                    Vector2 p = Vector2.Lerp(a, b, j / (float)trees);
                    for (int side = -1; side <= 1; side++)
                    {
                        float variation = Hash(j, i, side + 52);
                        Vector2 t = p + normal * (side * width * 0.26f + (variation - 0.5f) * 2f);
                        Tree(pixels, t.x, t.y, 2.5f + variation * 1.6f, j + i * 19 + side);
                    }
                }
            }
        }

        void DrawGrove(Vector2[] poly, Color32[] pixels)
        {
            if (poly.Length < 3) return;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in poly)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            for (float y = minY; y < maxY; y += 4f)
                for (float x = minX; x < maxX; x += 4f)
                {
                    float jitter = Hash(Mathf.FloorToInt(x), Mathf.FloorToInt(y), 77);
                    float tx = x + jitter * 2.1f, ty = y + (jitter - 0.5f) * 3f;
                    if (Inside(poly, tx, ty))
                        Tree(pixels, tx, ty, 2.5f + jitter * 1.8f, Mathf.FloorToInt(x + y));
                }
        }

        void Tree(Color32[] pixels, float x, float y, float radius, int seed)
        {
            float v = Hash(Mathf.RoundToInt(x), Mathf.RoundToInt(y), seed);
            Blob(pixels, x + 1.6f, y + 2.0f, radius * 1.05f, radius * 0.8f,
                new Color32(20, 34, 27, 255), 153);
            Blob(pixels, x, y, radius, radius * 0.9f,
                new Color32(31, 65, 36, 255), 255);
            Blob(pixels, x - 0.2f, y - 0.3f, radius * 0.82f, radius * 0.7f,
                v > 0.5f ? new Color32(66, 111, 52, 255) : new Color32(76, 115, 58, 255), 255);
            Blob(pixels, x - radius * 0.28f, y - radius * 0.35f,
                radius * 0.48f, radius * 0.32f, new Color32(136, 154, 76, 255), 153);
        }

        void Blob(Color32[] pixels, float x, float y, float rx, float ry, Color32 col, byte alpha)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt((x - rx) * PixelsPerMetre));
            int maxX = Mathf.Min(pixelWidth - 1, Mathf.CeilToInt((x + rx) * PixelsPerMetre));
            int minY = Mathf.Max(0, Mathf.FloorToInt((y - ry) * PixelsPerMetre));
            int maxY = Mathf.Min(pixelHeight - 1, Mathf.CeilToInt((y + ry) * PixelsPerMetre));
            for (int py = minY; py <= maxY; py++)
                for (int px = minX; px <= maxX; px++)
                {
                    float dx = ((px + 0.5f) / PixelsPerMetre - x) / Mathf.Max(0.001f, rx);
                    float dy = ((py + 0.5f) / PixelsPerMetre - y) / Mathf.Max(0.001f, ry);
                    if (dx * dx + dy * dy > 1f) continue;
                    int index = Idx(px, py);
                    if (alpha >= 255) pixels[index] = col;
                    else
                    {
                        Color32 from = pixels[index];
                        pixels[index] = new Color32(
                            (byte)((from.r * (255 - alpha) + col.r * alpha) / 255),
                            (byte)((from.g * (255 - alpha) + col.g * alpha) / 255),
                            (byte)((from.b * (255 - alpha) + col.b * alpha) / 255), 255);
                    }
                }
        }

        void MakeWind(byte[] crops)
        {
            windFrames = new Sprite[4];
            windTextures = new Texture2D[4];
            for (int frame = 0; frame < windFrames.Length; frame++)
            {
                var pixels = new Color32[crops.Length];
                for (int y = 0; y < pixelHeight; y++)
                    for (int x = 0; x < pixelWidth; x++)
                    {
                        int i = Idx(x, y);
                        if (crops[i] == 0) continue;
                        float phase = x * 0.078f + y * 0.033f + frame * Mathf.PI * 0.5f;
                        float strength = Mathf.Sin(phase) * 0.5f + 0.5f;
                        byte alpha = (byte)((crops[i] == 2 ? 22 : 10) * strength);
                        pixels[i] = new Color32(235, 223, 141, alpha);
                    }
                windTextures[frame] = Texture(pixels);
                windFrames[frame] = MakeSprite(windTextures[frame]);
            }
            wind.sprite = windFrames[0];
            wind.transform.position = Vector3.zero;
            windFrame = 0;
        }

        void MakeCloud()
        {
            const int w = 128, h = 72;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - 64f) / 60f, dy = (y - 36f) / 32f;
                    float body = Mathf.Clamp01(1f - dx * dx - dy * dy);
                    float rough = 0.5f + Hash(x / 6, y / 6, 59) * 0.5f;
                    pixels[y * w + x] = new Color32(23, 37, 27, (byte)(body * body * rough * 53));
                }
            cloudTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            cloudTexture.filterMode = FilterMode.Bilinear;
            cloudTexture.SetPixels32(pixels);
            cloudTexture.Apply(false, true);
            cloudSprite = Sprite.Create(cloudTexture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 0.9f);
            spritesToDestroy.Add(cloudSprite);
            cloud.sprite = cloudSprite;
            cloudClock = 0f;
            cloud.transform.position = new Vector3(-60f, 95f, 0f);
        }

        Texture2D Texture(Color32[] pixels)
        {
            var texture = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        Sprite MakeSprite(Texture2D tex)
        {
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), PixelsPerMetre);
            spritesToDestroy.Add(sprite);
            return sprite;
        }

        void FitCamera(float width, float height)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Battlefield Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.orthographicSize = Mathf.Max(height * 0.51f,
                width * 0.51f / Mathf.Max(0.1f, cam.aspect));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.17f, 0.12f);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) LoadMap(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) LoadMap(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) LoadMap(2);

            if (windFrames == null) return;
            windClock += Time.unscaledDeltaTime;
            if (windClock >= WindFrameSeconds)
            {
                windClock -= WindFrameSeconds;
                windFrame = (windFrame + 1) % windFrames.Length;
                wind.sprite = windFrames[windFrame];
            }
            cloudClock += Time.unscaledDeltaTime * 0.6f;
            cloud.transform.position = new Vector3(-65f + Mathf.Repeat(cloudClock, 340f) - 100f,
                95f - Mathf.Sin(cloudClock * 0.025f) * 25f, 0);
        }

        void OnGUI()
        {
            // Temporary preview controls, not the final battle HUD.
            float s = Mathf.Max(1f, Screen.width / 520f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            GUILayout.BeginArea(new Rect(8, 8, Screen.width / s - 16, 112));
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("TREELINE  /  STEPPE PATCH  /  " + mapTitle);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < MapFiles.Length; i++)
                if (GUILayout.Button((i + 1).ToString() + " · " + (i == 0 ? "Forest" : i == 1 ? "Balka" : "River"),
                    GUILayout.Height(34))) { if (i != mapIndex) LoadMap(i); }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.matrix = Matrix4x4.identity;
        }

        void ClearGenerated()
        {
            if (ground != null) ground.sprite = null;
            if (wind != null) wind.sprite = null;
            if (cloud != null) cloud.sprite = null;
            foreach (var sprite in spritesToDestroy) if (sprite != null) Destroy(sprite);
            spritesToDestroy.Clear();
            if (groundTexture != null) Destroy(groundTexture);
            if (cloudTexture != null) Destroy(cloudTexture);
            if (windTextures != null) foreach (var t in windTextures) if (t != null) Destroy(t);
        }

        void OnDestroy() { ClearGenerated(); }

        int Idx(int x, int y) => (pixelHeight - 1 - y) * pixelWidth + x;

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint z = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
                z = (z ^ (z >> 13)) * 1274126177u;
                return (z ^ (z >> 16)) / (float)uint.MaxValue;
            }
        }

        static Color32 Tint(Color32 c, float amount)
        {
            float k = 0.88f + amount;
            return new Color32((byte)Mathf.Clamp(c.r * k, 0, 255),
                (byte)Mathf.Clamp(c.g * k, 0, 255),
                (byte)Mathf.Clamp(c.b * k, 0, 255), 255);
        }

        static string Kind(Dictionary<string, object> f) => Str(f, "type");
        static string Str(Dictionary<string, object> f, string key)
            => f.TryGetValue(key, out var x) ? x.ToString() : "";
        static float Num(Dictionary<string, object> f, string key, float fallback)
            => f.TryGetValue(key, out var x) ? Convert.ToSingle(x) : fallback;

        static Vector2[] Points(Dictionary<string, object> f, string key)
        {
            if (!f.TryGetValue(key, out var v) || !(v is List<object> raw)) return Array.Empty<Vector2>();
            var pts = new Vector2[raw.Count];
            for (int i = 0; i < raw.Count; i++)
            {
                var p = (List<object>)raw[i];
                pts[i] = new Vector2(Convert.ToSingle(p[0]), Convert.ToSingle(p[1]));
            }
            return pts;
        }

        void Bounds(Vector2[] poly, out int left, out int top, out int right, out int bottom)
        {
            float a = float.MaxValue, b = float.MaxValue, c = float.MinValue, d = float.MinValue;
            foreach (var p in poly)
            {
                a = Mathf.Min(a, p.x); b = Mathf.Min(b, p.y);
                c = Mathf.Max(c, p.x); d = Mathf.Max(d, p.y);
            }
            left = Mathf.Max(0, Mathf.FloorToInt(a * PixelsPerMetre));
            top = Mathf.Max(0, Mathf.FloorToInt(b * PixelsPerMetre));
            right = Mathf.Min(pixelWidth - 1, Mathf.CeilToInt(c * PixelsPerMetre));
            bottom = Mathf.Min(pixelHeight - 1, Mathf.CeilToInt(d * PixelsPerMetre));
        }

        static bool Inside(Vector2[] poly, float x, float y)
        {
            bool result = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > y) != (poly[j].y > y) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].y) /
                    (poly[j].y - poly[i].y) + poly[i].x)
                    result = !result;
            return result;
        }
    }
}
