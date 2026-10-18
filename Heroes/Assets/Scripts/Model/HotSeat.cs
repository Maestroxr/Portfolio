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
    /// (<see cref="HandOverTo(GameState, int, int)"/>), so nobody sees the map as another person sees it, and how the
    /// realms stand at the end (<see cref="Standings"/>). The same rules hand the device over between the people who play
    /// online at one device against the others (<see cref="HandOverTo(GameState, int, int, ICollection{int})"/>).
    /// Engine-free, like the rest of the rules, so the model tests check it outside Unity.
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

        /// <summary>
        /// Whether a person still in the game plays <paramref name="player"/> at this device: any person when
        /// <paramref name="here"/> is null, else only the seats it holds.
        /// </summary>
        private static bool IsPerson(GameState state, int player, ICollection<int> here = null)
        {
            PlayerState seat = state.Player(player);
            return seat != null && seat.human && seat.alive && (here == null || here.Contains(player));
        }

        /// <summary>
        /// The person who plays next from <paramref name="from"/> on, in the order of turns: <paramref name="from"/>
        /// itself when a person still in the game plays it, else the first one after it. -1 when nobody is left.
        /// </summary>
        public static int NextPerson(GameState state, int from)
        {
            return NextPerson(state, from, null);
        }

        /// <summary>
        /// As <see cref="NextPerson(GameState, int)"/>, among the people at this device only (<paramref name="here"/>, the
        /// seats they play; null for every person of the game): online the realms of the others are skipped like the
        /// computer's.
        /// </summary>
        public static int NextPerson(GameState state, int from, ICollection<int> here)
        {
            int count = state.players.Count;
            for (int i = 0; i < count; i++)
            {
                int index = ((from + i) % count + count) % count;
                if (IsPerson(state, index, here))
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
            return HandOverTo(state, holder, acting, null);
        }

        /// <summary>
        /// As <see cref="HandOverTo(GameState, int, int)"/>, for a device that plays some of the seats only: online, where
        /// several people at one device play their realms against others (<paramref name="here"/>, the seats played at
        /// this device; null for a hot seat, where every person is here). The turns, questions and battles of the others
        /// hand nothing over, and while the others play, the person here who plays next holds the device, as while the
        /// computer plays in a hot seat.
        /// </summary>
        public static int HandOverTo(GameState state, int holder, int acting, ICollection<int> here)
        {
            if (state.over)
            {
                return -1;
            }
            int to;
            BattleState battle = state.battle;
            if (battle != null && battle.active)
            {
                return ToBattle(state, holder, acting, battle.attackerPlayer, battle.defenderPlayer, here);
            }
            if (state.pending.Count > 0)
            {
                int asked = state.pending[0].player;
                to = IsPerson(state, asked, here) ? asked : -1;
                return to != holder ? to : -1;
            }
            to = NextPerson(state, state.currentPlayer, here);
            return to != holder ? to : -1;
        }

        /// <summary>
        /// The person who has to hold the device for a battle between <paramref name="attacker"/> and
        /// <paramref name="defender"/>, or -1 when the one holding it may keep it, as <see cref="HandOverTo(GameState, int, int)"/>
        /// says of a battle: a person who fights in it and holds the device keeps it, else the person here whose troops act
        /// (<paramref name="acting"/>, -1 when nobody's do yet), the attacker or the defender takes it. Online the director
        /// asks it of a battle that is about to come onto the screen, which the rules, ahead of the screen, may have left
        /// behind already.
        /// </summary>
        public static int ToBattle(GameState state, int holder, int acting, int attacker, int defender, ICollection<int> here = null)
        {
            bool holderFights = holder >= 0 && (holder == attacker || holder == defender);
            if (holderFights && IsPerson(state, holder, here))
            {
                return -1;
            }
            int to = IsPerson(state, acting, here) ? acting
                : IsPerson(state, attacker, here) ? attacker
                : IsPerson(state, defender, here) ? defender
                : -1;
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
