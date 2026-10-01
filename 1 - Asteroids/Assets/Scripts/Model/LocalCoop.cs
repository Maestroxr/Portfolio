using System;
using System.Collections.Generic;
using System.Globalization;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The rules of a mission flown by two to four pilots at one device (local co-op): the actions every pilot binds
    /// controls to (<see cref="Scheme"/>), the setup the shared local play screen shows (<see cref="AsteroidsLocalRules"/>)
    /// and what its options mean. The pilots fly the mission selected on the title together, each in a ship of their own
    /// on the one screen, like the pilots of an online room.
    /// </summary>
    public static class LocalCoopRules
    {
        public const string Left = "left";
        public const string Right = "right";
        public const string Thrust = "thrust";
        public const string Brake = "brake";
        public const string Fire = "fire";
        public const string Dash = "dash";
        public const string Bomb = "bomb";
        public const string Cycle = "cycle";

        /// <summary>The option of an asteroid field mission: the ships of every pilot.</summary>
        public const string LivesKey = "lives";

        /// <summary>The option of a planet strike mission: the difficulty it is flown at.</summary>
        public const string DifficultyKey = "difficulty";

        /// <summary>Ships per pilot a first setup starts on (an index into <see cref="CoopRules.LivesChoices"/>: 3).</summary>
        public const int DefaultLivesChoice = 2;

        private static ControlScheme scheme;

        /// <summary>
        /// The actions of a pilot, one scheme for both kinds of mission: turn, thrust and brake (in a strike mission the
        /// four of them fly the ship in eight directions), fire, dash, the bomb (the nova bomb, the megabomb in strike) and
        /// the next special weapon of a strike pilot. The roles give every seat the shared defaults.
        /// </summary>
        public static ControlScheme Scheme => scheme ??= new ControlScheme()
            .Add(Left, "Turn Left", ControlRole.Left)
            .Add(Right, "Turn Right", ControlRole.Right)
            .Add(Thrust, "Thrust", ControlRole.Up)
            .Add(Brake, "Brake", ControlRole.Down)
            .Add(Fire, "Fire", ControlRole.Primary)
            .Add(Dash, "Dash", ControlRole.Secondary)
            .Add(Bomb, "Bomb", ControlRole.Tertiary)
            .Add(Cycle, "Next Special", ControlRole.Quaternary);

        /// <summary>The ships every pilot of an asteroid field mission gets, as the setup's option says.</summary>
        public static int Lives(LocalMatch match)
        {
            int choice = match != null ? match.Option(LivesKey) : -1;
            string value = choice >= 0 && choice < CoopRules.LivesChoices.Length ? CoopRules.LivesChoices[choice] : CoopRules.LivesChoices[DefaultLivesChoice];
            return Mathf.Clamp(int.Parse(value, CultureInfo.InvariantCulture), CoopRules.MinLives, CoopRules.MaxLives);
        }

        /// <summary>The difficulty of a planet strike mission, as the setup's option says (Veteran without one).</summary>
        public static StrikeDifficulty Difficulty(LocalMatch match)
        {
            int choice = match != null ? match.Option(DifficultyKey) : -1;
            return choice >= (int)StrikeDifficulty.Rookie && choice <= (int)StrikeDifficulty.Elite ? (StrikeDifficulty)choice : CoopRules.DefaultDifficulty;
        }
    }


    /// <summary>
    /// What Asteroids offers for local play: two to four pilots playing at once, each with controls of their own
    /// (<see cref="LocalCoopRules.Scheme"/>), no computer pilots, the seat colours of the online rooms (so the chips of the
    /// setup, the halos of the ships and the lines of the HUD agree), and the option of the kind of mission selected on
    /// the title (<see cref="ForMode"/>): the ships per pilot of an asteroid field mission, or the difficulty of a planet
    /// strike.
    /// </summary>
    public sealed class AsteroidsLocalRules : LocalPlayRules
    {
        private readonly LocalOption lives = new LocalOption(LocalCoopRules.LivesKey, "Ships per pilot", CoopRules.LivesChoices, LocalCoopRules.DefaultLivesChoice);
        private readonly LocalOption difficulty = new LocalOption(LocalCoopRules.DifficultyKey, "Strike difficulty", CoopRules.DifficultyLabels, (int)CoopRules.DefaultDifficulty);

        public AsteroidsLocalRules()
        {
            Game = GameType.Asteroids;
            Title = "Local Co-op";
            MinPlayers = CoopRules.MinPilots;
            MaxPlayers = CoopRules.MaxPilots;
            DefaultPlayers = 2;
            Simultaneous = true;
            Controls = LocalCoopRules.Scheme;
            AllowComputers = false;
            SeatColors = new Color[CoopRules.MaxPilots];
            for (int seat = 0; seat < SeatColors.Length; seat++)
            {
                SeatColors[seat] = CoopRules.SeatColor(seat);
            }
            ForMode(MissionMode.Field);
        }

        /// <summary>The kind of mission the setup is for.</summary>
        public MissionMode Mode { get; private set; }

        /// <summary>Why the selected mission cannot be flown now (in English), or null; asked when the pilots press Start.</summary>
        public Func<string> MissionProblem { get; set; }

        /// <summary>The setup is for the kind of mission selected on the title: its hint and its one option.</summary>
        public void ForMode(MissionMode mode)
        {
            Mode = mode;
            Options.Clear();
            Options.Add(mode == MissionMode.Strike ? difficulty : lives);
            Hint = mode == MissionMode.Strike
                ? "Fly the selected planet strike mission together, a ship each on this screen."
                : "Fly the selected asteroid field mission together, a ship each on this screen.";
        }

        public override string DefaultName(int seat)
        {
            return Loc.F(GameType.Asteroids, "Pilot {0}", seat + 1);
        }

        public override string Problem(LocalMatch match)
        {
            return MissionProblem?.Invoke();
        }
    }


    /// <summary>One pilot of a local co-op mission: the seat, the score (or the money of a strike mission) and the ships.</summary>
    public sealed class SquadPilot
    {
        public SquadPilot(int seat, string name, Color color, int lives)
        {
            Seat = seat;
            Name = name ?? string.Empty;
            Color = color;
            Lives = Mathf.Max(1, lives);
        }

        public int Seat { get; }

        public string Name { get; }

        public Color Color { get; }

        /// <summary>The pilot's own score, kills, combo and accuracy; the money of a strike mission.</summary>
        public ScoreKeeper Score { get; } = new ScoreKeeper();

        /// <summary>Ships left, the one flying included.</summary>
        public int Lives { get; private set; }

        public int LivesLost { get; private set; }

        /// <summary>Out of ships: the pilot's ship is gone for the rest of the mission.</summary>
        public bool Out { get; private set; }

        /// <summary>
        /// A ship of the pilot was destroyed: one ship less and the combo is gone. True when the pilot has another ship
        /// to come back in; false when the pilot is out (no ships left, or <paramref name="oneShip"/>: a strike pilot has one).
        /// </summary>
        public bool LoseShip(bool oneShip)
        {
            if (Out)
            {
                return false;
            }
            Lives = Mathf.Max(0, Lives - 1);
            LivesLost++;
            Score.BreakCombo();
            if (Lives <= 0 || oneShip)
            {
                Out = true;
            }
            return !Out;
        }

        /// <summary>A pickup gave the pilot another ship, up to <paramref name="limit"/>.</summary>
        public void AddLife(int limit)
        {
            if (!Out)
            {
                Lives = Mathf.Min(limit, Lives + 1);
            }
        }
    }


    /// <summary>
    /// The pilots of a local co-op mission and how it goes for them: every pilot has a score and ships of their own (like
    /// the pilots of an online room), a pilot out of ships is out, and the mission is lost when every pilot is out. The
    /// standings rank the pilots by score as the base server ranks a room (<see cref="Standings"/>).
    /// </summary>
    public sealed class LocalSquad
    {
        private readonly List<SquadPilot> pilots = new List<SquadPilot>();

        /// <summary>A squad of the playing seats of <paramref name="seats"/>, with <paramref name="lives"/> ships each.</summary>
        public LocalSquad(IReadOnlyList<LocalSeat> seats, int lives)
        {
            if (seats == null)
            {
                return;
            }
            foreach (LocalSeat seat in seats)
            {
                pilots.Add(new SquadPilot(seat.Index, seat.Name, seat.Color, lives));
            }
        }

        public IReadOnlyList<SquadPilot> Pilots => pilots;

        public int Count => pilots.Count;

        /// <summary>The pilot of <paramref name="seat"/>, or null.</summary>
        public SquadPilot this[int seat] => pilots.Find(pilot => pilot.Seat == seat);

        /// <summary>Every pilot is out of ships: the mission is lost.</summary>
        public bool AllOut => pilots.Count > 0 && pilots.TrueForAll(pilot => pilot.Out);

        /// <summary>The pilots still in the mission.</summary>
        public int InPlay => pilots.FindAll(pilot => !pilot.Out).Count;

        /// <summary>The score of the squad: every pilot's together (the money of a strike mission).</summary>
        public int TeamScore
        {
            get
            {
                long total = 0;
                foreach (SquadPilot pilot in pilots)
                {
                    total += pilot.Score.Score;
                }
                return (int)Math.Min(int.MaxValue, total);
            }
        }

        public int Kills => Sum(pilot => pilot.Score.Kills);

        public int Crystals => Sum(pilot => pilot.Score.Crystals);

        public int MaxCombo
        {
            get
            {
                int best = 0;
                foreach (SquadPilot pilot in pilots)
                {
                    best = Mathf.Max(best, pilot.Score.MaxCombo);
                }
                return best;
            }
        }

        /// <summary>The share of every pilot's shots that hit something.</summary>
        public float Accuracy
        {
            get
            {
                int fired = Sum(pilot => pilot.Score.ShotsFired);
                return fired > 0 ? Mathf.Clamp01(Sum(pilot => pilot.Score.ShotsLanded) / (float)fired) : 0f;
            }
        }

        /// <summary>A wave was cleared: every pilot still in the mission scores the bonus, as every pilot of a room does.</summary>
        public void WaveCleared(int bonus)
        {
            foreach (SquadPilot pilot in pilots)
            {
                if (!pilot.Out)
                {
                    pilot.Score.Add(bonus);
                }
            }
        }

        /// <summary>
        /// The mission is won: every pilot still in it scores <paramref name="perShip"/> for each ship left. Returns the
        /// bonus of the whole squad.
        /// </summary>
        public int Victory(int perShip)
        {
            int total = 0;
            foreach (SquadPilot pilot in pilots)
            {
                if (pilot.Out || perShip <= 0)
                {
                    continue;
                }
                int bonus = pilot.Lives * perShip;
                pilot.Score.Add(bonus);
                total += bonus;
            }
            return total;
        }

        /// <summary>The pilots best first, the lower seat first on equal scores, and their places (equal scores share one).</summary>
        public List<SquadPilot> Ranked(out int[] places)
        {
            var ranked = new List<SquadPilot>(pilots);
            places = Standings.Rank(ranked, pilot => pilot.Score.Score, pilot => pilot.Seat);
            return ranked;
        }

        /// <summary>
        /// The standings for the results, a line each: the place, the name in the seat's colour, the score as
        /// <paramref name="score"/> writes it and the <paramref name="detail"/> of the pilot (null for none).
        /// </summary>
        public string Describe(Func<SquadPilot, string> score, Func<SquadPilot, string> detail = null)
        {
            List<SquadPilot> ranked = Ranked(out int[] places);
            var lines = new List<string>();
            for (int i = 0; i < ranked.Count; i++)
            {
                SquadPilot pilot = ranked[i];
                lines.Add(Standings.Line(places[i], pilot.Name, false, score(pilot), pilot.Color, detail?.Invoke(pilot)));
            }
            return string.Join("\n", lines);
        }

        private int Sum(Func<SquadPilot, int> value)
        {
            int total = 0;
            foreach (SquadPilot pilot in pilots)
            {
                total += value(pilot);
            }
            return total;
        }
    }
}
