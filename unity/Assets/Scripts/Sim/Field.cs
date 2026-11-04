using System;
using System.Collections.Generic;

namespace Treeline.Sim
{
    /// <summary>A point in field metres. X across the field, Y from the enemy edge (0) to our line (Height).</summary>
    public readonly struct Pt
    {
        public readonly float X, Y;
        public Pt(float x, float y) { X = x; Y = y; }
    }

    /// <summary>One piece of terrain: a polygon or a polyline with a width, in metres.</summary>
    public sealed class Feature
    {
        public string Type;
        public Pt[] Poly;   // closed shape, or null
        public Pt[] Line;   // polyline, or null
        public float Width; // for lines
    }

    public sealed class Field
    {
        public string Name = "";
        public float Width = 200f, Height = 400f;
        public List<Feature> Features = new List<Feature>();
    }

    /// <summary>Reads a field file (the JSON the web prototype and tools/osm_to_field.py write).</summary>
    public static class FieldParser
    {
        public static Field Parse(string json)
        {
            var root = Json.Parse(json) as Dictionary<string, object>;
            if (root == null) throw new FormatException("Field file must be a JSON object");

            var field = new Field();
            if (root.TryGetValue("name", out var name) && name is string n) field.Name = n;
            if (root.TryGetValue("size", out var size) && size is List<object> sz && sz.Count >= 2)
            {
                field.Width = (float)(double)sz[0];
                field.Height = (float)(double)sz[1];
            }

            if (root.TryGetValue("features", out var feats) && feats is List<object> list)
            {
                foreach (var item in list)
                {
                    var d = item as Dictionary<string, object>;
                    if (d == null || !d.TryGetValue("type", out var t) || !(t is string type)) continue;
                    var f = new Feature { Type = type };
                    if (d.TryGetValue("poly", out var poly)) f.Poly = Points(poly);
                    if (d.TryGetValue("line", out var line)) f.Line = Points(line);
                    if (d.TryGetValue("width", out var w) && w is double wd) f.Width = (float)wd;
                    field.Features.Add(f);
                }
            }
            return field;
        }

        static Pt[] Points(object o)
        {
            var list = o as List<object>;
            if (list == null) return null;
            var pts = new Pt[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                var xy = (List<object>)list[i];
                pts[i] = new Pt((float)(double)xy[0], (float)(double)xy[1]);
            }
            return pts;
        }
    }
}
