using System;
using System.Collections.Generic;
using UnityEngine;
using Treeline.Sim;

namespace Treeline.Render
{
    /// <summary>
    /// Top-down unit art layer. In preview: displays the 23 real unit IDs in two loose groups,
    /// without modifying or pretending to run combat. In game: BindBattle reads live Sim positions.
    /// Visual allegiance is subtle (uniform + sleeve tape), not blue/red body tint.
    /// </summary>
    public sealed class BattlefieldUnitLayer : MonoBehaviour
    {
        sealed class Visual
        {
            public GameObject Root;
            public SpriteRenderer Body;
            public SpriteRenderer Shadow;
            public string UnitId;
            public Vector2 Previous;
        }

        readonly Dictionary<int, Visual> live = new Dictionary<int, Visual>();
        readonly List<int> missing = new List<int>();
        readonly List<Visual> showroom = new List<Visual>();

        UnitSpriteAtlas sprites;
        Battle battle;
        float widthMetres = 200f, heightMetres = 400f;
        bool showCatalog = true;
        bool ready;

        public void Initialize(float width, float height)
        {
            widthMetres = width;
            heightMetres = height;
            if (sprites == null)
            {
                try { sprites = new UnitSpriteAtlas(); }
                catch (Exception ex) { Debug.LogError("Unit atlas could not load: " + ex); return; }
            }
            ClearVisuals();
            ready = true;
            ShowPreview(showCatalog);
        }

        /// <summary>
        /// Hook for a future Game bridge. Reads battle state only; never calls Sim commands.
        /// If bound, the catalog is hidden automatically and actual combat units are shown.
        /// </summary>
        public void BindBattle(Battle current)
        {
            battle = current;
            ShowPreview(current == null && showCatalog);
        }

        public bool CatalogEnabled => showCatalog;

        public void ShowPreview(bool enabled)
        {
            showCatalog = enabled;
            for (int i = 0; i < showroom.Count; i++)
                if (showroom[i].Root != null) Destroy(showroom[i].Root);
            showroom.Clear();

            if (!ready || !enabled || battle != null) return;
            for (int i = 0; i < UnitSpriteAtlas.Ids.Length; i++)
            {
                string id = UnitSpriteAtlas.Ids[i];
                bool attacking = i < 14;
                int ordinal = attacking ? i : i - 14;
                int col = attacking ? ordinal % 7 : ordinal % 5;
                int row = attacking ? ordinal / 7 : ordinal / 5;

                float x = attacking ? 17f + col * 27.5f : 27f + col * 35f;
                float y = attacking ? (107f + row * 42f) : (292f + row * 52f);
                // These sample positions are layout only; no pathfinding or hit testing is used.
                Visual visual = Spawn(id, -1, new Vector2(x, y), attacking);
                if (visual != null) showroom.Add(visual);
            }
        }

        Visual Spawn(string unitId, int key, Vector2 location, bool attacker)
        {
            if (sprites == null || !sprites.TryGet(unitId, out var sprite)) return null;

            var root = new GameObject("Unit " + unitId + (key > 0 ? " #" + key : ""));
            root.transform.SetParent(transform, false);
            root.transform.localPosition = World(location);

            var silhouette = new GameObject("Shadow");
            silhouette.transform.SetParent(root.transform, false);
            silhouette.transform.localPosition = new Vector3(0.8f, -0.8f, 0f);
            var srShadow = silhouette.AddComponent<SpriteRenderer>();
            srShadow.sprite = sprite;
            srShadow.sortingOrder = 18;
            srShadow.color = new Color(0.08f, 0.12f, 0.09f, 0.31f);

            var shape = new GameObject("Pixel-lite sprite");
            shape.transform.SetParent(root.transform, false);
            var sr = shape.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 19;
            sr.color = Color.white;

            return new Visual {
                Root = root,
                Body = sr,
                Shadow = srShadow,
                UnitId = unitId,
                Previous = location
            };
        }

        Vector3 World(Vector2 loc) => new Vector3(
            loc.x - widthMetres * 0.5f, heightMetres * 0.5f - loc.y, 0f);

        void Update()
        {
            if (!ready || battle == null) return;

            missing.Clear();
            foreach (int key in live.Keys) missing.Add(key);

            foreach (var a in battle.Attackers)
                Sync(a.Id, a.Def.Id, new Vector2(a.X, a.Y), true);
            foreach (var d in battle.Defenders)
                Sync(d.Id, d.Def.Id, new Vector2(d.X, d.Y), false);
            foreach (var drone in battle.Drones)
            {
                string id = drone.Def.WeaponClass == "fpv" ? "fpv" : "recon_drone";
                Sync(drone.Id, id, new Vector2(drone.X, drone.Y), false);
            }

            // These keys are no longer alive. Remove their sprites without touching Sim.
            foreach (int key in missing)
                if (live.TryGetValue(key, out var visual))
                {
                    Destroy(visual.Root);
                    live.Remove(key);
                }
        }

        void Sync(int key, string type, Vector2 pos, bool attacking)
        {
            if (!live.TryGetValue(key, out var visual) || visual.UnitId != type)
            {
                if (visual != null) Destroy(visual.Root);
                visual = Spawn(type, key, pos, attacking);
                if (visual == null) return;
                live[key] = visual;
            }
            missing.Remove(key);

            Vector2 velocity = pos - visual.Previous;
            visual.Root.transform.localPosition = World(pos);
            if (velocity.sqrMagnitude > 0.04f)
            {
                // Visual top = world north. Convert field-Y-down direction to Unity-Y-up.
                float angle = Mathf.Atan2(-velocity.x, -velocity.y) * Mathf.Rad2Deg;
                visual.Root.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            visual.Previous = pos;
        }

        void ClearVisuals()
        {
            foreach (var pair in live) if (pair.Value.Root != null) Destroy(pair.Value.Root);
            live.Clear();
            foreach (Visual visual in showroom)
                if (visual.Root != null) Destroy(visual.Root);
            showroom.Clear();
        }

        void OnDestroy()
        {
            ClearVisuals();
            if (sprites != null) sprites.Dispose();
        }
    }
}
