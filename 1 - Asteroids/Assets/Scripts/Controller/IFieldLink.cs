using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// What a playfield shared with other pilots asks of whoever keeps the copies in step (<see cref="FieldReplication"/>).
    /// One client simulates the world: its field tells the link which bodies come and go and which blasts go off. On the
    /// other clients the bodies are puppets: what the local ship and its shots do to them is passed on, and the simulator
    /// decides what comes of it. A field without a link is the single player game. Several ships may be flown at one
    /// device (the companions of a room, as the wingmen of local co-op): what one of them does names that ship, or the
    /// seat of its pilot, so it goes out for the right pilot.
    /// </summary>
    public interface IFieldLink
    {
        /// <summary>This client simulates the world; the others show its bodies as puppets.</summary>
        bool Simulates { get; }

        /// <summary>A body entered the field. Its position and motion may still be set in the same frame.</summary>
        void BodyAdded(SpaceBody body);

        /// <summary>A body left the field; <see cref="SpaceBody.Exit"/> says why.</summary>
        void BodyRemoved(SpaceBody body);

        /// <summary>The simulator's field set off a blast: the other ships have to feel it too.</summary>
        void BlastSetOff(Blast blast);

        /// <summary>A shot or blast of a local ship hit a puppet (<see cref="DamageInfo.Seat"/> names a wingman's pilot).</summary>
        void PuppetHit(Shootable puppet, DamageInfo hit);

        /// <summary>The local ship <paramref name="ship"/> flew into a puppet (<paramref name="direction"/> points from the ship to it).</summary>
        void PuppetRammed(Shootable puppet, AsteroidsPlayer ship, Vector2 direction, bool dashing);

        /// <summary>A puppet shot stopped against a local ship.</summary>
        void PuppetShotAbsorbed(Shot puppet);

        /// <summary>The local ship <paramref name="ship"/> touched a pickup: the server gives it to the first pilot who claims it.</summary>
        void RewardTouched(Reward reward, AsteroidsPlayer ship);

        /// <summary>
        /// A local ship or one of its drones fired (the shot's <see cref="Shot.FiredBy"/>); <paramref name="kind"/> is the
        /// weapon, or <see cref="BodyCodec.DroneShot"/>.
        /// </summary>
        void ShotFired(Shot shot, byte kind, int level);

        /// <summary>A local ship set off a nova bomb: the device's own (<paramref name="seat"/> null) or a wingman, by its pilot's seat.</summary>
        void NovaFired(Vector2 center, float damage, float bossDamage, int? seat);

        /// <summary>A new local ship arrived at <paramref name="center"/> and needs a little room (<paramref name="seat"/> as for a nova).</summary>
        void RoomWanted(Vector2 center, int? seat);

        /// <summary>The simulator's strike scroll: how far the ground has scrolled and how fast it scrolls now.</summary>
        void ScrollReported(float distance, float speed);
    }
}
