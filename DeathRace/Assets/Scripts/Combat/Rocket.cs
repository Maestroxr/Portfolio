using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A rocket fired forward from a car's roof. It leaves at the car's speed and more, burns up to its top speed, and for
    /// its first seconds homes onto the nearest car in a narrow cone ahead (turning only so fast, so a sharp swerve still
    /// shakes it). It blows up on the first car or barrier it touches, or in the air when its fuel is out: a blast that
    /// hurts and throws the cars around it. A trail of smoke and the flame of its motor follow it; the kill is the
    /// shooter's.
    /// </summary>
    public class Rocket : MonoBehaviour
    {
        private const float Radius = 0.3f;
        private const float FlightHeight = 0.8f;

        /// <summary>Seconds the rocket cannot hit the car that fired it (it leaves from inside its outline).</summary>
        private const float OwnerGrace = 0.6f;

        private static readonly List<Rocket> active = new List<Rocket>();
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        private Race race;
        private Vector3 direction;
        private float speed;
        private float retarget;
        private bool exploded;
        private TrailRenderer trail;
        private RaceEffect flame;

        /// <summary>The rockets in the air now (the computer drivers swerve from them).</summary>
        public static IReadOnlyList<Rocket> Active => active;

        public Car Owner { get; private set; }
        public Car Target { get; private set; }
        public float Age { get; private set; }
        public Vector3 Position => transform.position;
        public Vector3 Velocity => direction * speed;

        /// <summary>A rocket blew up (online: the room hears of it).</summary>
        public static event System.Action<Rocket> Exploded;

        /// <summary>Fires a rocket of <paramref name="owner"/> from <paramref name="position"/> along <paramref name="forward"/> at <paramref name="launchSpeed"/>.</summary>
        public static Rocket Launch(Race race, DeathRaceContent content, Vector3 position, Vector3 forward, float launchSpeed, Car owner)
        {
            GameObject go;
            if (content != null && content.rocket != null)
            {
                go = Instantiate(content.rocket);
                go.name = "Rocket";
            }
            else
            {
                go = Fallback(race != null && race.Effects != null ? race.Effects.Theme : null);
            }
            go.transform.SetParent(race != null ? race.Root : null, false);
            foreach (Collider collider in go.GetComponentsInChildren<Collider>(true))
            {
                DestroyImmediate(collider);
            }
            Vector3 flat = new Vector3(forward.x, 0f, forward.z);
            flat = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
            go.transform.SetPositionAndRotation(new Vector3(position.x, FlightHeight, position.z), Quaternion.LookRotation(flat, Vector3.up));
            Rocket rocket = go.AddComponent<Rocket>();
            rocket.race = race;
            rocket.Owner = owner;
            rocket.direction = flat;
            rocket.speed = Mathf.Max(CombatRules.RocketLaunchSpeed, launchSpeed);
            rocket.Dress();
            active.Add(rocket);
            return rocket;
        }

        private void Dress()
        {
            var smoke = new GameObject("Smoke Trail");
            smoke.transform.SetParent(transform, false);
            smoke.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            trail = smoke.AddComponent<TrailRenderer>();
            trail.time = 0.9f;
            trail.minVertexDistance = 0.4f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.25f, 0.9f), new Keyframe(1f, 1.9f));
            // A puff every metre and a half or so, overlapping into one ribbon of smoke.
            trail.textureMode = LineTextureMode.Tile;
            trail.textureScale = new Vector2(0.6f, 1f);
            trail.numCapVertices = 2;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sharedMaterial = CombatArt.SmokeTrail;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.85f, 0.6f), 0f), new GradientColorKey(new Color(0.75f, 0.73f, 0.7f), 0.15f), new GradientColorKey(new Color(0.5f, 0.49f, 0.47f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.55f, 0.3f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            RaceEffects effects = race != null ? race.Effects : null;
            if (effects != null)
            {
                flame = effects.Loop(EffectKind.Fire, transform, new Vector3(0f, 0f, -0.7f), Quaternion.identity, 0.55f);
            }
        }

        private void FixedUpdate()
        {
            if (exploded)
            {
                return;
            }
            float dt = Time.fixedDeltaTime;
            Age += dt;
            speed = Mathf.Min(CombatRules.RocketTopSpeed, speed + CombatRules.RocketAcceleration * dt);
            if (Age < CombatRules.RocketHomingTime)
            {
                Home(dt);
            }
            float step = speed * dt;
            Vector3 position = transform.position;
            if (Cast(position, step, out RaycastHit hit))
            {
                transform.position = hit.point;
                Explode(hit.point);
                return;
            }
            transform.SetPositionAndRotation(position + direction * step, Quaternion.LookRotation(direction, Vector3.up));
            if (Age >= CombatRules.RocketLifetime)
            {
                Explode(transform.position);
            }
        }

        /// <summary>Turns toward the nearest car in the cone ahead, picking again now and then (the target may wreck or slip out).</summary>
        private void Home(float dt)
        {
            retarget -= dt;
            if (Target == null || Target.IsWrecked || retarget <= 0f)
            {
                retarget = 0.15f;
                Target = Targeting.NearestInCone(Targeting.CarsOf(race), Owner, transform.position, direction, CombatRules.RocketCone, CombatRules.RocketSeekRange);
            }
            if (Target == null)
            {
                return;
            }
            Vector3 aim = Targeting.Lead(transform.position, Target, speed) - transform.position;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f)
            {
                return;
            }
            Vector3 turned = Vector3.RotateTowards(direction, aim.normalized, CombatRules.RocketTurnRate * Mathf.Deg2Rad * dt, 0f);
            turned.y = 0f;
            direction = turned.normalized;
        }

        /// <summary>The first car or barrier on the way this step (the shooter's own car only after a moment).</summary>
        private bool Cast(Vector3 from, float distance, out RaycastHit found)
        {
            found = default;
            int count = Physics.SphereCastNonAlloc(from, Radius, direction, Hits, distance, DeathRaceLayers.ShotMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool any = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (hit.collider == null)
                {
                    continue;
                }
                Car car = hit.collider.GetComponentInParent<Car>();
                if (car != null && car == Owner && Age < OwnerGrace)
                {
                    continue;
                }
                // A cast that starts touching something reports distance 0 and no point: take where the rocket is.
                if (hit.distance <= 0f)
                {
                    hit.point = from;
                }
                if (hit.distance < best)
                {
                    best = hit.distance;
                    found = hit;
                    any = true;
                }
            }
            return any;
        }

        /// <summary>Blows the rocket up at <paramref name="point"/>.</summary>
        public void Explode(Vector3 point)
        {
            if (exploded)
            {
                return;
            }
            exploded = true;
            RaceEffects effects = race != null ? race.Effects : null;
            effects?.Explosion(point, 1.3f);
            effects?.Scorch(point, 0.85f);
            effects?.Shake(point, 0.5f);
            (race != null && race.Audio != null ? race.Audio : RaceAudio.Current)?.Explosion(point, false);
            Exploded?.Invoke(this);
            // Every device blows its copy up; each car's own device judges what the blast does to it (CarHealth.Authoritative).
            Blast.Explode(race, point, CombatRules.RocketBlastRadius, CombatRules.RocketDamage, CombatRules.RocketPush, DamageSource.Rocket, Owner);
            Leave();
        }

        /// <summary>The smoke stays and fades where it was; the rocket is gone.</summary>
        private void Leave()
        {
            if (flame != null)
            {
                flame.Stop();
                flame.transform.SetParent(race != null && race.Effects != null ? race.Effects.transform : null, true);
                flame = null;
            }
            if (trail != null)
            {
                trail.transform.SetParent(transform.parent, true);
                trail.emitting = false;
                Destroy(trail.gameObject, trail.time + 0.2f);
                trail = null;
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            active.Remove(this);
        }

        /// <summary>A stand-in until the builders made the rocket: a grey body with a red nose and a glowing nozzle.</summary>
        private static GameObject Fallback(DeathRaceTheme theme)
        {
            var root = new GameObject("Rocket");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
            CombatArt.Paint(body, new Color(0.78f, 0.78f, 0.74f, 1f), false, theme);
            var nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nose.name = "Nose";
            nose.transform.SetParent(root.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            nose.transform.localScale = new Vector3(0.3f, 0.3f, 0.45f);
            CombatArt.Paint(nose, new Color(0.85f, 0.15f, 0.1f, 1f), false, theme);
            var nozzle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nozzle.name = "Flame";
            nozzle.transform.SetParent(root.transform, false);
            nozzle.transform.localPosition = new Vector3(0f, 0f, -0.62f);
            nozzle.transform.localScale = new Vector3(0.34f, 0.34f, 0.5f);
            CombatArt.Paint(nozzle, new Color(3f, 1.8f, 0.6f, 1f), true, theme);
            return root;
        }
    }
}
