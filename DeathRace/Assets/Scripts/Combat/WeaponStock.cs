using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// What a car carries into the fight and how fast it spends it, apart from the car (<see cref="CarWeapons"/> drives
    /// it): rounds and the gun's rate of fire, mines, rockets, nitro charges and the burning one, and how many of each
    /// were used (the result of the race takes the used ones out of the career's stock).
    /// </summary>
    public sealed class WeaponStock
    {
        private float gunCooldown;
        private bool nitroHeld;

        public WeaponStock(int ammoCapacity, int mines, int rockets, int nitro)
        {
            AmmoCapacity = Mathf.Max(0, ammoCapacity);
            Ammo = AmmoCapacity;
            Mines = Mathf.Max(0, mines);
            Rockets = Mathf.Max(0, rockets);
            NitroCharges = Mathf.Max(0, nitro);
        }

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
            float interval = 1f / CombatRules.GunRoundsPerSecond;
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
                NitroLeft = CombatRules.NitroSeconds;
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
