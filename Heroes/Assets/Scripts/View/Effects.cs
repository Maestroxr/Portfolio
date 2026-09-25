using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The small spectacles of the game: numbers that float up from a wound, arrows, bolts and orbs of light in flight,
    /// bursts of sparks and puffs of smoke, flames and light for spells and treasure, all built in code from the two
    /// particle materials of the art (an additive glow and a softly blended one).
    /// </summary>
    public sealed class Effects : MonoBehaviour
    {
        [SerializeField] private Material particle;
        [SerializeField] private Material glow;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private GameObject arrow;
        [SerializeField] private GameObject bolt;

        private readonly Queue<ParticleSystem> bursts = new Queue<ParticleSystem>();
        private readonly Queue<ParticleSystem> smokes = new Queue<ParticleSystem>();
        private static Mesh quad;
        private Camera view;

        /// <summary>How fast shots fly and spells play: the battle speed of the settings while a battle is shown.</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>How much bigger floating words are drawn: a battlefield of its own is seen from farther than the map.</summary>
        public float TextScale { get; set; } = 1f;

        /// <summary>The camera floating words and orbs of light turn to: the map's, or the battlefield's while one is shown.</summary>
        public Camera View
        {
            get => view;
            set => view = value;
        }

        public void Setup(HeroesArt art, Camera camera)
        {
            view = camera;
            if (art != null)
            {
                font = art.bodyFont != null ? art.bodyFont : font;
                arrow = art.arrow != null ? art.arrow : arrow;
                bolt = art.bolt != null ? art.bolt : bolt;
                // The effects are added to the manager when the game starts, so nothing was serialized into them.
                particle = art.particleMaterial != null ? art.particleMaterial : particle;
                glow = art.glowMaterial != null ? art.glowMaterial : glow;
            }
        }

        // ------------------------------------------------------------------ floating text

        public void Float(Vector3 position, string text, Color color, float size = 3.2f, float rise = 1.6f, float duration = 1.4f)
        {
            var go = new GameObject("Float");
            go.transform.position = position;
            var tmp = go.AddComponent<TextMeshPro>();
            if (font != null)
            {
                tmp.font = font;
            }
            tmp.text = text;
            tmp.fontSize = size * TextScale;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.outlineWidth = 0.3f;
            tmp.outlineColor = new Color32(0, 0, 0, 220);
            tmp.rectTransform.sizeDelta = new Vector2(12f, 3f) * TextScale;
            tmp.sortingOrder = 50;
            if (view != null)
            {
                go.transform.rotation = view.transform.rotation;
            }
            StartCoroutine(FloatRoutine(go.transform, tmp, rise, duration));
        }

        private IEnumerator FloatRoutine(Transform t, TextMeshPro tmp, float rise, float duration)
        {
            Vector3 start = t.position;
            Color color = tmp.color;
            float time = 0f;
            while (time < duration && t != null)
            {
                time += Time.deltaTime;
                float k = time / duration;
                t.position = start + Vector3.up * (rise * Gamebox.Tween.OutQuad(k));
                if (view != null)
                {
                    t.rotation = view.transform.rotation;
                }
                color.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                tmp.color = color;
                t.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.6f, 1.1f, k / 0.15f) : Mathf.Lerp(1.1f, 1f, (k - 0.15f) / 0.85f));
                yield return null;
            }
            if (t != null)
            {
                Destroy(t.gameObject);
            }
        }

        // ------------------------------------------------------------------ particles

        private ParticleSystem MakeBurst(Material material, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.gravityModifier = 0.35f;
            main.maxParticles = 400;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        /// <summary>A burst of sparks: <paramref name="count"/> of them in <paramref name="color"/>, flying out from <paramref name="position"/>.</summary>
        public void Burst(Vector3 position, Color color, int count = 40, float speed = 3f, float size = 0.18f, float gravity = 0.35f, float radius = 0.25f)
        {
            Emit(bursts, glow != null ? glow : particle, "Burst", position, color, count, speed, size, gravity, radius, false);
        }

        /// <summary>
        /// A puff of smoke or dust: soft blended flakes that grow as they fade, so dark colors show (an additive burst
        /// cannot darken what is behind it).
        /// </summary>
        public void Smoke(Vector3 position, Color color, int count = 30, float speed = 1.5f, float size = 0.6f, float gravity = -0.08f, float radius = 0.4f)
        {
            Emit(smokes, particle != null ? particle : glow, "Smoke", position, color, count, speed, size, gravity, radius, true);
        }

        private void Emit(Queue<ParticleSystem> pool, Material material, string name, Vector3 position, Color color, int count,
            float speed, float size, float gravity, float radius, bool grow)
        {
            ParticleSystem system = pool.Count > 0 && !pool.Peek().IsAlive(true) ? pool.Dequeue() : MakeBurst(material, name);
            system.transform.position = position;
            ParticleSystem.MainModule main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, grow ? 0.15f : 0.4f));
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startLifetime = grow ? new ParticleSystem.MinMaxCurve(0.8f, 1.6f) : new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.gravityModifier = gravity;
            ParticleSystem.SizeOverLifetimeModule sizes = system.sizeOverLifetime;
            sizes.size = new ParticleSystem.MinMaxCurve(1f, grow ? AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f) : AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            ParticleSystem.ShapeModule shape = system.shape;
            shape.radius = radius;
            system.Emit(count);
            pool.Enqueue(system);
        }

        /// <summary>Sparks rising in a column (healing, buffs, a level up).</summary>
        public void Rise(Vector3 position, Color color, int count = 30)
        {
            Burst(position, color, count, 1.2f, 0.16f, -0.35f, 0.5f);
        }

        /// <summary>The flash of a blow landing: a few bright sparks.</summary>
        public void Spark(Vector3 position, Color color)
        {
            Burst(position, color, 14, 3.2f, 0.12f, 0.6f, 0.12f);
        }

        // ------------------------------------------------------------------ projectiles

        /// <summary>A shot from <paramref name="from"/> to <paramref name="to"/>; completes when it lands.</summary>
        public IEnumerator Shoot(ProjectileKind kind, Vector3 from, Vector3 to)
        {
            float distance = Vector3.Distance(from, to);
            float duration = Mathf.Clamp(distance / 18f, 0.25f, 0.8f) / Mathf.Max(0.25f, Speed);
            Transform shot;
            Color color;
            bool missile = kind == ProjectileKind.Arrow || kind == ProjectileKind.Bolt;
            switch (kind)
            {
                case ProjectileKind.Arrow:
                case ProjectileKind.Bolt:
                    color = new Color(1f, 0.95f, 0.8f);
                    GameObject prefab = kind == ProjectileKind.Bolt && bolt != null ? bolt : arrow;
                    if (prefab != null)
                    {
                        shot = Instantiate(prefab).transform;
                    }
                    else
                    {
                        shot = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
                        shot.localScale = new Vector3(0.05f, 0.35f, 0.05f);
                        Destroy(shot.GetComponent<Collider>());
                    }
                    break;
                case ProjectileKind.HolyLight:
                    color = new Color(1f, 0.92f, 0.55f);
                    shot = Orb(color, 0.9f);
                    break;
                case ProjectileKind.DeathCloud:
                    color = new Color(0.55f, 0.95f, 0.4f);
                    shot = Orb(color, 1.1f);
                    break;
                case ProjectileKind.Lightning:
                    color = new Color(0.6f, 0.8f, 1f);
                    shot = Orb(color, 0.8f);
                    break;
                case ProjectileKind.Fire:
                    color = new Color(1f, 0.55f, 0.15f);
                    shot = Orb(color, 1f);
                    break;
                default:
                    color = new Color(0.7f, 0.85f, 1f);
                    shot = Orb(color, 0.8f);
                    break;
            }
            float arc = missile ? Mathf.Min(3f, distance * 0.18f) : distance * 0.06f;
            float time = 0f;
            Vector3 previous = from;
            shot.position = from;
            while (time < duration)
            {
                time += Time.deltaTime;
                float k = Mathf.Clamp01(time / duration);
                Vector3 p = Vector3.Lerp(from, to, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * arc);
                shot.position = p;
                Vector3 direction = p - previous;
                if (!missile && view != null)
                {
                    // A ball of light is the same from every side: it faces the camera.
                    shot.rotation = view.transform.rotation;
                }
                else if (direction.sqrMagnitude > 1e-6f)
                {
                    shot.rotation = Quaternion.LookRotation(direction);
                }
                previous = p;
                if (!missile && Random.value < 0.7f)
                {
                    Burst(p, color, 2, 0.4f, 0.14f, 0f, 0.08f);
                }
                yield return null;
            }
            Destroy(shot.gameObject);
            if (!missile)
            {
                Burst(to, color, 30, 3f, 0.22f, 0.1f, 0.3f);
                if (kind == ProjectileKind.DeathCloud)
                {
                    Smoke(to, new Color(0.3f, 0.45f, 0.25f, 0.7f), 14, 0.8f, 0.7f, -0.05f, 0.3f);
                }
            }
        }

        /// <summary>
        /// A ball of light: two quads of the glow turned to the camera, a colored halo and a white hot core. (A sphere
        /// drawn with the soft dot of the glow shows as a lopsided smear.)
        /// </summary>
        private Transform Orb(Color color, float size)
        {
            var root = new GameObject("Orb").transform;
            Quad(root, color * 1.6f, size);
            Quad(root, Color.Lerp(color, Color.white, 0.7f) * 1.4f, size * 0.45f);
            if (view != null)
            {
                root.rotation = view.transform.rotation;
            }
            return root;
        }

        private void Quad(Transform parent, Color color, float size)
        {
            var go = new GameObject("Glow", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * size;
            go.GetComponent<MeshFilter>().sharedMesh = QuadMesh();
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = glow != null ? glow : particle;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(block);
        }

        private static Mesh QuadMesh()
        {
            if (quad != null)
            {
                return quad;
            }
            quad = new Mesh { name = "Effect Quad" };
            quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            quad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            quad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            // Wound to face a camera looking along the quad's forward, as the orbs are turned.
            quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quad.RecalculateBounds();
            return quad;
        }

        // ------------------------------------------------------------------ spells

        /// <summary>One jagged line of a bolt of lightning, <paramref name="width"/> across.</summary>
        private LineRenderer Bolt(Transform parent, float width, Color start, Color end)
        {
            var go = new GameObject("Bolt");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = glow != null ? glow : particle;
            line.positionCount = 12;
            line.widthMultiplier = width;
            line.startColor = start;
            line.endColor = end;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        public IEnumerator Lightning(Vector3 target)
        {
            var go = new GameObject("Lightning");
            // A wide blue glow with a white hot core down the middle, and a flash that lights the ground around.
            LineRenderer line = Bolt(go.transform, 0.55f, new Color(0.55f, 0.72f, 1f), new Color(0.4f, 0.6f, 1f));
            LineRenderer core = Bolt(go.transform, 0.16f, Color.white, new Color(0.85f, 0.92f, 1f));
            var lightGo = new GameObject("Bolt Light");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.position = target + Vector3.up * 3f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.7f, 0.82f, 1f);
            light.range = 14f;
            light.intensity = 0f;
            Vector3 top = target + new Vector3(Random.Range(-2f, 2f), 18f, Random.Range(-2f, 2f));
            for (int flash = 0; flash < 3; flash++)
            {
                for (int i = 0; i < line.positionCount; i++)
                {
                    float k = i / (float)(line.positionCount - 1);
                    Vector3 p = Vector3.Lerp(top, target, k);
                    if (i > 0 && i < line.positionCount - 1)
                    {
                        p += new Vector3(Random.Range(-0.7f, 0.7f), 0f, Random.Range(-0.7f, 0.7f));
                    }
                    line.SetPosition(i, p);
                    core.SetPosition(i, p);
                }
                line.enabled = core.enabled = true;
                light.intensity = 9f;
                yield return new WaitForSeconds(0.07f);
                line.enabled = core.enabled = flash == 2;
                light.intensity = flash == 2 ? 5f : 0.5f;
                yield return new WaitForSeconds(0.04f);
            }
            Burst(target, new Color(0.7f, 0.85f, 1f), 50, 5f, 0.2f, 0.2f, 0.2f);
            Smoke(target, new Color(0.35f, 0.38f, 0.45f, 0.6f), 12, 1f, 0.6f, -0.06f, 0.3f);
            yield return new WaitForSeconds(0.15f);
            Destroy(go);
        }

        public IEnumerator Explosion(Vector3 center, float radius, Color color)
        {
            Burst(center + Vector3.up * 0.5f, color, 120, 7f * radius, 0.35f, 0.15f, radius * 0.6f);
            // Soot that darkens the air where the fire was; an additive burst could not show it.
            Smoke(center + Vector3.up * 0.4f, new Color(0.2f, 0.17f, 0.15f, 0.75f), 36, 1.6f, radius * 0.9f, -0.12f, radius * 0.8f);
            var lightGo = new GameObject("Blast Light");
            lightGo.transform.position = center + Vector3.up * 2f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = radius * 6f;
            float time = 0f;
            while (time < 0.5f)
            {
                time += Time.deltaTime;
                light.intensity = Mathf.Lerp(6f, 0f, time / 0.5f);
                yield return null;
            }
            Destroy(lightGo);
        }

        public IEnumerator Meteors(Vector3 center, float radius)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 target = center + new Vector3(Random.Range(-radius, radius), 0f, Random.Range(-radius, radius));
                StartCoroutine(Shoot(ProjectileKind.Fire, target + new Vector3(-6f, 16f, 4f), target));
                yield return new WaitForSeconds(0.12f);
            }
            yield return new WaitForSeconds(0.45f);
            yield return Explosion(center, radius, new Color(1f, 0.5f, 0.15f));
        }

        /// <summary>A troop crushed from within: dark sparks drawn in, then a burst of violet.</summary>
        public IEnumerator Implosion(Vector3 center, float radius)
        {
            Smoke(center, new Color(0.25f, 0.1f, 0.3f, 0.7f), 30, 0.6f, radius * 0.8f, 0f, radius);
            yield return new WaitForSeconds(0.25f);
            Burst(center, new Color(0.75f, 0.35f, 1f), 90, 5f, 0.25f, 0.1f, 0.2f);
            yield return new WaitForSeconds(0.15f);
        }

        public void Aura(Vector3 position, Color color)
        {
            Rise(position, color, 40);
            Burst(position + Vector3.up * 1.2f, color, 20, 1.5f, 0.12f, 0f, 0.6f);
        }
    }
}
