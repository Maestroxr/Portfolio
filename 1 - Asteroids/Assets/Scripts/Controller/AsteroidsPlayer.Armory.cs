using UnityEngine;
using static Portfolio.Asteroids.AsteroidsText;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The armory's part of the ship: the Aegis Barrier of the asteroid field, and the equipment of the strike pilot's
    /// loadout: the armor plating (taken off every strike hit, see TakeStrikeDamage), the afterburner's speed, the repair
    /// drone, the emergency repair kits, the wingman drones (the Wing Drones' models, firing strike rounds, see
    /// <see cref="Drone"/>) and the salvage magnet (see <see cref="Reward"/>). The stand-ins of other pilots show the
    /// drones and the magnet from the pose's bits; everything else happens on the pilot's own device.
    /// </summary>
    public partial class AsteroidsPlayer
    {
        private static readonly Color RepairTint = new Color(0.4f, 1f, 0.5f);

        private float repairClock;

        private StrikeLoadout StrikeKit => Strike != null ? Strike.Loadout : null;

        /// <summary>Strike: the multiplier of the ship's top speed (the afterburner's); 1 otherwise.</summary>
        internal float StrikeSpeedFactor => Mode == MissionMode.Strike && StrikeKit != null && StrikeKit.Owns(StrikeItem.Afterburner) ? StrikeArmory.AfterburnerSpeed : 1f;

        /// <summary>Strike: the pilot carries the salvage magnet (pickups drift to the ship from far away; the pose's magnet bit).</summary>
        public bool HasSalvageMagnet => Mode == MissionMode.Strike && StrikeKit != null && StrikeKit.Owns(StrikeItem.SalvageMagnet);

        /// <summary>Strike: the pilot flies with the wingman drones (the pose's drones bit).</summary>
        public bool HasStrikeDrones => Mode == MissionMode.Strike && StrikeKit != null && StrikeKit.Owns(StrikeItem.WingmanDrones);


        /// <summary>
        /// The asteroid field's Aegis Barrier: while it lasts, the shield (kept full) takes every hit and the hull none.
        /// True when it took <paramref name="hit"/>.
        /// </summary>
        private bool AbsorbedByAegis(DamageInfo hit, bool continuous)
        {
            if (!IsPowerUpActive(PowerUpType.Aegis))
            {
                return false;
            }
            Shield = MaxShield;
            visuals?.ShieldHit(hit.Direction);
            Field?.Sounds?.ShieldHit(false);
            hurtCooldown = continuous ? 0f : 0.45f;
            return true;
        }


        /// <summary>
        /// One frame of the strike equipment while the mission runs: the repair drone's energy, a repair kit used when the
        /// energy runs low, and the wingman drones (out while owned).
        /// </summary>
        private void TickStrikeEquipment(StrikeGunnery gunnery, float deltaTime)
        {
            StrikeLoadout loadout = gunnery.Loadout;
            if (loadout == null || !IsAlive)
            {
                return;
            }
            AsteroidsGameManager manager = Asteroids;
            bool running = manager == null || manager.IsMissionActive;
            if (running && loadout.Owns(StrikeItem.RepairDrone) && loadout.Energy < StrikeRules.MaxEnergy && (Field == null || !IsBossDying(Field)))
            {
                repairClock += deltaTime;
                if (repairClock >= StrikeArmory.RepairDroneInterval)
                {
                    repairClock -= StrikeArmory.RepairDroneInterval;
                    loadout.AddEnergy(1f);
                }
            }
            else
            {
                repairClock = 0f;
            }
            if (running && loadout.WantsRepairKit && loadout.UseRepairKit())
            {
                Field?.Effects?.Pickup(Position, RepairTint);
                Field?.Effects?.Popup(Position + Vector2.up * 1.2f, F("REPAIR KIT  +{0}", StrikeArmory.RepairKitEnergy), RepairTint);
                Field?.Sounds?.Repair();
            }
            bool wingmen = loadout.Owns(StrikeItem.WingmanDrones);
            SetDrones(wingmen);
            if (!wingmen)
            {
                return;
            }
            foreach (Drone drone in drones)
            {
                if (drone != null && drone.isActiveAndEnabled)
                {
                    drone.Tick(this, deltaTime);
                }
            }
        }
    }
}
