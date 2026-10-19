using System;
using System.Collections.Generic;
using Gamebox.Racing;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The pickups of a race: placed on the track's pickup spots when the race is built (the spots of one cluster get a
    /// mix of kinds, seeded by the race), collected by any car that drives over one, back a few seconds later. What a
    /// pickup gives is <see cref="PickupEffect"/>'s: cash for the race, a quarter of the car's health, half its rounds or
    /// a nitro charge; the car's driver hears of it (<see cref="CarEventKind.Pickup"/>) with a sound and a floating text.
    /// Online the room decides who got a pickup first: <see cref="Claim"/> asks it, and <see cref="Grant"/> /
    /// <see cref="Take"/> carry out its answer.
    /// </summary>
    public class PickupField : MonoBehaviour
    {
        /// <summary>Spots closer than this to the first of a cluster belong to it.</summary>
        private const float ClusterReach = 12f;

        private readonly List<Pickup> pickups = new List<Pickup>();
        private Race race;

        public IReadOnlyList<Pickup> Pickups => pickups;

        /// <summary>
        /// Asked before a car collects a pickup: false keeps it (online, the claim goes to the room and the answer comes back
        /// through <see cref="Grant"/>). Null collects at once.
        /// </summary>
        public Func<Pickup, Car, bool> Claim { get; set; }

        /// <summary>A car collected a pickup, and what it gave.</summary>
        public event Action<Pickup, Car, PickupEffect> Collected;

        /// <summary>Places the pickups of <paramref name="race"/> under <paramref name="parent"/> (the race's root).</summary>
        public static PickupField Spawn(Race race, Transform parent)
        {
            var field = new GameObject("Pickups").AddComponent<PickupField>();
            field.transform.SetParent(parent, false);
            field.race = race;
            if (race == null || race.Setup == null || !race.Setup.Pickups || race.Track == null)
            {
                return field;
            }
            List<Vector3> spots = SpotsOf(race.Track);
            var random = new System.Random(unchecked(race.Setup.Seed * 31 + 977));
            DeathRaceContent content = race.Effects != null ? race.Effects.Content : null;
            DeathRaceTheme theme = race.Effects != null ? race.Effects.Theme : null;
            foreach (List<Vector3> cluster in Clusters(spots))
            {
                List<PickupKind> kinds = PickupEffect.KindsFor(cluster.Count, random);
                for (int i = 0; i < cluster.Count; i++)
                {
                    field.pickups.Add(Pickup.Create(field, kinds[i], field.pickups.Count, cluster[i], content, theme));
                }
            }
            return field;
        }

        /// <summary>The track's pickup spots; without any, three across the road at each of its pickup distances.</summary>
        private static List<Vector3> SpotsOf(Track track)
        {
            var spots = new List<Vector3>(track.PickupSpots);
            if (spots.Count > 0 || track.Path == null)
            {
                return spots;
            }
            float[] distances = track.Layout != null && track.Layout.PickupDistances != null && track.Layout.PickupDistances.Length > 0
                ? track.Layout.PickupDistances
                : new[] { track.Path.Length * 0.25f, track.Path.Length * 0.5f, track.Path.Length * 0.75f };
            float across = Mathf.Max(1.5f, track.HalfWidth - 3f);
            foreach (float distance in distances)
            {
                Vector2 centre = track.Path.PointAt(distance);
                Vector2 normal = track.Path.NormalAt(distance);
                for (int side = -1; side <= 1; side++)
                {
                    spots.Add(RacePath.ToWorld(centre + normal * (across * side)));
                }
            }
            return spots;
        }

        /// <summary>The spots in groups that lie together, in the order they came.</summary>
        public static List<List<Vector3>> Clusters(IReadOnlyList<Vector3> spots)
        {
            var clusters = new List<List<Vector3>>();
            foreach (Vector3 spot in spots)
            {
                List<Vector3> home = null;
                foreach (List<Vector3> cluster in clusters)
                {
                    Vector3 first = cluster[0];
                    if (new Vector2(spot.x - first.x, spot.z - first.z).sqrMagnitude <= ClusterReach * ClusterReach)
                    {
                        home = cluster;
                        break;
                    }
                }
                if (home == null)
                {
                    home = new List<Vector3>();
                    clusters.Add(home);
                }
                home.Add(spot);
            }
            return clusters;
        }

        /// <summary>A car drove through <paramref name="pickup"/>: it collects it, unless the room has to say first.</summary>
        internal void TryCollect(Pickup pickup, Car car)
        {
            if (pickup == null || !pickup.Available || car == null)
            {
                return;
            }
            if (Claim != null && !Claim(pickup, car))
            {
                return;
            }
            Grant(pickup.Index, car);
        }

        /// <summary>Pickup <paramref name="index"/> goes to <paramref name="car"/>: it gives what it gives and is gone a while.</summary>
        public void Grant(int index, Car car)
        {
            Pickup pickup = index >= 0 && index < pickups.Count ? pickups[index] : null;
            if (pickup == null || !pickup.Available || car == null)
            {
                return;
            }
            pickup.Hide(CombatRules.PickupRespawn);
            CarStats stats = car.Stats;
            PickupEffect effect = PickupEffect.Of(pickup.Kind, race != null && race.Setup != null ? race.Setup.CashPerPickup : 50, car.Health != null ? car.Health.Max : stats.MaxHealth, car.Weapons != null ? car.Weapons.AmmoCapacity : stats.AmmoCapacity);
            Apply(car, effect);
            Vector3 at = pickup.transform.position;
            (race != null && race.Audio != null ? race.Audio : RaceAudio.Current)?.Pickup(pickup.Kind, at);
            race?.Effects?.Sparks(at + Vector3.up, Vector3.up, 0.8f);
            ShowText(at + Vector3.up * 2f, Label(effect), Pickup.ColorOf(pickup.Kind));
            car.Raise(new CarEvent(CarEventKind.Pickup, effect.Amount) { Pickup = pickup.Kind });
            Collected?.Invoke(pickup, car, effect);
        }

        /// <summary>Someone (on another device) took pickup <paramref name="index"/>: it is gone here too.</summary>
        public void Take(int index)
        {
            Pickup pickup = index >= 0 && index < pickups.Count ? pickups[index] : null;
            if (pickup != null && pickup.Available)
            {
                pickup.Hide(CombatRules.PickupRespawn);
            }
        }

        /// <summary>Gives <paramref name="car"/> what <paramref name="effect"/> says.</summary>
        public static void Apply(Car car, PickupEffect effect)
        {
            if (effect.Cash > 0 && car.Racer != null)
            {
                car.Racer.CashPicked += effect.Cash;
            }
            if (effect.Repair > 0f && car.Health != null)
            {
                car.Health.Repair(effect.Repair);
            }
            if (effect.Ammo > 0 && car.Weapons != null)
            {
                car.Weapons.AddAmmo(effect.Ammo);
            }
            if (effect.Nitro > 0 && car.Weapons != null)
            {
                car.Weapons.AddNitro(effect.Nitro);
            }
        }

        /// <summary>The floating words of a pickup: "+$50", "+25% REPAIR", "+AMMO", "+NITRO".</summary>
        public static string Label(PickupEffect effect)
        {
            switch (effect.Kind)
            {
                case PickupKind.Cash: return L.F("+${0}", effect.Cash);
                case PickupKind.Repair: return L.F("+{0}% REPAIR", Mathf.RoundToInt(CombatRules.RepairShare * 100f));
                case PickupKind.Ammo: return L.T("+AMMO");
                default: return L.T("+NITRO");
            }
        }

        /// <summary>
        /// Where the floating words are drawn among the canvases: over the race HUD, under the shared menu (100), so the
        /// pause menu covers words frozen in mid air.
        /// </summary>
        private const int TextSortingOrder = 60;

        /// <summary>Shows <paramref name="text"/> rising from <paramref name="at"/> in every view of the race.</summary>
        private void ShowText(Vector3 at, string text, Color color)
        {
            RaceEffects effects = race != null ? race.Effects : null;
            // Behind the main menu (attract mode) the pickups are scenery: no words over the menu.
            if (effects == null || string.IsNullOrEmpty(text) || race.Setup == null || race.Setup.Mode == RaceMode.Attract)
            {
                return;
            }
            foreach (RaceCamera view in effects.Views())
            {
                if (view != null && view.Camera != null)
                {
                    FloatingTexts.For(view.Camera, TextSortingOrder).Show(at, text, color, 1.3f);
                }
            }
        }
    }
}
