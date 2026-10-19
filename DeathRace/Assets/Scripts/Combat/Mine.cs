using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A mine dropped behind a car. It lies still for a moment, then arms with a beep: its light blinks and a red ring on
    /// the road shows how near is too near. Any car that comes within the ring once it is armed (the one that dropped it
    /// too) sets it off: a blast that hurts and throws every car around it, a flash, a scorch mark. The kill is the
    /// dropper's. Only so many lie on a track at once; the oldest fizzles when another is dropped.
    /// </summary>
    public class Mine : MonoBehaviour
    {
        private static readonly List<Mine> active = new List<Mine>();
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color ArmedColor = new Color(1f, 0.12f, 0.08f, 1f);

        private Race race;
        private Renderer[] beacons = new Renderer[0];
        private Renderer ring;
        private Light blinkLight;
        private MaterialPropertyBlock block;
        private bool exploded;
        private float blinkTimer;
        private bool blinkOn;

        /// <summary>The mines lying on the track now (the computer drivers steer round them).</summary>
        public static IReadOnlyList<Mine> Active => active;

        public Car Owner { get; private set; }
        public bool Armed { get; private set; }
        public float Age { get; private set; }
        public Vector3 Position => transform.position;

        /// <summary>The number of the mine in its race (online: the action that dropped it).</summary>
        public int Id { get; private set; }

        /// <summary>
        /// Online: a mine everyone shows but only the device of the car it hits judges; when false, touching it sets it off
        /// only through <see cref="Detonate"/> from outside.
        /// </summary>
        public bool Authoritative { get; set; } = true;

        /// <summary>A mine went off (online: the room hears of it).</summary>
        public static event System.Action<Mine> Detonated;

        /// <summary>Drops a mine of <paramref name="owner"/> at <paramref name="position"/> on the ground of <paramref name="race"/>.</summary>
        public static Mine Spawn(Race race, DeathRaceContent content, Vector3 position, Quaternion rotation, Car owner, int id = 0)
        {
            Prune(race);
            GameObject go;
            if (content != null && content.mine != null)
            {
                go = Instantiate(content.mine);
                go.name = "Mine";
            }
            else
            {
                go = Fallback(race != null && race.Effects != null ? race.Effects.Theme : null);
            }
            Transform parent = race != null ? race.Root : null;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(new Vector3(position.x, 0f, position.z), Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            foreach (Collider collider in go.GetComponentsInChildren<Collider>(true))
            {
                DestroyImmediate(collider);
            }
            SetLayer(go, DeathRaceLayers.Hazard);
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = CombatRules.MineTriggerRadius;
            trigger.center = new Vector3(0f, 0.6f, 0f);
            Mine mine = go.AddComponent<Mine>();
            mine.race = race;
            mine.Owner = owner;
            mine.Id = id;
            mine.Build(race != null && race.Effects != null && race.Effects.Night);
            active.Add(mine);
            return mine;
        }

        /// <summary>Too many on the track: the oldest of the race fizzles out.</summary>
        private static void Prune(Race race)
        {
            int count = 0;
            Mine oldest = null;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Mine mine = active[i];
                if (mine == null)
                {
                    active.RemoveAt(i);
                    continue;
                }
                if (mine.race != race)
                {
                    continue;
                }
                count++;
                if (oldest == null || mine.Age > oldest.Age)
                {
                    oldest = mine;
                }
            }
            if (count >= CombatRules.MaxMines && oldest != null)
            {
                oldest.Fizzle();
            }
        }

        private void Build(bool night)
        {
            var list = new List<Renderer>();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name.IndexOf("Beacon", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    list.Add(renderer);
                }
            }
            beacons = list.ToArray();
            ring = DangerRing(transform);
            if (night)
            {
                blinkLight = new GameObject("Blink").AddComponent<Light>();
                blinkLight.transform.SetParent(transform, false);
                blinkLight.transform.localPosition = new Vector3(0f, 1f, 0f);
                blinkLight.type = LightType.Point;
                blinkLight.color = ArmedColor;
                blinkLight.range = 4f;
                blinkLight.intensity = 0f;
                blinkLight.shadows = LightShadows.None;
            }
            SetGlow(0.25f, 0f);
        }

        private void Update()
        {
            Age += Time.deltaTime;
            if (!Armed)
            {
                if (Age >= CombatRules.MineArmTime)
                {
                    Armed = true;
                    blinkTimer = 0f;
                    Sounds?.MineArm(transform.position);
                }
                else
                {
                    SetGlow(0.25f, 0f);
                    return;
                }
            }
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                blinkOn = !blinkOn;
                blinkTimer = blinkOn ? 0.12f : 0.55f;
            }
            float pulse = 0.55f + 0.45f * Mathf.Sin(Age * 6f);
            SetGlow(blinkOn ? 1f : 0.15f, pulse);
        }

        private RaceAudio Sounds => race != null && race.Audio != null ? race.Audio : RaceAudio.Current;

        private void OnTriggerStay(Collider other)
        {
            if (!Armed || exploded || !Authoritative)
            {
                return;
            }
            Car car = other.GetComponentInParent<Car>();
            if (car != null && !car.IsWrecked && (race == null || car.Race == null || car.Race == race))
            {
                Detonate();
            }
        }

        /// <summary>Blows the mine up now.</summary>
        public void Detonate()
        {
            if (exploded)
            {
                return;
            }
            exploded = true;
            Vector3 at = transform.position;
            RaceEffects effects = race != null ? race.Effects : null;
            effects?.Explosion(at + Vector3.up * 0.4f, 1.25f);
            effects?.Scorch(at, 1f);
            effects?.Shake(at, 0.45f);
            Sounds?.Explosion(at, false);
            Detonated?.Invoke(this);
            Blast.Explode(race, at, CombatRules.MineBlastRadius, CombatRules.MineDamage, CombatRules.MinePush, DamageSource.Mine, Owner);
            Destroy(gameObject);
        }

        /// <summary>Goes away quietly (too many on the track, or the race was torn down).</summary>
        public void Fizzle()
        {
            if (exploded)
            {
                return;
            }
            exploded = true;
            race?.Effects?.Dust(transform.position, 0.6f);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            active.Remove(this);
        }

        private void SetGlow(float beacon, float ringAlpha)
        {
            block ??= new MaterialPropertyBlock();
            Color glow = ArmedColor * (0.4f + 3.2f * beacon);
            glow.a = 1f;
            foreach (Renderer renderer in beacons)
            {
                if (renderer == null)
                {
                    continue;
                }
                renderer.GetPropertyBlock(block);
                block.SetColor(ColorId, glow);
                block.SetColor(BaseColorId, glow);
                block.SetColor(EmissionId, glow);
                renderer.SetPropertyBlock(block);
            }
            if (ring != null)
            {
                ring.enabled = ringAlpha > 0f;
                Color tint = ArmedColor * 2.2f;
                tint.a = 0.5f + 0.4f * ringAlpha;
                ring.GetPropertyBlock(block);
                block.SetColor(ColorId, tint);
                ring.SetPropertyBlock(block);
            }
            if (blinkLight != null)
            {
                blinkLight.intensity = Armed ? (blinkOn ? 6f : 0.4f) : 0f;
            }
        }

        /// <summary>The red ring on the road the size of the trigger: what not to drive into, readable from far above.</summary>
        private static Renderer DangerRing(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Danger Ring";
            DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, RaceEffects.GroundMarkHeight + 0.03f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float size = CombatRules.MineTriggerRadius * 2.3f;
            go.transform.localScale = new Vector3(size, size, 1f);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CombatArt.Ring;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return renderer;
        }

        /// <summary>A stand-in until the builders made the mine: a dark disc with a stud and a light on top.</summary>
        private static GameObject Fallback(DeathRaceTheme theme)
        {
            var root = new GameObject("Mine");
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Body";
            disc.transform.SetParent(root.transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            disc.transform.localScale = new Vector3(1.2f, 0.12f, 1.2f);
            CombatArt.Paint(disc, new Color(0.16f, 0.16f, 0.15f, 1f), false, theme);
            var stud = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stud.name = "Stud";
            stud.transform.SetParent(root.transform, false);
            stud.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            stud.transform.localScale = new Vector3(0.7f, 0.06f, 0.7f);
            CombatArt.Paint(stud, new Color(0.55f, 0.5f, 0.2f, 1f), false, theme);
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "Beacon";
            beacon.transform.SetParent(root.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            beacon.transform.localScale = new Vector3(0.36f, 0.22f, 0.36f);
            CombatArt.Paint(beacon, ArmedColor, true, theme);
            return root;
        }

        internal static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayer(child.gameObject, layer);
            }
        }
    }
}
