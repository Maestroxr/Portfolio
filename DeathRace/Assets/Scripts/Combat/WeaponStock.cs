using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// What a car carries into the fight and how fast it spends it, apart from the car (<see cref="CarWeapons"/> drives
    /// it): rounds and the gun's rate of fire, mines, rockets, nitro charges and the burning one, the special weapons,
    /// and how many of each were used (the result of the race takes the used ones out of the career's stock).
    /// </summary>
    public sealed class WeaponStock
    {
        private readonly int[] specials = new int[Specials.Count];
        private readonly int[] specialsUsed = new int[Specials.Count];
        private float gunCooldown;
        private bool nitroHeld;

        public WeaponStock(int ammoCapacity, int mines, int rockets, int nitro)
            : this(ammoCapacity, mines, rockets, nitro, 0, 0, 0)
        {
        }

        /// <param name="roundsPerSecond">The gun's rate of fire (0: the machine gun's).</param>
        /// <param name="nitroSeconds">How long a charge burns (0: <see cref="CombatRules.NitroSeconds"/>).</param>
        public WeaponStock(int ammoCapacity, int mines, int rockets, int nitro, int oil, int emp, int mortar, float roundsPerSecond = 0f,
            float nitroSeconds = 0f)
        {
            AmmoCapacity = Mathf.Max(0, ammoCapacity);
            Ammo = AmmoCapacity;
            Mines = Mathf.Max(0, mines);
            Rockets = Mathf.Max(0, rockets);
            NitroCharges = Mathf.Max(0, nitro);
            specials[(int)SpecialKind.Oil] = Mathf.Max(0, oil);
            specials[(int)SpecialKind.Emp] = Mathf.Max(0, emp);
            specials[(int)SpecialKind.Mortar] = Mathf.Max(0, mortar);
            RoundsPerSecond = roundsPerSecond > 0f ? roundsPerSecond : CombatRules.GunRoundsPerSecond;
            NitroSeconds = nitroSeconds > 0f ? nitroSeconds : CombatRules.NitroSeconds;
        }

        /// <summary>How many rounds the gun fires a second with the trigger held.</summary>
        public float RoundsPerSecond { get; set; }

        /// <summary>How long one nitro charge burns.</summary>
        public float NitroSeconds { get; }

        public int Ammo { get; private set; }
        public int AmmoCapacity { get; }
        public int Mines { get; private set; }
        public int Rockets { get; private set; }
        public int NitroCharges { get; private set; }
        public int RoundsFired { get; private set; }
        public int MinesUsed { get; private set; }
        public int RocketsUsed { get; private set; }
        public int NitroUsed { get; private set; }

        /// <summary>Seconds left of the charge burning, 0 when none.</summary>
        public float NitroLeft { get; private set; }

        public bool NitroActive => NitroLeft > 0f;

        /// <summary>
        /// The trigger held (or not) for <paramref name="deltaTime"/> seconds: how many rounds leave the barrel in that time
        /// at <see cref="CombatRules.GunRoundsPerSecond"/>, as many as there are rounds. The first round of a burst goes at
        /// once.
        /// </summary>
        public int Fire(bool held, float deltaTime)
        {
            gunCooldown = Mathf.Max(-deltaTime, gunCooldown - deltaTime);
            if (!held)
            {
                gunCooldown = Mathf.Max(0f, gunCooldown);
                return 0;
            }
            int rounds = 0;
            float interval = 1f / Mathf.Max(0.1f, RoundsPerSecond);
            while (gunCooldown <= 0f && Ammo > 0)
            {
                Ammo--;
                RoundsFired++;
                rounds++;
                gunCooldown += interval;
            }
            if (Ammo == 0)
            {
                gunCooldown = Mathf.Max(0f, gunCooldown);
            }
            return rounds;
        }

        public bool TakeMine()
        {
            if (Mines <= 0)
            {
                return false;
            }
            Mines--;
            MinesUsed++;
            return true;
        }

        public bool TakeRocket()
        {
            if (Rockets <= 0)
            {
                return false;
            }
            Rockets--;
            RocketsUsed++;
            return true;
        }

        /// <summary>
        /// The nitro held (or not) for <paramref name="deltaTime"/> seconds. Pressing it lights a charge when none burns;
        /// still holding it when the charge runs out lights the next one. Returns whether a charge was lit this step.
        /// </summary>
        public bool Nitro(bool held, float deltaTime)
        {
            bool wasBurning = NitroActive;
            if (NitroLeft > 0f)
            {
                NitroLeft = Mathf.Max(0f, NitroLeft - deltaTime);
            }
            bool lit = false;
            bool pressed = held && !nitroHeld;
            bool chained = held && wasBurning && !NitroActive;
            if ((pressed || chained) && !NitroActive && NitroCharges > 0)
            {
                NitroCharges--;
                NitroUsed++;
                NitroLeft = NitroSeconds;
                lit = true;
            }
            nitroHeld = held;
            return lit;
        }

        /// <summary>Puts out the burning charge (the car was wrecked).</summary>
        public void Douse()
        {
            NitroLeft = 0f;
        }

        /// <summary>How many of the special weapon <paramref name="kind"/> are left.</summary>
        public int SpecialsLeft(SpecialKind kind)
        {
            return specials[Index(kind)];
        }

        public int SpecialsUsed(SpecialKind kind)
        {
            return specialsUsed[Index(kind)];
        }

        /// <summary>Takes one <paramref name="kind"/> to fire; false when there is none.</summary>
        public bool TakeSpecial(SpecialKind kind)
        {
            int index = Index(kind);
            if (specials[index] <= 0)
            {
                return false;
            }
            specials[index]--;
            specialsUsed[index]++;
            return true;
        }

        public void AddSpecials(SpecialKind kind, int count)
        {
            specials[Index(kind)] += Mathf.Max(0, count);
        }

        /// <summary>
        /// The special the button fires: <paramref name="armed"/> when there is one left, else the next kind (in the order
        /// of the enum, round from the armed one) that has any; null when there are none at all.
        /// </summary>
        public SpecialKind? NextSpecial(SpecialKind armed)
        {
            for (int step = 0; step < specials.Length; step++)
            {
                var kind = (SpecialKind)(((int)armed + step) % specials.Length);
                if (specials[Index(kind)] > 0)
                {
                    return kind;
                }
            }
            return null;
        }

        private static int Index(SpecialKind kind)
        {
            return Mathf.Clamp((int)kind, 0, Specials.Count - 1);
        }

        public int AddAmmo(int rounds)
        {
            int before = Ammo;
            Ammo = Mathf.Min(AmmoCapacity, Ammo + Mathf.Max(0, rounds));
            return Ammo - before;
        }

        public void AddNitro(int charges)
        {
            NitroCharges += Mathf.Max(0, charges);
        }

        public void AddMines(int mines)
        {
            Mines += Mathf.Max(0, mines);
        }

        public void AddRockets(int rockets)
        {
            Rockets += Mathf.Max(0, rockets);
        }
    }
}
