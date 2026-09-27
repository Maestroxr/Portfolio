using System.Collections.Generic;
using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The player's progress through the Asteroids campaign: the stars and best scores the base
    /// <see cref="CampaignProgress"/> keeps per mission, plus the ship chosen in the hangar, the endless records and the
    /// pilot of the strike campaign.
    /// </summary>
    public class AsteroidsProgress : CampaignProgress
    {
        private const string ShipKey = "Ship";
        private const string EndlessWaveKey = "EndlessWave";
        private const string EndlessScoreKey = "EndlessScore";

        // The strike pilot, as named values. Energy and shield points are kept in hundredths, so a fraction taken on Rookie
        // survives the round trip; money stays within the wallet cap, which a stored float holds exactly.
        private const string PilotKey = "Strike.Pilot";
        private const string MoneyKey = "Strike.Money";
        private const string EnergyKey = "Strike.Energy";
        private const string PhaseShieldsKey = "Strike.PhaseShields";
        private const string ShieldPointsKey = "Strike.ShieldPoints";
        private const string MegabombsKey = "Strike.Megabombs";
        private const string SpecialKey = "Strike.Special";
        private const string DifficultyKey = "Strike.Difficulty";
        private const string ItemKey = "Strike.Item.";
        private const float Hundredths = 100f;

        public AsteroidsProgress(IStorageStrategy storage, GameType type) : base(storage, type)
        {
        }

        public int SelectedShip
        {
            get => Value(ShipKey);
            set => SetValue(ShipKey, value);
        }

        public int EndlessBestWave => (int)Record(EndlessWaveKey);

        public int EndlessBestScore => (int)Record(EndlessScoreKey);

        /// <summary>Whether a strike pilot has been saved (else <see cref="LoadPilot"/> makes a new one).</summary>
        public bool HasPilot => Value(PilotKey) > 0;

        /// <summary>Keeps the best endless run. Returns true when <paramref name="score"/> is a new record.</summary>
        public bool RecordEndless(int wave, int score)
        {
            RecordMax(EndlessWaveKey, wave);
            return RecordMax(EndlessScoreKey, score);
        }

        /// <summary>
        /// The saved strike pilot (money, energy, phase shields and the points of the one in use, megabombs, the items owned,
        /// the selected special and the difficulty), put back inside the rules; a new pilot when none is saved.
        /// </summary>
        public StrikeLoadout LoadPilot()
        {
            if (!HasPilot)
            {
                return StrikeLoadout.NewPilot();
            }
            var pilot = new StrikeLoadout
            {
                Money = Value(MoneyKey),
                Energy = Value(EnergyKey) / Hundredths,
                PhaseShields = Value(PhaseShieldsKey),
                ShieldPoints = Value(ShieldPointsKey) / Hundredths,
                Megabombs = Value(MegabombsKey),
                Special = (StrikeItem)Value(SpecialKey),
                Difficulty = (StrikeDifficulty)Value(DifficultyKey, (int)StrikeRules.NewPilotDifficulty)
            };
            foreach (StrikeItem item in ItemsKept())
            {
                pilot.SetCount(item, Value(ItemKey + item));
            }
            pilot.Normalise();
            return pilot;
        }

        /// <summary>Saves <paramref name="pilot"/> as the strike pilot (after a won mission, the Supply Room, the debug menu).</summary>
        public void SavePilot(StrikeLoadout pilot)
        {
            if (pilot == null)
            {
                return;
            }
            Put(MoneyKey, System.Math.Max(0, System.Math.Min(StrikeRules.WalletCap, pilot.Money)));
            Put(EnergyKey, (int)System.Math.Round(pilot.Energy * Hundredths));
            Put(PhaseShieldsKey, pilot.PhaseShields);
            Put(ShieldPointsKey, ShieldHundredths(pilot.ShieldPoints));
            Put(MegabombsKey, pilot.Megabombs);
            Put(SpecialKey, (int)pilot.Special);
            Put(DifficultyKey, (int)pilot.Difficulty);
            foreach (StrikeItem item in ItemsKept())
            {
                Put(ItemKey + item, pilot.Count(item));
            }
            Put(PilotKey, 1);
            Persist();
        }

        /// <summary>
        /// The points of the phase shield in use as saved, in hundredths rounded down: a damaged shield never comes back
        /// whole (sellable), and what is left of one never comes back as 0 (which loading reads as a missing shield and
        /// fills up).
        /// </summary>
        internal static int ShieldHundredths(float points)
        {
            if (points <= 0f)
            {
                return 0;
            }
            // The small allowance keeps a float product such as 3332.9999 at 3333.
            return System.Math.Max(1, (int)System.Math.Floor(points * Hundredths + 0.001f));
        }

        /// <summary>Forgets the strike pilot: the next <see cref="LoadPilot"/> is a new pilot.</summary>
        public void ResetPilot()
        {
            Reset(0, null, PilotValues());
        }

        /// <summary>Forgets the strike pilot and the stars and best scores of every strike mission of <paramref name="campaign"/>.</summary>
        public void ResetStrike(AsteroidsCampaign campaign)
        {
            if (campaign != null)
            {
                for (int i = 0; i < campaign.Count; i++)
                {
                    if (campaign.ModeOf(i) == MissionMode.Strike)
                    {
                        Delete($"L{i}.Stars");
                        Delete($"L{i}.Best");
                    }
                }
            }
            ResetPilot();
        }

        /// <summary>Forgets every star, score, record, the hangar choice and the strike pilot.</summary>
        public void ResetAll(int levelCount)
        {
            var values = new List<string> { ShipKey };
            values.AddRange(PilotValues());
            Reset(levelCount, new[] { EndlessWaveKey, EndlessScoreKey }, values);
        }

        /// <summary>Sets the named value <paramref name="name"/> like <see cref="CampaignProgress.SetValue"/>, without writing the store yet.</summary>
        private void Put(string name, int value)
        {
            Set($"Value.{name}", value);
        }

        /// <summary>The items kept as counts of their own (megabombs and phase shields have theirs, the energy module is none).</summary>
        private static IEnumerable<StrikeItem> ItemsKept()
        {
            for (var item = StrikeItem.MachineGun; item <= StrikeItem.IonScanner; item++)
            {
                if (item != StrikeItem.MegaBomb && item != StrikeItem.PhaseShield && item != StrikeItem.EnergyModule)
                {
                    yield return item;
                }
            }
        }

        /// <summary>Every named value of the strike pilot.</summary>
        private static List<string> PilotValues()
        {
            var values = new List<string>
            {
                PilotKey, MoneyKey, EnergyKey, PhaseShieldsKey, ShieldPointsKey, MegabombsKey, SpecialKey, DifficultyKey
            };
            foreach (StrikeItem item in ItemsKept())
            {
                values.Add(ItemKey + item);
            }
            return values;
        }
    }
}
