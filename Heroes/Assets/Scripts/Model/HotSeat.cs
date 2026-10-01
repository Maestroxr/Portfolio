using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>Who plays a seat of a hot seat game, as its setup made it: a person by name, or the computer at a level.</summary>
    [Serializable]
    public sealed class HotSeatPlayer
    {
        public string name = "";
        public bool human = true;
        /// <summary>0 easy, 1 normal, 2 hard, for a computer seat.</summary>
        public int aiLevel = 1;
    }

    /// <summary>
    /// The rules of a hot seat: several people take their turns at one device, with the computer playing any other seat.
    /// Which seat is whose (<see cref="Spec"/>), who has to hold the device before the game goes on
    /// (<see cref="HandOverTo"/>), so nobody sees the map as another person sees it, and how the realms stand at the end
    /// (<see cref="Standings"/>). Engine-free, like the rest of the rules, so the model tests check it outside Unity.
    /// </summary>
    public static class HotSeat
    {
        /// <summary>The longest name a person can give a realm.</summary>
        public const int NameLength = 24;

        /// <summary>
        /// The recipe of a hot seat game on <paramref name="map"/>: a skirmish at <paramref name="size"/> (0 small to 2
        /// large) with <paramref name="treasure"/> and <paramref name="monsters"/> as the settings give them, whose seats
        /// are played as <paramref name="players"/> says, in order. A seat keeps its faction and team; a seat the list
        /// does not reach is the computer's at its own level. The computer's levels are taken as they are: the settings'
        /// difficulty, which shifts them in a game against the computer, does not apply.
        /// </summary>
        public static MapSpec Spec(MapSpec map, IList<HotSeatPlayer> players, int size, int treasure, int monsters)
        {
            MapSpec spec = map.Clone();
            spec.Skirmish(size, treasure, monsters);
            for (int i = 0; i < spec.players.Count; i++)
            {
                PlayerSpec seat = spec.players[i];
                HotSeatPlayer player = players != null && i < players.Count ? players[i] : null;
                if (player == null)
                {
                    seat.human = false;
                    continue;
                }
                seat.human = player.human;
                seat.aiLevel = Math.Max(0, Math.Min(2, player.aiLevel));
                string name = (player.name ?? "").Trim();
                if (name.Length > NameLength)
                {
                    name = name.Substring(0, NameLength).Trim();
                }
                if (name.Length > 0)
                {
                    seat.name = name;
                }
                else if (map.players[i].human || string.IsNullOrEmpty(seat.name))
                {
                    // Without a name of its own a realm keeps the map's, or is called after its faction (the map's name
                    // for the seat of a person is "You").
                    seat.name = Land.FactionName(seat.faction);
                }
            }
            return spec;
        }

        /// <summary>How many seats of the game people play (whether still in it or not).</summary>
        public static int People(GameState state)
        {
            int people = 0;
            foreach (PlayerState player in state.players)
            {
                people += player.human ? 1 : 0;
            }
            return people;
        }

        private static bool IsPerson(GameState state, int player)
        {
            PlayerState seat = state.Player(player);
            return seat != null && seat.human && seat.alive;
        }

        /// <summary>
        /// The person who plays next from <paramref name="from"/> on, in the order of turns: <paramref name="from"/>
        /// itself when a person still in the game plays it, else the first one after it. -1 when nobody is left.
        /// </summary>
        public static int NextPerson(GameState state, int from)
        {
            int count = state.players.Count;
            for (int i = 0; i < count; i++)
            {
                int index = ((from + i) % count + count) % count;
                if (IsPerson(state, index))
                {
                    return index;
                }
            }
            return -1;
        }

        /// <summary>
        /// The person who has to hold the device before the game goes on, or -1 when the one holding it
        /// (<paramref name="holder"/>, -1 for nobody yet) may keep it. <paramref name="acting"/> is the seat the rules
        /// wait for (<see cref="HeroesGame.WaitingPlayer"/>).
        /// <list type="bullet">
        /// <item>A finished game hands nothing over.</item>
        /// <item>A battle: a person who fights in it and holds the device keeps it, and the other side, a person too,
        /// fights at the same screen; otherwise the person who fights in it takes the device (the one whose troops
        /// act first). The computer's battles among themselves are nobody's to watch.</item>
        /// <item>A question for a person (a level up, a chest): that person.</item>
        /// <item>Otherwise the person whose turn it is, or while the computer plays the person who plays next, who
        /// watches the computer's moves as their own realm sees the map.</item>
        /// </list>
        /// </summary>
        public static int HandOverTo(GameState state, int holder, int acting)
        {
            if (state.over)
            {
                return -1;
            }
            int to;
            BattleState battle = state.battle;
            if (battle != null && battle.active)
            {
                bool holderFights = holder >= 0 && (holder == battle.attackerPlayer || holder == battle.defenderPlayer);
                if (holderFights && IsPerson(state, holder))
                {
                    return -1;
                }
                to = IsPerson(state, acting) ? acting
                    : IsPerson(state, battle.attackerPlayer) ? battle.attackerPlayer
                    : IsPerson(state, battle.defenderPlayer) ? battle.defenderPlayer
                    : -1;
                return to != holder ? to : -1;
            }
            if (state.pending.Count > 0)
            {
                int asked = state.pending[0].player;
                to = IsPerson(state, asked) ? asked : -1;
                return to != holder ? to : -1;
            }
            to = NextPerson(state, state.currentPlayer);
            return to != holder ? to : -1;
        }

        /// <summary>
        /// The realms in the order they finished: the winner first, then those still standing, then those knocked out;
        /// among equals the one with more towns, heroes, battles won and creatures slain, then the order of seats.
        /// </summary>
        public static List<PlayerState> Standings(GameState state)
        {
            var order = new List<PlayerState>(state.players);
            int Rank(PlayerState player) => player.index == state.winner ? 0 : player.alive ? 1 : 2;
            int Heroes(PlayerState player)
            {
                int count = 0;
                foreach (int id in player.heroes)
                {
                    HeroState hero = state.Hero(id);
                    count += hero != null && hero.alive ? 1 : 0;
                }
                return count;
            }
            order.Sort((a, b) =>
            {
                int compare = Rank(a).CompareTo(Rank(b));
                if (compare == 0)
                {
                    compare = b.towns.Count.CompareTo(a.towns.Count);
                }
                if (compare == 0)
                {
                    compare = Heroes(b).CompareTo(Heroes(a));
                }
                if (compare == 0)
                {
                    compare = b.battlesWon.CompareTo(a.battlesWon);
                }
                if (compare == 0)
                {
                    compare = b.creaturesKilled.CompareTo(a.creaturesKilled);
                }
                return compare != 0 ? compare : a.index.CompareTo(b.index);
            });
            return order;
        }
    }
}
