using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>What hurt a car.</summary>
    public enum DamageSource
    {
        Gun = 0,
        Mine = 1,
        Rocket = 2,
        Ram = 3,
        Wall = 4,
        /// <summary>The blast of a car being wrecked nearby.</summary>
        Explosion = 5
    }

    /// <summary>
    /// One hit on a car: how much (before the car's armour when it is handed to <see cref="CarHealth.TakeDamage"/>, after it
    /// in <see cref="CarHealth.Damaged"/>), where and which way it came from (sparks fly back along it, a blast pushes
    /// along it), what kind of hit it was and which car dealt it: the kill and the bounty go to the attacker.
    /// </summary>
    public struct DamageInfo
    {
        public float Amount;
        public Vector3 Point;

        /// <summary>The way the hit travelled (from the attacker into the car), flat on the ground.</summary>
        public Vector3 Direction;

        public DamageSource Source;

        /// <summary>The car that dealt the hit: the shooter, the one who dropped the mine, the rammer; null for a barrier.</summary>
        public Car Attacker;

        public DamageInfo(float amount, DamageSource source, Car attacker, Vector3 point, Vector3 direction)
        {
            Amount = amount;
            Source = source;
            Attacker = attacker;
            Point = point;
            Direction = direction;
        }

        /// <summary>The same hit with another amount (after armour, for the event).</summary>
        public DamageInfo WithAmount(float amount)
        {
            DamageInfo copy = this;
            copy.Amount = amount;
            return copy;
        }

        public override string ToString()
        {
            string by = Attacker != null && Attacker.Entrant != null ? Attacker.Entrant.Name : "-";
            return $"{Source} {Amount:0.0} by {by}";
        }
    }
}
