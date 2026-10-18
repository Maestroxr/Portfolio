using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The armory's part of the spawner: the numbers of the new field weapons that change with the weapon level (the Arc
    /// Caster's chains, the Flak Burster's shrapnel, the Ion Wave's growth) and the Phase Rounds' extra pierce, put on a
    /// shot as it is fired (or shown as another pilot's ghost); and the fragments of burst shells, which every client makes
    /// for the shells it shows and never sends.
    /// </summary>
    public partial class SpawnService
    {
        [Header("Armory")]
        [Tooltip("The Flak Burster's shrapnel (the fragments of the asteroid field's flak shells).")]
        [SerializeField] internal ShotPool flakShrapnelPool;


        /// <summary>
        /// The level's numbers of an armory weapon's shot of <paramref name="type"/> (nothing for the first four weapons'),
        /// and two more targets of pierce under the Phase Rounds of <paramref name="shooter"/>.
        /// </summary>
        private void ConfigureArmoryShot(Shot shot, WeaponType type, int level, AsteroidsPlayer shooter)
        {
            switch (shot)
            {
                case ArcShot arc:
                    arc.Chains = WeaponRules.Chains(type, level);
                    arc.ChainRange = WeaponRules.ChainRange(type, level);
                    arc.ChainDamage = WeaponRules.ChainDamage(type, level);
                    break;
                case BurstShot burst:
                    burst.Fragments = WeaponRules.Fragments(type, level);
                    burst.FragmentDamage = WeaponRules.FragmentDamage(type, level);
                    break;
                case WaveShot wave:
                    wave.Growth = WeaponRules.Growth(type, level);
                    break;
            }
            if (shooter != null && shooter.IsPowerUpActive(PowerUpType.Piercing))
            {
                shot.Pierce += PowerUps.ExtraPierce;
            }
        }


        /// <summary>
        /// A fragment of <paramref name="parent"/> (a burst shell): from the field's shrapnel pool or the pool of the strike
        /// shot kind the shell names, flying from <paramref name="position"/> at <paramref name="velocity"/> for
        /// <paramref name="lifetime"/> seconds. It belongs to the shell's pilot, is a ghost when the shell is one, keeps its
        /// prefab's reach, pierce and blast, and is not sent to the other pilots (their clients burst their own ghost of the
        /// shell). Null when the pool is missing or empty.
        /// </summary>
        public Shot FireFragment(BurstShot parent, Vector2 position, Vector2 velocity, float damage, float lifetime)
        {
            if (parent == null)
            {
                return null;
            }
            int kind = parent.FragmentKind;
            ShotPool pool = kind == BurstShot.FieldShrapnel ? flakShrapnelPool : kind >= 0 && kind < strikeShotPools.Length ? strikeShotPools[kind] : null;
            var shot = Deploy(pool) as Shot;
            if (shot == null)
            {
                WarnOnce($"no fragment of kind {kind} to burst into (its pool is {(pool == null ? "missing" : "empty")}).");
                return null;
            }
            Shot prefab = kind >= 0 ? StrikeShotPrefab(kind) : null;
            shot.Position = position;
            shot.Velocity = velocity;
            shot.transform.rotation = Quaternion.identity;
            shot.Lifetime = Mathf.Max(0.05f, lifetime > 0f ? lifetime : prefab != null ? prefab.Lifetime : 0.3f);
            shot.Damage = damage;
            shot.Pierce = prefab != null ? prefab.Pierce : 1;
            shot.BlastRadius = prefab != null ? prefab.BlastRadius : 0f;
            shot.FiredBy = parent.FiredBy;
            shot.IsGhost = parent.IsGhost;
            shot.IsFragment = true;
            field.Add(shot);
            return shot;
        }


        /// <summary>The pierce of another pilot's strike shot of ghost kind <paramref name="kind"/>: its prefab's (the rail gun's slug goes through).</summary>
        private int StrikeGhostPierce(byte kind)
        {
            Shot prefab = StrikeShotPrefab(kind - StrikeGhostBase);
            return prefab != null ? Mathf.Max(1, prefab.Pierce) : 1;
        }
    }
}
