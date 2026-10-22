using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The status a shared screen publishes for the seat of a remote control it flies (the base server's remote status, in
    /// the <c>key=value;key=value</c> form of room options), and the rows the remote panel shows of it: in an asteroid
    /// field mission the ships left, the score, the weapon and its level, the nova bombs, the hull and the shield; in a
    /// planet strike the money earned, the energy, the phase shields, the megabombs and the special weapon. A strike
    /// status also carries the whole loadout (<see cref="StrikeLoadoutText"/>), which the remote's device takes over when
    /// its player switches to playing on it mid-mission.
    /// </summary>
    public static class RemoteSeatStatus
    {
        public const string Ships = "ships";
        public const string Score = "score";
        public const string Weapon = "weapon";
        public const string Level = "level";
        public const string Novas = "novas";
        public const string Hull = "hull";
        public const string Shield = "shield";
        public const string Money = "money";
        public const string Energy = "energy";
        public const string Shields = "shields";
        public const string Megabombs = "megabombs";
        public const string Special = "special";
        public const string Flying = "flying";
        public const string Loadout = "loadout";

        private static readonly string[] FieldKeys = { Ships, Score, Weapon, Level, Novas, Hull, Shield };
        private static readonly string[] StrikeKeys = { Money, Energy, Shields, Megabombs, Special };

        private static readonly Dictionary<string, string> labels = new Dictionary<string, string>
        {
            { Ships, "Ships" },
            { Score, "Score" },
            { Weapon, "Weapon" },
            { Level, "Level" },
            { Novas, "Novas" },
            { Hull, "Hull" },
            { Shield, "Shield" },
            { Money, "Money" },
            { Energy, "Energy" },
            { Shields, "Phase shields" },
            { Megabombs, "Megabombs" },
            { Special, "Special" }
        };

        /// <summary>The keys the remote panel shows as rows, in order, for a mission of <paramref name="mode"/>.</summary>
        public static string[] PanelKeys(MissionMode mode)
        {
            return mode == MissionMode.Strike ? StrikeKeys : FieldKeys;
        }

        /// <summary>The English label of every key (the panel translates them through the game's table).</summary>
        public static IReadOnlyDictionary<string, string> Labels => labels;

        /// <summary>The status of a seat in an asteroid field mission.</summary>
        public static string WriteField(int lives, long score, WeaponType weapon, int level, int bombs, float hull, float shield, bool flying)
        {
            return RoomOptions.Write(Ships, lives, Score, score.ToString("N0", CultureInfo.InvariantCulture), Weapon, WeaponRules.ShortTitle(weapon),
                Level, level, Novas, bombs, Hull, Percent(hull), Shield, Percent(shield), Flying, flying ? 1 : 0);
        }

        /// <summary>The status of a seat in a planet strike mission (<paramref name="special"/> is the name of the special weapon, "-" for none).</summary>
        public static string WriteStrike(long money, float energy, int phaseShields, int megabombs, string special, bool flying, string loadout)
        {
            return RoomOptions.Write(Money, money.ToString("N0", CultureInfo.InvariantCulture), Energy, Mathf.RoundToInt(energy), Shields, phaseShields,
                Megabombs, megabombs, Special, string.IsNullOrEmpty(special) ? "-" : special, Flying, flying ? 1 : 0, Loadout, loadout ?? "");
        }

        /// <summary>Whether a status says the seat's ship still flies.</summary>
        public static bool IsFlying(string status)
        {
            return RoomOptions.Number(status, Flying, 1) != 0;
        }

        /// <summary>
        /// A won strike mission ends on a remote control's device: its saved <paramref name="pilot"/> takes over the kit
        /// the shared screen flew the seat with (<paramref name="flown"/>, the loadout of the last status, through the
        /// mission's <paramref name="working"/> copy) and the money <paramref name="earned"/>, as the device's own ship
        /// would land; the pilot's own difficulty stays (the room's was for that room only). Without a known kit only
        /// the money is banked, as for a party's wingman.
        /// </summary>
        public static void BankFlownSeat(StrikeLoadout pilot, StrikeLoadout working, StrikeLoadout flown, int earned)
        {
            if (pilot == null)
            {
                return;
            }
            StrikeDifficulty own = pilot.Difficulty;
            bool kit = flown != null && working != null;
            if (kit)
            {
                working.CopyFrom(flown);
                working.Difficulty = own;
            }
            pilot.CommitMission(kit ? working : null, earned);
            pilot.Difficulty = own;
        }

        private static string Percent(float fraction)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(fraction) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }
    }


    /// <summary>
    /// A strike loadout as one line of text, to travel between a remote control's device and the shared screen that
    /// flies its seat: <c>m:money|e:energy|s:shields|p:points|b:megabombs|x:special|d:difficulty|i:item.count,item.count</c>
    /// (no <c>=</c> or <c>;</c>, so it fits inside a room option value). A remote sends its loadout as a room action
    /// when the mission starts (the screen flies the seat with it) and the screen sends it back in the seat's status.
    /// </summary>
    public static class StrikeLoadoutText
    {
        private const char Separator = '|';

        /// <summary>The line of <paramref name="loadout"/>; "" for null.</summary>
        public static string Write(StrikeLoadout loadout)
        {
            if (loadout == null)
            {
                return "";
            }
            var items = new StringBuilder();
            for (int i = 0; i < StrikeArmory.ItemCount; i++)
            {
                int count = loadout.Count((StrikeItem)i);
                if (count <= 0)
                {
                    continue;
                }
                items.Append(items.Length > 0 ? "," : "").Append(i).Append('.').Append(count);
            }
            CultureInfo c = CultureInfo.InvariantCulture;
            return string.Join(Separator.ToString(),
                "m:" + loadout.Money.ToString(c),
                "e:" + loadout.Energy.ToString("R", c),
                "s:" + loadout.PhaseShields.ToString(c),
                "p:" + loadout.ShieldPoints.ToString("R", c),
                "b:" + loadout.Megabombs.ToString(c),
                "x:" + ((int)loadout.Special).ToString(c),
                "d:" + ((int)loadout.Difficulty).ToString(c),
                "i:" + items);
        }

        /// <summary>The loadout of a line written by <see cref="Write"/>, put back inside the rules; null for a line that is none.</summary>
        public static StrikeLoadout Parse(string text)
        {
            if (string.IsNullOrEmpty(text) || !text.StartsWith("m:", System.StringComparison.Ordinal))
            {
                return null;
            }
            CultureInfo c = CultureInfo.InvariantCulture;
            var loadout = new StrikeLoadout();
            foreach (string part in text.Split(Separator))
            {
                if (part.Length < 2 || part[1] != ':')
                {
                    continue;
                }
                string value = part.Substring(2);
                switch (part[0])
                {
                    case 'm':
                        loadout.Money = Int(value, c);
                        break;
                    case 'e':
                        loadout.Energy = Float(value, c);
                        break;
                    case 's':
                        loadout.PhaseShields = Int(value, c);
                        break;
                    case 'p':
                        loadout.ShieldPoints = Float(value, c);
                        break;
                    case 'b':
                        loadout.Megabombs = Int(value, c);
                        break;
                    case 'x':
                        loadout.Special = (StrikeItem)Int(value, c);
                        break;
                    case 'd':
                        loadout.Difficulty = (StrikeDifficulty)Int(value, c);
                        break;
                    case 'i':
                        foreach (string item in value.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
                        {
                            int dot = item.IndexOf('.');
                            if (dot <= 0)
                            {
                                continue;
                            }
                            int index = Int(item.Substring(0, dot), c);
                            if (index >= 0 && index < StrikeArmory.ItemCount)
                            {
                                loadout.SetCount((StrikeItem)index, Int(item.Substring(dot + 1), c));
                            }
                        }
                        break;
                }
            }
            loadout.Normalise();
            return loadout;
        }

        private static int Int(string text, CultureInfo culture)
        {
            return int.TryParse(text, NumberStyles.Integer, culture, out int value) ? value : 0;
        }

        private static float Float(string text, CultureInfo culture)
        {
            return float.TryParse(text, NumberStyles.Float, culture, out float value) ? value : 0f;
        }
    }


    /// <summary>
    /// What a remote control's pad sends for a ship flown by the shared screen, made from an <see cref="IShipInput"/> (the
    /// autopilot of a tour flying the remote's own stand-in): the stick and the bits of the actions of
    /// <see cref="LocalCoopRules.Scheme"/>, as <see cref="OnlineGameController.SetPadState"/> takes them. The screen reads
    /// them back through <see cref="LocalShipInput"/>: the stick's x turns (left is a positive turn), its y thrusts or
    /// brakes; in a strike mission the stick is the move itself.
    /// </summary>
    public static class RemotePadPacket
    {
        /// <summary>Packs <paramref name="input"/> as read this frame.</summary>
        public static void From(IShipInput input, bool strike, out Vector2 stick, out uint held, out uint pressed)
        {
            stick = Vector2.zero;
            held = 0;
            pressed = 0;
            if (input == null)
            {
                return;
            }
            if (strike)
            {
                stick = Vector2.ClampMagnitude(new Vector2(Mathf.Clamp(input.Move.x, -1f, 1f), Mathf.Clamp(input.Move.y, -1f, 1f)), 1f);
            }
            else
            {
                float y = input.Thrust > 0f ? Mathf.Clamp01(input.Thrust) : input.Brake ? -1f : 0f;
                stick = Vector2.ClampMagnitude(new Vector2(-Mathf.Clamp(input.Turn, -1f, 1f), y), 1f);
            }
            if (input.Fire)
            {
                held |= Bit(LocalCoopRules.Fire);
            }
            if (input.DashPressed)
            {
                pressed |= Bit(LocalCoopRules.Dash);
            }
            if (input.BombPressed)
            {
                pressed |= Bit(LocalCoopRules.Bomb);
            }
            if (input.CyclePressed)
            {
                pressed |= Bit(LocalCoopRules.Cycle);
            }
        }

        /// <summary>The bit of the action <paramref name="id"/> of the pilots' scheme in a packet.</summary>
        public static uint Bit(string id)
        {
            return RemoteActionSource.Bit(LocalCoopRules.Scheme.IndexOf(id));
        }
    }
}
