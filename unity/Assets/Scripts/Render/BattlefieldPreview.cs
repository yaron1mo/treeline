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
        const float WindFrameSeconds = 0.23f;
        static readonly string[] MapFiles = { "forest-belts", "balka-and-railway", "river-crossing", "village-street", "village-crossroads" };

        SpriteRenderer ground, wind, cloud, cloudTwo, cloudThree;
        BattlefieldEffects effects;
        BattlefieldUnitLayer unitLayer;
        bool demoEffects;
        bool showUnits = true;
        Texture2D groundTexture, cloudTexture;
        Sprite[] windFrames;
        Texture2D[] windTextures;
        Sprite cloudSprite;
        int mapIndex, pixelWidth, pixelHeight, windFrame;
        float windClock, cloudClock, ambientSmokeClock;
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

        // Awake, not Start: the Game playtest may call ShowField before this component's Start.
        // This also fixes the NullReferenceException from LoadMap accessing an uninitialized ground.
        void Awake()
        {
            EnsureLayers();
        }

        void EnsureLayers()
        {
            if (ground != null) return;
            ground = Layer("Terrain", -100);
            wind = Layer("Moving crops", -98);
            cloud = Layer("Cloud shadow", -95);
            cloudTwo = Layer("Cloud shadow 2", -94);
            cloudThree = Layer("Cloud shadow 3", -93);
            var fxObject = new GameObject("Pooled battlefield FX");
            fxObject.transform.SetParent(transform, false);
            effects = fxObject.AddComponent<BattlefieldEffects>();
            var unitsObject = new GameObject("Unit sprites");
            unitsObject.transform.SetParent(transform, false);
            unitLayer = unitsObject.AddComponent<BattlefieldUnitLayer>();
        }

        void Start()
        {
            var debugView = FindFirstObjectByType<Treeline.Game.FieldDebugView>();
            if (debugView != null) Destroy(debugView.gameObject);
            EnsureLayers();
            // Let the PlaytestView choose its field; standalone art preview defaults to Forest.
            if (ground.sprite == null) LoadMap(0);
        }

        static bool DrivenByPlaytest() => GameObject.Find("PlaytestView") != null;

        // The Game assembly sends this message; Render depends on Game, not vice versa.
        public void ShowField(string resourceName)
        {
            EnsureLayers();
            int index = Array.IndexOf(MapFiles, resourceName);
            if (index < 0) return;
            if (index != mapIndex || ground.sprite == null) LoadMap(index);
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
            EnsureLayers();
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
            PaintAtmosphereDecor(terrain);
            ApplySunlight(terrain);
            MarkWater(terrain, crops);

            groundTexture = Texture(terrain);
            ground.sprite = MakeSprite(groundTexture);
            ground.transform.position = Vector3.zero;
            MakeWind(crops);
            MakeCloud();
            effects.Initialize(width, height);
            effects.SetDemo(demoEffects && !DrivenByPlaytest());
            unitLayer.Initialize(width, height);
            unitLayer.BindEffects(effects);
            // Preview catalog only when standalone; Playtest will send BindBattle.
            unitLayer.ShowPreview(showUnits && !DrivenByPlaytest());
            ambientSmokeClock = 0f;
            SeedAmbientSmoke();
            if (!DrivenByPlaytest()) FitCamera(width, height);
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
                if (Kind(f) == "field" || Kind(f) == "sunflower")
                {
                    string tone = Kind(f) == "sunflower" ? "sunflower" : Str(f, "tone");
                    int angle = Mathf.RoundToInt(Num(f, "angle", 0));
                    Color32 c = tone == "plowed" ? new Color32(150, 104, 67, 255)
                        : tone == "sunflower" ? new Color32(130, 154, 76, 255)
                        : new Color32(201, 169, 105, 255);
                    PaintPolygon(Points(f, "poly"), pixels, c, tone, angle, crops);
                }

            ScatterFieldDetail(pixels, crops);

            foreach (var f in entries)
            {
                string type = Kind(f);
                if (type == "field" || type == "sunflower" || type == "tracks" || type == "craters" ||
                    type == "grove" || type == "belt" || type == "bridge_ruins" ||
                    type == "house" || type == "ruin" || type == "fence" || type == "burned_belt") continue;

                Color32 tint;
                switch (type)
                {
                    case "pond": tint = new Color32(64, 111, 117, 255); break;
                    case "river":
                        DrawRiver(Points(f, "line"), Num(f, "width", 16f), pixels);
                        continue;
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
                    DrawTractorTracks(Points(f, "line"), pixels);

            foreach (var f in entries)
                if (Kind(f) == "craters" && f.TryGetValue("points", out var value) && value is List<object> spots)
                    foreach (var point in spots)
                    {
                        var coords = (List<object>)point;
                        float x = Convert.ToSingle(coords[0]), y = Convert.ToSingle(coords[1]);
                        float r = coords.Count > 2 ? Convert.ToSingle(coords[2]) : 1f;
                        DrawCrater(pixels, x, y, r);
                    }

            // Decorative, deliberately broken bridge approaches; they do not alter the navigation grid.
            foreach (var f in entries)
                if (Kind(f) == "bridge_ruins")
                    DrawBridgeRuins(Points(f, "line"), Num(f, "width", 7f), pixels);

            foreach (var f in entries)
            {
                if (Kind(f) == "belt") DrawBelt(Points(f, "line"), Num(f, "width", 14f), pixels);
                if (Kind(f) == "grove")
                {
                    if (mapIndex >= 3) DrawGarden(Points(f, "poly"), pixels);
                    else DrawGrove(Points(f, "poly"), pixels);
                }
                if (Kind(f) == "burned_belt") DrawBurnedBelt(Points(f, "line"), Num(f, "width", 14f), pixels);
                if (Kind(f) == "fence") DrawFence(Points(f, "line"), pixels);
                if (Kind(f) == "house") DrawVillageHouse(Points(f, "poly"), pixels, false);
                if (Kind(f) == "ruin") DrawVillageHouse(Points(f, "poly"), pixels, true);
            }
        }

        void DrawVillageHouse(Vector2[] poly, Color32[] pixels, bool ruin)
        {
            if (poly.Length < 3) return;
            Bounds(poly, out int l, out int t, out int r, out int b);
            float x0 = l / (float)PixelsPerMetre, x1 = r / (float)PixelsPerMetre;
            float y0 = t / (float)PixelsPerMetre, y1 = b / (float)PixelsPerMetre;
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
            int variant = (int)(Hash(l, t, 76) * 4f) % 4;
            PaintPolygon(poly, pixels, ruin ? new Color32(100, 91, 77, 255) :
                new Color32(62, 65, 59, 255), "house", 0, null);
            if (ruin)
            {
                Blob(pixels, cx, cy, (x1-x0)*0.35f, (y1-y0)*0.34f, new Color32(159, 141, 111, 255), 255);
                for (int i = 0; i < 7; i++)
                    Blob(pixels, x0 + (x1-x0)*Hash(l, i, 13), y0 + (y1-y0)*Hash(t, i, 17),
                        0.65f, 0.4f, new Color32(78, 69, 58, 255), 255);
                return;
            }
            Color32 roof = variant == 0 ? new Color32(112, 119, 116, 255) :
                variant == 1 ? new Color32(129, 120, 103, 255) :
                variant == 2 ? new Color32(91, 102, 98, 255) : new Color32(117, 108, 96, 255);
            Vector2[] inset = { new Vector2(x0+0.5f,y0+0.5f), new Vector2(x1-0.5f,y0+0.5f),
                new Vector2(x1-0.5f,y1-0.5f), new Vector2(x0+0.5f,y1-0.5f) };
            PaintPolygon(inset, pixels, roof, "roof", 0, null);
            for (float x = x0+1.5f; x < x1-1f; x += 2f)
                PaintPath(new[] { new Vector2(x,y0+0.8f), new Vector2(x,y1-0.8f) }, 0.35f, pixels, new Color32(75,82,78,255));
            PaintPath(new[] { new Vector2(x0+0.7f,cy), new Vector2(x1-0.7f,cy) }, 0.5f, pixels, new Color32(157,155,137,255));
            Blob(pixels, x0+(x1-x0)*0.3f, y0+(y1-y0)*0.3f, 0.8f, 0.8f, new Color32(72,62,54,255), 255);
            PaintPath(new[] { new Vector2(cx-1f,y1), new Vector2(cx+1f,y1) }, 1.2f, pixels, new Color32(153,139,111,255));
            if (variant == 3) Blob(pixels, cx+1f, cy-1f, 1.2f, 0.8f, new Color32(39,37,33,255), 255);
        }

        void DrawFence(Vector2[] line, Color32[] pixels)
        {
            PaintPath(line, 0.65f, pixels, new Color32(93, 73, 53, 255));
            for (int i = 1; i < line.Length; i++)
            {
                int count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(line[i-1], line[i]) / 3f));
                for (int j = 0; j <= count; j++)
                {
                    Vector2 p = Vector2.Lerp(line[i-1], line[i], j / (float)count);
                    Blob(pixels, p.x, p.y, 0.55f, 0.55f, new Color32(158, 135, 94, 255), 255);
                }
            }
        }

        void DrawGarden(Vector2[] poly, Color32[] pixels)
        {
            if (poly.Length < 3) return;
            Bounds(poly, out int l, out int t, out int r, out int b);
            for (float y = t / (float)PixelsPerMetre + 3f; y < b / (float)PixelsPerMetre; y += 6f)
                for (float x = l / (float)PixelsPerMetre + 3f; x < r / (float)PixelsPerMetre; x += 6f)
                    if (Inside(poly, x, y)) Tree(pixels, x, y, 1.8f, Mathf.RoundToInt(x+y));
        }

        void DrawBurnedBelt(Vector2[] line, float width, Color32[] pixels)
        {
            PaintPath(line, width, pixels, new Color32(90, 87, 76, 255));
            for (int i = 1; i < line.Length; i++)
            {
                Vector2 a = line[i-1], b = line[i];
                Vector2 normal = new Vector2(-(b-a).y, (b-a).x).normalized;
                int count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a,b) / 4f));
                for (int j = 0; j <= count; j++)
                {
                    Vector2 p = Vector2.Lerp(a,b,j/(float)count) + normal * (Hash(i,j,31)-0.5f)*width*0.7f;
                    Blob(pixels, p.x, p.y, 1.2f, 1.2f, new Color32(49,46,42,255), 255);
                    PaintPath(new[] { p, p + new Vector2(1.4f,1.8f) }, 0.65f, pixels, new Color32(35,34,31,255));
                }
            }
        }

        void DrawTractorTracks(Vector2[] path, Color32[] pixels)
        {
            if (path.Length < 2) return;
            // Two dark wheel ruts and the lighter strip between them, never gameplay obstacles.
            PaintPath(path, 3.2f, pixels, new Color32(174, 146, 95, 255));
            for (int i = 1; i < path.Length; i++)
            {
                Vector2 direction = (path[i] - path[i - 1]).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 offset = normal * (side * 1.05f);
                    Vector2[] track = { path[i - 1] + offset, path[i] + offset };
                    PaintPath(track, 0.47f, pixels, new Color32(107, 83, 54, 255));
                }
            }
        }

        void DrawCrater(Color32[] pixels, float x, float y, float nominalRadius)
        {
            int cx = Mathf.RoundToInt(x * 10f), cy = Mathf.RoundToInt(y * 10f);
            float r = nominalRadius * (0.70f + Hash(cx, cy, 81) * 0.78f);
            // Ejected loam and scorch: roughness, varying sizes and shared sunlight direction.
            Blob(pixels, x + 0.7f, y + 1.25f, r * 1.90f, r * 1.5f,
                new Color32(55, 52, 41, 255), 100);
            Blob(pixels, x, y, r * 1.75f, r * 1.58f,
                new Color32(194, 155, 100, 255), 168);
            for (int j = 0; j < 10; j++)
            {
                float angle = j * Mathf.PI * 0.2f +
                    Hash(cx, cy, j * 3 + 7) * 0.35f;
                float dist = r * (1.05f + Hash(cx, cy, j + 12) * 0.78f);
                Blob(pixels, x + Mathf.Cos(angle) * dist,
                    y + Mathf.Sin(angle) * dist,
                    r * (0.20f + Hash(cx, j, cy) * 0.26f),
                    r * (0.20f + Hash(cy, j, cx) * 0.24f),
                    j % 3 == 0 ? new Color32(210, 172, 119, 255)
                        : new Color32(114, 93, 69, 255), 185);
            }
            Blob(pixels, x + 0.25f, y + 0.30f, r * 1.07f, r * 0.91f,
                new Color32(71, 60, 49, 255), 239);
            Blob(pixels, x + 0.39f, y + 0.50f, r * 0.67f, r * 0.59f,
                new Color32(44, 43, 39, 255), 224);
        }

        void ApplySunlight(Color32[] pixels)
        {
            // Gentle NW-to-SE daylight gradient: texture is baked once, no dynamic 2D lights.
            // All object shadows are displaced to the SE in the same direction.
            float inverseX = 1f / Mathf.Max(1, pixelWidth - 1);
            float inverseY = 1f / Mathf.Max(1, pixelHeight - 1);
            for (int y = 0; y < pixelHeight; y++)
            {
                float fy = y * inverseY;
                for (int x = 0; x < pixelWidth; x++)
                {
                    float fx = x * inverseX;
                    float shade = 1.08f - 0.13f * (0.52f * fy + 0.48f * fx);
                    int i = Idx(x, y);
                    Color32 c = pixels[i];
                    pixels[i] = new Color32(
                        (byte)Mathf.Clamp(c.r * shade * 1.025f, 0f, 255f),
                        (byte)Mathf.Clamp(c.g * shade * 1.007f, 0f, 255f),
                        (byte)Mathf.Clamp(c.b * shade * 0.985f, 0f, 255f), c.a);
                }
            }
        }

        void PaintAtmosphereDecor(Color32[] pixels)
        {
            // All props below are render-only scale cues; no geometry or movement rules change.
            // Chosen by map, to keep a sparse and readable battlefield at phone size.
            switch (mapIndex)
            {
                case 0: // Forest Belts
                    PowerLine(pixels, new[] {
                        new Vector2(190, 26), new Vector2(193, 114),
                        new Vector2(190, 202), new Vector2(192, 300),
                        new Vector2(189, 374) });
                    HayBale(pixels, 24, 28, 2.5f);
                    HayBale(pixels, 32, 34, 1.9f);
                    HayBale(pixels, 65, 284, 2.2f);
                    RuinedHouse(pixels, 178, 120);
                    BurntVehicle(pixels, 112, 222);
                    break;
                case 1: // Balka and Railway
                    PowerLine(pixels, new[] {
                        new Vector2(189, 24), new Vector2(190, 112),
                        new Vector2(183, 226), new Vector2(192, 354) });
                    HayBale(pixels, 130, 304, 2.6f);
                    HayBale(pixels, 140, 311, 1.9f);
                    RuinedHouse(pixels, 177, 347);
                    BurntVehicle(pixels, 110, 228);
                    break;
                case 2: // River Crossing
                    // Cables do not cross the river where the bridge is destroyed.
                    PowerLine(pixels, new[] {
                        new Vector2(189, 35), new Vector2(185, 117) });
                    PowerLine(pixels, new[] {
                        new Vector2(188, 295), new Vector2(190, 365) });
                    HayBale(pixels, 31, 86, 2.5f);
                    HayBale(pixels, 46, 90, 2.0f);
                    RuinedHouse(pixels, 180, 125);
                    BurntVehicle(pixels, 159, 274);
                    break;
            }
        }

        void PowerLine(Color32[] pixels, Vector2[] poles)
        {
            if (poles.Length < 1) return;
            for (int i = 1; i < poles.Length; i++)
            {
                Vector2[] cable = { poles[i - 1], poles[i] };
                PaintPath(cable, 0.24f, pixels, new Color32(99, 98, 81, 255));
            }
            foreach (Vector2 p in poles)
            {
                // Strong SE shadow from the same sun direction as all the tree crowns.
                Vector2[] shadow = { p + new Vector2(0.5f, 0.7f),
                    p + new Vector2(3.7f, 6.1f) };
                PaintPath(shadow, 1.0f, pixels, new Color32(55, 57, 47, 255));
                Blob(pixels, p.x, p.y, 0.9f, 0.9f, new Color32(78, 69, 55, 255), 255);
                Vector2[] bar = {
                    p + new Vector2(-2.0f, -0.2f),
                    p + new Vector2(2.0f, -0.2f)
                };
                PaintPath(bar, 0.8f, pixels, new Color32(50, 49, 40, 255));
                Blob(pixels, p.x - 1.8f, p.y - 0.2f, 0.5f, 0.5f,
                    new Color32(222, 213, 172, 255), 255);
                Blob(pixels, p.x + 1.8f, p.y - 0.2f, 0.5f, 0.5f,
                    new Color32(222, 213, 172, 255), 255);
            }
        }

        void HayBale(Color32[] pixels, float x, float y, float radius)
        {
            Blob(pixels, x + 1.8f, y + 2.1f, radius * 1.14f, radius * 0.8f,
                new Color32(54, 58, 43, 255), 163);
            Blob(pixels, x, y, radius, radius * 0.73f,
                new Color32(146, 105, 61, 255), 255);
            Blob(pixels, x - 0.35f, y - 0.3f, radius * 0.8f, radius * 0.57f,
                new Color32(225, 188, 103, 255), 255);
            Blob(pixels, x - 0.6f, y - 0.35f, radius * 0.34f, radius * 0.2f,
                new Color32(245, 209, 124, 255), 190);
        }

        void RuinedHouse(Color32[] pixels, float x, float y)
        {
            // A derelict single-storey house footprint: no Sim blocker.
            Vector2 back = new Vector2(x, y);
            Vector2 front = new Vector2(x, y + 8.0f);
            FillBridgeQuad(pixels, back + new Vector2(3f, 3.5f),
                front + new Vector2(3f, 3.5f), 11f, new Color32(48, 51, 41, 255));
            FillBridgeQuad(pixels, back, front, 10.8f,
                new Color32(84, 78, 64, 255));
            FillBridgeQuad(pixels, back + new Vector2(-0.6f, 0.5f),
                front + new Vector2(-0.6f, -1.0f), 7f,
                new Color32(173, 146, 108, 255));
            Blob(pixels, x + 1.3f, y + 3.3f, 3.0f, 2.3f,
                new Color32(35, 34, 29, 255), 255);
            Vector2[] beam = {
                new Vector2(x - 2.9f, y + 1.0f),
                new Vector2(x + 2.7f, y + 7.3f)
            };
            PaintPath(beam, 0.9f, pixels, new Color32(69, 58, 43, 255));
        }

        void BurntVehicle(Color32[] pixels, float x, float y)
        {
            Blob(pixels, x, y, 6f, 5.0f, new Color32(64, 52, 41, 255), 127);
            Blob(pixels, x + 2.4f, y + 2.8f, 4.8f, 3.0f,
                new Color32(33, 37, 32, 255), 173);
            FillBridgeQuad(pixels, new Vector2(x, y - 3.5f),
                new Vector2(x, y + 3.5f), 4.2f,
                new Color32(47, 49, 44, 255));
            FillBridgeQuad(pixels, new Vector2(x + 0.1f, y - 2.3f),
                new Vector2(x + 0.1f, y + 1.2f), 3.3f,
                new Color32(99, 75, 57, 255));
            Blob(pixels, x + 0.5f, y + 0.9f, 1.4f, 1.9f,
                new Color32(23, 28, 27, 255), 241);
        }

        void ScatterFieldDetail(Color32[] pixels, byte[] crops)
        {
            // Individual stalks would be wasteful; a few low-resolution marks imply crop texture.
            // These are baked once and will be covered by roads, water, tracks and later objects.
            for (int y = 2; y < pixelHeight - 2; y += 7)
                for (int x = 2; x < pixelWidth - 2; x += 7)
                {
                    int cell = Idx(x, y);
                    float n = Hash(x / 7, y / 7, 42);
                    float wx = x / (float)PixelsPerMetre;
                    float wy = y / (float)PixelsPerMetre;
                    if (crops[cell] == 2 && n > 0.32f)
                    {
                        Blob(pixels, wx, wy, 0.55f, 0.65f,
                            new Color32(66, 99, 45, 255), 178);
                        if (n > 0.70f)
                            Blob(pixels, wx - 0.16f, wy - 0.25f, 0.27f, 0.28f,
                                new Color32(212, 184, 93, 255), 195);
                    }
                    else if (crops[cell] == 1 && n > 0.69f)
                        Blob(pixels, wx, wy, 0.4f, 0.5f,
                            new Color32(230, 199, 125, 255), 180);
                }
        }

        void PaintPolygon(Vector2[] poly, Color32[] pixels, Color32 color,
            string tone, int angle, byte[] crops)
        {
            if (poly.Length < 3) return;
            Bounds(poly, out int left, out int top, out int right, out int bottom);
            // These are visual-only fringes; the actual map polygon and movement mask stay unchanged.
            bool naturalField = tone == "plowed" || tone == "stubble" || tone == "sunflower";
            int fringe = naturalField ? 7 * PixelsPerMetre : 0;
            left = Mathf.Max(0, left - fringe);
            top = Mathf.Max(0, top - fringe);
            right = Mathf.Min(pixelWidth - 1, right + fringe);
            bottom = Mathf.Min(pixelHeight - 1, bottom + fringe);
            float degrees = angle * Mathf.Deg2Rad;
            float dx = Mathf.Cos(degrees), dy = Mathf.Sin(degrees);
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
                {
                    float wx = (x + 0.5f) / PixelsPerMetre;
                    float wy = (y + 0.5f) / PixelsPerMetre;
                    bool inside = Inside(poly, wx, wy);
                    if (!inside && !naturalField) continue;
                    float blend = 1f;
                    if (naturalField)
                    {
                        float distance = EdgeDistance(poly, wx, wy);
                        if (!inside && distance > 7f) continue;
                        // Irregular, feathered strips break straight field borders only.
                        float irregular = (Hash(x / 13, y / 13, 71) - 0.5f) * 3f +
                            (Hash(x / 31, y / 31, 37) - 0.5f) * 3.6f;
                        float signed = (inside ? distance : -distance) + irregular;
                        blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((signed + 4.3f) / 8.6f));
                        if (blend < 0.01f) continue;
                    }

                    // Broad field patches and actual crop/furrow ROWS instead of uniform noise.
                    float across = wx * dx + wy * dy;
                    float spacing = tone == "plowed" ? 4.7f : tone == "sunflower" ? 3.6f : 5.2f;
                    float row = Mathf.Repeat(across, spacing) / spacing;
                    float ridge = tone == "plowed"
                        ? (row < 0.22f ? -0.23f : row > 0.80f ? 0.14f : -0.015f)
                        : tone == "sunflower"
                            ? (row < 0.26f ? -0.18f : 0.07f)
                            : (row < 0.12f ? -0.12f : 0.02f);
                    float patch = Hash(x / 45, y / 43, 24) * 0.14f +
                        Hash(x / 19, y / 23, 66) * 0.07f;
                    float grain = Hash(x / 3, y / 3, 7) * 0.07f +
                        Hash(x, y, 19) * 0.04f;
                    int i = Idx(x, y);
                    Color32 target = Tint(color, patch + grain + ridge);
                    pixels[i] = Mix(pixels[i], target, blend);
                    if (crops != null && inside && blend > 0.58f)
                        crops[i] = tone == "sunflower" ? (byte)2 :
                            tone == "stubble" ? (byte)1 : (byte)0;
                }
        }

        static Color32 Mix(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)(a.r + (b.r - a.r) * t),
                (byte)(a.g + (b.g - a.g) * t),
                (byte)(a.b + (b.b - a.b) * t), 255);
        }

        float EdgeDistance(Vector2[] poly, float x, float y)
        {
            float best = 1e6f;
            float boundX = pixelWidth / (float)PixelsPerMetre;
            float boundY = pixelHeight / (float)PixelsPerMetre;
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
                // Do not fade the outer bounds of the map, only real field transitions.
                if ((a.x <= 0f && b.x <= 0f) ||
                    (a.x >= boundX && b.x >= boundX) ||
                    (a.y <= 0f && b.y <= 0f) ||
                    (a.y >= boundY && b.y >= boundY)) continue;
                Vector2 ab = b - a;
                float projection = Mathf.Clamp01(Vector2.Dot(new Vector2(x, y) - a, ab) /
                    Mathf.Max(0.00001f, ab.sqrMagnitude));
                float d = Vector2.Distance(new Vector2(x, y), a + projection * ab);
                if (d < best) best = d;
            }
            return best;
        }

        void DrawRiver(Vector2[] centreline, float width, Color32[] pixels)
        {
            if (centreline.Length < 2) return;
            // Subtle curvature and bands of wet earth make the riverbanks look natural.
            Vector2[] smooth = SmoothLine(centreline);
            PaintPath(smooth, width + 8.5f, pixels, new Color32(118, 130, 82, 255));
            PaintPath(smooth, width + 4.0f, pixels, new Color32(166, 148, 104, 255));
            PaintPath(smooth, width + 1.0f, pixels, new Color32(47, 91, 100, 255));
            PaintPath(smooth, width - 1.2f, pixels, new Color32(77, 131, 144, 255));
        }

        static Vector2[] SmoothLine(Vector2[] points)
        {
            var output = new List<Vector2>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 p0 = points[Mathf.Max(0, i - 1)];
                Vector2 p1 = points[i];
                Vector2 p2 = points[i + 1];
                Vector2 p3 = points[Mathf.Min(points.Length - 1, i + 2)];
                int parts = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) / 2f));
                for (int j = 0; j < parts; j++)
                {
                    float t = j / (float)parts;
                    float t2 = t * t, t3 = t2 * t;
                    // Catmull-Rom: keeps the original anchor points and smooths only the visual edge.
                    output.Add(0.5f * ((2f * p1) + (-p0 + p2) * t +
                        (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                        (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            output.Add(points[points.Length - 1]);
            return output.ToArray();
        }

        void DrawBridgeRuins(Vector2[] line, float width, Color32[] pixels)
        {
            if (line.Length < 2) return;
            Vector2 a = line[0], b = line[line.Length - 1];
            Vector2 along = (b - a).normalized;
            Vector2 sideways = new Vector2(-along.y, along.x);
            // A broken causeway, not two boats: straight-sided dark abutments and wooden decking.
            // Both ends visibly stop before the water; the gap is intentionally not passable.
            for (int side = 0; side < 2; side++)
            {
                float from = side == 0 ? 0f : 0.70f;
                float to = side == 0 ? 0.30f : 1f;
                Vector2 start = Vector2.Lerp(a, b, from);
                Vector2 finish = Vector2.Lerp(a, b, to);
                FillBridgeQuad(pixels, start + new Vector2(1.2f, 1f),
                    finish + new Vector2(1.2f, 1f), width + 2f,
                    new Color32(34, 42, 37, 255));
                FillBridgeQuad(pixels, start, finish, width + 1.2f,
                    new Color32(51, 53, 48, 255));
                FillBridgeQuad(pixels, start, finish, width - 0.5f,
                    new Color32(161, 140, 108, 255));
                float length = Vector2.Distance(start, finish);
                for (float dist = 1.1f; dist < length; dist += 2f)
                {
                    Vector2 c = start + along * dist;
                    Vector2[] plank = {
                        c - sideways * width * 0.43f,
                        c + sideways * width * 0.43f
                    };
                    PaintPath(plank, 0.5f, pixels, new Color32(82, 74, 60, 255));
                }
                Vector2 severed = side == 0 ? finish : start;
                Vector2[] brokenRim = {
                    severed - sideways * (width * 0.5f),
                    severed + sideways * (width * 0.5f)
                };
                PaintPath(brokenRim, 1.4f, pixels, new Color32(44, 44, 40, 255));
            }
            Vector2 water = Vector2.Lerp(a, b, 0.5f);
            // Short broken beams and scattered fragments inside the missing span.
            for (int i = 0; i < 6; i++)
            {
                float x = water.x + (Hash(i, 13, 8) - 0.5f) * 12f;
                float y = water.y + (Hash(i, 9, 4) - 0.5f) * 10f;
                Blob(pixels, x, y, 0.6f + Hash(i, 15, 5), 0.75f,
                    new Color32(83, 78, 64, 255), 218);
            }
        }

        void FillBridgeQuad(Color32[] pixels, Vector2 a, Vector2 b,
            float width, Color32 color)
        {
            Vector2 tangent = (b - a).normalized;
            Vector2 normal = new Vector2(-tangent.y, tangent.x) * (width * 0.5f);
            Vector2[] corners = { a + normal, b + normal, b - normal, a - normal };
            Bounds(corners, out int l, out int t, out int r, out int bottom);
            for (int y = t; y <= bottom; y++)
                for (int x = l; x <= r; x++)
                    if (Inside(corners, (x + 0.5f) / PixelsPerMetre,
                        (y + 0.5f) / PixelsPerMetre))
                        pixels[Idx(x, y)] = color;
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
                        float along = (Hash(j, i, side + 91) - 0.5f) * 2.6f;
                        Vector2 t = p + (b - a).normalized * along + normal *
                            (side * width * 0.25f + (variation - 0.5f) * 4.6f);
                        Tree(pixels, t.x, t.y, 2.3f + variation * 2f, j + i * 19 + side);
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
            Blob(pixels, x + 1.7f, y + 2.1f, radius * 1.24f, radius * 0.85f,
                new Color32(20, 34, 27, 255), 134);
            Blob(pixels, x, y, radius, radius * 0.9f,
                new Color32(31, 65, 36, 255), 255);
            Blob(pixels, x - 0.2f, y - 0.3f, radius * 0.82f, radius * 0.7f,
                v > 0.5f ? new Color32(66, 111, 52, 255) : new Color32(76, 115, 58, 255), 255);
            Blob(pixels, x + radius * 0.32f, y + radius * 0.12f,
                radius * 0.45f, radius * 0.32f, new Color32(35, 76, 37, 255), 165);
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

        void MarkWater(Color32[] pixels, byte[] crops)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = pixels[i];
                // Only final blue/teal water surfaces; ford, roads, and plants remain distinct.
                if (c.b > c.r + 18 && c.g > c.r + 16 && c.b >= 104)
                    crops[i] = 3;
            }
        }

        void MakeWind(byte[] crops)
        {
            windFrames = new Sprite[6];
            windTextures = new Texture2D[6];
            for (int frame = 0; frame < windFrames.Length; frame++)
            {
                var pixels = new Color32[crops.Length];
                for (int y = 0; y < pixelHeight; y++)
                    for (int x = 0; x < pixelWidth; x++)
                    {
                        int i = Idx(x, y);
                        if (crops[i] == 0) continue;
                        float phase = x * 0.077f + y * 0.024f + frame * Mathf.PI / 3f;
                        float strength = Mathf.Sin(phase) * 0.5f + 0.5f;
                        float ripple = Mathf.Sin(y * 0.25f + x * 0.11f - frame * Mathf.PI / 3f);
                        if (crops[i] == 3)
                        {
                            // Fine water reflections move with the same wind phase, no particles needed.
                            float glint = Mathf.Pow(Mathf.Max(0f,
                                Mathf.Sin(x * 0.18f + y * 0.07f + frame * Mathf.PI / 3f)), 6f);
                            pixels[i] = new Color32(196, 219, 197, (byte)(glint * 36f));
                            continue;
                        }
                        byte alpha = (byte)((crops[i] == 2 ? 91 : 54) * strength *
                            (0.7f + 0.3f * (0.5f + 0.5f * ripple)));
                        pixels[i] = new Color32(237, 218, 138, alpha);
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
                    pixels[y * w + x] = new Color32(22, 31, 24, (byte)(body * body * rough * 96));
                }
            cloudTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            cloudTexture.filterMode = FilterMode.Bilinear;
            cloudTexture.SetPixels32(pixels);
            cloudTexture.Apply(false, true);
            cloudSprite = Sprite.Create(cloudTexture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 0.9f);
            spritesToDestroy.Add(cloudSprite);
            cloud.sprite = cloudSprite;
            cloudTwo.sprite = cloudSprite;
            cloudThree.sprite = cloudSprite;
            cloudTwo.transform.localScale = new Vector3(1.4f, 1.1f, 1f);
            cloudThree.transform.localScale = new Vector3(0.85f, 1.25f, 1f);
            cloudClock = 0f;
            cloud.transform.localPosition = new Vector3(-70f, 95f, 0f);
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

        void SeedAmbientSmoke()
        {
            if (mapIndex >= 3) return;
            // Wrecks are graphic-only scale cues; the smoke is purely decorative.
            Vector2 wreck = mapIndex == 0 ? new Vector2(112, 222)
                : mapIndex == 1 ? new Vector2(110, 228)
                : new Vector2(159, 274);
            effects.WreckSmoke(wreck);
        }

        void Update()
        {
            if (windFrames == null) return;
            windClock += Time.unscaledDeltaTime;
            if (windClock >= WindFrameSeconds)
            {
                windClock -= WindFrameSeconds;
                windFrame = (windFrame + 1) % windFrames.Length;
                wind.sprite = windFrames[windFrame];
            }
            ambientSmokeClock += Time.unscaledDeltaTime;
            if (ambientSmokeClock > 15f)
            {
                ambientSmokeClock = 0f;
                SeedAmbientSmoke();
            }
            cloudClock += Time.unscaledDeltaTime * 2.3f;
            float drift = Mathf.Repeat(cloudClock, 460f);
            cloud.transform.localPosition = new Vector3(-220f + drift,
                90f - Mathf.Sin(cloudClock * 0.026f) * 14f, 0f);
            cloudTwo.transform.localPosition = new Vector3(-320f + Mathf.Repeat(drift + 190f, 470f),
                -17f - Mathf.Sin(cloudClock * 0.018f) * 21f, 0f);
            cloudThree.transform.localPosition = new Vector3(-230f + Mathf.Repeat(drift + 320f, 470f),
                -142f + Mathf.Cos(cloudClock * 0.029f) * 17f, 0f);
        }

        void OnGUI()
        {
            // The existing playtest owns this GUI and input; never cover its actual battle.
            if (DrivenByPlaytest()) return;
            // Temporary preview controls, not the final battle HUD.
            float s = Mathf.Max(1f, Screen.width / 520f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            GUILayout.BeginArea(new Rect(8, 8, Screen.width / s - 16, 208));
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("TREELINE  /  STEPPE PATCH  /  " + mapTitle);
            GUILayout.Label("ART PREVIEW — all 23 unit types / no simulated battle");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < MapFiles.Length; i++)
                if (GUILayout.Button((i + 1).ToString() + " · " + MapFiles[i],
                    GUILayout.Height(34))) { if (i != mapIndex) LoadMap(i); }
            GUILayout.EndHorizontal();
            if (GUILayout.Button(showUnits ? "UNITS: ON — 23 types" : "UNITS: OFF",
                GUILayout.Height(30)))
            {
                showUnits = !showUnits;
                if (unitLayer != null) unitLayer.ShowPreview(showUnits);
            }
            if (GUILayout.Button(demoEffects ? "FX DEMO: ON (not Sim)" : "FX DEMO: OFF",
                GUILayout.Height(30)))
            {
                demoEffects = !demoEffects;
                if (effects != null) effects.SetDemo(demoEffects);
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.matrix = Matrix4x4.identity;
        }

        void ClearGenerated()
        {
            if (ground != null) ground.sprite = null;
            if (wind != null) wind.sprite = null;
            if (cloud != null) cloud.sprite = null;
            if (cloudTwo != null) cloudTwo.sprite = null;
            if (cloudThree != null) cloudThree.sprite = null;
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
