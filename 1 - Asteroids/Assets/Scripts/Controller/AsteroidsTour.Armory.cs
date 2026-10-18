using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The armory's steps of the tour:
    /// <list type="bullet">
    /// <item><c>armory</c>: the Supply Room with money (the tour's, else 6,000,000): both pages before and after buying one
    /// of every armory item (three armor plates and repair kits), the details of a few new items, and the sell list.</item>
    /// <item><c>kit:&lt;item&gt;[+&lt;item&gt;...][:&lt;n&gt;]</c>: the saved pilot gets the items (a special is selected; equipment
    /// and kits as many as they hold) and flies strike mission n (default 1), e.g. <c>kit:RailGun+WingmanDrones:2</c>.</item>
    /// <item><c>weapon:&lt;type&gt;[:&lt;level&gt;[:&lt;index&gt;]]</c>: the field mission at campaign index (default 0) flown for
    /// 20 seconds with that weapon at that level (default 4), e.g. <c>weapon:Arc:4</c>.</item>
    /// <item><c>power:&lt;type&gt;[:&lt;index&gt;]</c>: the same with a power-up switched on (and the weapon it came with).</item>
    /// <item><c>showcase</c>: the first field mission, cleared, with a cluster of rocks in front of the still ship for each
    /// of the four new weapons at level 4: one volley, pictured as its trick plays (the lightning, the slug, the burst, the
    /// wave).</item>
    /// </list>
    /// Item, weapon and power-up names are the enum names, any case. The pilot and the progress are put back when the tour
    /// ends, like every other step's.
    /// </summary>
    public partial class AsteroidsTour
    {
        private const int ArmoryMoney = 6000000;


        private IEnumerator Armory()
        {
            yield return ToMenu();
            manager.SelectMode(MissionMode.Strike);
            yield return Wait(0.5f);
            manager.Pilot.Money = money >= 0 ? money : ArmoryMoney;
            manager.Pilot.NotifyChanged();
            manager.OpenSupply();
            yield return Wait(1f);
            StrikeUI screens = manager.StrikeScreens;
            if (screens == null || !screens.IsSupplyOpen)
            {
                Note("FAIL the Supply Room did not open");
                yield break;
            }
            screens.SetSelling(false);
            screens.SetCategory(StrikeUI.SupplyCategory.Weapons);
            yield return Wait(0.6f);
            yield return Shot("armory_weapons");
            screens.SetCategory(StrikeUI.SupplyCategory.Gear);
            yield return Wait(0.6f);
            yield return Shot("armory_gear");

            for (int value = (int)StrikeItem.FlakCannon; value < StrikeArmory.ItemCount; value++)
            {
                var item = (StrikeItem)value;
                int copies = Math.Max(1, StrikeArmory.Info(item).Max <= 3 ? StrikeArmory.Info(item).Max : 1);
                for (int i = 0; i < copies; i++)
                {
                    int before = manager.Pilot.Money;
                    BuyResult expected = StrikeArmory.CanBuy(manager.Pilot, item);
                    manager.BuyItem(item);
                    Note($"buy {item}: expected {expected}, money {before} -> {manager.Pilot.Money}, owned {manager.Pilot.Count(item)}");
                }
                yield return Wait(0.15f);
            }
            foreach (StrikeItem item in new[] { StrikeItem.RailGun, StrikeItem.ArcLightning, StrikeItem.WingmanDrones })
            {
                manager.Pilot.NotifyChanged();
                screens.ShowSupply(manager.Pilot, item);
                yield return Wait(0.5f);
                yield return Shot($"armory_{item}");
            }
            screens.SetCategory(StrikeUI.SupplyCategory.Weapons);
            yield return Wait(0.5f);
            yield return Shot("armory_weapons_bought");
            screens.SetCategory(StrikeUI.SupplyCategory.Gear);
            yield return Wait(0.5f);
            yield return Shot("armory_gear_bought");
            screens.SetSelling(true);
            yield return Wait(0.5f);
            yield return Shot("armory_gear_sell");
            screens.SetCategory(StrikeUI.SupplyCategory.Weapons);
            yield return Wait(0.5f);
            yield return Shot("armory_weapons_sell");
            screens.SetSelling(false);
            Note($"armory: pilot {Describe(manager.Pilot)}, specials [{string.Join(", ", manager.Pilot.OwnedSpecials())}]");
            manager.CloseSupply();
            yield return Wait(0.5f);
        }


        /// <summary>The saved pilot gets <paramref name="items"/> ("+" between them) and flies strike mission <paramref name="number"/>.</summary>
        private IEnumerator Kit(string items, int number)
        {
            yield return ToMenu();
            StrikeLoadout pilot = manager.Pilot;
            StrikeItem special = StrikeItem.MachineGun;
            var kit = new HashSet<StrikeItem>();
            foreach (string name in items.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!Enum.TryParse(name.Trim(), true, out StrikeItem item) || !Enum.IsDefined(typeof(StrikeItem), item))
                {
                    Note($"kit: no item named {name}");
                    continue;
                }
                kit.Add(item);
                int max = Math.Max(1, StrikeArmory.Info(item).Max);
                int count = StrikeWeaponRules.IsSpecial(item) ? 1 : Math.Min(max, 3);
                pilot.SetCount(item, Math.Max(pilot.Count(item), count));
                if (StrikeWeaponRules.IsSpecial(item) && special == StrikeItem.MachineGun)
                {
                    special = item;
                }
            }
            if (special != StrikeItem.MachineGun)
            {
                // Only the kit's specials: the autopilot would otherwise cycle to an older one for the layer it wants.
                foreach (StrikeItem other in StrikeWeaponRules.Specials)
                {
                    if (!kit.Contains(other))
                    {
                        pilot.SetCount(other, 0);
                    }
                }
                pilot.Special = special;
            }
            pilot.Energy = StrikeRules.MaxEnergy;
            pilot.NotifyChanged();
            manager.Progress?.SavePilot(pilot);
            Note($"kit {items}: pilot {Describe(pilot)}");
            yield return Strike(number, false);
        }


        /// <summary>The field mission at <paramref name="index"/> flown for a while with <paramref name="type"/> at <paramref name="level"/>.</summary>
        private IEnumerator Weapon(WeaponType type, int level, int index, PowerUpType? power)
        {
            AsteroidsLevel mission = manager.AsteroidsCampaign != null ? manager.AsteroidsCampaign.Mission(index) : null;
            if (mission == null || mission.Mode != MissionMode.Field)
            {
                Note($"no field mission at {index}");
                yield break;
            }
            yield return ToMenu();
            if (!StartMission(index))
            {
                yield break;
            }
            yield return WaitUntil(() => manager.IsMissionActive || Ended, 12f, "mission start");
            AsteroidsPlayer ship = manager.Ship;
            ship.Weapons.Set(type, level);
            if (power.HasValue)
            {
                ship.ActivatePowerUp(power.Value, PowerUps.Duration(power.Value) * 2f);
            }
            SetAutopilot(true, false);
            string name = power.HasValue ? $"power_{power.Value}" : $"weapon_{type}{level}";
            Note($"{name}: {mission.Title}, weapon {ship.Weapons.Type} {ship.Weapons.Level}");
            float started = Time.unscaledTime;
            float[] moments = { 3f, 7f, 12f };
            int taken = 0;
            while (!Ended && Time.unscaledTime - started < 20f)
            {
                Time.timeScale = speed;
                if (ship.IsAlive && ship.Weapons.Type != type)
                {
                    // A crate picked up on the way switched weapons: the step is about this one.
                    ship.Weapons.Set(type, level);
                }
                if (taken < moments.Length && Time.unscaledTime - started > moments[taken])
                {
                    taken++;
                    yield return Shot($"{name}_{taken}");
                }
                yield return null;
            }
            Time.timeScale = 1f;
            SetAutopilot(false, false);
            Note($"{name} after 20 s: score {manager.Scoring.Score}, lives {manager.Lives}, kills {manager.Scoring.Kills}, " +
                 $"accuracy {manager.Scoring.Accuracy:0.00}, state {manager.State.BaseState}");
            manager.ReturnToMissionSelect();
            yield return Wait(1f);
        }


        /// <summary>The four new field weapons, each fired once at level 4 into a cluster of rocks and pictured as its trick plays.</summary>
        private IEnumerator Showcase()
        {
            yield return ToMenu();
            if (!StartMission(0))
            {
                yield break;
            }
            yield return WaitUntil(() => manager.IsMissionActive || Ended, 12f, "mission start");
            SetAutopilot(false, false);
            // The GO! of the countdown goes first.
            yield return Wait(2.5f);
            AsteroidsPlayer ship = manager.Ship;
            SpaceField field = manager.Field;
            var barrels = new List<Barrel>();
            (WeaponType type, float delay)[] shows = { (WeaponType.Arc, 0.24f), (WeaponType.Rail, 0.11f), (WeaponType.Flak, 0.4f), (WeaponType.Wave, 0.55f) };
            foreach ((WeaponType type, float delay) in shows)
            {
                field.Clear();
                ship.Position = new Vector2(0f, -7f);
                ship.transform.rotation = Quaternion.identity;
                ship.Simulation.Stop();
                ship.Weapons.Set(type, WeaponRules.MaxLevel);
                // A loose cluster of small rocks ahead of the gun, and a few to the sides for the lightning to jump to.
                Vector2[] rocks =
                {
                    new Vector2(0f, 0f), new Vector2(-1.6f, 1.2f), new Vector2(1.7f, 1.4f), new Vector2(-0.4f, 3f), new Vector2(1.2f, 4.4f),
                    new Vector2(-2.6f, 3.6f), new Vector2(3.4f, 2.6f), new Vector2(0.5f, 6.2f), new Vector2(-3.8f, 0.8f)
                };
                foreach (Vector2 at in rocks)
                {
                    Asteroid rock = manager.spawner.SpawnAsteroid(AsteroidKind.Rock, AsteroidSize.Small, at, Vector2.zero, false);
                    if (rock != null)
                    {
                        rock.Velocity = Vector2.zero;
                    }
                }
                yield return Wait(0.4f);
                WeaponRules.Volley(type, WeaponRules.MaxLevel, barrels);
                Vector2 gun = ship.Position + new Vector2(0f, 0.8f);
                foreach (Barrel barrel in barrels)
                {
                    Vector2 direction = Quaternion.Euler(0f, 0f, -barrel.Angle) * Vector2.up;
                    manager.spawner.FirePlayerShot(type, WeaponRules.MaxLevel, gun + Vector2.right * barrel.Offset, direction, Vector2.zero, ship);
                }
                field.Sounds?.Fire(type);
                float until = Time.unscaledTime + delay;
                while (Time.unscaledTime < until)
                {
                    Time.timeScale = 1f;
                    yield return null;
                }
                yield return Shot($"showcase_{type}");
                yield return Wait(0.6f);
                Note($"showcase {type}: {field.CountTargets(target => target is Asteroid)} of {rocks.Length} rocks left");
            }

            // The armory's pickups: the field's new weapon crates and power-ups on one row, the strike items' on the next.
            field.Clear();
            ship.Position = new Vector2(0f, -8f);
            var fieldPickups = new List<Reward>();
            var strikePickups = new List<Reward>();
            foreach (RewardPool pool in manager.spawner.rewardPools)
            {
                Reward prefab = pool != null && pool.PooledPrefab != null ? pool.PooledPrefab.GetComponent<Reward>() : null;
                if (prefab is WeaponReward weapon && weapon.Weapon >= WeaponType.Arc || prefab is PowerUpReward power && power.PowerUp >= PowerUpType.Piercing)
                {
                    fieldPickups.Add(prefab);
                }
                else if (prefab is StrikeReward item && item.Item >= StrikeItem.FlakCannon)
                {
                    strikePickups.Add(prefab);
                }
            }
            for (int i = 0; i < fieldPickups.Count; i++)
            {
                manager.spawner.SpawnReward(fieldPickups[i], new Vector2((i - (fieldPickups.Count - 1) * 0.5f) * 3f, 2.5f), Vector2.zero);
            }
            for (int i = 0; i < strikePickups.Count; i++)
            {
                manager.spawner.SpawnReward(strikePickups[i], new Vector2((i - (strikePickups.Count - 1) * 0.5f) * 3.2f, -2f), Vector2.zero);
            }
            yield return Wait(0.8f);
            yield return Shot("showcase_pickups");
            Note($"showcase pickups: {fieldPickups.Count} field, {strikePickups.Count} strike");
            manager.ReturnToMissionSelect();
            yield return Wait(1f);
        }


        /// <summary>One armory step of the plan (<c>armory</c>, <c>kit</c>, <c>weapon</c>, <c>power</c> or <c>showcase</c>), split at its colons.</summary>
        private IEnumerator ArmoryStep(string[] parts)
        {
            string argument = parts.Length > 1 ? parts[1] : string.Empty;
            switch (parts[0])
            {
                case "armory":
                    yield return Armory();
                    break;
                case "showcase":
                    yield return Showcase();
                    break;
                case "kit":
                    yield return Kit(argument, parts.Length > 2 ? ParseInt(parts[2], 1) : 1);
                    break;
                case "weapon":
                    if (!Enum.TryParse(argument, true, out WeaponType type) || !Enum.IsDefined(typeof(WeaponType), type))
                    {
                        Note($"weapon: no weapon named {argument}");
                        break;
                    }
                    yield return Weapon(type, parts.Length > 2 ? ParseInt(parts[2], WeaponRules.MaxLevel) : WeaponRules.MaxLevel,
                        parts.Length > 3 ? ParseInt(parts[3], 0) : 0, null);
                    break;
                case "power":
                    if (!Enum.TryParse(argument, true, out PowerUpType power) || !Enum.IsDefined(typeof(PowerUpType), power))
                    {
                        Note($"power: no power-up named {argument}");
                        break;
                    }
                    yield return Weapon(WeaponType.Flak, 3, parts.Length > 2 ? ParseInt(parts[2], 0) : 0, power);
                    break;
            }
        }
    }
}
