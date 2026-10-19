using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A bolt of the plasma cannon: a glowing round that flies straight from the muzzle at
    /// <see cref="CombatRules.PlasmaSpeed"/> (on top of the car's own speed), so a target far away can still be missed by
    /// a swerve and a shooter has to lead it. The first car or barrier in its way stops it: the car it hits takes the
    /// bolt's whole damage, and the bolt bursts there (<see cref="CombatRules.PlasmaSplash"/>) with a share of it for the
    /// cars round it (<see cref="Blast"/>: blast shielding helps against that). Past its range it fizzles. A violet trail
    /// follows it. As with the guns, the device of the car hit judges the hit (<see cref="CarHealth.Authoritative"/>),
    /// so a puppet of another device fires real bolts that hurt only the cars driven here.
    /// </summary>
    public class PlasmaBolt : MonoBehaviour
    {
        private const float Radius = 0.25f;
        private const float FlightHeight = 0.75f;

        private static readonly List<PlasmaBolt> active = new List<PlasmaBolt>();
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        private Race race;
        private Vector3 direction;
        private float speed;
        private float damage;
        private float range;
        private float travelled;
        private bool spent;
        private TrailRenderer trail;
        private Action<Car> hit;

        /// <summary>The bolts in the air now (the computer drivers could swerve from them; development checks).</summary>
        public static IReadOnlyList<PlasmaBolt> Active => active;

        public Car Owner { get; private set; }
        public float Age { get; private set; }
        public Vector3 Position => transform.position;
        public Vector3 Velocity => direction * speed;

        /// <summary>
        /// Fires a bolt of <paramref name="owner"/> from <paramref name="position"/> along <paramref name="forward"/> at
        /// <paramref name="launchSpeed"/>, dealing <paramref name="boltDamage"/> (before armour) to the car it hits, flying
        /// <paramref name="reach"/> metres at most; <paramref name="onHit"/> hears of the car hit (the shooter's count).
        /// </summary>
        public static PlasmaBolt Launch(Race race, DeathRaceContent content, Vector3 position, Vector3 forward, float launchSpeed, Car owner,
            float boltDamage, float reach = CombatRules.PlasmaRange, Action<Car> onHit = null)
        {
            GameObject go;
            if (content != null && content.plasmaBolt != null)
            {
                go = Instantiate(content.plasmaBolt);
                go.name = "Plasma Bolt";
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
            Vector3 flat = new Vector3(forward.x, 0f, forward.z);
            flat = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
            go.transform.SetPositionAndRotation(new Vector3(position.x, FlightHeight, position.z), Quaternion.LookRotation(flat, Vector3.up));
            PlasmaBolt bolt = go.AddComponent<PlasmaBolt>();
            bolt.race = race;
            bolt.Owner = owner;
            bolt.direction = flat;
            bolt.speed = Mathf.Max(10f, launchSpeed);
            bolt.damage = Mathf.Max(0f, boltDamage);
            bolt.range = Mathf.Max(1f, reach);
            bolt.hit = onHit;
            bolt.Dress();
            active.Add(bolt);
            return bolt;
        }

        private void Dress()
        {
            var streak = new GameObject("Trail");
            streak.transform.SetParent(transform, false);
            trail = streak.AddComponent<TrailRenderer>();
            trail.time = 0.16f;
            trail.minVertexDistance = 0.3f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.9f), new Keyframe(1f, 0.08f));
            trail.numCapVertices = 2;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sharedMaterial = CombatArt.Glow;
            Color color = EffectFallbacks.PlasmaColor;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.6f), 0f), new GradientColorKey(color, 0.4f), new GradientColorKey(color * 0.6f, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
        }

        private void FixedUpdate()
        {
            Step(Time.fixedDeltaTime);
        }

        /// <summary>One step of the flight (public for the tests, which step it by hand).</summary>
        public void Step(float deltaTime)
        {
            if (spent || deltaTime <= 0f)
            {
                return;
            }
            Age += deltaTime;
            float step = Mathf.Min(speed * deltaTime, range - travelled);
            Vector3 position = transform.position;
            if (Cast(position, step, out RaycastHit found))
            {
                Vector3 point = found.distance > 0f ? found.point : position;
                transform.position = new Vector3(point.x, position.y, point.z);
                Car victim = found.collider != null ? found.collider.GetComponentInParent<Car>() : null;
                Burst(transform.position, victim);
                return;
            }
            travelled += step;
            transform.position = position + direction * step;
            if (travelled >= range - 0.01f)
            {
                Fizzle();
            }
        }

        /// <summary>The first car or barrier on the way this step (the shooter's own car only after a moment).</summary>
        private bool Cast(Vector3 from, float distance, out RaycastHit found)
        {
            found = default;
            if (distance <= 0f)
            {
                return false;
            }
            int count = Physics.SphereCastNonAlloc(from, Radius, direction, Hits, distance, DeathRaceLayers.ShotMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool any = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = Hits[i];
                if (candidate.collider == null)
                {
                    continue;
                }
                // A bolt flies straight on: it never comes back to the car that fired it.
                Car car = candidate.collider.GetComponentInParent<Car>();
                if (car != null && car == Owner)
                {
                    continue;
                }
                if (candidate.distance < best)
                {
                    best = candidate.distance;
                    found = candidate;
                    any = true;
                }
            }
            return any;
        }

        /// <summary>The bolt stops at <paramref name="point"/>: the car hit takes it whole, the burst a share round it.</summary>
        private void Burst(Vector3 point, Car victim)
        {
            spent = true;
            if (victim != null && !victim.IsWrecked)
            {
                hit?.Invoke(victim);
                // Whether it hurts is the victim's to judge (online, only the device that drives a car hurts it).
                victim.Health?.TakeDamage(new DamageInfo(damage, DamageSource.Plasma, Owner, point, direction));
            }
            RaceEffects effects = race != null ? race.Effects : null;
            effects?.PlasmaBurst(point, 1f);
            (race != null && race.Audio != null ? race.Audio : RaceAudio.Current)?.PlasmaBurst(point);
            Blast.Explode(race, point, CombatRules.PlasmaSplash, damage * CombatRules.PlasmaSplashShare, CombatRules.PlasmaPush, DamageSource.Plasma,
                Owner, victim);
            Leave();
        }

        /// <summary>Out of range: a small pop of violet, and it is gone.</summary>
        private void Fizzle()
        {
            spent = true;
            (race != null ? race.Effects : null)?.PlasmaBurst(transform.position, 0.45f);
            Leave();
        }

        /// <summary>The trail stays and fades where it was; the bolt is gone.</summary>
        private void Leave()
        {
            if (trail != null)
            {
                trail.transform.SetParent(transform.parent, true);
                trail.emitting = false;
                CombatArt.Discard(trail.gameObject, trail.time + 0.1f);
                trail = null;
            }
            active.Remove(this);
            CombatArt.Discard(gameObject);
        }

        private void OnDestroy()
        {
            active.Remove(this);
        }

        /// <summary>A stand-in until the builders made the bolt: a stretched glowing ball.</summary>
        private static GameObject Fallback()
        {
            var root = new GameObject("Plasma Bolt");
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(root.transform, false);
            core.transform.localScale = new Vector3(0.4f, 0.4f, 0.9f);
            CombatArt.Paint(core, Color.Lerp(EffectFallbacks.PlasmaColor, Color.white, 0.5f) * 3f, true, null);
            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halo.name = "Halo";
            halo.transform.SetParent(root.transform, false);
            halo.transform.localScale = new Vector3(0.75f, 0.75f, 1.4f);
            Color glow = EffectFallbacks.PlasmaColor * 2f;
            glow.a = 0.45f;
            CombatArt.Paint(halo, glow, true, null);
            return root;
        }
    }
}
