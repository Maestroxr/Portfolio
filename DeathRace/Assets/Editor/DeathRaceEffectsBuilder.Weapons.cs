using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The effects of the weapons that came with the workshop: the flamethrower's jet, a plasma bolt's burst, an EMP
    /// blast's ring, the sparks crawling over a jammed car and the welder's. As the others: roots without physics, world
    /// space, one-shots that do not loop and loops that do, authored at size 1 (the EMP's ring 1 m in radius: the race
    /// sizes it to the blast), "Tint" in the name for the systems that take the look's colour (the flame takes the
    /// explosion colour); plasma and EMP keep their own violet and blue in every look (<see cref="EffectFallbacks"/>).
    /// </summary>
    internal static partial class DeathRaceEffectsBuilder
    {
        private static Color Plasma => EffectFallbacks.PlasmaColor;

        private static Color Emp => EffectFallbacks.EmpColor;

        /// <summary>The prefabs of the weapons' effects (called by <see cref="BuildPrefabs"/>).</summary>
        private static void BuildWeaponPrefabs()
        {
            Save(FlameJet());
            Save(PlasmaBurst());
            Save(EmpBlast());
            Save(Electric());
            Save(Weld());
        }

        /// <summary>The particles leave with this share of the emitter's own velocity (a jet out of a moving car).</summary>
        private static void Inherit(ParticleSystem system, float share)
        {
            ParticleSystem.InheritVelocityModule inherit = system.inheritVelocity;
            inherit.enabled = true;
            inherit.mode = ParticleSystemInheritVelocityMode.Initial;
            inherit.curveMultiplier = share;
        }

        private static void Drag(ParticleSystem system, float drag)
        {
            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 100f;
            limit.drag = drag;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;
        }

        // ------------------------------------------------------------------ the flamethrower

        /// <summary>
        /// The flamethrower's jet along +z, bold enough to read from forty metres up on sand or asphalt: a body of tinted
        /// flame (alpha blended, so it shows on bright ground too) that leaves fast, slows, swells and darkens into smoke
        /// some eighteen metres on (the reach of the flame), carrying the car's own speed; a brighter additive glow inside
        /// it; a white-hot tongue at the nozzle in the car's space; smoke curling off the end; embers falling out.
        /// </summary>
        private static GameObject FlameJet()
        {
            GameObject root = Root("FlameJet");
            Transform t = root.transform;
            ParticleSystem body = System(t, "BodyTint", "Smoke", true, 1f);
            ParticleFactory.Rate(body, 55f);
            ParticleFactory.MaxParticles(body, 60);
            ParticleFactory.Lifetime(body, 0.5f, 0.7f);
            ParticleFactory.Speed(body, 26f, 32f);
            ParticleFactory.Size(body, 1.1f, 1.6f);
            Forward(body, 7f, 0.1f);
            Drag(body, 0.8f);
            Inherit(body, 0.95f);
            Grow(body, new Keyframe(0f, 0.55f), new Keyframe(0.5f, 1.7f), new Keyframe(1f, 2.6f));
            Over(body, (0f, new Color(1f, 1f, 0.9f, 0f)), (0.06f, new Color(1f, 0.95f, 0.85f, 0.95f)), (0.5f, new Color(1f, 0.75f, 0.55f, 0.85f)),
                (0.8f, new Color(0.35f, 0.22f, 0.16f, 0.55f)), (1f, new Color(0.2f, 0.17f, 0.15f, 0f)));
            ParticleFactory.Spin(body, 160f);
            ParticleFactory.Noise(body, 0.6f, 1.2f);

            ParticleSystem fire = System(t, "FireTint", "FireAdd", true, 1f);
            ParticleFactory.Rate(fire, 80f);
            ParticleFactory.MaxParticles(fire, 70);
            ParticleFactory.Lifetime(fire, 0.35f, 0.55f);
            ParticleFactory.Speed(fire, 26f, 32f);
            ParticleFactory.Size(fire, 0.9f, 1.3f);
            Forward(fire, 6f, 0.08f);
            Drag(fire, 0.8f);
            Inherit(fire, 0.95f);
            Grow(fire, new Keyframe(0f, 0.6f), new Keyframe(0.5f, 1.6f), new Keyframe(1f, 2.1f));
            Over(fire, (0f, Color.white), (0.4f, new Color(1f, 0.85f, 0.6f, 1f)), (0.8f, new Color(1f, 0.5f, 0.2f, 0.6f)), (1f, new Color(0.6f, 0.2f, 0.05f, 0f)));
            ParticleFactory.Spin(fire, 200f);
            Order(fire, 2);

            ParticleSystem core = System(t, "Core", "FireAdd", true, 1f);
            ParticleSystem.MainModule main = core.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleFactory.Rate(core, 50f);
            ParticleFactory.MaxParticles(core, 14);
            ParticleFactory.Lifetime(core, 0.1f, 0.16f);
            ParticleFactory.Speed(core, 14f, 18f);
            ParticleFactory.Size(core, 0.75f, 1f);
            ParticleFactory.Colors(core, Hot, new Color(1f, 0.9f, 0.6f, 1f));
            Forward(core, 4f, 0.03f);
            Grow(core, new Keyframe(0f, 0.6f), new Keyframe(1f, 1.5f));
            Over(core, (0f, Color.white), (1f, new Color(1f, 0.6f, 0.25f, 0f)));
            Order(core, 3);

            ParticleSystem smoke = System(t, "Smoke", "Smoke", true, 1f);
            ParticleFactory.Rate(smoke, 16f);
            ParticleFactory.MaxParticles(smoke, 30);
            ParticleFactory.Lifetime(smoke, 0.8f, 1.2f);
            ParticleFactory.Speed(smoke, 16f, 20f);
            ParticleFactory.Size(smoke, 1.2f, 1.7f);
            ParticleFactory.Colors(smoke, new Color(0.2f, 0.18f, 0.17f, 0.7f), new Color(0.3f, 0.28f, 0.26f, 0.6f));
            Forward(smoke, 9f, 0.1f);
            Drag(smoke, 1.4f);
            Inherit(smoke, 0.9f);
            Rise(smoke, 0.6f, 1.4f);
            Grow(smoke, new Keyframe(0f, 0.6f), new Keyframe(1f, 3f));
            Over(smoke, (0f, new Color(1f, 1f, 1f, 0f)), (0.35f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            ParticleFactory.Spin(smoke, 40f);
            Order(smoke, -1);

            ParticleSystem embers = System(t, "Embers", "SparkAdd", true, 1f);
            ParticleFactory.Rate(embers, 18f);
            ParticleFactory.MaxParticles(embers, 30);
            ParticleFactory.Lifetime(embers, 0.4f, 0.8f);
            ParticleFactory.Speed(embers, 14f, 22f);
            ParticleFactory.Size(embers, 0.08f, 0.14f);
            ParticleFactory.Colors(embers, new Color(1f, 0.75f, 0.3f), new Color(1f, 0.5f, 0.15f));
            Forward(embers, 14f, 0.05f);
            Inherit(embers, 0.9f);
            ParticleFactory.Gravity(embers, 0.8f);
            Stretch(embers, 0.04f, 1.2f);
            Over(embers, (0f, Color.white), (1f, new Color(1f, 0.4f, 0.1f, 0f)));
            return root;
        }

        // ------------------------------------------------------------------ the plasma cannon

        /// <summary>A bolt bursting: a white-violet flash, violet sparks thrown out, a ring on the road, a short violet light.</summary>
        private static GameObject PlasmaBurst()
        {
            GameObject root = Root("PlasmaBurst");
            Transform t = root.transform;
            ParticleSystem flash = System(t, "Flash", "FlareAdd", false, 0.1f);
            Burst(flash, 2);
            ParticleFactory.Lifetime(flash, 0.14f, 0.2f);
            ParticleFactory.Speed(flash, 0f, 0f);
            ParticleFactory.Size(flash, 4.2f, 5.2f);
            ParticleFactory.Colors(flash, Color.white, Color.Lerp(Plasma, Color.white, 0.5f));
            Over(flash, (0f, Color.white), (0.5f, Plasma), (1f, new Color(Plasma.r, Plasma.g, Plasma.b, 0f)));
            Order(flash, 3);

            ParticleSystem core = System(t, "Core", "FireAdd", false, 0.1f);
            Burst(core, 6);
            ParticleFactory.Lifetime(core, 0.2f, 0.35f);
            ParticleFactory.Speed(core, 0.5f, 2.5f);
            ParticleFactory.Size(core, 1.4f, 2.2f);
            ParticleFactory.Colors(core, Color.Lerp(Plasma, Color.white, 0.6f), Plasma);
            Ball(core, 0.2f);
            Grow(core, new Keyframe(0f, 0.6f), new Keyframe(1f, 1.4f));
            Over(core, (0f, Color.white), (1f, new Color(Plasma.r, Plasma.g, Plasma.b, 0f)));

            ParticleSystem sparks = System(t, "Sparks", "SparkAdd", false, 0.1f);
            Burst(sparks, 20);
            ParticleFactory.Lifetime(sparks, 0.2f, 0.45f);
            ParticleFactory.Speed(sparks, 8f, 18f);
            ParticleFactory.Size(sparks, 0.08f, 0.16f);
            ParticleFactory.Colors(sparks, Color.Lerp(Plasma, Color.white, 0.4f), Plasma);
            Ball(sparks, 0.3f, true);
            Drag(sparks, 3f);
            Stretch(sparks, 0.05f, 1.3f);
            Over(sparks, (0f, Color.white), (1f, new Color(Plasma.r, Plasma.g, Plasma.b, 0f)));

            ParticleSystem ring = System(t, "Ring", "RingAdd", false, 0.1f);
            Burst(ring, 1);
            ParticleFactory.Lifetime(ring, 0.3f, 0.32f);
            ParticleFactory.Speed(ring, 0f, 0f);
            ParticleFactory.Size(ring, 2f, 2f);
            ParticleFactory.Colors(ring, Plasma, Plasma);
            Flat(ring);
            Grow(ring, new Keyframe(0f, 0.3f), new Keyframe(1f, 2.6f));
            Over(ring, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            ring.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            FlashLight(t, 8f, 5f, Plasma);
            return root;
        }

        // ------------------------------------------------------------------ the EMP

        /// <summary>
        /// An EMP blast: a crisp blue ring racing out over the road to 1 m radius at size 1 (the race sizes it to the
        /// blast's reach), a softer halo behind it, a white flash at the car, blue sparks spat out, a cold light.
        /// </summary>
        private static GameObject EmpBlast()
        {
            GameObject root = Root("EmpBlast");
            Transform t = root.transform;
            // The ring texture's band lies at 0.82 of its radius: a particle 2.44 across draws a ring of 1 m radius.
            ParticleSystem ring = System(t, "Ring", "RingAdd", false, 0.1f);
            Burst(ring, 1);
            ParticleFactory.Lifetime(ring, 0.45f, 0.45f);
            ParticleFactory.Speed(ring, 0f, 0f);
            ParticleFactory.Size(ring, 2.44f, 2.44f);
            ParticleFactory.Colors(ring, Color.Lerp(Emp, Color.white, 0.3f), Color.Lerp(Emp, Color.white, 0.3f));
            Flat(ring);
            Grow(ring, new Keyframe(0f, 0.08f), new Keyframe(0.6f, 0.92f), new Keyframe(1f, 1f));
            Over(ring, (0f, Color.white), (0.7f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            ring.GetComponent<ParticleSystemRenderer>().maxParticleSize = 4f;
            ring.transform.localPosition = new Vector3(0f, 0.2f, 0f);

            ParticleSystem halo = System(t, "Halo", "RingAdd", false, 0.1f);
            Burst(halo, 1);
            ParticleFactory.Lifetime(halo, 0.6f, 0.6f);
            ParticleFactory.Speed(halo, 0f, 0f);
            ParticleFactory.Size(halo, 2.2f, 2.2f);
            ParticleFactory.Colors(halo, new Color(Emp.r, Emp.g, Emp.b, 0.45f), new Color(Emp.r, Emp.g, Emp.b, 0.45f));
            Flat(halo);
            Grow(halo, new Keyframe(0f, 0.05f), new Keyframe(1f, 0.9f));
            Over(halo, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            halo.GetComponent<ParticleSystemRenderer>().maxParticleSize = 4f;
            halo.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            ParticleSystem flash = System(t, "Flash", "FlareAdd", false, 0.1f);
            Burst(flash, 1);
            ParticleFactory.Lifetime(flash, 0.15f, 0.2f);
            ParticleFactory.Speed(flash, 0f, 0f);
            ParticleFactory.Size(flash, 0.5f, 0.6f);
            ParticleFactory.Colors(flash, Color.white, Color.Lerp(Emp, Color.white, 0.6f));
            Over(flash, (0f, Color.white), (1f, new Color(Emp.r, Emp.g, Emp.b, 0f)));
            flash.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            Order(flash, 3);

            ParticleSystem sparks = System(t, "Sparks", "SparkAdd", false, 0.15f);
            Burst(sparks, 40);
            ParticleFactory.Lifetime(sparks, 0.25f, 0.5f);
            ParticleFactory.Speed(sparks, 2f, 4f);
            ParticleFactory.Size(sparks, 0.012f, 0.022f);
            ParticleFactory.Colors(sparks, Color.white, Emp);
            Ball(sparks, 0.2f, true);
            Stretch(sparks, 0.08f, 1.5f);
            Over(sparks, (0f, Color.white), (1f, new Color(Emp.r, Emp.g, Emp.b, 0f)));
            sparks.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            // The light's range grows with the root of the size it is played at (the blast's radius): 4.3 reaches some 16 m.
            FlashLight(t, 4.3f, 6f, Emp);
            return root;
        }

        /// <summary>Little blue arcs crawling over a jammed car (in the car's space) and a flickering blue glow under them.</summary>
        private static GameObject Electric()
        {
            GameObject root = Root("Electric");
            Transform t = root.transform;
            ParticleSystem arcs = System(t, "Arcs", "SparkAdd", true, 1f);
            ParticleSystem.MainModule main = arcs.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleFactory.Rate(arcs, 80f);
            ParticleFactory.MaxParticles(arcs, 40);
            ParticleFactory.Lifetime(arcs, 0.06f, 0.14f);
            ParticleFactory.Speed(arcs, 8f, 16f);
            ParticleFactory.Size(arcs, 0.22f, 0.4f);
            ParticleFactory.Colors(arcs, Color.white, Emp);
            ParticleFactory.Box(arcs, new Vector3(1.6f, 0.6f, 3.6f));
            ParticleSystem.ShapeModule shape = arcs.shape;
            shape.randomDirectionAmount = 1f;
            Stretch(arcs, 0.09f, 2f);
            Over(arcs, (0f, Color.white), (1f, new Color(Emp.r, Emp.g, Emp.b, 0f)));

            ParticleSystem glow = System(t, "Glow", "FlareAdd", true, 1f);
            ParticleSystem.MainModule glowMain = glow.main;
            glowMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleFactory.Rate(glow, 20f);
            ParticleFactory.MaxParticles(glow, 8);
            ParticleFactory.Lifetime(glow, 0.06f, 0.12f);
            ParticleFactory.Speed(glow, 0f, 0.2f);
            ParticleFactory.Size(glow, 2.4f, 3.6f);
            ParticleFactory.Colors(glow, new Color(Emp.r, Emp.g, Emp.b, 0.7f), new Color(0.7f, 0.9f, 1f, 0.6f));
            ParticleFactory.Box(glow, new Vector3(1.2f, 0.4f, 3f));
            Over(glow, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));

            // A flickering blue pool on the road round the car: the jam reads from above even under the car's own lights.
            ParticleSystem pool = System(t, "Pool", "FlareAdd", true, 1f);
            ParticleSystem.MainModule poolMain = pool.main;
            poolMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleFactory.Rate(pool, 12f);
            ParticleFactory.MaxParticles(pool, 4);
            ParticleFactory.Lifetime(pool, 0.1f, 0.2f);
            ParticleFactory.Speed(pool, 0f, 0f);
            ParticleFactory.Size(pool, 5f, 6f);
            ParticleFactory.Colors(pool, new Color(Emp.r, Emp.g, Emp.b, 0.45f), new Color(Emp.r, Emp.g, Emp.b, 0.3f));
            Flat(pool);
            Over(pool, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            pool.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            return root;
        }

        // ------------------------------------------------------------------ the field welder

        /// <summary>A field welder at work: a blue-white point (in the car's space) spitting short orange sparks that fall.</summary>
        private static GameObject Weld()
        {
            GameObject root = Root("Weld");
            Transform t = root.transform;
            ParticleSystem sparks = System(t, "Sparks", "SparkAdd", true, 1f);
            ParticleFactory.Rate(sparks, 50f);
            ParticleFactory.MaxParticles(sparks, 40);
            ParticleFactory.Lifetime(sparks, 0.25f, 0.45f);
            ParticleFactory.Speed(sparks, 3f, 8f);
            ParticleFactory.Size(sparks, 0.16f, 0.26f);
            ParticleFactory.Colors(sparks, new Color(1f, 0.9f, 0.6f), new Color(1f, 0.6f, 0.2f));
            Ball(sparks, 0.08f, true);
            ParticleFactory.Gravity(sparks, 1.2f);
            Stretch(sparks, 0.05f, 1.2f);
            Over(sparks, (0f, Color.white), (1f, new Color(1f, 0.4f, 0.1f, 0f)));

            ParticleSystem point = System(t, "Point", "FlareAdd", true, 1f);
            ParticleSystem.MainModule main = point.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleFactory.Rate(point, 30f);
            ParticleFactory.MaxParticles(point, 5);
            ParticleFactory.Lifetime(point, 0.04f, 0.09f);
            ParticleFactory.Speed(point, 0f, 0.1f);
            ParticleFactory.Size(point, 1.8f, 2.4f);
            ParticleFactory.Colors(point, new Color(0.85f, 0.92f, 1f), Color.white);
            Over(point, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            return root;
        }
    }
}
