using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace Treeline.Render
{
    /// <summary>
    /// Original point-filtered 2D sprites for every unit ID in units.csv.
    /// One small, 18-color atlas packed as a text/GZip asset: no binary importer or LFS dependency.
    /// Each 48x48 atlas tile is oriented toward the top of the screen.
    /// PNG export/repack: tools/art/unit_atlas.py (palette and tile order are shared).
    /// </summary>
    public sealed class UnitSpriteAtlas : IDisposable
    {
        const int Tile = 48;
        const int Columns = 8;
        const int Rows = 3;
        const int Width = Columns * Tile;
        const int Height = Rows * Tile;
        const float PixelsPerWorldMetre = 4f;

        public static readonly string[] Ids =
        {
            "recruit", "regular", "special_forces", "old_tank",
            "modern_tank", "ifv", "mrap", "buggy",
            "motorbike", "van", "recon_uav", "fpv",
            "one_way_drone", "helicopter", "rifle_squad", "machine_gun",
            "sniper", "mortar", "anti_tank", "air_defence",
            "fpv_operator", "bomber_operator", "recon_drone"
        };

        static readonly Color32[] Palette =
        {
            new Color32(0, 0, 0, 0),
            new Color32(32, 42, 37, 255),
            new Color32(38, 43, 43, 255),
            new Color32(41, 46, 43, 255),
            new Color32(48, 58, 47, 255),
            new Color32(57, 62, 50, 255),
            new Color32(71, 91, 82, 255),
            new Color32(73, 90, 78, 255),
            new Color32(102, 118, 76, 255),
            new Color32(113, 91, 70, 255),
            new Color32(122, 133, 112, 255),
            new Color32(127, 139, 89, 255),
            new Color32(137, 130, 105, 255),
            new Color32(153, 76, 58, 255),
            new Color32(183, 160, 118, 255),
            new Color32(203, 181, 141, 255),
            new Color32(212, 203, 175, 255),
            new Color32(214, 173, 72, 255)
        };

        readonly Dictionary<string, Sprite> byId =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Sprite[]> walkFrames =
            new Dictionary<string, Sprite[]>(StringComparer.OrdinalIgnoreCase);
        Sprite[] recruitCrutchFrames;
        Sprite[] recruitDiagonalWalkFrames;
        Sprite[] recruitDiagonalCrutchFrames;
        readonly List<Texture2D> walkTextures = new List<Texture2D>();
        readonly Dictionary<string, Sprite> aimSprites = new Dictionary<string, Sprite>();
        readonly Dictionary<string, Sprite> headlessSprites = new Dictionary<string, Sprite>();
        readonly Dictionary<string, Sprite> helmetSprites = new Dictionary<string, Sprite>();
        readonly Dictionary<string, Sprite> snoutSprites = new Dictionary<string, Sprite>();
        readonly Dictionary<string, Sprite> cleanSnouts = new Dictionary<string, Sprite>();
        readonly Dictionary<string, Sprite> looseHelmets = new Dictionary<string, Sprite>();
        readonly Dictionary<string, Sprite> bareBodies = new Dictionary<string, Sprite>();
        readonly List<Texture2D> infantryTextures = new List<Texture2D>();
        readonly Dictionary<string, Sprite[]> fallPoses = new Dictionary<string, Sprite[]>();
        Texture2D atlasTexture;

        public UnitSpriteAtlas()
        {
            TextAsset packed = Resources.Load<TextAsset>("Art/Units/units_atlas_indexed");
            if (packed == null)
                throw new InvalidOperationException("Missing Art/Units/units_atlas_indexed.txt");

            byte[] compressed = Convert.FromBase64String(packed.text.Trim());
            byte[] indices = new byte[Width * Height];
            using (var bytes = new MemoryStream(compressed))
            using (var gzip = new GZipStream(bytes, CompressionMode.Decompress))
            {
                int read = 0;
                while (read < indices.Length)
                {
                    int count = gzip.Read(indices, read, indices.Length - read);
                    if (count <= 0) throw new InvalidDataException("Incomplete sprite atlas");
                    read += count;
                }
            }

            var pixels = new Color32[Width * Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    byte colorIndex = indices[y * Width + x];
                    if (colorIndex >= Palette.Length)
                        throw new InvalidDataException("Invalid unit palette index " + colorIndex);
                    pixels[(Height - 1 - y) * Width + x] = Palette[colorIndex];
                }

            atlasTexture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            atlasTexture.filterMode = FilterMode.Point;
            atlasTexture.wrapMode = TextureWrapMode.Clamp;
            atlasTexture.SetPixels32(pixels);
            atlasTexture.Apply(false, true);

            for (int i = 0; i < Ids.Length; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                var rect = new Rect(column * Tile, Height - (row + 1) * Tile, Tile, Tile);
                byId[Ids[i]] = Sprite.Create(atlasTexture, rect,
                    new Vector2(0.5f, 0.5f), PixelsPerWorldMetre,
                    0, SpriteMeshType.FullRect);
            }

            for (int unit = 0; unit < 3; unit++)
            {
                const int frameCount = 4;
                const int size = 64;
                int variantCount = 1;
                int atlasWidth = size * frameCount * variantCount;
                var idle = LoadInfantryTexture(Ids[unit], "idle");
                var aim = LoadInfantryTexture(Ids[unit], "aim");
                UnityEngine.Object.Destroy(byId[Ids[unit]]);
                byId[Ids[unit]] = Sprite.Create(idle, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), size / 12f, 0, SpriteMeshType.FullRect);
                aimSprites[Ids[unit]] = Sprite.Create(aim, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), size / 12f, 0, SpriteMeshType.FullRect);
                Color32[] idlePixels = idle.GetPixels32();
                {
                    var snoutPixels = new Color32[10 * 8];
                    for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 10; x++)
                    {
                        int snoutBottom = unit == 2 ? 23 : 19;
                        Color32 pixel = idlePixels[(snoutBottom + y) * size + 27 + x];
                        bool pink = pixel.r > 140 && pixel.r > pixel.g * 1.2f && pixel.r > pixel.b * 1.1f;
                        bool nostril = x >= 2 && x <= 7 && y >= 2 && y <= 5 && pixel.r < 140;
                        if (pink || nostril) snoutPixels[y * 10 + x] = pixel;
                    }
                    var snoutTexture = new Texture2D(10, 8, TextureFormat.RGBA32, false);
                    snoutTexture.filterMode = FilterMode.Point;
                    snoutTexture.SetPixels32(snoutPixels);
                    snoutTexture.Apply();
                    infantryTextures.Add(snoutTexture);
                    cleanSnouts[Ids[unit]] = Sprite.Create(snoutTexture, new Rect(0, 0, 10, 8),
                        new Vector2(0.5f, 0.5f), size / 12f, 0, SpriteMeshType.FullRect);
                }
                fallPoses[Ids[unit]] = BuildFallPoses(idlePixels);
                var headlessPixels = (Color32[])idlePixels.Clone();
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (y >= 29 || (y >= 20 && x >= 23 && x < 41))
                        headlessPixels[y * size + x] = new Color32(0, 0, 0, 0);
                var headless = new Texture2D(size, size, TextureFormat.RGBA32, false);
                headless.filterMode = FilterMode.Point;
                headless.SetPixels32(headlessPixels);
                headless.Apply();
                infantryTextures.Add(headless);
                headlessSprites[Ids[unit]] = Sprite.Create(headless, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), size / 12f, 0, SpriteMeshType.FullRect);
                helmetSprites[Ids[unit]] = Sprite.Create(idle, new Rect(0, 29, 64, 35),
                    new Vector2(0.5f, 0.5f), size / 12f, 0, SpriteMeshType.FullRect);
                snoutSprites[Ids[unit]] = Sprite.Create(idle, new Rect(22, 18, 20, 12),
                    new Vector2(0.5f, 0.5f), size / 12f, 0, SpriteMeshType.FullRect);
                var walkPixels = new Color32[atlasWidth * size];
                int[] forwardLeft = { -3, -2, -4, -1, -3 };
                int[] forwardRight = { 2, 3, 1, 3, 1 };
                int[] backLeft = { 1, 3, 2, 2, 3 };
                int[] backRight = { -2, -2, -1, -3, -1 };
                for (int variant = 0; variant < variantCount; variant++)
                for (int frame = 0; frame < frameCount; frame++)
                {
                    int frameX = (variant * frameCount + frame) * size;
                    for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        int stride = frame == 1 ? (x < size / 2 ? forwardLeft[variant] : forwardRight[variant]) :
                            frame == 3 ? (x < size / 2 ? backLeft[variant] : backRight[variant]) : 0;
                        float footWeight = Mathf.Clamp01((17f - y) / 12f);
                        int sourceY = Mathf.Clamp(Mathf.RoundToInt(y - stride * footWeight), 0, size - 1);
                        walkPixels[y * atlasWidth + frameX + x] = idlePixels[sourceY * size + x];
                    }
                }
                var texture = new Texture2D(atlasWidth, size, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(walkPixels);
                texture.Apply(false, true);
                walkTextures.Add(texture);
                var frames = new Sprite[frameCount * variantCount];
                for (int frame = 0; frame < frames.Length; frame++)
                    frames[frame] = Sprite.Create(texture, new Rect(frame * size, 0, size, size),
                        new Vector2(0.5f, 0.5f), size / 12f, 0, SpriteMeshType.FullRect);
                if (unit < 3)
                {
                    var drawnWalk = LoadInfantryTexture(Ids[unit], "walk");
                    for (int frame = 0; frame < frames.Length; frame++)
                    {
                        UnityEngine.Object.Destroy(frames[frame]);
                        frames[frame] = Sprite.Create(drawnWalk, new Rect(frame * size, 0, size, size),
                            new Vector2(0.5f, 0.5f), size / 12f, 0, SpriteMeshType.FullRect);
                    }
                }
                walkFrames[Ids[unit]] = frames;
                if (unit == 0)
                {
                    var crutch = LoadInfantryTexture(Ids[unit], "crutch");
                    recruitCrutchFrames = new Sprite[frameCount];
                    for (int frame = 0; frame < frameCount; frame++)
                        recruitCrutchFrames[frame] = Sprite.Create(crutch, new Rect(frame * size, 0, size, size),
                            new Vector2(0.5f, 0.5f), size / 12f, 0, SpriteMeshType.FullRect);
                    recruitDiagonalWalkFrames = LoadRecruitDiagonalWalk("diagonal_walk");
                    recruitDiagonalCrutchFrames = LoadRecruitDiagonalWalk("diagonal_crutch_walk");
                }
            }
        }

        public Sprite GetWalkFrame(string unitId, int frame, int variant = 0) =>
            unitId == "recruit" && variant == 0 ? recruitCrutchFrames[frame] : walkFrames[unitId][frame];

        Sprite[] LoadRecruitDiagonalWalk(string pose)
        {
            var texture = LoadInfantryTexture("recruit", pose);
            var frames = new Sprite[4];
            for (int frame = 0; frame < frames.Length; frame++)
                frames[frame] = Sprite.Create(texture, new Rect(frame * 64, 0, 64, 64),
                    new Vector2(0.5f, 0.5f), 64f / 12f, 0, SpriteMeshType.FullRect);
            return frames;
        }

        public Sprite GetRecruitDiagonalWalkFrame(int frame, int variant) =>
            variant == 0 ? recruitDiagonalCrutchFrames[frame] : recruitDiagonalWalkFrames[frame];

        public Sprite GetAim(string unitId) => aimSprites[unitId];
        public Sprite GetHeadless(string unitId) => headlessSprites[unitId];
        public Sprite GetHelmet(string unitId) => helmetSprites[unitId];
        public Sprite GetSnout(string unitId) => snoutSprites[unitId];
        public Sprite GetCleanSnout(string unitId) => cleanSnouts[unitId];

        public Sprite GetLooseHelmet(string unitId) => GetStaticInfantrySprite(unitId, "loose_helmet", looseHelmets);
        public Sprite GetBareBody(string unitId) => GetStaticInfantrySprite(unitId, "bare", bareBodies);

        Sprite GetStaticInfantrySprite(string unitId, string pose, Dictionary<string, Sprite> cache)
        {
            if (!cache.TryGetValue(unitId, out var sprite))
            {
                var texture = LoadInfantryTexture(unitId, pose);
                sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f),
                    64f / 12f, 0, SpriteMeshType.FullRect);
                cache[unitId] = sprite;
            }
            return sprite;
        }

        public Sprite GetFallPose(string unitId, int pose) => fallPoses[unitId][pose];

        readonly Dictionary<string, Sprite[]> infantryFallFrames = new Dictionary<string, Sprite[]>();
        readonly Dictionary<string, Sprite[]> infantryShootFrames = new Dictionary<string, Sprite[]>();

        public Sprite GetInfantryShootFrame(string unitId, int frame, int variant, bool diagonal = false)
        {
            string artId = unitId == "recruit" && diagonal
                ? (variant == 0 ? "recruit_diagonal_crutch" : "recruit_diagonal")
                : unitId == "recruit" && variant == 0 ? "recruit_crutch" : unitId;
            if (!infantryShootFrames.TryGetValue(artId, out var frames))
            {
                Texture2D texture = LoadInfantryTexture(artId, "shoot");
                frames = new Sprite[3];
                for (int i = 0; i < frames.Length; i++)
                    frames[i] = Sprite.Create(texture, new Rect(i * 64, 0, 64, 64),
                        new Vector2(0.5f, 0.5f), 64f / 12f, 0, SpriteMeshType.FullRect);
                infantryShootFrames[artId] = frames;
            }
            return frames[frame];
        }
        public Sprite GetInfantryFallFrame(string unitId, int frame, bool diagonal = false, int variant = 1)
        {
            string key = unitId == "recruit" && variant == 0
                ? (diagonal ? "recruit_diagonal_crutch" : "recruit_crutch")
                : diagonal && unitId == "recruit" ? "recruit_diagonal" : unitId;
            if (!infantryFallFrames.TryGetValue(key, out var frames))
            {
                Texture2D texture = LoadInfantryTexture(key, "fall");
                frames = new Sprite[6];
                for (int i = 0; i < frames.Length; i++)
                    frames[i] = Sprite.Create(texture, new Rect(i * 64, 0, 64, 64),
                        new Vector2(0.5f, 0.5f), 64f / 12f, 0, SpriteMeshType.FullRect);
                infantryFallFrames[key] = frames;
            }
            return frames[frame];
        }

        public Sprite GetBareFallFrame(string unitId, int frame, bool diagonal = false, int variant = 1)
        {
            string artId = unitId == "recruit" && variant == 0
                ? (diagonal ? "recruit_diagonal_crutch" : "recruit_crutch")
                : diagonal && unitId == "recruit" ? "recruit_diagonal" : unitId;
            string cacheKey = artId + "_bare";
            if (!infantryFallFrames.TryGetValue(cacheKey, out var frames))
            {
                Texture2D texture = LoadInfantryTexture(artId, "bare_fall");
                frames = new Sprite[6];
                for (int i = 0; i < frames.Length; i++)
                    frames[i] = Sprite.Create(texture, new Rect(i * 64, 0, 64, 64),
                        new Vector2(0.5f, 0.5f), 64f / 12f, 0, SpriteMeshType.FullRect);
                infantryFallFrames[cacheKey] = frames;
            }
            return frames[frame];
        }


        // Draw the limbs first. Use the unit art for the cloth and helmet.
        Sprite[] BuildFallPoses(Color32[] idle)
        {
            const int size = 64;
            var frames = new Sprite[3];
            for (int pose = 0; pose < frames.Length; pose++)
            {
                bool kneel = pose == 0;
                bool back = pose == 2;
                var pixels = new Color32[size * size];
                Color32 edge = Palette[1];
                Color32 boot = Palette[3];
                Color32 cloth = Palette[7];
                Color32 webbing = Palette[9];
                int facing = back ? 1 : -1;

                // All coordinates start at the hips. Positive Y points toward the head.
                void Stroke(int x1, int y1, int x2, int y2, int radius, Color32 fill, bool fabric = false)
                {
                    Vector2 a = new Vector2(32 + x1, 31 + facing * y1);
                    Vector2 b = new Vector2(32 + x2, 31 + facing * y2);
                    Vector2 line = b - a;
                    for (int y = Mathf.Max(0, (int)Mathf.Min(a.y, b.y) - radius);
                         y <= Mathf.Min(63, (int)Mathf.Max(a.y, b.y) + radius); y++)
                    for (int x = Mathf.Max(0, (int)Mathf.Min(a.x, b.x) - radius);
                         x <= Mathf.Min(63, (int)Mathf.Max(a.x, b.x) + radius); x++)
                    {
                        Vector2 delta = new Vector2(x, y) - a;
                        float t = Mathf.Clamp01(Vector2.Dot(delta, line) / Mathf.Max(1f, line.sqrMagnitude));
                        float distance = (delta - t * line).sqrMagnitude;
                        if (distance > radius * radius) continue;
                        Color32 color = fabric ? idle[(13 + y % 8) * size + 25 + x % 14] : fill;
                        if (color.a < 200) color = fill;
                        pixels[y * size + x] = distance > (radius - 1) * (radius - 1) ? edge : color;
                    }
                }

                int leftFoot = kneel ? -5 : -23;
                int rightFoot = kneel ? -6 : -19;
                int leftX = kneel ? -10 : back ? -14 : -8;
                Stroke(-4, -2, -8, -10, 4, cloth, true);
                Stroke(-8, -10, leftX, leftFoot, 3, cloth, true);
                Stroke(leftX, leftFoot, leftX - 2, leftFoot - 2, 3, boot);
                Stroke(4, -2, 9, -10, 4, cloth, true);
                Stroke(9, -10, 12, rightFoot, 3, cloth, true);
                Stroke(12, rightFoot, 14, rightFoot - 2, 3, boot);
                Stroke(-7, 9, -15, back ? 4 : 12, 3, cloth, true);
                Stroke(-15, back ? 4 : 12, kneel ? -12 : -19, back ? 8 : 18, 2, webbing);
                Stroke(7, 9, kneel ? 12 : 17, back ? 6 : 11, 3, cloth, true);
                Stroke(kneel ? 12 : 17, back ? 6 : 11, kneel ? 10 : 19, back ? 1 : 19, 2, webbing);
                Stroke(0, -2, 0, 8, 8, cloth, true);
                if (back)
                {
                    Stroke(-5, 10, 5, 1, 2, webbing);
                    Stroke(-5, 0, 5, 0, 2, boot);
                }
                else Stroke(-1, 2, -1, 8, 5, Palette[5]);

                // Map the helmet crown from the idle sprite onto the lowered head.
                int headY = kneel ? 11 : 17;
                for (int y = -7; y <= 7; y++)
                for (int x = -8; x <= 8; x++)
                {
                    int r = x * x * 49 + y * y * 64;
                    if (r > 3136) continue;
                    Color32 c = idle[(45 + y) * size + 32 + x];
                    pixels[(31 + facing * headY + y) * size + 32 + x] =
                        r > 2450 ? edge : c.a > 200 ? c : cloth;
                }
                if (back) Stroke(-2, headY - 4, 2, headY - 4, 2, Palette[14]);

                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                infantryTextures.Add(texture);
                frames[pose] = Sprite.Create(texture, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), size / 12f, 0, SpriteMeshType.FullRect);
            }
            return frames;
        }

        Texture2D LoadInfantryTexture(string unitId, string pose)
        {
            var asset = Resources.Load<TextAsset>("Art/Units/infantry_" + unitId + "_" + pose);
            if (asset == null) throw new InvalidOperationException("Missing infantry art: " + unitId + " " + pose);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(Convert.FromBase64String(asset.text.Trim())))
                throw new InvalidDataException("Invalid infantry art: " + unitId + " " + pose);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            infantryTextures.Add(texture);
            return texture;
        }

        public bool TryGet(string unitId, out Sprite sprite)
            => byId.TryGetValue(unitId, out sprite);

        public Sprite Get(string unitId)
        {
            if (!byId.TryGetValue(unitId, out var sprite))
                throw new KeyNotFoundException("Unit art is missing for id " + unitId);
            return sprite;
        }

        public void Dispose()
        {
            foreach (var frames in infantryShootFrames.Values)
                foreach (var frame in frames) UnityEngine.Object.Destroy(frame);
            infantryShootFrames.Clear();
            if (recruitCrutchFrames != null)
                foreach (var frame in recruitCrutchFrames) UnityEngine.Object.Destroy(frame);
            if (recruitDiagonalWalkFrames != null)
                foreach (var frame in recruitDiagonalWalkFrames) UnityEngine.Object.Destroy(frame);
            if (recruitDiagonalCrutchFrames != null)
                foreach (var frame in recruitDiagonalCrutchFrames) UnityEngine.Object.Destroy(frame);
            foreach (var sprite in cleanSnouts.Values) UnityEngine.Object.Destroy(sprite);
            foreach (var sprite in looseHelmets.Values) UnityEngine.Object.Destroy(sprite);
            foreach (var sprite in bareBodies.Values) UnityEngine.Object.Destroy(sprite);
            foreach (var frames in infantryFallFrames.Values)
                foreach (var frame in frames) UnityEngine.Object.Destroy(frame);
            infantryFallFrames.Clear();
            foreach (var poses in fallPoses.Values)
                foreach (var pose in poses) UnityEngine.Object.Destroy(pose);
            fallPoses.Clear();
            foreach (var sprite in headlessSprites.Values) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            foreach (var sprite in helmetSprites.Values) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            foreach (var sprite in snoutSprites.Values) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            headlessSprites.Clear();
            helmetSprites.Clear();
            snoutSprites.Clear();
            foreach (var sprite in aimSprites.Values)
                if (sprite != null) UnityEngine.Object.Destroy(sprite);
            aimSprites.Clear();
            foreach (var texture in infantryTextures)
                if (texture != null) UnityEngine.Object.Destroy(texture);
            infantryTextures.Clear();
            foreach (var frames in walkFrames.Values)
                foreach (var frame in frames)
                    if (frame != null) UnityEngine.Object.Destroy(frame);
            walkFrames.Clear();
            foreach (var texture in walkTextures)
                if (texture != null) UnityEngine.Object.Destroy(texture);
            walkTextures.Clear();
            foreach (var sprite in byId.Values)
                if (sprite != null) UnityEngine.Object.Destroy(sprite);
            byId.Clear();
            if (atlasTexture != null) UnityEngine.Object.Destroy(atlasTexture);
            atlasTexture = null;
        }
    }
}
