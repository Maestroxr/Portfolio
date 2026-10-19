using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// An electric pulse round a car: a blue ring races out over the road, and every other car within
    /// <see cref="CombatRules.EmpRadius"/> takes a jolt (<see cref="CombatRules.EmpDamage"/>) and is jammed for
    /// <see cref="CombatRules.EmpJamSeconds"/> (<see cref="CarStatus.Jam"/>: no gun, mines, rockets, specials or nitro, a
    /// burning charge put out, the top speed cut to <see cref="CombatRules.EmpTopSpeed"/>, sparks crawling over it). An
    /// arc of lightning jumps to each car it caught. The jolt's kill is the car that fired it.
    ///
    /// Each device judges only the cars it drives (<see cref="CarHealth.Authoritative"/>): a pulse shown for the room
    /// online (<see cref="Authoritative"/> false) jams and jolts the cars of this device and only shows itself to the
    /// others. <see cref="Fire"/> plays the ring, the flash and the zap.
    /// </summary>
    public class EmpBlast : MonoBehaviour
    {
        /// <summary>How long the arcs to the cars caught flicker, and how long the blast object stays.</summary>
        private const float ArcSeconds = 0.35f;

        private const float LifeSeconds = 1f;

        private static readonly List<EmpBlast> active = new List<EmpBlast>();

        private readonly List<Car> caught = new List<Car>();
        private readonly List<LineRenderer> arcs = new List<LineRenderer>();
        private Race race;
        private System.Random random;
        private float age;
        private float flicker;

        /// <summary>The pulses going off now (development checks).</summary>
        public static IReadOnlyList<EmpBlast> Active => active;

        public Car Owner { get; private set; }
        public Vector3 Position => transform.position;

        /// <summary>The cars the pulse caught (jammed here, or shown so for a car of another device).</summary>
        public IReadOnlyList<Car> Caught => caught;

        /// <summary>Online: every device shows the pulse; each jams only the cars it drives.</summary>
        public bool Authoritative { get; set; } = true;

        /// <summary>Sets off a pulse of <paramref name="owner"/> at <paramref name="position"/>.</summary>
        public static EmpBlast Fire(Race race, DeathRaceContent content, Vector3 position, Car owner, bool authoritative = true)
        {
            var go = new GameObject("EMP Blast");
            go.transform.SetParent(race != null ? race.Root : null, false);
            go.transform.position = new Vector3(position.x, 0f, position.z);
            EmpBlast blast = go.AddComponent<EmpBlast>();
            blast.race = race;
            blast.Owner = owner;
            blast.Authoritative = authoritative;
            blast.random = new System.Random(unchecked((int)(position.x * 131f) ^ (int)(position.z * 977f)));
            active.Add(blast);
            RaceEffects effects = race != null ? race.Effects : null;
            effects?.EmpBlast(position, CombatRules.EmpRadius);
            effects?.Shake(position, 0.3f);
            (race != null && race.Audio != null ? race.Audio : RaceAudio.Current)?.Emp(position);
            blast.Strike();
            return blast;
        }

        /// <summary>Every other running car in reach is jolted and jammed (the ones judged here), and an arc jumps to it.</summary>
        private void Strike()
        {
            Vector2 centre = new Vector2(transform.position.x, transform.position.z);
            float radius = CombatRules.EmpRadius;
            foreach (Car car in Targeting.CarsOf(race))
            {
                if (car == null || car == Owner || car.IsWrecked || car.HasFinished || (race != null && car.Race != null && car.Race != race))
                {
                    continue;
                }
                Vector2 to = car.PhysicsPosition2D - centre;
                if (to.sqrMagnitude > radius * radius)
                {
                    continue;
                }
                caught.Add(car);
                if (car.Health != null ? car.Health.Authoritative : Authoritative)
                {
                    car.Status?.Jam(Owner, CombatRules.EmpJamSeconds);
                    Vector3 direction = new Vector3(to.x, 0f, to.y);
                    car.Health?.TakeDamage(new DamageInfo(CombatRules.EmpDamage, DamageSource.Emp, Owner, car.transform.position + Vector3.up,
                        direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward));
                }
                arcs.Add(Arc());
            }
            DrawArcs();
        }

        private void Update()
        {
            age += Time.deltaTime;
            flicker -= Time.deltaTime;
            if (age >= LifeSeconds)
            {
                active.Remove(this);
                CombatArt.Discard(gameObject);
                return;
            }
            if (flicker <= 0f)
            {
                flicker = 0.05f;
                DrawArcs();
            }
        }

        /// <summary>A jagged line from the blast to each car caught, new zigzags every flicker, fading out.</summary>
        private void DrawArcs()
        {
            float fade = Mathf.Clamp01(1f - age / ArcSeconds);
            Vector3 from = transform.position + Vector3.up * 1.2f;
            for (int i = 0; i < arcs.Count; i++)
            {
                LineRenderer arc = arcs[i];
                Car car = i < caught.Count ? caught[i] : null;
                if (arc == null)
                {
                    continue;
                }
                arc.enabled = fade > 0f && car != null;
                if (!arc.enabled)
                {
                    continue;
                }
                Vector3 to = car.transform.position + Vector3.up * 1f;
                Vector3 across = Vector3.Cross(to - from, Vector3.up).normalized;
                int points = arc.positionCount;
                for (int p = 0; p < points; p++)
                {
                    float t = p / (points - 1f);
                    float jag = p == 0 || p == points - 1 ? 0f : ((float)random.NextDouble() * 2f - 1f) * 1.1f;
                    arc.SetPosition(p, Vector3.Lerp(from, to, t) + across * jag + Vector3.up * jag * 0.3f);
                }
                Color color = Color.Lerp(EffectFallbacks.EmpColor, Color.white, 0.5f) * 2.5f;
                color.a = fade;
                arc.startColor = color;
                arc.endColor = color;
            }
        }

        private LineRenderer Arc()
        {
            var go = new GameObject("Arc");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 8;
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.widthMultiplier = 0.28f;
            line.numCapVertices = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = CombatArt.Tracer;
            return line;
        }

        private void OnDestroy()
        {
            active.Remove(this);
        }
    }
}
