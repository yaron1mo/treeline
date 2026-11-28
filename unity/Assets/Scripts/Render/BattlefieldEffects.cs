using UnityEngine;

namespace Treeline.Render
{
    /// <summary>
    /// Lightweight pooled 2D battlefield FX. All coordinates at the public boundary are Field
    /// metres (x to the right, y downward). No Sim reference or gameplay changes.
    /// The Editor battlefield preview can exercise these effects in demo mode; later the Game
    /// bridge may call these same entry points from real combat events.
    /// </summary>
    public sealed class BattlefieldEffects : MonoBehaviour
    {
        const int MaxParticles = 96;
        const int BirdCount = 8;
        sealed class Particle
        {
            public SpriteRenderer Renderer;
            public Transform Transform;
            public float Remaining, Lifetime, Size, Expansion;
            public Vector2 Position, Velocity;
            public Color Color;
        }

        sealed class Bird
        {
            public Transform Transform;
            public Vector2 Position;
            public Vector2 Velocity;
            public bool Alert;
        }

        readonly Particle[] particles = new Particle[MaxParticles];
        readonly Bird[] birds = new Bird[BirdCount];
        Sprite particleSprite, birdSprite;
        Texture2D particleTexture, birdTexture;
        float mapWidth = 200f, mapHeight = 400f;
        float elapsed, birdTimer;
        int cursor;
        bool ready, demo;
        float demoClock;
        Camera shakeCamera;
        Vector3 shakeOffset;
        float shakeLeft, shakeStrength;
        readonly Vector2[] longSmoke = new Vector2[6];
        readonly float[] smokeTime = new float[6];
        readonly float[] smokeLeft = new float[6];

        public void Initialize(float widthMetres, float heightMetres)
        {
            mapWidth = widthMetres;
            mapHeight = heightMetres;
            if (!ready) Build();
            Clear();
        }

        void Build()
        {
            particleTexture = MakeDisc(24);
            particleSprite = Sprite.Create(particleTexture, new Rect(0, 0, 24, 24),
                new Vector2(0.5f, 0.5f), 24f);
            birdTexture = MakeBird();
            birdSprite = Sprite.Create(birdTexture, new Rect(0, 0, 12, 10),
                new Vector2(0.5f, 0.5f), 2.8f);

            for (int i = 0; i < particles.Length; i++)
            {
                GameObject go = new GameObject("FX");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = particleSprite;
                renderer.sortingOrder = 15;
                renderer.enabled = false;
                particles[i] = new Particle { Renderer = renderer, Transform = go.transform };
            }

            for (int i = 0; i < birds.Length; i++)
            {
                GameObject go = new GameObject("Ambient bird");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = birdSprite;
                renderer.color = new Color(0.11f, 0.15f, 0.12f, 0.75f);
                renderer.sortingOrder = 5;
                birds[i] = new Bird { Transform = go.transform };
            }
            ready = true;
        }

        static Texture2D MakeDisc(int size)
        {
            var data = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                    float dy = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                    float fade = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    data[y * size + x] = new Color32(255, 255, 255, (byte)(fade * fade * 255f));
                }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(data);
            tex.Apply(false, true);
            return tex;
        }

        static Texture2D MakeBird()
        {
            const int width = 12, height = 10;
            var pixels = new Color32[width * height];
            for (int x = 1; x <= 5; x++)
            {
                int top = 3 + (5 - x) / 2;
                pixels[top * width + x] = new Color32(255, 255, 255, 255);
                pixels[top * width + (11 - x)] = new Color32(255, 255, 255, 255);
            }
            pixels[4 * width + 6] = new Color32(255, 255, 255, 255);
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        public void Clear()
        {
            if (!ready) return;
            elapsed = birdTimer = demoClock = 0f;
            cursor = 0;
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Remaining = 0;
                particles[i].Renderer.enabled = false;
            }
            for (int i = 0; i < smokeLeft.Length; i++) smokeLeft[i] = 0f;
            for (int i = 0; i < birds.Length; i++)
            {
                float x = mapWidth * (0.18f + i * 0.052f);
                float y = mapHeight * 0.13f + (i % 3) * 2.2f;
                birds[i].Position = new Vector2(x, y);
                birds[i].Velocity = new Vector2(2.8f + i * 0.3f, -0.5f);
                birds[i].Alert = false;
                birds[i].Transform.localPosition = World(birds[i].Position);
            }
        }

        public void SetDemo(bool enabled)
        {
            demo = enabled;
            demoClock = 0f;
            if (!demo) for (int i = 0; i < smokeLeft.Length; i++) smokeLeft[i] = 0f;
        }

        Vector3 World(Vector2 p) => new Vector3(
            p.x - mapWidth * 0.5f, mapHeight * 0.5f - p.y, 0f);

        float Random01(int salt)
        {
            float v = Mathf.Sin((cursor * 79 + salt * 137) * 12.9898f) * 43758.5453f;
            return Mathf.Repeat(v, 1f);
        }

        void Emit(Vector2 location, Vector2 velocity, Color color, float seconds,
            float metres, float expansion = 0f)
        {
            if (!ready) return;
            Particle p = particles[cursor++ % particles.Length];
            p.Position = location;
            p.Velocity = velocity;
            p.Color = color;
            p.Lifetime = p.Remaining = Mathf.Max(0.05f, seconds);
            p.Size = Mathf.Max(0.15f, metres);
            p.Expansion = expansion;
            p.Transform.localPosition = World(location);
            p.Transform.localScale = Vector3.one * p.Size;
            p.Renderer.color = color;
            p.Renderer.enabled = true;
        }

        public void MuzzleFlash(Vector2 at)
        {
            Emit(at, Vector2.zero, new Color(1f, 0.87f, 0.38f, 1f), 0.16f, 3.7f, 0.7f);
            Emit(at, new Vector2(2f, -1.4f), new Color(0.93f, 0.82f, 0.55f, 0.50f),
                0.35f, 2.4f, 1f);
        }

        public void Explosion(Vector2 at, float radiusMetres)
        {
            float r = Mathf.Clamp(radiusMetres, 2f, 26f);
            if (radiusMetres >= 8f)
            {
                shakeLeft = 0.16f;
                shakeStrength = Mathf.Clamp01(radiusMetres / 16f);
            }
            Emit(at, Vector2.zero, new Color(1f, 0.87f, 0.42f, 1f),
                0.30f, r * 0.75f, r * 1.45f);
            for (int j = 0; j < 6; j++)
            {
                float angle = (j / 6f) * Mathf.PI * 2f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Emit(at + direction * (r * 0.1f), direction * (r * 0.45f),
                    new Color(0.39f, 0.32f, 0.26f, 0.70f), 1f + 0.1f * j,
                    r * 0.28f, r * 0.38f);
                Emit(at + direction * (r * 0.3f), direction * (r * 0.68f),
                    new Color(0.79f, 0.65f, 0.44f, 0.65f), 0.65f,
                    r * 0.19f, r * 0.21f);
            }
            Emit(at, new Vector2(0.8f, -1.6f), new Color(0.20f, 0.21f, 0.19f, 0.73f),
                3.8f, r * 0.45f, r * 0.55f);
            FrightenBirds(at);
        }

        public void VehicleDust(Vector2 at)
        {
            for (int j = 0; j < 3; j++)
            {
                float spread = (Random01(10 + j) - 0.5f) * 3f;
                Emit(at + new Vector2(spread, j - 1f),
                    new Vector2(spread * 0.4f + 0.9f, -0.8f),
                    new Color(0.81f, 0.69f, 0.48f, 0.53f), 1.5f,
                    2.5f + Random01(j) * 2f, 3.2f);
            }
        }

        /// <summary>Smoke lasts up to 20 seconds. Calls can be repeated for a new wreck at a new position.</summary>
        public void WreckSmoke(Vector2 at)
        {
            int index = 0;
            for (int i = 0; i < smokeLeft.Length; i++)
                if (smokeLeft[i] > 0f &&
                    (longSmoke[i] - at).sqrMagnitude < 5f * 5f)
                {
                    index = i;
                    break;
                }
            if (smokeLeft[index] <= 0f ||
                (longSmoke[index] - at).sqrMagnitude >= 5f * 5f)
                for (int i = 1; i < smokeLeft.Length; i++)
                    if (smokeLeft[i] < smokeLeft[index]) index = i;
            longSmoke[index] = at;
            smokeLeft[index] = 20f;
            smokeTime[index] = 0f;
        }

        public void FrightenBirds(Vector2 at)
        {
            for (int i = 0; i < birds.Length; i++)
            {
                Vector2 direction = birds[i].Position - at;
                if (direction.sqrMagnitude > 120f * 120f) continue;
                direction = direction.sqrMagnitude > 0.1f ? direction.normalized : Vector2.right;
                birds[i].Velocity = direction * (7f + i * 0.9f) + new Vector2(1f, -3f);
                birds[i].Alert = true;
            }
        }

        void Update()
        {
            if (!ready) return;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            elapsed += dt;
            birdTimer += dt;

            for (int i = 0; i < particles.Length; i++)
            {
                Particle p = particles[i];
                if (p.Remaining <= 0f) continue;
                p.Remaining -= dt;
                if (p.Remaining <= 0f) { p.Renderer.enabled = false; continue; }
                float progress = 1f - p.Remaining / p.Lifetime;
                p.Position += p.Velocity * dt;
                p.Transform.localPosition = World(p.Position);
                p.Transform.localScale = Vector3.one * (p.Size + p.Expansion * progress);
                Color c = p.Color;
                c.a *= 1f - progress;
                p.Renderer.color = c;
            }

            for (int i = 0; i < smokeLeft.Length; i++)
            {
                if (smokeLeft[i] <= 0f) continue;
                smokeLeft[i] -= dt;
                smokeTime[i] -= dt;
                if (smokeTime[i] <= 0f)
                {
                    smokeTime[i] += 0.36f;
                    Emit(longSmoke[i], new Vector2(1.2f, -2.3f),
                        new Color(0.20f, 0.22f, 0.20f, 0.52f),
                        3.5f, 3f, 5.8f);
                }
            }

            for (int i = 0; i < birds.Length; i++)
            {
                Bird bird = birds[i];
                bird.Position += bird.Velocity * dt;
                bird.Transform.localPosition = World(bird.Position);
                float wave = Mathf.Sin(elapsed * 7f + i * 1.7f);
                bird.Transform.localScale = new Vector3(1f, 0.8f + wave * 0.2f, 1f);
                if (bird.Position.x > mapWidth + 16f || bird.Position.y < -16f ||
                    bird.Position.y > mapHeight + 16f)
                {
                    bird.Position = new Vector2(-14f - i * 3f,
                        mapHeight * (0.11f + (i % 4) * 0.05f));
                    bird.Velocity = new Vector2(2.8f + i * 0.3f, -0.4f);
                    bird.Alert = false;
                }
                if (bird.Alert)
                    bird.Velocity = Vector2.Lerp(bird.Velocity,
                        new Vector2(4f, -1f), dt * 0.3f);
            }

            // Purely decorative scripted FX test — these are not simulation attacks.
            if (demo)
            {
                demoClock += dt;
                if (demoClock > 3.5f)
                {
                    demoClock = 0f;
                    float x = mapWidth * (0.43f + Random01(29) * 0.12f);
                    float y = mapHeight * (0.39f + Random01(20) * 0.10f);
                    MuzzleFlash(new Vector2(mapWidth * 0.68f, mapHeight * 0.78f));
                    Explosion(new Vector2(x, y), 6.5f);
                    VehicleDust(new Vector2(mapWidth * 0.32f, mapHeight * 0.33f));
                    WreckSmoke(new Vector2(mapWidth * 0.60f, mapHeight * 0.48f));
                }
            }
        }

        void LateUpdate()
        {
            if (shakeCamera != null) shakeCamera.transform.position -= shakeOffset;
            shakeOffset = Vector3.zero;
            if (shakeLeft <= 0f) { shakeCamera = null; return; }
            shakeLeft = Mathf.Max(0f, shakeLeft - Mathf.Min(0.05f, Time.unscaledDeltaTime));
            shakeCamera = Camera.main;
            if (shakeCamera == null) return;
            float strength = shakeCamera.orthographicSize * 0.003f * shakeStrength * (shakeLeft / 0.16f);
            float t = Time.unscaledTime * 87f;
            shakeOffset = new Vector3(Mathf.Sin(t), Mathf.Cos(t * 1.37f), 0f) * strength;
            shakeCamera.transform.position += shakeOffset;
        }

        void OnDisable()
        {
            if (shakeCamera != null) shakeCamera.transform.position -= shakeOffset;
            shakeCamera = null;
            shakeOffset = Vector3.zero;
            shakeLeft = 0f;
        }

        void OnDestroy()
        {
            if (particleSprite != null) Destroy(particleSprite);
            if (birdSprite != null) Destroy(birdSprite);
            if (particleTexture != null) Destroy(particleTexture);
            if (birdTexture != null) Destroy(birdTexture);
        }
    }
}
