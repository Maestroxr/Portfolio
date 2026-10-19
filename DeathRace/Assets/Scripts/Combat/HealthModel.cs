using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The arithmetic of a car's health, apart from the car (<see cref="CarHealth"/> drives it): armour on every hit, a
    /// short grace against the same crash landing again while two bodies grind, and who a wreck counts for. Attackers
    /// are ids (the seat of the car, or <see cref="Nobody"/>) so the tests need no cars.
    /// </summary>
    public sealed class HealthModel
    {
        public const int Nobody = -1;

        /// <summary>The crashes of the last moments (a ring): who, what kind, when.</summary>
        private readonly Crash[] crashes = new Crash[8];
        private int nextCrash;

        private struct Crash
        {
            public int By;
            public DamageSource Source;
            public float Time;
        }

        public HealthModel(float max, float start01, float damageTaken)
        {
            Max = Mathf.Max(1f, max);
            Health = Max * Mathf.Clamp01(start01);
            DamageTaken = damageTaken;
            for (int i = 0; i < crashes.Length; i++)
            {
                crashes[i] = new Crash { By = Nobody, Source = DamageSource.Wall, Time = float.NegativeInfinity };
            }
            LastAttacker = Nobody;
            LastAttackTime = float.NegativeInfinity;
        }

        public float Max { get; }
        public float Health { get; private set; }

        /// <summary>The share of damage the armour lets through.</summary>
        public float DamageTaken { get; set; }

        public float Health01 => Mathf.Clamp01(Health / Max);
        public bool Dead => Health <= 0f;

        /// <summary>The last other car that hurt this one, and when (for the kill of a car pushed into the wall).</summary>
        public int LastAttacker { get; private set; }

        public float LastAttackTime { get; private set; }

        /// <summary>
        /// A hit of <paramref name="amount"/> (before armour) from <paramref name="source"/>, dealt by <paramref name="attacker"/>
        /// (or <see cref="Nobody"/>) on the car <paramref name="self"/> at time <paramref name="now"/>. Returns the damage
        /// taken: zero when the car is already wrecked or the same crash lands again within the cooldown.
        /// </summary>
        public float Apply(float amount, DamageSource source, int attacker, int self, float now)
        {
            if (Dead || amount <= 0f)
            {
                return 0f;
            }
            if (IsCrash(source))
            {
                if (CrashedRecently(source, attacker, now))
                {
                    return 0f;
                }
                crashes[nextCrash] = new Crash { By = attacker, Source = source, Time = now };
                nextCrash = (nextCrash + 1) % crashes.Length;
            }
            float taken = Mathf.Min(Health, CombatRules.AfterArmour(amount, DamageTaken));
            Health -= taken;
            if (Health < 0.001f)
            {
                Health = 0f;
            }
            if (attacker != Nobody && attacker != self)
            {
                LastAttacker = attacker;
                LastAttackTime = now;
            }
            return taken;
        }

        /// <summary>
        /// The steady wear of grinding along a barrier: <paramref name="amount"/> before armour, never held back by the crash
        /// cooldown (it comes a little every step). Returns the damage taken.
        /// </summary>
        public float Wear(float amount)
        {
            if (Dead || amount <= 0f)
            {
                return 0f;
            }
            float taken = Mathf.Min(Health, CombatRules.AfterArmour(amount, DamageTaken));
            Health -= taken;
            if (Health < 0.001f)
            {
                Health = 0f;
            }
            return taken;
        }

        /// <summary>Sets the health as it is (what another device says of its car), 0 to the maximum.</summary>
        public void Set(float health)
        {
            Health = Mathf.Clamp(health, 0f, Max);
        }

        /// <summary>
        /// Who wrecked the car when the blow that did it came from <paramref name="attacker"/>: that car; for a blow of no
        /// car (a barrier) or of the car itself (its own mine), the last other car that hurt it within
        /// <see cref="CombatRules.AssistWindow"/> seconds; else nobody.
        /// </summary>
        public int KillerOf(int attacker, int self, float now)
        {
            if (attacker != Nobody && attacker != self)
            {
                return attacker;
            }
            if (LastAttacker != Nobody && now - LastAttackTime <= CombatRules.AssistWindow)
            {
                return LastAttacker;
            }
            return Nobody;
        }

        /// <summary>Puts back up to <paramref name="amount"/> health; returns what was put back.</summary>
        public float Repair(float amount)
        {
            if (Dead || amount <= 0f)
            {
                return 0f;
            }
            float before = Health;
            Health = Mathf.Min(Max, Health + amount);
            return Health - before;
        }

        /// <summary>Whether <paramref name="attacker"/> crashed into the car this way within the cooldown.</summary>
        public bool CrashedRecently(DamageSource source, int attacker, float now)
        {
            foreach (Crash crash in crashes)
            {
                if (crash.Source == source && crash.By == attacker && now - crash.Time < CombatRules.CrashCooldown)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Whether a source is a crash (two bodies touching), which can land again and again while they grind.</summary>
        public static bool IsCrash(DamageSource source)
        {
            return source == DamageSource.Ram || source == DamageSource.Wall;
        }

        /// <summary>How hurt a car looks: 0 fine, 1 smoking, 2 burning.</summary>
        public static int DamageStage(float health01)
        {
            if (health01 < CombatRules.BurningAt)
            {
                return 2;
            }
            return health01 < CombatRules.SmokingAt ? 1 : 0;
        }
    }
}
