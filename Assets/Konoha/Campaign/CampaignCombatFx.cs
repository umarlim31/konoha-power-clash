using System;
using System.Collections.Generic;
using Konoha.Networking;
using UnityEngine;
using UnityEngine.Rendering;

namespace Konoha.Campaign
{
    // 0.2.3: combat effects of Jalur Takhta. Hit sparks and flashes, punch arcs, shockwave
    // rings, dust, and one signature effect per skill (Kerbau Rakyat for SERUAN IBU, a
    // stampede for MONCONG, Kader shields, a leap impact, marching ranks, golden wings,
    // lightning, speech rings, traffic cones...).
    //
    // Local presentation only (no NetworkObjects, no colliders). Opaque shared materials from
    // the generator; colours by property block. Sparks and dust come from fixed pools;
    // signature objects are built from primitives on cast and destroyed when they finish.
    public sealed class CampaignCombatFx : MonoBehaviour
    {
        // Unlit glow (colour per property block), lit dust, and the kerbau / people / props.
        public Material glow, dust, kerbauHide, kerbauHorn, skin, cloth, pants, hair, wood, cone, white;
        public int sparkPool = 60, dustPool = 60;

        private sealed class Particle
        {
            public Transform transform;
            public Renderer renderer;
            public Vector3 velocity;
            public float age, life, gravity, size;
            public bool active;
        }

        private sealed class Timed
        {
            public GameObject root;
            public float start, duration;
            public Action<float> tick;
            public Action onBegin;
            public bool begun;
        }

        private readonly List<Particle> sparks = new List<Particle>();
        private readonly List<Particle> dusts = new List<Particle>();
        private readonly List<Timed> timed = new List<Timed>();
        private MaterialPropertyBlock block;
        private Mesh ringMesh, arcMesh;
        private Transform poolRoot;
        private Font font;
        private int nextSpark, nextDust;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public static readonly Color MegaColor = new Color(.92f, .16f, .18f);
        public static readonly Color GemoyColor = new Color(1f, .78f, .25f);
        public static readonly Color AbahColor = new Color(.35f, .80f, 1f);
        public static readonly Color PakWiColor = new Color(1f, .55f, .15f);
        public static readonly Color HitColor = new Color(1f, .92f, .62f);
        public static readonly Color ShieldColor = new Color(.45f, .90f, 1f);

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            ringMesh = BuildRing(.86f, 1f, 360f, 48);
            arcMesh = BuildRing(.62f, 1f, 130f, 20);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            poolRoot = new GameObject("CombatFxPool").transform;
            poolRoot.SetParent(transform, false);
            for (int i = 0; i < sparkPool; i++)
                sparks.Add(MakeParticle(PrimitiveType.Cube, glow));
            for (int i = 0; i < dustPool; i++)
                dusts.Add(MakeParticle(PrimitiveType.Sphere, dust));
        }

        public static Color HeroColor(PrototypeHero hero)
        {
            switch (hero)
            {
                case PrototypeHero.Mega: return MegaColor;
                case PrototypeHero.Prabowo: return GemoyColor;
                case PrototypeHero.Abah: return AbahColor;
                default: return PakWiColor;
            }
        }

        // --- Update -----------------------------------------------------------------------

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;
            TickParticles(sparks, dt, true);
            TickParticles(dusts, dt, false);

            float now = Time.time;
            for (int i = timed.Count - 1; i >= 0; i--)
            {
                Timed item = timed[i];
                float t = (now - item.start) / item.duration;
                if (t < 0f)
                    continue;
                if (!item.begun)
                {
                    item.begun = true;
                    if (item.root != null) item.root.SetActive(true);
                    item.onBegin?.Invoke();
                }
                try
                {
                    item.tick?.Invoke(Mathf.Clamp01(t));
                }
                catch (Exception error)
                {
                    Debug.LogException(error);
                    t = 1f;
                }
                if (t >= 1f)
                {
                    if (item.root != null) Destroy(item.root);
                    timed.RemoveAt(i);
                }
            }
        }

        private void TickParticles(List<Particle> list, float dt, bool shrink)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Particle p = list[i];
                if (!p.active)
                    continue;
                p.age += dt;
                if (p.age >= p.life)
                {
                    p.active = false;
                    p.transform.gameObject.SetActive(false);
                    continue;
                }
                p.velocity += Vector3.down * p.gravity * dt;
                p.velocity *= 1f - Mathf.Min(1f, 2.2f * dt);
                p.transform.position += p.velocity * dt;
                float k = p.age / p.life;
                if (shrink)
                {
                    // Sparks: thin streaks along their flight, shrinking out.
                    float s = Mathf.Max(.001f, p.size * (1f - k));
                    p.transform.localScale = new Vector3(s * .5f, s * .5f, s * 2.6f);
                    p.transform.rotation = Quaternion.LookRotation(p.velocity.sqrMagnitude > .01f ? p.velocity : Vector3.up);
                }
                else
                {
                    // Dust: puffs that swell, then fade by shrinking.
                    float s = Mathf.Max(.001f, p.size * (.6f + .9f * k) * (1f - k * k));
                    p.transform.localScale = Vector3.one * s;
                }
            }
        }

        // --- Building blocks ---------------------------------------------------------------

        public void Sparks(Vector3 point, Vector3 direction, Color color, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Particle p = sparks[nextSpark];
                nextSpark = (nextSpark + 1) % sparks.Count;
                Vector3 spread = UnityEngine.Random.onUnitSphere;
                Vector3 v = (direction.normalized * .8f + spread * .9f).normalized * speed * UnityEngine.Random.Range(.5f, 1.2f);
                Launch(p, point, v, UnityEngine.Random.Range(.18f, .34f), 9f, UnityEngine.Random.Range(.07f, .14f));
                p.transform.localScale = new Vector3(p.size * .5f, p.size * .5f, p.size * 2.6f);
                Tint(p.renderer, color);
            }
        }

        public void Dust(Vector3 point, int count, float radius, float rise)
        {
            for (int i = 0; i < count; i++)
            {
                Particle p = dusts[nextDust];
                nextDust = (nextDust + 1) % dusts.Count;
                Vector2 disc = UnityEngine.Random.insideUnitCircle;
                Vector3 offset = new Vector3(disc.x, 0f, disc.y) * radius;
                Vector3 v = new Vector3(disc.x * 2.2f, rise * UnityEngine.Random.Range(.6f, 1.2f), disc.y * 2.2f);
                Launch(p, point + offset + Vector3.up * .15f, v, UnityEngine.Random.Range(.5f, .9f), -.4f, UnityEngine.Random.Range(.35f, .7f));
                p.transform.localScale = Vector3.one * p.size * .6f;
            }
        }

        private static void Launch(Particle p, Vector3 point, Vector3 velocity, float life, float gravity, float size)
        {
            p.active = true;
            p.age = 0f;
            p.life = life;
            p.gravity = gravity;
            p.size = size;
            p.velocity = velocity;
            p.transform.position = point;
            p.transform.gameObject.SetActive(true);
        }

        // A flat shockwave ring on the ground growing from r0 to r1.
        public void Shockwave(Vector3 center, Color color, float r0, float r1, float seconds, float delay = 0f, float height = .08f)
        {
            var go = new GameObject("FxShockwave");
            go.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = glow;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Tint(renderer, color);
            go.transform.position = new Vector3(center.x, center.y + height, center.z);
            go.transform.localScale = new Vector3(r0, 1f, r0);
            Add(go, seconds, delay, t =>
            {
                float r = Mathf.Lerp(r0, r1, 1f - (1f - t) * (1f - t));
                float thin = 1f - t * .7f;
                go.transform.localScale = new Vector3(r, thin, r);
                Tint(renderer, Color.Lerp(color, Color.black, t * .85f));
            });
        }

        // Punch arc in front of an attacker, sweeping across.
        public void Slash(Transform attacker, Color color, bool fromLeft, float size = 1.3f)
        {
            if (attacker == null)
                return;
            var go = new GameObject("FxSlash");
            go.AddComponent<MeshFilter>().sharedMesh = arcMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = glow;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Tint(renderer, color);
            Vector3 origin = attacker.position + Vector3.up * 1.25f;
            Quaternion facing = Quaternion.LookRotation(Flat(attacker.forward));
            float roll = fromLeft ? 22f : -22f;
            Add(go, .2f, .06f, t =>
            {
                float sweep = Mathf.Lerp(fromLeft ? -55f : 55f, fromLeft ? 45f : -45f, Mathf.SmoothStep(0f, 1f, t * 1.4f));
                go.transform.SetPositionAndRotation(origin + facing * Vector3.forward * .15f, facing * Quaternion.Euler(0f, sweep, roll));
                float s = size * (t < .7f ? 1f : 1f - (t - .7f) / .3f);
                go.transform.localScale = new Vector3(s, 1f, s);
            });
        }

        // Bright burst where a hit lands.
        public void Impact(Vector3 point, Vector3 direction, Color color, float strength)
        {
            strength = Mathf.Clamp01(strength);
            Sparks(point, direction, color, 5 + Mathf.RoundToInt(strength * 9f), 6f + strength * 5f);
            var flash = Primitive(PrimitiveType.Sphere, null, point, Vector3.one * .2f, glow, Color.white);
            float peak = .55f + strength * .6f;
            Add(flash, .12f, 0f, t =>
            {
                float s = t < .35f ? Mathf.Lerp(.2f, peak, t / .35f) : Mathf.Lerp(peak, .05f, (t - .35f) / .65f);
                flash.transform.localScale = Vector3.one * s;
            });
        }

        public void FloatText(string text, Vector3 position, Color color, float size = .09f, float seconds = 1.1f)
        {
            var go = new GameObject("FxText " + text);
            go.transform.position = position;
            var label = go.AddComponent<TextMesh>();
            label.font = font;
            label.text = text;
            label.fontSize = 48;
            label.characterSize = size;
            label.fontStyle = FontStyle.Bold;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            var renderer = go.GetComponent<MeshRenderer>();
            if (font != null && renderer != null)
                renderer.sharedMaterial = font.material;
            Add(go, seconds, 0f, t =>
            {
                go.transform.position = position + Vector3.up * (t * .9f);
                Billboard(go.transform);
                float pop = t < .15f ? Mathf.Lerp(.4f, 1.15f, t / .15f) : t < .25f ? Mathf.Lerp(1.15f, 1f, (t - .15f) / .1f) : 1f;
                go.transform.localScale = Vector3.one * pop;
                label.color = new Color(color.r, color.g, color.b, t < .75f ? 1f : 1f - (t - .75f) / .25f);
            });
        }

        // --- Signature skill effects ------------------------------------------------------

        // One entry point per cast. `from` is where the hero stood before any dash or leap.
        public void Skill(NetworkHeroKit kit, PrototypeHero hero, int slot, Vector3 position, Vector3 direction, Vector3 from)
        {
            direction = Flat(direction);
            Transform caster = kit != null ? kit.transform : null;
            Vector3 head = (caster != null ? caster.position : position) + Vector3.up * 2.6f;
            Color color = HeroColor(hero);
            switch (hero)
            {
                case PrototypeHero.Mega:
                    if (slot == 1) { SeruanIbu(from, direction); FloatText("SERUAN IBU!", head, color); }
                    else if (slot == 2) { KaderShields(position, caster != null ? caster.right : Vector3.right, direction); FloatText("PERISAI RAKYAT!", head, color); }
                    else { Stampede(from, direction); FloatText("KERBAU RAKYAT!", head, color, .12f, 1.4f); }
                    break;
                case PrototypeHero.Prabowo:
                    if (slot == 1) { LeapImpact(position); FloatText("CMD LEAP!", position + Vector3.up * 2.6f, color); }
                    else if (slot == 2) { Baris(position, direction); FloatText("BARIS!", head, color); }
                    else { GoldenWings(caster, position); FloatText("GARUDA!", head, color, .12f, 1.4f); }
                    break;
                case PrototypeHero.Abah:
                    if (slot == 1) { NarasiZone(position); FloatText("NARASI!", head, color); }
                    else if (slot == 2) { Lightning(from, from + direction * 5.2f); FloatText("ELECTRIC DASH!", head, color); }
                    else { Pidato(caster != null ? caster.position : position, direction); FloatText("PIDATO KEBANGSAAN!", head + Vector3.up * .3f, color, .12f, 1.6f); }
                    break;
                default:
                    if (slot == 1) { RoadWorks(position, direction); FloatText("INFRASTRUKTUR!", head, color); }
                    else if (slot == 2) { Blusukan(from, direction, 5.8f); FloatText("BLUSUKAN!", head, color); }
                    else { ProyekNasional(caster != null ? caster.position : position); FloatText("PROYEK NASIONAL!", head, color, .12f, 1.4f); }
                    break;
            }
            Sound(slot >= 3 ? CampaignSound.Hantam : CampaignSound.Wuss, caster != null ? caster.position : position, slot >= 3 ? 1f : .7f);
        }

        // MEGA S1: a water buffalo charges through the line of the push.
        private void SeruanIbu(Vector3 from, Vector3 direction)
        {
            Kerbau(from - direction * 2f, from + direction * 9f, .55f, 0f, 1f);
            Shockwave(from, MegaColor, .4f, 2.2f, .3f);
            Shockwave(from + direction * 6.2f, MegaColor, .5f, 3f, .35f, .35f);
            Delay(.3f, () => CampaignCameraShake.Shake(.45f, .3f));
            Sound(CampaignSound.Kerbau, from, 1f);
        }

        // MEGA ultimate: three kerbau stampede down the 12 m line.
        private void Stampede(Vector3 from, Vector3 direction)
        {
            Vector3 side = new Vector3(direction.z, 0f, -direction.x);
            for (int i = -1; i <= 1; i++)
                Kerbau(from + side * (i * 1.9f) - direction * 3f, from + side * (i * 1.9f) + direction * 15f, .85f, Mathf.Abs(i) * .08f, 1.15f);
            Shockwave(from, MegaColor, .5f, 4.5f, .5f);
            Shockwave(from + direction * 12f, HitColor, .5f, 3.5f, .4f, .6f);
            Delay(.15f, () => CampaignCameraShake.Shake(.8f, .7f));
            Sound(CampaignSound.Kerbau, from, 1f);
            Delay(.2f, () => Sound(CampaignSound.Kerbau, from + direction * 6f, .8f));
        }

        private void Kerbau(Vector3 start, Vector3 end, float seconds, float delay, float scale)
        {
            Vector3 direction = Flat(end - start);
            var root = new GameObject("FxKerbauRakyat");
            root.transform.SetPositionAndRotation(start, Quaternion.LookRotation(direction));
            root.transform.localScale = Vector3.one * scale;
            Transform t = root.transform;
            Primitive(PrimitiveType.Sphere, t, new Vector3(0f, 1.1f, 0f), new Vector3(1.15f, 1.05f, 2.3f), kerbauHide, null, Vector3.zero, true);
            Primitive(PrimitiveType.Sphere, t, new Vector3(0f, 1.45f, .55f), new Vector3(1.05f, .85f, 1f), kerbauHide, null, Vector3.zero, true);
            var head = new GameObject("Kepala").transform;
            head.SetParent(t, false);
            head.localPosition = new Vector3(0f, 1.3f, 1.35f);
            head.localRotation = Quaternion.Euler(28f, 0f, 0f);
            Primitive(PrimitiveType.Sphere, head, new Vector3(0f, 0f, .25f), new Vector3(.58f, .6f, .85f), kerbauHide);
            Primitive(PrimitiveType.Sphere, head, new Vector3(0f, -.12f, .65f), new Vector3(.46f, .36f, .36f), pants);
            foreach (int s in new[] { -1, 1 })
            {
                // Wide swept-back horns of the Indonesian kerbau.
                Primitive(PrimitiveType.Capsule, head, new Vector3(s * .5f, .22f, .1f), new Vector3(.16f, .38f, .16f), kerbauHorn, null, new Vector3(0f, 0f, s * 75f));
                Primitive(PrimitiveType.Capsule, head, new Vector3(s * .86f, .36f, -.12f), new Vector3(.13f, .3f, .13f), kerbauHorn, null, new Vector3(-35f, 0f, s * 25f));
                Primitive(PrimitiveType.Sphere, head, new Vector3(s * .22f, .12f, .55f), Vector3.one * .08f, glow, new Color(.95f, .25f, .15f));
                Primitive(PrimitiveType.Cube, head, new Vector3(s * .36f, .05f, .05f), new Vector3(.25f, .08f, .16f), kerbauHide);
            }
            var legs = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var hip = new GameObject("Kaki").transform;
                hip.SetParent(t, false);
                hip.localPosition = new Vector3(i % 2 == 0 ? -.38f : .38f, .85f, i < 2 ? .7f : -.75f);
                Primitive(PrimitiveType.Cylinder, hip, new Vector3(0f, -.42f, 0f), new Vector3(.24f, .42f, .24f), kerbauHide);
                Primitive(PrimitiveType.Cube, hip, new Vector3(0f, -.85f, .02f), new Vector3(.26f, .1f, .3f), pants);
                legs[i] = hip;
            }
            Primitive(PrimitiveType.Cylinder, t, new Vector3(0f, 1.2f, -1.25f), new Vector3(.06f, .45f, .06f), kerbauHide, null, new Vector3(-30f, 0f, 0f));
            root.SetActive(false);

            float lastDust = -1f;
            Add(root, seconds, delay, k =>
            {
                float e = k;
                Vector3 p = Vector3.Lerp(start, end, e);
                float gallop = k * seconds * 11f;
                p.y = start.y + Mathf.Abs(Mathf.Sin(gallop)) * .18f;
                t.position = p;
                t.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(Mathf.Sin(gallop) * 5f, 0f, 0f);
                for (int i = 0; i < 4; i++)
                    legs[i].localRotation = Quaternion.Euler(Mathf.Sin(gallop + (i < 2 ? 0f : Mathf.PI) + (i % 2) * .6f) * 42f, 0f, 0f);
                // Enter/leave by growing and sinking so it never pops.
                float appear = Mathf.Clamp01(k / .12f) * Mathf.Clamp01((1f - k) / .15f);
                t.localScale = Vector3.one * scale * Mathf.Lerp(.3f, 1f, appear);
                if (k - lastDust > .08f)
                {
                    lastDust = k;
                    Dust(p, 3, .6f, 1.4f);
                }
            });
        }

        // MEGA S2: two Kader with tall shields where the (invisible) blockers stand.
        private void KaderShields(Vector3 center, Vector3 right, Vector3 facing)
        {
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 at = center + Flat(right) * (1.2f * s);
                var kader = Person(at, facing, cloth, MegaColor, true);
                Transform t = kader.transform;
                Dust(at, 6, .8f, 1.2f);
                Shockwave(at, ShieldColor, .3f, 1.6f, .35f);
                Add(kader, 6f, 0f, k =>
                {
                    float rise = Mathf.Clamp01(k * 6f / .3f);
                    float sink = Mathf.Clamp01((1f - k) * 6f / .3f);
                    t.position = at + Vector3.down * (1.9f * (1f - Mathf.Min(rise, sink)));
                });
            }
        }

        // GEMOY S1: landing crater.
        private void LeapImpact(Vector3 landing)
        {
            Delay(.34f, () =>
            {
                Shockwave(landing, GemoyColor, .5f, 3.6f, .4f);
                Shockwave(landing, HitColor, .3f, 2.2f, .25f);
                Dust(landing, 18, 1.2f, 2.2f);
                Sparks(landing + Vector3.up * .3f, Vector3.up, GemoyColor, 14, 8f);
                CampaignCameraShake.Shake(.6f, .35f);
                Sound(CampaignSound.Hantam, landing, .9f);
            });
        }

        // GEMOY S2: a rank of soldiers steps out in front and marches forward.
        private void Baris(Vector3 position, Vector3 direction)
        {
            Vector3 side = new Vector3(direction.z, 0f, -direction.x);
            for (int i = -2; i <= 2; i++)
            {
                Vector3 at = position + direction * 1.4f + side * (i * .9f);
                var soldier = Person(at, direction, cloth, new Color(.18f, .25f, .16f), false);
                Transform t = soldier.transform;
                Add(soldier, 1.1f, Mathf.Abs(i) * .04f, k =>
                {
                    float rise = Mathf.Clamp01(k / .18f);
                    float sink = Mathf.Clamp01((1f - k) / .2f);
                    float march = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - .15f) / .7f));
                    t.position = at + direction * (3f * march) + Vector3.down * (1.9f * (1f - Mathf.Min(rise, sink)));
                });
            }
            ConeSweep(position, direction, 5f, GemoyColor);
            Dust(position + direction * 2f, 10, 2f, 1f);
            Delay(.2f, () => CampaignCameraShake.Shake(.3f, .25f));
        }

        // GEMOY ultimate: golden wings on the hero for the 8 s buff.
        private void GoldenWings(Transform hero, Vector3 position)
        {
            Shockwave(position, GemoyColor, .5f, 4f, .5f);
            Sparks(position + Vector3.up * 1f, Vector3.up, GemoyColor, 20, 9f);
            CampaignCameraShake.Shake(.5f, .4f);
            if (hero == null)
                return;
            var root = new GameObject("FxSayapEmas");
            root.transform.SetPositionAndRotation(hero.position, hero.rotation);
            var wings = new Transform[2];
            for (int w = 0; w < 2; w++)
            {
                int s = w == 0 ? -1 : 1;
                var wing = new GameObject("Sayap").transform;
                wing.SetParent(root.transform, false);
                wing.localPosition = new Vector3(s * .2f, 1.45f, -.25f);
                for (int f = 0; f < 6; f++)
                {
                    float angle = 10f + f * 14f;
                    var feather = Primitive(PrimitiveType.Cube, wing, Vector3.zero, new Vector3(.14f, .05f, 1f), glow, GemoyColor);
                    feather.transform.localRotation = Quaternion.Euler(0f, s * (70f + f * 4f), s * (-angle));
                    feather.transform.localPosition = Quaternion.Euler(0f, 0f, s * (-angle)) * new Vector3(s * (.45f + f * .1f), 0f, 0f);
                    feather.transform.localScale = new Vector3(.12f, .04f, 1.1f - f * .08f);
                }
                wings[w] = wing;
            }
            Add(root, 8f, 0f, k =>
            {
                if (hero == null) return;
                root.transform.SetPositionAndRotation(hero.position, hero.rotation);
                float open = Mathf.Clamp01(k * 8f / .4f) * Mathf.Clamp01((1f - k) * 8f / .4f);
                float flap = Mathf.Sin(Time.time * 4f) * 9f;
                for (int w = 0; w < 2; w++)
                {
                    int s = w == 0 ? -1 : 1;
                    wings[w].localRotation = Quaternion.Euler(0f, s * 12f, s * flap);
                    wings[w].localScale = Vector3.one * Mathf.Max(.01f, open);
                }
            });
        }

        // ABAH S1: speech rings pulse over the zone while floating words rise.
        private void NarasiZone(Vector3 center)
        {
            for (int i = 0; i < 7; i++)
                Shockwave(center, AbahColor, .3f, 4.6f, .9f, i * .8f, .06f);
            string[] words = { "GAGASAN", "DATA", "DIALOG", "NARASI", "SOLUSI", "RENCANA" };
            for (int i = 0; i < words.Length; i++)
            {
                Vector2 disc = UnityEngine.Random.insideUnitCircle * 3.5f;
                string word = words[i];
                Vector3 at = center + new Vector3(disc.x, 1.4f, disc.y);
                Delay(i * .8f, () => FloatText(word, at, AbahColor, .07f, 1.4f));
            }
        }

        // ABAH S2: a lightning streak along the dash.
        private void Lightning(Vector3 from, Vector3 to)
        {
            var root = new GameObject("FxPetir");
            const int segments = 9;
            Vector3 previous = from + Vector3.up * 1.1f;
            for (int i = 1; i <= segments; i++)
            {
                Vector3 next = Vector3.Lerp(from, to, i / (float)segments) + Vector3.up * 1.1f;
                if (i < segments)
                    next += new Vector3(UnityEngine.Random.Range(-.45f, .45f), UnityEngine.Random.Range(-.35f, .45f), UnityEngine.Random.Range(-.45f, .45f));
                Vector3 delta = next - previous;
                var bolt = Primitive(PrimitiveType.Cube, root.transform, (previous + next) * .5f, new Vector3(.07f, .07f, delta.magnitude), glow, AbahColor);
                bolt.transform.rotation = Quaternion.LookRotation(delta.normalized);
                previous = next;
            }
            Add(root, .32f, 0f, k => root.SetActive(Mathf.Repeat(k * 9f, 1f) < .7f || k < .3f));
            Sparks(from + Vector3.up, Flat(to - from), AbahColor, 10, 7f);
            Delay(.12f, () => Sparks(to + Vector3.up, Flat(to - from), AbahColor, 12, 8f));
            Shockwave(to, AbahColor, .3f, 1.8f, .3f, .1f);
            CampaignCameraShake.Shake(.25f, .2f);
            Sound(CampaignSound.Petir, from, .9f);
        }

        // ABAH ultimate: a podium and microphone, three sound rings to 7 m.
        private void Pidato(Vector3 hero, Vector3 facing)
        {
            Vector3 at = hero + facing * 1.1f;
            var podium = new GameObject("FxPodium");
            podium.transform.SetPositionAndRotation(at + Vector3.down * 1.3f, Quaternion.LookRotation(-facing));
            Primitive(PrimitiveType.Cube, podium.transform, new Vector3(0f, .55f, 0f), new Vector3(.8f, 1.1f, .5f), wood);
            Primitive(PrimitiveType.Cube, podium.transform, new Vector3(0f, .7f, -.26f), new Vector3(.55f, .35f, .02f), glow, GemoyColor);
            Primitive(PrimitiveType.Cylinder, podium.transform, new Vector3(0f, 1.3f, .1f), new Vector3(.03f, .22f, .03f), pants, null, new Vector3(-25f, 0f, 0f));
            Primitive(PrimitiveType.Sphere, podium.transform, new Vector3(0f, 1.5f, .02f), Vector3.one * .1f, pants);
            Add(podium, 1.8f, 0f, k =>
            {
                float rise = Mathf.Clamp01(k / .12f) * Mathf.Clamp01((1f - k) / .12f);
                podium.transform.position = at + Vector3.down * (1.3f * (1f - rise));
            });
            for (int i = 0; i < 3; i++)
                Shockwave(hero, i == 1 ? Color.white : AbahColor, .5f, 7f, .6f, .15f + i * .18f, .1f + i * .02f);
            Delay(.15f, () => CampaignCameraShake.Shake(.55f, .45f));
        }

        // PAK WI S1: traffic cones and a project sign along the road.
        private void RoadWorks(Vector3 center, Vector3 direction)
        {
            Quaternion facing = Quaternion.LookRotation(direction);
            for (int i = 0; i < 6; i++)
            {
                Vector3 local = new Vector3(i % 2 == 0 ? -1.95f : 1.95f, 0f, -3.6f + (i / 2) * 3.6f);
                Vector3 at = center + facing * local;
                var trafficCone = Cone(at);
                Add(trafficCone, 8f, i * .07f, k => trafficCone.transform.localScale = Vector3.one * Mathf.Clamp01(Mathf.Min(k * 8f / .2f, (1f - k) * 8f / .3f)));
                Dust(at, 2, .3f, .8f);
            }
            var sign = new GameObject("FxPapanProyek");
            Vector3 signAt = center + facing * new Vector3(0f, 0f, 5.1f);
            sign.transform.SetPositionAndRotation(signAt, facing * Quaternion.Euler(0f, 180f, 0f));
            foreach (int s in new[] { -1, 1 })
                Primitive(PrimitiveType.Cylinder, sign.transform, new Vector3(s * .9f, .8f, 0f), new Vector3(.06f, .8f, .06f), white);
            Primitive(PrimitiveType.Cube, sign.transform, new Vector3(0f, 1.6f, 0f), new Vector3(2.2f, .75f, .06f), cone);
            // The sign's front (local +z) faces the hero; text reads from that side.
            var label = Text(sign.transform, "PROYEK STRATEGIS\nKONOHA", new Vector3(0f, 1.6f, .05f), .045f, Color.black);
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Add(sign, 8f, .3f, k => sign.transform.localScale = Vector3.one * Mathf.Clamp01(Mathf.Min(k * 8f / .25f, (1f - k) * 8f / .3f)));
        }

        // PAK WI S2: a dusty trail along the dash.
        private void Blusukan(Vector3 from, Vector3 direction, float distance)
        {
            for (int i = 0; i < 7; i++)
            {
                Vector3 at = from + direction * (distance * i / 6f);
                Delay(i * .025f, () => Dust(at, 3, .35f, 1.1f));
            }
            Shockwave(from + direction * distance, PakWiColor, .3f, 1.6f, .3f, .15f);
        }

        // PAK WI ultimate (solo, 0.2.4): a ground-breaking blast around PAK WI; three
        // construction sites with cones pop up on the 7 m ring.
        private void ProyekNasional(Vector3 center)
        {
            Shockwave(center, PakWiColor, .5f, HeroBalance.SoloProyekRadius, .55f);
            Shockwave(center, HitColor, .4f, HeroBalance.SoloProyekRadius * .7f, .4f, .12f);
            Dust(center, 20, 3f, 2.2f);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2f / 3f + .5f;
                Vector3 site = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 4.5f;
                Dust(site, 6, 1f, 1.8f);
                foreach (float side in new[] { -.7f, .7f })
                {
                    var trafficCone = Cone(site + new Vector3(side, 0f, 0f));
                    Add(trafficCone, 4f, i * .08f, k => trafficCone.transform.localScale =
                        Vector3.one * Mathf.Clamp01(Mathf.Min(k * 4f / .2f, (1f - k) * 4f / .3f)));
                }
            }
            CampaignCameraShake.Shake(.6f, .45f);
        }

        // A flat fan of light showing the BARIS! cone.
        private void ConeSweep(Vector3 origin, Vector3 direction, float radius, Color color)
        {
            var go = new GameObject("FxKerucut");
            go.AddComponent<MeshFilter>().sharedMesh = arcMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = glow;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Tint(renderer, color);
            go.transform.SetPositionAndRotation(origin + Vector3.up * .09f, Quaternion.LookRotation(direction));
            Add(go, .45f, 0f, k =>
            {
                float r = Mathf.Lerp(.5f, radius, 1f - (1f - k) * (1f - k));
                go.transform.localScale = new Vector3(r, 1f, r);
                Tint(renderer, Color.Lerp(color, Color.black, k * .8f));
            });
        }

        // --- Props ------------------------------------------------------------------------

        // Simple standing figure (Kader, prajurit). Shield: carries a tall glowing-rim shield.
        private GameObject Person(Vector3 at, Vector3 facing, Material shirt, Color shirtColor, bool shield)
        {
            var root = new GameObject("FxOrang");
            root.transform.SetPositionAndRotation(at, Quaternion.LookRotation(Flat(facing)));
            Transform t = root.transform;
            foreach (int s in new[] { -1, 1 })
            {
                // 0.3.0: rounded limbs and torso like the hero bodies.
                Primitive(PrimitiveType.Capsule, t, new Vector3(s * .11f, .47f, 0f), new Vector3(.16f, .47f, .16f), pants);
                Primitive(PrimitiveType.Capsule, t, new Vector3(s * .27f, 1.2f, .12f), new Vector3(.12f, .32f, .12f), shirt, shirtColor, new Vector3(-50f, 0f, 0f));
            }
            Primitive(PrimitiveType.Capsule, t, new Vector3(0f, 1.2f, 0f), new Vector3(.44f, .34f, .27f), shirt, shirtColor, Vector3.zero, true);
            Primitive(PrimitiveType.Sphere, t, new Vector3(0f, 1.68f, 0f), new Vector3(.23f, .27f, .24f), skin);
            Primitive(PrimitiveType.Sphere, t, new Vector3(0f, 1.75f, -.02f), new Vector3(.25f, .18f, .26f), hair);
            if (shield)
            {
                Primitive(PrimitiveType.Cube, t, new Vector3(0f, 1.05f, .5f), new Vector3(1.05f, 1.7f, .08f), white);
                Primitive(PrimitiveType.Cube, t, new Vector3(0f, 1.05f, .54f), new Vector3(1.12f, .12f, .05f), glow, ShieldColor);
                Primitive(PrimitiveType.Cube, t, new Vector3(0f, 1.8f, .54f), new Vector3(1.12f, .12f, .05f), glow, ShieldColor);
                Primitive(PrimitiveType.Cube, t, new Vector3(0f, .3f, .54f), new Vector3(1.12f, .12f, .05f), glow, ShieldColor);
            }
            root.SetActive(false);
            return root;
        }

        private GameObject Cone(Vector3 at)
        {
            var root = new GameObject("FxKerucutLalin");
            root.transform.position = at;
            Primitive(PrimitiveType.Cube, root.transform, new Vector3(0f, .03f, 0f), new Vector3(.55f, .06f, .55f), pants);
            Primitive(PrimitiveType.Cylinder, root.transform, new Vector3(0f, .3f, 0f), new Vector3(.34f, .25f, .34f), cone);
            Primitive(PrimitiveType.Cylinder, root.transform, new Vector3(0f, .6f, 0f), new Vector3(.2f, .08f, .2f), white);
            Primitive(PrimitiveType.Cylinder, root.transform, new Vector3(0f, .76f, 0f), new Vector3(.12f, .08f, .12f), cone);
            root.transform.localScale = Vector3.zero;
            return root;
        }

        private TextMesh Text(Transform parent, string text, Vector3 local, float size, Color color)
        {
            var go = new GameObject("FxLabel");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var label = go.AddComponent<TextMesh>();
            label.font = font;
            label.text = text;
            label.fontSize = 48;
            label.characterSize = size;
            label.fontStyle = FontStyle.Bold;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            var renderer = go.GetComponent<MeshRenderer>();
            if (font != null && renderer != null)
                renderer.sharedMaterial = font.material;
            return label;
        }

        private GameObject Primitive(PrimitiveType type, Transform parent, Vector3 local, Vector3 scale, Material material,
            Color? tint = null, Vector3 euler = default, bool shadow = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "Fx " + type;
            // Immediately: a collider alive until the end of the frame could nudge a hero.
            DestroyImmediate(go.GetComponent<Collider>());
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (tint.HasValue)
                Tint(renderer, tint.Value);
            return go;
        }

        private Particle MakeParticle(PrimitiveType type, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "FxParticle";
            DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(poolRoot, false);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            go.SetActive(false);
            return new Particle { transform = go.transform, renderer = renderer };
        }

        private void Tint(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColor, color);
            renderer.SetPropertyBlock(block);
        }

        private void Add(GameObject root, float seconds, float delay, Action<float> tick)
        {
            timed.Add(new Timed { root = root, start = Time.time + delay, duration = Mathf.Max(.01f, seconds), tick = tick });
            if (root != null && delay > 0f)
                root.SetActive(false);
        }

        public void Delay(float seconds, Action action)
        {
            timed.Add(new Timed { start = Time.time + seconds, duration = .01f, onBegin = action });
        }

        private static void Sound(CampaignSound sound, Vector3 at, float volume)
        {
            if (CampaignAudio.Instance != null)
                CampaignAudio.Instance.PlayAt(sound, at, volume);
        }

        private static void Billboard(Transform t)
        {
            Camera view = Camera.main;
            if (view == null) return;
            Vector3 d = t.position - view.transform.position;
            if (d.sqrMagnitude > .001f)
                t.rotation = Quaternion.LookRotation(d);
        }

        public static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > .0001f ? v.normalized : Vector3.forward;
        }

        // Flat ring (or arc of `degrees` centred on +z) in the XZ plane, visible from both sides.
        public static Mesh BuildRing(float inner, float outer, float degrees, int segments)
        {
            var mesh = new Mesh { name = "FxRing" };
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 12];
            float half = degrees * .5f * Mathf.Deg2Rad;
            for (int i = 0; i <= segments; i++)
            {
                float a = -half + 2f * half * i / segments;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                vertices[i * 2] = dir * inner;
                vertices[i * 2 + 1] = dir * outer;
            }
            for (int i = 0; i < segments; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3, k = i * 12;
                triangles[k] = a; triangles[k + 1] = b; triangles[k + 2] = c;
                triangles[k + 3] = c; triangles[k + 4] = b; triangles[k + 5] = d;
                triangles[k + 6] = a; triangles[k + 7] = c; triangles[k + 8] = b;
                triangles[k + 9] = c; triangles[k + 10] = d; triangles[k + 11] = b;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
