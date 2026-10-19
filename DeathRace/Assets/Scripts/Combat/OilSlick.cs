using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A slick of oil poured behind a car: a dark glossy puddle that spreads out in a moment, lies on the road for
    /// <see cref="CombatRules.OilLifetime"/> seconds and fades over its last ones. A car that drives into it is slicked
    /// (<see cref="CarStatus.Slick"/>: its tyres keep <see cref="CombatRules.OilGrip"/> of their grip and its wheel
    /// <see cref="CombatRules.OilSteering"/> of its turn for <see cref="CombatRules.OilSlickSeconds"/>, and the first touch
    /// throws it into a little spin); the car that poured it drives through it unharmed for a moment. Nothing of it hurts,
    /// but a barrier it sends a car into counts as the pourer's kill. Only so many lie on a track at once; the oldest dries
    /// up when another is poured. The computer's drivers steer round the ones they see (<see cref="Active"/>).
    ///
    /// Each device judges only the cars it drives (<see cref="CarHealth.Authoritative"/>), so a slick shown for the room
    /// online (<see cref="Authoritative"/> false) still slicks the cars of this device. <see cref="Spawn"/> plays the pour.
    /// </summary>
    public class OilSlick : MonoBehaviour
    {
        /// <summary>Seconds a fresh slick takes to spread to its full size.</summary>
        private const float SpreadSeconds = 0.35f;

        /// <summary>How far into a car's outline the oil has to reach (metres beyond the slick's radius, to the car's middle).</summary>
        private const float Reach = 0.6f;

        private static readonly List<OilSlick> active = new List<OilSlick>();
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Race race;
        private Renderer[] renderers = new Renderer[0];
        private Color[] colors = new Color[0];
        private MaterialPropertyBlock block;
        private float shownAlpha = -1f;
        private bool gone;

        /// <summary>The slicks lying on the track now (the computer drivers steer round them).</summary>
        public static IReadOnlyList<OilSlick> Active => active;

        public Car Owner { get; private set; }

        /// <summary>The number of the slick in its race (online: the action that poured it).</summary>
        public int Id { get; private set; }

        public float Age { get; private set; }

        /// <summary>The race the slick lies in.</summary>
        public Race Race => race;

        /// <summary>The radius it covers now: growing as it spreads, a little less as it dries.</summary>
        public float Radius => CombatRules.OilRadius * Spread;

        public Vector3 Position => transform.position;

        /// <summary>Online: a slick everyone shows; each device judges only the cars it drives.</summary>
        public bool Authoritative { get; set; } = true;

        /// <summary>How far it has spread (0 to 1) and how much of it is left as it dries.</summary>
        private float Spread
        {
            get
            {
                float spread = Mathf.SmoothStep(0.35f, 1f, Mathf.Clamp01(Age / SpreadSeconds));
                return spread * Mathf.Lerp(0.75f, 1f, Left01);
            }
        }

        /// <summary>1 while the slick is fresh, down to 0 over its last <see cref="CombatRules.OilFade"/> seconds.</summary>
        private float Left01 => Mathf.Clamp01((CombatRules.OilLifetime - Age) / CombatRules.OilFade);

        /// <summary>Pours a slick of <paramref name="owner"/> at <paramref name="position"/> on the ground of <paramref name="race"/>.</summary>
        public static OilSlick Spawn(Race race, DeathRaceContent content, Vector3 position, Quaternion rotation, Car owner, int id = 0)
        {
            Prune(race);
            GameObject go;
            if (content != null && content.oilSlick != null)
            {
                go = Instantiate(content.oilSlick);
                go.name = "Oil Slick";
            }
            else
            {
                go = Fallback();
            }
            go.transform.SetParent(race != null ? race.Root : null, false);
            foreach (Collider collider in go.GetComponentsInChildren<Collider>(true))
            {
                DestroyImmediate(collider);
            }
            // A little above the scorch marks (they lift themselves up to 2 cm), each slick a hair above the last.
            float lift = RaceEffects.GroundMarkHeight + 0.025f + (id % 8) * 0.0015f;
            // A turn of its own, so two slicks never look the same.
            float yaw = rotation.eulerAngles.y + (id * 67.3f + position.x * 13.1f) % 360f;
            go.transform.SetPositionAndRotation(new Vector3(position.x, lift, position.z), Quaternion.Euler(0f, yaw, 0f));
            OilSlick slick = go.AddComponent<OilSlick>();
            slick.race = race;
            slick.Owner = owner;
            slick.Id = id;
            slick.Prepare();
            slick.Show();
            active.Add(slick);
            (race != null && race.Audio != null ? race.Audio : RaceAudio.Current)?.OilPour(position);
            return slick;
        }

        /// <summary>Too many on the track: the oldest of the race dries up.</summary>
        private static void Prune(Race race)
        {
            int count = 0;
            OilSlick oldest = null;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                OilSlick slick = active[i];
                if (slick == null)
                {
                    active.RemoveAt(i);
                    continue;
                }
                if (slick.race != race)
                {
                    continue;
                }
                count++;
                if (oldest == null || slick.Age > oldest.Age)
                {
                    oldest = slick;
                }
            }
            if (count >= CombatRules.MaxOilSlicks && oldest != null)
            {
                oldest.DryUp();
            }
        }

        /// <summary>The renderers of the puddle and the colours their materials have (the fade multiplies them).</summary>
        private void Prepare()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            colors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material shared = renderers[i].sharedMaterial;
                colors[i] = shared == null ? Color.white : shared.HasProperty(BaseColorId) ? shared.GetColor(BaseColorId) : shared.HasProperty(ColorId) ? shared.color : Color.white;
            }
        }

        private void FixedUpdate()
        {
            Step(Time.fixedDeltaTime);
        }

        /// <summary>One step: the slick ages, spreads and fades, and slicks the cars in it (public for the tests).</summary>
        public void Step(float deltaTime)
        {
            if (gone)
            {
                return;
            }
            Age += deltaTime;
            if (Age >= CombatRules.OilLifetime)
            {
                DryUp();
                return;
            }
            Show();
            float radius = Radius + Reach;
            Vector2 centre = new Vector2(transform.position.x, transform.position.z);
            foreach (Car car in Targeting.CarsOf(race))
            {
                if (car == null || car.IsWrecked || car.HasFinished || car.Status == null || !Judges(car))
                {
                    continue;
                }
                if (car == Owner && Age < CombatRules.OilOwnerGrace)
                {
                    continue;
                }
                if (race != null && car.Race != null && car.Race != race)
                {
                    continue;
                }
                Vector2 to = car.PhysicsPosition2D - centre;
                if (to.sqrMagnitude > radius * radius)
                {
                    continue;
                }
                car.Status.Slick(Owner, CombatRules.OilSlickSeconds, SpinFor(car));
            }
        }

        /// <summary>
        /// The spin a car is thrown into when it hits the slick: harder the faster it goes, the way it was already turning
        /// (or, going straight, the way its seat says: the same car always spins the same way on the same slick).
        /// </summary>
        private float SpinFor(Car car)
        {
            float speed = car.Body != null ? Mathf.Abs(car.Body.ForwardSpeed) : 0f;
            float turning = car.Body != null ? car.Body.YawRate : 0f;
            int seat = car.Entrant != null ? car.Entrant.Seat : 0;
            float side = Mathf.Abs(turning) > 0.05f ? Mathf.Sign(turning) : ((seat + Id) % 2 == 0 ? 1f : -1f);
            return side * CombatRules.OilSpin * Mathf.Clamp01(speed / CombatRules.OilSpinSpeed);
        }

        /// <summary>Whether this device judges what the slick does to <paramref name="car"/> (online: only the cars it drives).</summary>
        private bool Judges(Car car)
        {
            return car.Health != null ? car.Health.Authoritative : Authoritative;
        }

        /// <summary>The puddle's size and fade.</summary>
        private void Show()
        {
            float size = Radius;
            transform.localScale = new Vector3(size, 1f, size);
            float alpha = Mathf.Lerp(0.15f, 1f, Left01);
            if (Mathf.Abs(alpha - shownAlpha) < 0.01f)
            {
                return;
            }
            shownAlpha = alpha;
            block ??= new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }
                Color shown = colors[i];
                shown.a *= alpha;
                renderer.GetPropertyBlock(block);
                block.SetColor(ColorId, shown);
                block.SetColor(BaseColorId, shown);
                renderer.SetPropertyBlock(block);
            }
        }

        /// <summary>Goes away (dried up, too many on the track, or the race was torn down).</summary>
        public void DryUp()
        {
            if (gone)
            {
                return;
            }
            gone = true;
            active.Remove(this);
            CombatArt.Discard(gameObject);
        }

        private void OnDestroy()
        {
            active.Remove(this);
        }

        /// <summary>A stand-in until the builders made the puddle: a dark disc of glossy oil, 1 m in radius.</summary>
        private static GameObject Fallback()
        {
            var root = new GameObject("Oil Slick");
            var puddle = new GameObject("Puddle");
            puddle.transform.SetParent(root.transform, false);
            puddle.AddComponent<MeshFilter>().sharedMesh = CombatArt.DiscMesh;
            var renderer = puddle.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CombatArt.Oil;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return root;
        }
    }
}
