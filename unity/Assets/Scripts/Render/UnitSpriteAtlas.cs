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
            foreach (var sprite in byId.Values)
                if (sprite != null) UnityEngine.Object.Destroy(sprite);
            byId.Clear();
            if (atlasTexture != null) UnityEngine.Object.Destroy(atlasTexture);
            atlasTexture = null;
        }
    }
}
