using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The small spectacles of the game: numbers that float up from a wound, arrows and bolts in flight, bursts of
    /// sparks, flames and light for spells and treasure, all built in code from a few particle materials.
    /// </summary>
    public sealed class Effects : MonoBehaviour
    {
        [SerializeField] private Material particle;
        [SerializeField] private Material glow;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private GameObject arrow;

        private readonly Queue<ParticleSystem> bursts = new Queue<ParticleSystem>();
        private Camera view;

        public float Speed { get; set; } = 1f;

        public void Setup(HeroesArt art, Camera camera)
        {
            view = camera;
            if (art != null)
            {
                font = art.bodyFont != null ? art.bodyFont : font;
                arrow = art.arrow != null ? art.arrow : arrow;
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
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(0, 0, 0, 220);
            tmp.rectTransform.sizeDelta = new Vector2(12f, 3f);
            tmp.sortingOrder = 50;
            StartCoroutine(FloatRoutine(go.transform, tmp, rise, duration));
        }

        private IEnumerator FloatRoutine(Transform t, TextMeshPro tmp, float rise, float duration)
        {
            Vector3 start = t.position;
            Color color = tmp.color;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float k = time / duration;
                t.position = start + Vector3.up * (rise * (1f - (1f - k) * (1f - k)));
                if (view != null)
                {
                    t.rotation = view.transform.rotation;
                }
                color.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                tmp.color = color;
                t.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.6f, 1.1f, k / 0.15f) : Mathf.Lerp(1.1f, 1f, (k - 0.15f) / 0.85f));
                yield return null;
            }
            Destroy(t.gameObject);
        }

        // ------------------------------------------------------------------ particles

        private ParticleSystem MakeBurst()
        {
            var go = new GameObject("Burst");
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
            renderer.sharedMaterial = glow != null ? glow : particle;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        /// <summary>A burst of sparks: <paramref name="count"/> of them in <paramref name="color"/>, flying out from <paramref name="position"/>.</summary>
        public void Burst(Vector3 position, Color color, int count = 40, float speed = 3f, float size = 0.18f, float gravity = 0.35f, float radius = 0.25f)
        {
            ParticleSystem system = bursts.Count > 0 && !bursts.Peek().IsAlive(true) ? bursts.Dequeue() : MakeBurst();
            system.transform.position = position;
            ParticleSystem.MainModule main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.4f));
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.gravityModifier = gravity;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.radius = radius;
            system.Emit(count);
            bursts.Enqueue(system);
        }

        /// <summary>Sparks rising in a column (healing, buffs, a level up).</summary>
        public void Rise(Vector3 position, Color color, int count = 30)
        {
            Burst(position, color, count, 1.2f, 0.16f, -0.35f, 0.5f);
        }

        // ------------------------------------------------------------------ projectiles

        /// <summary>A shot from <paramref name="from"/> to <paramref name="to"/>; completes when it lands.</summary>
        public IEnumerator Shoot(ProjectileKind kind, Vector3 from, Vector3 to)
        {
            float distance = Vector3.Distance(from, to);
            float duration = Mathf.Clamp(distance / 18f, 0.25f, 0.8f) / Mathf.Max(0.25f, Speed);
            Transform shot;
            Color color;
            switch (kind)
            {
                case ProjectileKind.Arrow:
                    color = new Color(1f, 0.95f, 0.8f);
                    shot = arrow != null ? Instantiate(arrow).transform : GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
                    if (arrow == null)
                    {
                        shot.localScale = new Vector3(0.05f, 0.35f, 0.05f);
                        Destroy(shot.GetComponent<Collider>());
                    }
                    break;
                case ProjectileKind.HolyLight:
                    color = new Color(1f, 0.92f, 0.55f);
                    shot = Orb(color, 0.35f);
                    break;
                case ProjectileKind.DeathCloud:
                    color = new Color(0.55f, 0.95f, 0.4f);
                    shot = Orb(color, 0.45f);
                    break;
                case ProjectileKind.Lightning:
                    color = new Color(0.6f, 0.8f, 1f);
                    shot = Orb(color, 0.3f);
                    break;
                case ProjectileKind.Fire:
                    color = new Color(1f, 0.55f, 0.15f);
                    shot = Orb(color, 0.4f);
                    break;
                default:
                    color = new Color(0.7f, 0.85f, 1f);
                    shot = Orb(color, 0.3f);
                    break;
            }
            float arc = kind == ProjectileKind.Arrow ? Mathf.Min(3f, distance * 0.18f) : distance * 0.06f;
            float time = 0f;
            Vector3 previous = from;
            while (time < duration)
            {
                time += Time.deltaTime;
                float k = Mathf.Clamp01(time / duration);
                Vector3 p = Vector3.Lerp(from, to, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * arc);
                shot.position = p;
                Vector3 direction = p - previous;
                if (direction.sqrMagnitude > 1e-6f)
                {
                    shot.rotation = Quaternion.LookRotation(direction);
                }
                previous = p;
                if (kind != ProjectileKind.Arrow && Random.value < 0.6f)
                {
                    Burst(p, color, 2, 0.4f, 0.12f, 0f, 0.05f);
                }
                yield return null;
            }
            Destroy(shot.gameObject);
            if (kind != ProjectileKind.Arrow)
            {
                Burst(to, color, 30, 3f, 0.22f, 0.1f, 0.3f);
            }
        }

        private Transform Orb(Color color, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * size;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            if (glow != null)
            {
                renderer.sharedMaterial = glow;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color * 2f);
                renderer.SetPropertyBlock(block);
            }
            return go.transform;
        }

        // ------------------------------------------------------------------ spells

        public IEnumerator Lightning(Vector3 target)
        {
            var go = new GameObject("Lightning");
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = glow != null ? glow : particle;
            line.positionCount = 12;
            line.widthMultiplier = 0.25f;
            line.startColor = new Color(0.85f, 0.92f, 1f);
            line.endColor = new Color(0.55f, 0.75f, 1f);
            Vector3 top = target + new Vector3(Random.Range(-2f, 2f), 18f, Random.Range(-2f, 2f));
            for (int flash = 0; flash < 3; flash++)
            {
                for (int i = 0; i < line.positionCount; i++)
                {
                    float k = i / (float)(line.positionCount - 1);
                    Vector3 p = Vector3.Lerp(top, target, k);
                    if (i > 0 && i < line.positionCount - 1)
                    {
                        p += new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.6f, 0.6f));
                    }
                    line.SetPosition(i, p);
                }
                line.enabled = true;
                yield return new WaitForSeconds(0.06f);
                line.enabled = flash == 2;
                yield return new WaitForSeconds(0.04f);
            }
            Burst(target, new Color(0.7f, 0.85f, 1f), 50, 5f, 0.2f, 0.2f, 0.2f);
            yield return new WaitForSeconds(0.15f);
            Destroy(go);
        }

        public IEnumerator Explosion(Vector3 center, float radius, Color color)
        {
            Burst(center + Vector3.up * 0.5f, color, 120, 7f * radius, 0.35f, 0.15f, radius * 0.6f);
            Burst(center + Vector3.up * 0.3f, new Color(0.25f, 0.2f, 0.18f), 50, 2f, 0.5f, -0.1f, radius);
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

        public void Aura(Vector3 position, Color color)
        {
            Rise(position, color, 40);
            Burst(position + Vector3.up * 1.2f, color, 20, 1.5f, 0.12f, 0f, 0.6f);
        }
    }
}
