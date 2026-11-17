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
        BattlefieldEffects effects;
        readonly Dictionary<int, float> previousShotTimes = new Dictionary<int, float>();
        readonly Dictionary<int, Projectile> priorProjectiles = new Dictionary<int, Projectile>();
        readonly Dictionary<int, Attacker> priorAttackers = new Dictionary<int, Attacker>();
        readonly List<int> gone = new List<int>();
        float dustClock;
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
            // The owning BattlefieldPreview decides if this is an art catalog or a real match.
            // Do not create demo squads here: Destroy() is deferred until end-of-frame in Unity.
        }

        /// <summary>
        /// Hook for a future Game bridge. Reads battle state only; never calls Sim commands.
        /// If bound, the catalog is hidden automatically and actual combat units are shown.
        /// </summary>
        public void BindEffects(BattlefieldEffects value) { effects = value; }

        public void BindBattle(Battle current)
        {
            if (battle == current) return;
            ClearVisuals();
            previousShotTimes.Clear();
            priorProjectiles.Clear();
            priorAttackers.Clear();
            dustClock = 0f;
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
            // Avoid oversized 12m infantry icons: keep approximate real silhouettes.
            float scale = DisplayScale(unitId);
            root.transform.localScale = Vector3.one * scale;

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

        static float DisplayScale(string id)
        {
            if (id == "old_tank" || id == "modern_tank") return 0.86f;
            if (id == "helicopter" || id == "one_way_drone") return 1.0f;
            if (id == "ifv" || id == "mrap" || id == "van") return 0.72f;
            if (id == "buggy" || id == "motorbike") return 0.55f;
            if (id == "air_defence") return 0.68f;
            if (id == "fpv" || id == "recon_uav" || id == "recon_drone") return 0.39f;
            if (id == "mortar" || id == "anti_tank") return 0.45f;
            return 0.40f;
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
                // The Body ID identifies the flying object; the operator's Def is not the sprite.
                string id = drone.Body != null && drone.Body.Id == "fpv" ? "fpv" : "recon_drone";
                Sync(drone.Id, id, new Vector2(drone.X, drone.Y), false);
            }

            UpdateCombatVisuals();

            // These keys are no longer alive. Remove their sprites without touching Sim.
            foreach (int key in missing)
                if (live.TryGetValue(key, out var visual))
                {
                    Destroy(visual.Root);
                    live.Remove(key);
                }
        }

        void UpdateCombatVisuals()
        {
            if (effects == null) return;

            // Traces are owned by Sim and last 1.5s. Trigger each muzzle flash once only.
            foreach (var trace in battle.ShotTraces)
            {
                if (previousShotTimes.TryGetValue(trace.ShooterId, out float fired) &&
                    fired >= trace.FiredAt - 0.0001f) continue;
                previousShotTimes[trace.ShooterId] = trace.FiredAt;
                effects.MuzzleFlash(new Vector2(trace.FromX, trace.FromY));
            }

            // Shells/missiles are live projectile objects. They disappear on impact.
            gone.Clear();
            foreach (var pair in priorProjectiles) gone.Add(pair.Key);
            foreach (var projectile in battle.Projectiles)
            {
                priorProjectiles[projectile.Id] = projectile;
                gone.Remove(projectile.Id);
            }
            foreach (int key in gone)
            {
                Projectile impact = priorProjectiles[key];
                float radius = impact.Weapon == null ? 5f :
                    Mathf.Clamp(impact.Weapon.BlastRadiusM, 3f, 16f);
                effects.Explosion(new Vector2(impact.X, impact.Y), radius);
                priorProjectiles.Remove(key);
            }

            // A removed vehicle only gets a wreck cloud when Sim has marked it dead.
            gone.Clear();
            foreach (var pair in priorAttackers) gone.Add(pair.Key);
            dustClock += Time.deltaTime;
            int dustCount = 0;
            foreach (var attacker in battle.Attackers)
            {
                priorAttackers[attacker.Id] = attacker;
                gone.Remove(attacker.Id);
                if (dustClock >= 0.32f && dustCount < 8 &&
                    attacker.Class == UnitClass.Vehicle &&
                    (attacker.VX * attacker.VX + attacker.VY * attacker.VY) > 0.2f)
                {
                    effects.VehicleDust(new Vector2(attacker.X, attacker.Y));
                    dustCount++;
                }
            }
            if (dustClock >= 0.32f) dustClock = 0f;
            foreach (int key in gone)
            {
                Attacker former = priorAttackers[key];
                if (!former.Alive && former.Class == UnitClass.Vehicle)
                {
                    Vector2 point = new Vector2(former.X, former.Y);
                    effects.Explosion(point, 4f);
                    effects.WreckSmoke(point);
                }
                priorAttackers.Remove(key);
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
