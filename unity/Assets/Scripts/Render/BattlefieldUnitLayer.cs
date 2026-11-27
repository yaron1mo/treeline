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
            public float WalkPhase;
            public float MotionAge;
            public int WalkVariant;
            public int DirectionView;
            public float ShotUntil;
            public float PreviousHp = -1f;
            public float HitUntil;
            public Vector2 HitDirection;
            public SpriteRenderer MuzzleFlash;
            public Vector2 BloodDirection;
            public float DeathStarted;
            public int DeathVariant;
            public bool HelmetComesOff;
            public Vector3 HelmetLanding;
            public SpriteRenderer Spirit;
            public Transform Helmet;
            public Transform Snout;
        }

        sealed class BloodMark
        {
            public GameObject Root;
            public float CreatedAt;
            public Transform[] Drops;
            public Vector3[] Targets;
            public Vector3[] Scales;
            public bool Settled;
        }

        sealed class DetachedSnout
        {
            public GameObject Root;
            public float CreatedAt;
        }

        readonly Dictionary<int, Visual> live = new Dictionary<int, Visual>();
        readonly List<int> missing = new List<int>();
        readonly List<Visual> showroom = new List<Visual>();
        readonly List<Visual> falling = new List<Visual>();
        readonly List<GameObject> corpses = new List<GameObject>();
        readonly List<BloodMark> bloodMarks = new List<BloodMark>();
        readonly List<DetachedSnout> detachedSnouts = new List<DetachedSnout>();
        const string BloodSetting = "Treeline.BloodEnabled";
        const int MaxBloodMarks = 64;
        const int MaxCorpses = 64;
        bool bloodEnabled;
        Texture2D bloodTexture;
        Sprite bloodSprite;
        Texture2D spiritTexture;
        Sprite spiritSprite;
        Texture2D muzzleTexture;
        Sprite muzzleSprite;

        UnitSpriteAtlas sprites;
        UnitTable units;
        Battle battle;
        BattlefieldEffects effects;
        readonly Dictionary<int, float> previousShotTimes = new Dictionary<int, float>();
        readonly Dictionary<int, Projectile> priorProjectiles = new Dictionary<int, Projectile>();
        readonly Dictionary<int, Attacker> priorAttackers = new Dictionary<int, Attacker>();
        readonly List<int> gone = new List<int>();
        float dustClock;
        float lastShotClock;
        int killStreak;
        float lastKillTime = -100f;
        float streakDisplayUntil;
        string streakLabel;
        GUIStyle streakStyle;
        float widthMetres = 200f, heightMetres = 400f;
        bool showCatalog = true;
        bool ready;

        public void Initialize(float width, float height)
        {
            widthMetres = width;
            heightMetres = height;
            bloodEnabled = PlayerPrefs.GetInt(BloodSetting, 1) == 1;
            if (units == null)
            {
                string path = System.IO.Path.Combine(Application.dataPath, "Data", "units.csv");
                if (System.IO.File.Exists(path))
                    units = UnitTable.Parse(System.IO.File.ReadAllText(path));
            }
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

        public void BindUnitTable(UnitTable value) { units = value; }

        public void BindBattle(Battle current)
        {
            if (battle == current) return;
            ClearVisuals();
            previousShotTimes.Clear();
            priorProjectiles.Clear();
            priorAttackers.Clear();
            dustClock = 0f;
            lastShotClock = 0f;
            killStreak = 0;
            lastKillTime = -100f;
            streakDisplayUntil = 0f;
            streakLabel = null;
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
            float scale = units != null && units.TryGet(unitId, out var definition)
                && definition.DisplayScale > 0f ? definition.DisplayScale : 1f;
            root.transform.localScale = Vector3.one * scale;

            var silhouette = new GameObject("Shadow");
            silhouette.transform.SetParent(root.transform, false);
            silhouette.transform.localPosition = new Vector3(0.8f, -0.8f, 0f);
            var srShadow = silhouette.AddComponent<SpriteRenderer>();
            srShadow.sprite = sprite;
            srShadow.sortingOrder = 18;
            srShadow.color = new Color(0.08f, 0.12f, 0.09f, 0.31f);
            if (unitId == "recruit" || unitId == "regular" || unitId == "special_forces")
                silhouette.SetActive(false);

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
                Previous = location,
                WalkPhase = key * 1.7f,
                WalkVariant = unitId == "recruit" && key > 0 ? key % 5 : 0
            };
        }

        Vector3 World(Vector2 loc) => new Vector3(
            loc.x - widthMetres * 0.5f, heightMetres * 0.5f - loc.y, 0f);

        void Update()
        {
            if (!ready || battle == null) return;

            for (int i = bloodMarks.Count - 1; i >= 0; i--)
            {
                BloodMark mark = bloodMarks[i];
                float age = Time.time - mark.CreatedAt;
                if (!bloodEnabled)
                {
                    Destroy(mark.Root);
                    bloodMarks.RemoveAt(i);
                    continue;
                }
                if (mark.Settled) continue;
                float travel = Mathf.SmoothStep(0f, 1f, age / 0.24f);
                for (int d = 0; d < mark.Drops.Length; d++)
                    if (mark.Drops[d] != null)
                    {
                        mark.Drops[d].localPosition = mark.Targets[d] * travel;
                        mark.Drops[d].localScale = mark.Scales[d] * travel;
                    }
                if (age >= 0.24f) mark.Settled = true;
            }

            for (int i = detachedSnouts.Count - 1; i >= 0; i--)
            {
                if (Time.time - detachedSnouts[i].CreatedAt < 10f) continue;
                if (detachedSnouts[i].Root != null) Destroy(detachedSnouts[i].Root);
                detachedSnouts.RemoveAt(i);
            }

            for (int i = falling.Count - 1; i >= 0; i--)
            {
                Visual fallen = falling[i];
                float elapsed = Time.time - fallen.DeathStarted;
                UpdateFallPose(fallen, elapsed);
                if (elapsed >= 2f)
                {
                    Destroy(fallen.Spirit.gameObject);
                    corpses.Add(fallen.Root);
                    if (corpses.Count > MaxCorpses)
                    {
                        Destroy(corpses[0]);
                        corpses.RemoveAt(0);
                    }
                    falling.RemoveAt(i);
                }
            }

            missing.Clear();
            foreach (int key in live.Keys) missing.Add(key);

            foreach (var a in battle.Attackers)
            {
                Sync(a.Id, a.Def.Id, new Vector2(a.X, a.Y), true);
                if (live.TryGetValue(a.Id, out var hitVisual))
                {
                    if (hitVisual.PreviousHp >= 0f && a.Hp < hitVisual.PreviousHp)
                    {
                        hitVisual.HitUntil = Time.time + 0.15f;
                        hitVisual.HitDirection = FindImpactDirection(a);
                    }
                    hitVisual.PreviousHp = a.Hp;
                }
            }
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

            if (battle.RoundTime < lastShotClock)
            {
                previousShotTimes.Clear();
                killStreak = 0;
                lastKillTime = -100f;
                streakLabel = null;
            }
            lastShotClock = battle.RoundTime;

            // Traces are owned by Sim and last 1.5s. Trigger each muzzle flash once only.
            foreach (var trace in battle.ShotTraces)
            {
                if (previousShotTimes.TryGetValue(trace.ShooterId, out float fired) &&
                    fired >= trace.FiredAt - 0.0001f) continue;
                previousShotTimes[trace.ShooterId] = trace.FiredAt;
                if (trace.ByAttacker && live.TryGetValue(trace.ShooterId, out var shooter) &&
                    (shooter.UnitId == "recruit" || shooter.UnitId == "regular" || shooter.UnitId == "special_forces"))
                    BeginInfantryShot(shooter);
                else
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
                if (!former.Alive && former.Hp <= 0f) RegisterKill(battle.RoundTime);
                if (!former.Alive && former.Class == UnitClass.Vehicle)
                {
                    Vector2 point = new Vector2(former.X, former.Y);
                    effects.Explosion(point, 4f);
                    effects.WreckSmoke(point);
                }
                else if (!former.Alive && former.Hp <= 0f && former.Class == UnitClass.Infantry &&
                    live.TryGetValue(key, out var fallen) &&
                    (fallen.UnitId == "recruit" || fallen.UnitId == "regular" || fallen.UnitId == "special_forces"))
                {
                    fallen.BloodDirection = FindImpactDirection(former);
                    BeginFall(fallen, key);
                }
                priorAttackers.Remove(key);
            }
        }

        void BeginFall(Visual fallen, int key)
        {
            if (fallen.MuzzleFlash != null) fallen.MuzzleFlash.enabled = false;
            fallen.DeathStarted = Time.time;
            fallen.DeathVariant = (key & int.MaxValue) % 2;
            uint helmetChoice = unchecked((uint)key * 2654435761u);
            fallen.HelmetComesOff = (helmetChoice >> 16) % 5 == 0;
            if (fallen.HelmetComesOff) fallen.DeathVariant = 1;
            fallen.Spirit = CreateSpirit(fallen.Root.transform);
            if (fallen.HelmetComesOff)
            {
                fallen.Helmet = CreatePart(fallen.Root.transform, sprites.GetLooseHelmet(fallen.UnitId), "Loose helmet");
                int direction = (int)((helmetChoice >> 20) % 3);
                fallen.HelmetLanding = direction == 0 ? new Vector3(-4f, 3.4f, 0f)
                    : direction == 1 ? new Vector3(4f, 3.4f, 0f) : new Vector3(0.8f, 5.4f, 0f);
                fallen.Helmet.localPosition = new Vector3(0f, 2.4f, 0f);
            }
            fallen.Snout = CreatePart(fallen.Root.transform, sprites.GetCleanSnout(fallen.UnitId), "Visible snout");
            fallen.Snout.localPosition = new Vector3(0f, -4.2f, 0f);
            fallen.Snout.gameObject.SetActive(false);
            if (fallen.DeathVariant == 2)
            {
                fallen.Body.sprite = sprites.GetHeadless(fallen.UnitId);
                fallen.Helmet = CreatePart(fallen.Root.transform, sprites.GetHelmet(fallen.UnitId), "Helmet");
                var snout = CreateSpirit(fallen.Root.transform);
                snout.gameObject.name = "Detached snout";
                snout.color = new Color(1f, 0.55f, 0.62f, 1f);
                snout.transform.localScale = Vector3.one * 0.6f;
                fallen.Snout = snout.transform;
                detachedSnouts.Add(new DetachedSnout { Root = snout.gameObject, CreatedAt = Time.time });
            }
            UpdateFallPose(fallen, 0f);
            if (bloodEnabled) AddBlood(fallen.Root.transform.localPosition, fallen.BloodDirection);
            falling.Add(fallen);
            live.Remove(key);
        }

        void UpdateFallPose(Visual fallen, float elapsed)
        {
            Transform body = fallen.Body.transform;
            bool diagonal = fallen.UnitId == "recruit" && fallen.DirectionView != 0;
            bool crutch = fallen.UnitId == "recruit" && fallen.WalkVariant == 0;
            if (fallen.DeathVariant == 1 || diagonal || crutch)
            {
                int frame = Mathf.Min(5, Mathf.FloorToInt(elapsed / 0.09f));
                fallen.Body.sprite = fallen.HelmetComesOff ? sprites.GetBareFallFrame(fallen.UnitId, frame, diagonal, fallen.WalkVariant)
                    : sprites.GetInfantryFallFrame(fallen.UnitId, frame, diagonal, fallen.WalkVariant);
                fallen.Snout.gameObject.SetActive(!diagonal && !crutch && frame >= 4);
                if (fallen.HelmetComesOff)
                {
                    float time = Mathf.Clamp01(elapsed / 0.65f);
                    float flight = Mathf.SmoothStep(0f, 1f, time);
                    float arc = Mathf.Sin(time * Mathf.PI);
                    fallen.Helmet.localPosition = Vector3.Lerp(new Vector3(0f, 2.4f, 0f), fallen.HelmetLanding, flight)
                        + Vector3.up * (1.5f * arc);
                    fallen.Helmet.localScale = Vector3.one * (1f + 0.2f * arc);
                    float spin = fallen.HelmetLanding.x < 0f ? -120f : 120f;
                    fallen.Helmet.localRotation = Quaternion.Euler(0f, 0f, spin * flight);
                }
                float fallProgress = Mathf.Clamp01(elapsed / 0.18f);
                float targetScale = fallen.UnitId == "special_forces" ? 1f : 0.82f;
                float scale = Mathf.Lerp(1f, targetScale, fallProgress);
                body.localPosition = new Vector3(0f, -0.8f * fallProgress, 0f);
                body.localScale = Vector3.one * scale;
                body.localRotation = Quaternion.identity;
                if (fallen.UnitId == "special_forces")
                    fallen.Snout.localPosition = new Vector3(0f, -5f, 0f);
            }
            else if (fallen.DeathVariant == 2)
            {
                float progress = Mathf.Clamp01(elapsed / 0.22f);
                body.localPosition = new Vector3(0f, 1.1f * progress, 0f);
                body.localScale = new Vector3(1f, 1f - progress * 0.3f, 1f);
                body.localRotation = Quaternion.identity;
            }
            else
            {
                bool forward = fallen.DeathVariant == 1;
                float tuck = Mathf.SmoothStep(0f, 1f, elapsed / (forward ? 0.18f : 0.12f));
                float drop = Mathf.SmoothStep(0f, 1f, (elapsed - (forward ? 0.26f : 0.10f)) / 0.24f);
                float settle = Mathf.Sin(drop * Mathf.PI);
                fallen.Body.sprite = elapsed < 0.08f ? sprites.Get(fallen.UnitId)
                    : sprites.GetFallPose(fallen.UnitId, forward && drop < 0.38f ? 0 : forward ? 1 : 2);
                body.localPosition = new Vector3((forward ? -0.26f : 0.56f) * drop,
                    (forward ? -1.65f : 1.55f) * drop - 0.25f * tuck, 0f);
                body.localScale = new Vector3(1f + 0.10f * settle, 1f - 0.33f * tuck + 0.33f * drop, 1f);
                body.localRotation = Quaternion.Euler(0f, 0f, (forward ? -12f : 27f) * drop);
                fallen.Snout.localPosition = new Vector3(0.56f * drop, -2.4f + 1.2f * drop, 0f);
                fallen.Snout.gameObject.SetActive(elapsed >= 0.3f);
            }
            float rise = Mathf.Max(0f, elapsed - 0.2f);
            fallen.Spirit.transform.localPosition = new Vector3(0f, 2f + rise * 3f, 0f);
            fallen.Spirit.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - rise / 1.8f));
            if (fallen.DeathVariant == 2)
            {
                float flight = Mathf.Clamp01(elapsed / 0.65f);
                fallen.Helmet.localPosition = new Vector3(flight * 3.5f, 2.7f + flight * 2f, 0f);
                fallen.Helmet.localRotation = Quaternion.Euler(0f, 0f, flight * 70f);
                fallen.Snout.localPosition = new Vector3(-flight * 8.5f, -1.5f + Mathf.Sin(flight * Mathf.PI) * 2f, 0f);
            }
        }

        void BeginInfantryShot(Visual visual)
        {
            visual.ShotUntil = Time.time + 0.3f;
            if (visual.MuzzleFlash != null) return;
            if (muzzleSprite == null)
            {
                muzzleTexture = new Texture2D(12, 12, TextureFormat.RGBA32, false);
                muzzleTexture.filterMode = FilterMode.Point;
                var pixels = new Color32[144];
                for (int y = 0; y < 12; y++)
                for (int x = 0; x < 12; x++)
                {
                    int dx = Mathf.Abs(x * 2 - 11);
                    int dy = Mathf.Abs(y * 2 - 11);
                    if ((dx <= 3 && dy <= 11) || (dy <= 3 && dx <= 11) || dx + dy <= 10)
                        pixels[y * 12 + x] = dx + dy <= 6
                            ? new Color32(255, 250, 203, 255)
                            : new Color32(255, 162, 40, 255);
                }
                muzzleTexture.SetPixels32(pixels);
                muzzleTexture.Apply();
                muzzleSprite = Sprite.Create(muzzleTexture, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f), 4f);
            }
            Transform flash = CreatePart(visual.Root.transform, muzzleSprite, "Muzzle flash");
            visual.MuzzleFlash = flash.GetComponent<SpriteRenderer>();
            visual.MuzzleFlash.sortingOrder = 22;
            float muzzleX = visual.UnitId == "recruit" ? (visual.WalkVariant == 0 ? -1.5f : -2.2f)
                : visual.UnitId == "special_forces" ? -0.5f : 0f;
            float muzzleY = visual.UnitId == "recruit" ? (visual.WalkVariant == 0 ? -1.5f : -3.7f) : -3.2f;
            flash.localPosition = new Vector3(muzzleX, muzzleY, 0f);
            visual.MuzzleFlash.enabled = false;
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
            bool infantry = attacking && (type == "recruit" || type == "regular" || type == "special_forces");
            if (infantry)
            {
                if (velocity.sqrMagnitude > 0.000001f)
                {
                    visual.MotionAge = 0f;
                    if (type == "recruit")
                        visual.DirectionView = Mathf.Abs(velocity.x) < Mathf.Abs(velocity.y) * 0.41421356f
                            ? 0 : velocity.x < 0f ? -1 : 1;
                }
                else visual.MotionAge += Time.deltaTime;

                if (visual.MotionAge < 0.15f)
                {
                    float pace = type == "recruit" ? 4.5f : type == "regular" ? 6.5f : 10.5f;
                    if (type == "recruit") pace *= 0.85f + visual.WalkVariant * 0.075f;
                    visual.WalkPhase += Time.deltaTime * pace;
                    float step = Mathf.Sin(visual.WalkPhase);
                    int frame = Mathf.FloorToInt(Mathf.Repeat(visual.WalkPhase, Mathf.PI * 2f) / (Mathf.PI * 0.5f));
                    if (type == "recruit")
                    {
                        float cycle = Mathf.Repeat(visual.WalkPhase, Mathf.PI * 2f) / (Mathf.PI * 2f);
                        frame = cycle < 0.34f ? 0 : cycle < 0.54f ? 1 : cycle < 0.76f ? 2 : 3;
                    }
                    visual.Body.sprite = type == "recruit" && visual.DirectionView != 0
                        ? sprites.GetRecruitDiagonalWalkFrame(frame, visual.WalkVariant)
                        : sprites.GetWalkFrame(type, frame, visual.WalkVariant);
                    visual.Body.flipX = type == "recruit" && visual.DirectionView > 0;
                    visual.Body.transform.localRotation = Quaternion.identity;
                    float hop = type == "recruit" && visual.WalkVariant == 0 && frame == 2 ? 0.25f : 0f;
                    visual.Body.transform.localPosition = new Vector3(0f, Mathf.Abs(step) * 0.04f + hop, 0f);
                }
                else
                {
                    visual.Body.transform.localRotation = Quaternion.identity;
                    visual.Body.transform.localPosition = Vector3.zero;
                    visual.Body.sprite = type == "recruit" && visual.DirectionView != 0
                        ? sprites.GetRecruitDiagonalWalkFrame(1, visual.WalkVariant)
                        : sprites.GetWalkFrame(type, 1, visual.WalkVariant);
                    visual.Body.flipX = type == "recruit" && visual.DirectionView > 0;
                }
                if (Time.time < visual.ShotUntil)
                {
                    float shotElapsed = 0.3f - (visual.ShotUntil - Time.time);
                    int shotFrame = shotElapsed < 0.08f ? 0 : shotElapsed < 0.18f ? 1 : 2;
                    bool diagonal = type == "recruit" && visual.DirectionView != 0;
                    visual.Body.sprite = sprites.GetInfantryShootFrame(type, shotFrame, visual.WalkVariant, diagonal);
                    visual.Body.flipX = diagonal && visual.DirectionView > 0;
                    visual.Body.transform.localRotation = Quaternion.identity;
                    visual.Body.transform.localPosition = Vector3.zero;
                    if (visual.MuzzleFlash != null)
                    {
                        if (diagonal)
                            visual.MuzzleFlash.transform.localPosition = new Vector3(visual.DirectionView > 0 ? 4.4f : -4.4f, -2.9f, 0f);
                        else if (type == "recruit")
                            visual.MuzzleFlash.transform.localPosition = visual.WalkVariant == 0
                                ? new Vector3(-1.5f, -1.5f, 0f) : new Vector3(-2.2f, -3.7f, 0f);
                        visual.MuzzleFlash.enabled = shotElapsed < 0.14f;
                    }
                }
                else if (visual.MuzzleFlash != null) visual.MuzzleFlash.enabled = false;
                if (Time.time < visual.HitUntil)
                {
                    float strength = Mathf.Clamp01((visual.HitUntil - Time.time) / 0.15f);
                    visual.Body.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), strength);
                    visual.Body.transform.localPosition += new Vector3(visual.HitDirection.x, visual.HitDirection.y, 0f) * (0.45f * strength);
                }
                else visual.Body.color = Color.white;
            }
            if (!infantry && velocity.sqrMagnitude > 0.04f)
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
            foreach (Visual visual in falling)
                if (visual.Root != null) Destroy(visual.Root);
            falling.Clear();
            foreach (GameObject corpse in corpses)
                if (corpse != null) Destroy(corpse);
            corpses.Clear();
            foreach (BloodMark mark in bloodMarks)
                if (mark.Root != null) Destroy(mark.Root);
            bloodMarks.Clear();
            detachedSnouts.Clear();
        }

        void RegisterKill(float time)
        {
            killStreak = time - lastKillTime <= 4f ? killStreak + 1 : 1;
            lastKillTime = time;
            if (killStreak < 2) return;
            streakLabel = killStreak == 2 ? "DOUBLE KILL" : killStreak == 3 ? "TRIPLE KILL" : killStreak == 4 ? "MULTI KILL" : "ULTRA KILL";
            streakDisplayUntil = time + 1.8f;
        }

        Vector2 FindImpactDirection(Attacker attacker)
        {
            float closestDistance = 36f;
            Vector2 direction = Vector2.down;
            foreach (var trace in battle.ShotTraces)
            {
                float age = battle.RoundTime - trace.ArrivesAt;
                if (trace.ByAttacker || !trace.Hit || age < 0f || age > 0.35f) continue;
                float dx = trace.ToX - attacker.X;
                float dy = trace.ToY - attacker.Y;
                float distance = dx * dx + dy * dy;
                if (distance >= closestDistance) continue;
                Vector2 shotDirection = new Vector2(trace.ToX - trace.FromX, trace.FromY - trace.ToY);
                if (shotDirection.sqrMagnitude < 0.0001f) continue;
                closestDistance = distance;
                direction = shotDirection.normalized;
            }
            return direction;
        }

        void AddBlood(Vector3 position, Vector2 impactDirection)
        {
            if (bloodMarks.Count >= MaxBloodMarks)
            {
                Destroy(bloodMarks[0].Root);
                bloodMarks.RemoveAt(0);
            }
            if (bloodSprite == null)
            {
                bloodTexture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                bloodTexture.filterMode = FilterMode.Point;
                var pixels = new Color32[64];
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int dx = x * 2 - 7;
                    int dy = y * 2 - 7;
                    if (dx * dx + dy * dy <= 40) pixels[y * 8 + x] = new Color32(198, 26, 37, 230);
                }
                bloodTexture.SetPixels32(pixels);
                bloodTexture.Apply();
                bloodSprite = Sprite.Create(bloodTexture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 2f);
            }
            var mark = new GameObject("Blood effect");
            mark.transform.SetParent(transform, false);
            mark.transform.localPosition = position;
            Vector2 direction = impactDirection.sqrMagnitude > 0.0001f ? impactDirection.normalized : Vector2.down;
            Vector2 across = new Vector2(-direction.y, direction.x);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            const int count = 10;
            var drops = new Transform[count];
            var targets = new Vector3[count];
            var scales = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var drop = new GameObject("Blood drop");
                drop.transform.SetParent(mark.transform, false);
                float distance = i == 0 ? 5f : 3f + i * 1.15f;
                float side = i == 0 ? 0f : ((i % 2 == 0) ? 1f : -1f) * (0.45f + (i % 3) * 0.7f);
                Vector2 target = direction * distance + across * side;
                targets[i] = new Vector3(target.x, target.y, 0f);
                drops[i] = drop.transform;
                scales[i] = i == 0 ? new Vector3(3.1f, 0.32f, 1f)
                    : new Vector3(0.6f + (i % 3) * 0.35f, 0.14f + (i % 2) * 0.08f, 1f);
                drop.transform.localScale = Vector3.zero;
                drop.transform.localRotation = Quaternion.Euler(0f, 0f, angle + (i == 0 ? 0f : side * 4f));
                var renderer = drop.AddComponent<SpriteRenderer>();
                renderer.sprite = bloodSprite;
                renderer.sortingOrder = 17;
            }
            bloodMarks.Add(new BloodMark { Root = mark, CreatedAt = Time.time, Drops = drops, Targets = targets, Scales = scales });
        }

        Transform CreatePart(Transform parent, Sprite sprite, string name)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 20;
            return part.transform;
        }

        SpriteRenderer CreateSpirit(Transform parent)
        {
            if (spiritSprite == null)
            {
                spiritTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                spiritTexture.filterMode = FilterMode.Point;
                var pixels = new Color32[256];
                for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float dx = (x - 7.5f) / 7f;
                    float dy = (y - 8f) / 5f;
                    if (dx * dx + dy * dy <= 1f) pixels[y * 16 + x] = new Color32(242, 199, 212, 220);
                    if ((x == 5 || x == 10) && (y == 7 || y == 8)) pixels[y * 16 + x] = new Color32(110, 75, 92, 230);
                }
                spiritTexture.SetPixels32(pixels);
                spiritTexture.Apply();
                spiritSprite = Sprite.Create(spiritTexture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 4f);
            }
            var spirit = new GameObject("Snout spirit");
            spirit.transform.SetParent(parent, false);
            var renderer = spirit.AddComponent<SpriteRenderer>();
            renderer.sprite = spiritSprite;
            renderer.sortingOrder = 21;
            return renderer;
        }

        void OnGUI()
        {
            if (!ready || battle == null) return;
            if (streakLabel != null && battle.RoundTime <= streakDisplayUntil)
            {
                if (streakStyle == null)
                {
                    streakStyle = new GUIStyle(GUI.skin.label);
                    streakStyle.alignment = TextAnchor.MiddleCenter;
                    streakStyle.fontStyle = FontStyle.Bold;
                }
                streakStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.055f);
                var title = new Rect(Screen.width * 0.30f, Screen.height * 0.11f, Screen.width * 0.55f, Screen.height * 0.09f);
                streakStyle.normal.textColor = new Color(0.18f, 0.02f, 0.02f, 0.95f);
                GUI.Label(new Rect(title.x + 3f, title.y + 3f, title.width, title.height), streakLabel, streakStyle);
                streakStyle.normal.textColor = new Color(1f, 0.84f, 0.45f, 1f);
                GUI.Label(title, streakLabel, streakStyle);
            }
            float scale = Screen.height / 720f;
            var rect = new Rect(Screen.width - 160f * scale, 8f * scale, 150f * scale, 34f * scale);
            if (GUI.Button(rect, bloodEnabled ? "Blood: ON" : "Blood: OFF"))
            {
                bloodEnabled = !bloodEnabled;
                PlayerPrefs.SetInt(BloodSetting, bloodEnabled ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        void OnDestroy()
        {
            ClearVisuals();
            if (bloodSprite != null) Destroy(bloodSprite);
            if (bloodTexture != null) Destroy(bloodTexture);
            if (spiritSprite != null) Destroy(spiritSprite);
            if (spiritTexture != null) Destroy(spiritTexture);
            if (muzzleSprite != null) Destroy(muzzleSprite);
            if (muzzleTexture != null) Destroy(muzzleTexture);
            if (sprites != null) sprites.Dispose();
        }
    }
}
