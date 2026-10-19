using System;
using System.Collections.Generic;
using Gamebox.Economy;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A car the player owns: its loadout (model, upgrade levels, equipment, the health it is in; the mines, rockets
    /// and nitro are the career's, not the car's) and how it has done.
    /// </summary>
    [Serializable]
    public sealed class OwnedCar
    {
        public CarLoadout loadout = new CarLoadout();
        public int races;
        public int kills;

        public CarModel Model => loadout.model;

        public static OwnedCar New(CarModel model)
        {
            return new OwnedCar { loadout = new CarLoadout { model = model, health = 1f } };
        }
    }

    /// <summary>One rival of the ladder as the career has it now: their points, the tier of their car, how well they drive, their colour and record.</summary>
    [Serializable]
    public sealed class RivalState
    {
        public string id;
        public string name;
        public int points;

        /// <summary>1 to <see cref="CareerRules.MaxRivalTier"/>: the car they drive (<see cref="CarSpecs.OfTier"/>).</summary>
        public int tier = 1;

        /// <summary>How well they drive and shoot on the first day, 0 to 1 (they sharpen with the days: <see cref="CareerRules.SkillOn"/>).</summary>
        public float skill = 0.5f;

        /// <summary>Their colour: an index into the theme's driver colours.</summary>
        public int colorIndex;

        public int wins;
        public int races;
        public int kills;

        public CarModel Car => CarSpecs.OfTier(tier);
    }

    /// <summary>
    /// A race on offer today: how hard it is, the track and laps, the three rivals it draws (ids, from the right third of
    /// the ladder), and the seed it is raced with. A Showdown is the race against The Warden.
    /// </summary>
    [Serializable]
    public sealed class RaceOffer
    {
        public Difficulty difficulty;
        public int track;
        public int laps;
        public List<string> rivals = new List<string>();
        public int seed;

        public bool IsShowdown => difficulty == Difficulty.Showdown;

        public int Prize(int place)
        {
            return CareerRules.Prize(difficulty, place);
        }
    }

    /// <summary>
    /// A whole career, as it is saved (JsonUtility through the named YAML saves, so lists and plain fields only): the
    /// driver, the day, the money, the cars and what they carry, the nineteen rivals of the ladder, the record, the
    /// offers of the day and the sabotage bought for the next race. <see cref="Career"/> makes and moves it on.
    /// </summary>
    [Serializable]
    public sealed class CareerState
    {
        /// <summary>The layout of the fields; a save of a later one is refused (2: leagues, guns, specials and the new fittings).</summary>
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;

        /// <summary>The league the career races in (<see cref="LeagueKind"/>); a save from before the leagues reads 0, the first.</summary>
        public int league;

        public string driverName = "";

        /// <summary>The player's colour: an index into the theme's driver colours (no rival shares it).</summary>
        public int colorIndex;

        /// <summary>The day of the season, from 1: one race a day.</summary>
        public int day = 1;

        public Wallet wallet = new Wallet(CareerRules.StartMoney);

        /// <summary>The car the player drives (one of <see cref="garage"/>).</summary>
        public CarModel car = CareerRules.StartCar;

        public List<OwnedCar> garage = new List<OwnedCar>();

        /// <summary>The mines, rockets and nitro the player carries (ids <see cref="CareerShop.MineId"/>, ...).</summary>
        public Inventory consumables = new Inventory();

        public List<RivalState> rivals = new List<RivalState>();

        public int points;
        public int races;
        public int wins;
        public int kills;

        /// <summary>Times the player's car was wrecked.</summary>
        public int wrecks;

        /// <summary>All the money races paid (prizes, bounties, cash picked up).</summary>
        public long earned;

        /// <summary>The best rank reached so far (what opens tracks).</summary>
        public int bestRank = CareerRules.LadderSize;

        public bool champion;

        /// <summary>The day The Warden was beaten, or 0.</summary>
        public int championDay;

        public int showdownsLost;

        /// <summary>What the career's offers, the computer's luck and the races are seeded with.</summary>
        public int seed;

        /// <summary>The races on offer today.</summary>
        public List<RaceOffer> offers = new List<RaceOffer>();

        /// <summary>The offer being raced (an index into <see cref="offers"/>), or -1.</summary>
        public int entered = -1;

        /// <summary>The rival whose car was sabotaged before the next race (an id), or empty.</summary>
        public string sabotage = "";

        /// <summary>How many tracks the career has opened (from the first).</summary>
        public int tracksOpen = CareerRules.StartTracks;

        /// <summary>The car the player drives now (never null in a valid career).</summary>
        public OwnedCar Current
        {
            get
            {
                foreach (OwnedCar owned in garage)
                {
                    if (owned != null && owned.loadout != null && owned.loadout.model == car)
                    {
                        return owned;
                    }
                }
                return garage.Count > 0 ? garage[0] : null;
            }
        }

        public bool HasSabotage => !string.IsNullOrEmpty(sabotage);

        /// <summary>The league of the career: its ladder, its numbers, its champion.</summary>
        public LeagueSpec League => Leagues.Of(league);

        public RivalState Rival(string id)
        {
            foreach (RivalState rival in rivals)
            {
                if (rival.id == id)
                {
                    return rival;
                }
            }
            return null;
        }
    }
}
