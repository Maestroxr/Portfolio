using System;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>What a weapon can leave a car with for a few seconds.</summary>
    public enum CarStatusKind
    {
        /// <summary>Set alight by a flamethrower: it loses health while it burns.</summary>
        Burning = 0,

        /// <summary>On oil: its tyres hardly hold and the wheel hardly turns it.</summary>
        Slicked = 1,

        /// <summary>Hit by an EMP blast: no weapons, no nitro, a coughing engine.</summary>
        Jammed = 2
    }

    /// <summary>
    /// The statuses a car is under (<see cref="CarStatusKind"/>): burning, slicked, jammed, each with the seconds it has
    /// left and the car that caused it. A weapon hands a car a status (<see cref="Burn"/>, <see cref="Slick"/>,
    /// <see cref="Jam"/>); the car steps it every physics step before its body (<see cref="Step"/>), which counts it down,
    /// deals the burning's damage a few ticks a second, and tells the body what it leaves of the grip, the steering and the
    /// top speed (<see cref="CarBody.GripFactor"/> and the others); the weapons fall silent while the car is jammed. The
    /// HUD shows them and the computer's drivers read them.
    ///
    /// A status is judged where the car's hits are (<see cref="CarHealth.Authoritative"/>): online, the device that drives
    /// a car is the only one that puts it on fire, on oil or out of action, as it is the only one that hurts it. A car
    /// wrecked or past the finish line is under none. The numbers are <see cref="CombatRules"/>'s.
    /// </summary>
    public class CarStatus : MonoBehaviour
    {
        private static readonly int Kinds = Enum.GetValues(typeof(CarStatusKind)).Length;

        private readonly float[] left = new float[Kinds];
        private readonly Car[] causes = new Car[Kinds];
        private Car car;
        private float burnTick;
        private RaceEffect flames;
        private RaceEffect sparks;

        public bool IsBurning => left[(int)CarStatusKind.Burning] > 0f;
        public bool IsSlicked => left[(int)CarStatusKind.Slicked] > 0f;
        public bool IsJammed => left[(int)CarStatusKind.Jammed] > 0f;

        /// <summary>Whether the car is under any status.</summary>
        public bool Any => IsBurning || IsSlicked || IsJammed;

        /// <summary>A status began (not when one that was on is made longer): the HUD's toast, the sound.</summary>
        public event Action<CarStatusKind> Started;

        /// <summary>Whether the statuses of this car are judged at this device (online: only of the cars driven here).</summary>
        public bool Judged => car == null || car.Health == null || car.Health.Authoritative;

        public bool Has(CarStatusKind kind)
        {
            return left[Index(kind)] > 0f;
        }

        /// <summary>The seconds <paramref name="kind"/> has left (0 when the car is not under it).</summary>
        public float SecondsLeft(CarStatusKind kind)
        {
            return left[Index(kind)];
        }

        /// <summary>The car that set <paramref name="kind"/> off (null: none, or nobody's).</summary>
        public Car Cause(CarStatusKind kind)
        {
            return left[Index(kind)] > 0f ? causes[Index(kind)] : null;
        }

        /// <summary>Called by the factory: the car the statuses are of.</summary>
        public virtual void Setup(Car owner)
        {
            car = owner;
            Clear();
        }

        /// <summary>Sets the car alight for <paramref name="seconds"/> (or longer, if it already burns longer); false when it cannot be.</summary>
        public virtual bool Burn(Car attacker, float seconds = CombatRules.BurnSeconds)
        {
            if (!Begin(CarStatusKind.Burning, attacker, seconds))
            {
                return false;
            }
            if (flames == null || !flames.Playing)
            {
                burnTick = CombatRules.BurnTick;
                RaceEffects effects = Effects;
                if (effects != null)
                {
                    flames = effects.Loop(EffectKind.Fire, transform, BodyPoint(0.55f), Quaternion.identity, 0.7f);
                }
            }
            return true;
        }

        /// <summary>
        /// Puts the car on oil for <paramref name="seconds"/>; the first touch throws it into a spin of
        /// <paramref name="spin"/> degrees a second (a car already on oil only stays on it longer).
        /// </summary>
        public virtual bool Slick(Car attacker, float seconds, float spin)
        {
            bool already = IsSlicked;
            if (!Begin(CarStatusKind.Slicked, attacker, seconds))
            {
                return false;
            }
            if (!already)
            {
                car?.Body?.Spin(spin);
                // Nothing of the oil hurts, but the barrier it sends the car into counts for whoever poured it.
                car?.Health?.Assist(attacker);
            }
            return true;
        }

        /// <summary>Jams the car for <paramref name="seconds"/>: no weapons, no nitro, less top speed.</summary>
        public virtual bool Jam(Car attacker, float seconds)
        {
            bool already = IsJammed;
            if (!Begin(CarStatusKind.Jammed, attacker, seconds))
            {
                return false;
            }
            if (!already)
            {
                RaceEffects effects = Effects;
                if (effects != null && (sparks == null || !sparks.Playing))
                {
                    sparks = effects.Loop(EffectKind.Electric, transform, BodyPoint(0.5f), Quaternion.identity, Mathf.Max(0.8f, CarLength / 4.4f));
                }
                Audio?.Jammed(transform.position);
            }
            return true;
        }

        /// <summary>A status of <paramref name="kind"/> for at least <paramref name="seconds"/>, if this car can be put under one now.</summary>
        private bool Begin(CarStatusKind kind, Car attacker, float seconds)
        {
            if (seconds <= 0f || !Judged || (car != null && (car.IsWrecked || car.HasFinished)))
            {
                return false;
            }
            int index = Index(kind);
            bool fresh = left[index] <= 0f;
            left[index] = Mathf.Max(left[index], seconds);
            if (attacker != null || fresh)
            {
                causes[index] = attacker;
            }
            if (fresh)
            {
                Started?.Invoke(kind);
            }
            return true;
        }

        /// <summary>One physics step: the statuses count down, the fire burns, and the body learns what it has left.</summary>
        public virtual void Step(float deltaTime)
        {
            if (car != null && (car.IsWrecked || car.HasFinished))
            {
                if (Any)
                {
                    Clear();
                }
                return;
            }
            if (IsBurning)
            {
                burnTick -= deltaTime;
                if (burnTick <= 0f)
                {
                    burnTick += CombatRules.BurnTick;
                    Car attacker = causes[(int)CarStatusKind.Burning];
                    car?.Health?.TakeDamage(new DamageInfo(CombatRules.BurnDamagePerSecond * CombatRules.BurnTick, DamageSource.Flame, attacker,
                        transform.position + Vector3.up, Vector3.zero));
                }
            }
            for (int i = 0; i < Kinds; i++)
            {
                if (left[i] > 0f)
                {
                    left[i] = Mathf.Max(0f, left[i] - deltaTime);
                    if (left[i] <= 0f)
                    {
                        Ended((CarStatusKind)i);
                    }
                }
            }
            ApplyToBody();
        }

        /// <summary>Every status ends now (the car was wrecked or finished, or a new race).</summary>
        public void Clear()
        {
            for (int i = 0; i < Kinds; i++)
            {
                left[i] = 0f;
                causes[i] = null;
            }
            StopLoop(ref flames);
            StopLoop(ref sparks);
            ApplyToBody();
        }

        private void Ended(CarStatusKind kind)
        {
            causes[(int)kind] = null;
            if (kind == CarStatusKind.Burning)
            {
                StopLoop(ref flames);
            }
            else if (kind == CarStatusKind.Jammed)
            {
                StopLoop(ref sparks);
            }
        }

        /// <summary>What the oil leaves of the tyres and the wheel, and the jam of the engine.</summary>
        private void ApplyToBody()
        {
            CarBody body = car != null ? car.Body : null;
            if (body == null)
            {
                return;
            }
            body.GripFactor = IsSlicked ? CombatRules.OilGrip : 1f;
            body.SteerFactor = IsSlicked ? CombatRules.OilSteering : 1f;
            body.TopSpeedFactor = CombatRules.TopSpeedShare(IsJammed);
        }

        private RaceEffects Effects => car != null && car.Race != null ? car.Race.Effects : null;

        private RaceAudio Audio => car != null && car.Race != null && car.Race.Audio != null ? car.Race.Audio : RaceAudio.Current;

        private float CarLength => car != null && car.Loadout != null ? car.Loadout.Spec.Size.z : 4.2f;

        /// <summary>A point on the car's body: <paramref name="height"/> of its height up, in the middle.</summary>
        private Vector3 BodyPoint(float height)
        {
            Vector3 size = car != null && car.Loadout != null ? car.Loadout.Spec.Size : new Vector3(2f, 1.4f, 4.2f);
            return new Vector3(0f, size.y * height + 0.2f, 0f);
        }

        private static int Index(CarStatusKind kind)
        {
            return Mathf.Clamp((int)kind, 0, Kinds - 1);
        }

        private static void StopLoop(ref RaceEffect effect)
        {
            if (effect != null)
            {
                effect.Stop();
            }
            effect = null;
        }

        private void OnDestroy()
        {
            StopLoop(ref flames);
            StopLoop(ref sparks);
        }
    }
}
