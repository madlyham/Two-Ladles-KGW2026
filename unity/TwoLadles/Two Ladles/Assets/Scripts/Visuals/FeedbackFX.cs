using System.Collections.Generic;
using UnityEngine;

namespace TwoLadles.Visuals
{
    public enum FxType { None, Combine, CookDone, Serve, Pass, Error, Pickup }

    /// <summary>
    /// Small particle bursts that acknowledge player input (combine, cook done, serve, pass, error, pickup).
    /// Purely visual: never touches gameplay state. Fixed pool of POOL_SIZE bursts (each MAX_PARTICLES_PER_BURST
    /// sprite quads), built once and reused round-robin, so playing an effect allocates nothing.
    /// Uses plain SpriteRenderers (no ParticleSystem module dependency).
    /// Host/solo: triggered from interaction toasts. LAN guest: triggered from station snapshot diffs.
    /// </summary>
    public class FeedbackFX : MonoBehaviour
    {
        public static FeedbackFX Instance { get; private set; }

        public const int POOL_SIZE = 24;
        public const int MAX_PARTICLES_PER_BURST = 12;
        private const float HEIGHT_OFFSET = 1.2f;
        private const float GRAVITY = 3.4f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() { EnsureInstance(); }

        public static FeedbackFX EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("FeedbackFX");
            if (Application.isPlaying) DontDestroyOnLoad(go);
            Instance = go.AddComponent<FeedbackFX>();
            Instance.BuildPool();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        // ---------------- Pure mapping (unit tested) ----------------

        /// <summary>Maps an interaction toast to an effect. Failed actions always play the red error burst.</summary>
        public static FxType FxForToast(string msg, bool ok)
        {
            if (string.IsNullOrEmpty(msg)) return FxType.None;
            if (!ok) return FxType.Error;
            if (msg.StartsWith("Dish Served")) return FxType.Serve;
            if (msg.StartsWith("Added ") && msg.Contains(" to ")) return FxType.Combine;
            if (msg.StartsWith("Assembled") || msg.StartsWith("Completed dish")) return FxType.Combine;
            if (msg.StartsWith("Passed!") || msg.StartsWith("Pass Caught") || msg.StartsWith("Thrown into Pass Counter")) return FxType.Pass;
            if (msg.StartsWith("Grabbed")) return FxType.Pickup;
            return FxType.None;
        }

        /// <summary>Maps a station snapshot change (LAN guest) to an effect. Use -1 for "not applicable".</summary>
        public static FxType FxForStationChange(int oldCookState, int newCookState, int oldParts, int newParts, int oldPassFill, int newPassFill)
        {
            if (oldCookState == 1 && newCookState == 2) return FxType.CookDone; // COOKING -> OPTIMAL
            if (oldParts >= 0 && newParts > oldParts) return FxType.Combine;
            if (oldPassFill >= 0 && newPassFill > oldPassFill) return FxType.Pass;
            return FxType.None;
        }

        public static Color ColorFor(FxType t)
        {
            switch (t)
            {
                case FxType.Combine: return new Color(1.00f, 0.82f, 0.30f); // gold
                case FxType.CookDone: return new Color(0.95f, 0.95f, 0.95f); // white steam
                case FxType.Serve: return new Color(0.35f, 1.00f, 0.50f); // green stars
                case FxType.Pass: return new Color(0.40f, 0.70f, 1.00f); // blue
                case FxType.Error: return new Color(1.00f, 0.35f, 0.35f); // red
                case FxType.Pickup: return new Color(0.90f, 0.90f, 0.80f); // soft
                default: return Color.white;
            }
        }

        private static int CountFor(FxType t)
        {
            switch (t)
            {
                case FxType.Serve: return 12;
                case FxType.Combine: return 10;
                case FxType.CookDone: return 8;
                case FxType.Pass: return 8;
                case FxType.Error: return 8;
                case FxType.Pickup: return 4;
                default: return 0;
            }
        }


        // ---------------- Visibility tuning (unit tested) ----------------

        public const float MIN_PARTICLE_SIZE = 0.35f;
        public const float MAX_PARTICLE_SIZE = 0.6f;
        public const float MIN_PARTICLE_LIFE = 0.8f;
        public const float MAX_PARTICLE_LIFE = 1.0f;
        public const float POP_MAX_SCALE = 1.6f;
        private const float POP_LIFE = 0.45f;
        private const float ICON_LIFE = 0.95f;
        private const float ICON_SIZE = 0.75f;
        private const float ICON_RISE = 0.9f;
        /// <summary>FX sorting order: above every world sprite (held items/station icons top out ~1450).
        /// Sorting order is a 16-bit value, so pop/particles/icon use SORT_ORDER..SORT_ORDER+2 (max 32767).</summary>
        public const int SORT_ORDER = 32000;
        /// <summary>World units each burst is moved toward the camera. Orthographic camera, so screen position
        /// is unchanged; it only puts the FX in front of 3D counters/appliances in the depth test.</summary>
        public const float CAMERA_PULL = 3f;

        // Kenney Particle Pack (CC0), pixelated to match the game. See Resources/Sprites/FX/CREDITS.txt.
        private static Sprite sStar6, sStar9, sMagic3, sCircle2, sSmoke7, sCircle5, sCheck, sCross;

        private static Sprite Load(ref Sprite slot, string name)
        {
            if (slot == null) slot = Resources.Load<Sprite>("Sprites/FX/" + name);
            return slot;
        }

        /// <summary>Particle sprite for an effect (cached after first load). Null for None.</summary>
        public static Sprite SpriteFor(FxType t)
        {
            switch (t)
            {
                case FxType.Combine: return Load(ref sStar6, "fx_star_06");
                case FxType.Pickup: return Load(ref sStar6, "fx_star_06");
                case FxType.Serve: return Load(ref sStar9, "fx_star_09");
                case FxType.Pass: return Load(ref sCircle2, "fx_circle_02");
                case FxType.CookDone: return Load(ref sSmoke7, "fx_smoke_07");
                case FxType.Error: return Load(ref sCircle5, "fx_circle_05");
                default: return null;
            }
        }

        /// <summary>Floating icon shown above the action: check for success moments, cross for errors.</summary>
        public static Sprite IconFor(FxType t)
        {
            switch (t)
            {
                case FxType.Serve:
                case FxType.Combine: return Load(ref sCheck, "fx_check");
                case FxType.Error: return Load(ref sCross, "fx_cross");
                default: return null;
            }
        }

        private static Sprite PopSprite() { return Load(ref sMagic3, "fx_magic_03"); }

        private static float SizeScaleFor(FxType t)
        {
            switch (t)
            {
                case FxType.Pickup: return 0.6f;
                case FxType.Serve: return 1.15f;
                default: return 1f;
            }
        }

        // ---------------- Playback ----------------

        private class Burst
        {
            public Transform root;
            public readonly Transform[] tf = new Transform[MAX_PARTICLES_PER_BURST];
            public readonly SpriteRenderer[] sr = new SpriteRenderer[MAX_PARTICLES_PER_BURST];
            public readonly Vector3[] vel = new Vector3[MAX_PARTICLES_PER_BURST];
            public readonly float[] size = new float[MAX_PARTICLES_PER_BURST];
            public readonly float[] life = new float[MAX_PARTICLES_PER_BURST];
            public readonly float[] rot = new float[MAX_PARTICLES_PER_BURST];
            public readonly float[] spin = new float[MAX_PARTICLES_PER_BURST];
            public Transform popTf, iconTf;
            public SpriteRenderer popSr, iconSr;
            public float popMax;
            public bool hasIcon;
            public int count;
            public float age;
            public float gravity;
            public Color color;
            public bool active;
        }

        private readonly List<Burst> pool = new List<Burst>(POOL_SIZE);
        private int next;
        private Sprite dot;
        private Transform cam;

        public int PoolCount => pool.Count;

        /// <summary>Convenience: play the effect matching a toast above a station/chef position.
        /// Any other successful action still gets a small puff, so every input is visibly acknowledged.</summary>
        public static void PlayForToast(string msg, bool ok, Vector3 pos)
        {
            if (string.IsNullOrEmpty(msg)) return;
            FxType t = FxForToast(msg, ok);
            if (t == FxType.None) t = FxType.Pickup;
            if (t == FxType.None) return;
            var fx = EnsureInstance();
            if (fx != null) fx.Play(t, pos);
        }

        /// <summary>Static helper used by gameplay hooks; safe to call anywhere.</summary>
        public static void PlayAt(FxType t, Vector3 pos)
        {
            if (t == FxType.None) return;
            var fx = EnsureInstance();
            if (fx != null) fx.Play(t, pos);
        }

        public void Play(FxType t, Vector3 pos)
        {
            if (t == FxType.None || pool.Count == 0) return;
            Burst b = pool[next];
            next = (next + 1) % pool.Count;
            if (b == null || b.root == null) return;

            bool steam = t == FxType.CookDone;
            bool error = t == FxType.Error;
            int count = Mathf.Min(CountFor(t), MAX_PARTICLES_PER_BURST);
            float sizeScale = SizeScaleFor(t);
            Sprite spr = SpriteFor(t);
            if (spr == null) spr = dot;

            b.color = ColorFor(t);
            b.count = count;
            b.age = 0f;
            b.gravity = steam ? -0.5f : GRAVITY; // steam drifts up, others arc down
            b.active = true;
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            Vector3 towardCam = cam != null ? -cam.forward : Vector3.zero;
            b.root.position = pos + Vector3.up * HEIGHT_OFFSET + towardCam * CAMERA_PULL;
            b.root.gameObject.SetActive(true);

            for (int i = 0; i < MAX_PARTICLES_PER_BURST; i++)
            {
                bool on = i < count;
                b.tf[i].gameObject.SetActive(on);
                if (!on) continue;
                float a = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                float spread = error ? 1.6f : (steam ? 0.45f : 1.25f);
                float up = steam ? Random.Range(0.7f, 1.1f) : Random.Range(1.8f, 2.8f);
                b.vel[i] = new Vector3(Mathf.Cos(a) * spread, up, Mathf.Sin(a) * spread);
                b.size[i] = Random.Range(MIN_PARTICLE_SIZE, MAX_PARTICLE_SIZE) * sizeScale * (steam ? 1.3f : 1f);
                b.life[i] = Random.Range(MIN_PARTICLE_LIFE, MAX_PARTICLE_LIFE);
                b.rot[i] = Random.Range(0f, 360f);
                b.spin[i] = Random.Range(-240f, 240f);
                b.tf[i].localPosition = Vector3.zero;
                b.tf[i].localScale = Vector3.one * b.size[i];
                b.sr[i].sprite = spr;
                b.sr[i].color = b.color;
            }

            // Centre pop ring: one quick bright flash so the eye finds the spot.
            Sprite pop = PopSprite();
            b.popSr.sprite = pop != null ? pop : dot;
            b.popSr.color = b.color;
            b.popMax = POP_MAX_SCALE * sizeScale;
            b.popTf.localPosition = Vector3.zero;
            b.popTf.localScale = Vector3.one * 0.3f;
            b.popTf.gameObject.SetActive(true);

            // Floating check / cross icon.
            Sprite icon = IconFor(t);
            b.hasIcon = icon != null;
            b.iconTf.gameObject.SetActive(b.hasIcon);
            if (b.hasIcon)
            {
                b.iconSr.sprite = icon;
                b.iconSr.color = error ? new Color(1f, 0.45f, 0.45f) : new Color(0.55f, 1f, 0.6f);
                b.iconTf.localPosition = Vector3.zero;
                b.iconTf.localScale = Vector3.zero;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Quaternion face = Quaternion.identity;
            bool haveCam = false;

            for (int p = 0; p < pool.Count; p++)
            {
                Burst b = pool[p];
                if (!b.active) continue;
                if (!haveCam)
                {
                    if (cam == null && Camera.main != null) cam = Camera.main.transform;
                    if (cam != null) face = cam.rotation;
                    haveCam = true;
                }

                b.age += dt;
                bool any = false;
                for (int i = 0; i < b.count; i++)
                {
                    float k = b.age / b.life[i];
                    if (k >= 1f)
                    {
                        if (b.tf[i].gameObject.activeSelf) b.tf[i].gameObject.SetActive(false);
                        continue;
                    }
                    any = true;
                    b.vel[i].y -= b.gravity * dt;
                    b.tf[i].localPosition += b.vel[i] * dt;
                    b.rot[i] += b.spin[i] * dt;
                    b.tf[i].rotation = face * Quaternion.Euler(0f, 0f, b.rot[i]);
                    b.tf[i].localScale = Vector3.one * (b.size[i] * Mathf.Lerp(1f, 0.4f, k));
                    Color c = b.color;
                    c.a = k < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.65f) / 0.35f);
                    b.sr[i].color = c;
                }

                // Pop ring: fast ease-out grow + fade.
                float pk = b.age / POP_LIFE;
                if (pk < 1f)
                {
                    any = true;
                    float e = 1f - (1f - pk) * (1f - pk);
                    b.popTf.rotation = face;
                    b.popTf.localScale = Vector3.one * Mathf.Lerp(0.3f, b.popMax, e);
                    Color c = b.color;
                    c.a = 1f - pk;
                    b.popSr.color = c;
                }
                else if (b.popTf.gameObject.activeSelf) b.popTf.gameObject.SetActive(false);

                // Icon: overshoot pop-in, rise, then fade.
                if (b.hasIcon)
                {
                    float ik = b.age / ICON_LIFE;
                    if (ik < 1f)
                    {
                        any = true;
                        float s = ik < 0.15f ? Mathf.Lerp(0f, 1.25f, ik / 0.15f)
                                : (ik < 0.3f ? Mathf.Lerp(1.25f, 1f, (ik - 0.15f) / 0.15f) : 1f);
                        b.iconTf.rotation = face;
                        b.iconTf.localScale = Vector3.one * (ICON_SIZE * s);
                        b.iconTf.localPosition = Vector3.up * (0.35f + ICON_RISE * (1f - (1f - ik) * (1f - ik)));
                        Color c = b.iconSr.color;
                        c.a = ik < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (ik - 0.65f) / 0.35f);
                        b.iconSr.color = c;
                    }
                    else
                    {
                        b.hasIcon = false;
                        b.iconTf.gameObject.SetActive(false);
                    }
                }

                if (!any)
                {
                    b.active = false;
                    b.root.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>Lowest sorting order across all pooled FX renderers (test hook).</summary>
        public int LowestSortingOrder()
        {
            int low = int.MaxValue;
            for (int p = 0; p < pool.Count; p++)
            {
                Burst b = pool[p];
                if (b.popSr != null) low = Mathf.Min(low, b.popSr.sortingOrder);
                if (b.iconSr != null) low = Mathf.Min(low, b.iconSr.sortingOrder);
                for (int i = 0; i < MAX_PARTICLES_PER_BURST; i++)
                    if (b.sr[i] != null) low = Mathf.Min(low, b.sr[i].sortingOrder);
            }
            return low;
        }

        private SpriteRenderer MakeRenderer(Transform parent, string name, int order)
        {
            var q = new GameObject(name);
            q.transform.SetParent(parent, false);
            var sr = q.AddComponent<SpriteRenderer>();
            sr.sprite = dot;
            sr.sortingOrder = order;
            q.SetActive(false);
            return sr;
        }

        private void BuildPool()
        {
            if (pool.Count > 0) return;

            // Fallback dot sprite, used only if an FX sprite fails to load.
            const int N = 8;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            float r = (N - 1) * 0.5f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
                    px[y * N + x] = d <= r + 0.2f ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            dot = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);

            for (int i = 0; i < POOL_SIZE; i++)
            {
                var b = new Burst();
                var root = new GameObject("FxBurst_" + i);
                root.transform.SetParent(transform, false);
                b.root = root.transform;
                b.popSr = MakeRenderer(b.root, "pop", SORT_ORDER);
                b.popTf = b.popSr.transform;
                for (int j = 0; j < MAX_PARTICLES_PER_BURST; j++)
                {
                    var sr = MakeRenderer(b.root, "p", SORT_ORDER + 1);
                    b.tf[j] = sr.transform;
                    b.sr[j] = sr;
                }
                b.iconSr = MakeRenderer(b.root, "icon", SORT_ORDER + 2);
                b.iconTf = b.iconSr.transform;
                root.SetActive(false);
                pool.Add(b);
            }
        }
    }
}
