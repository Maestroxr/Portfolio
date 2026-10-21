using System;
using System.Collections.Generic;
using System.Text;

namespace Portfolio.Heroes
{
    /// <summary>What a remote control of a seat has to offer its player right now (<see cref="RemoteView.ModeOf"/>).</summary>
    public enum RemoteMode
    {
        /// <summary>The seat waits: the game waits for somebody else, or is over.</summary>
        Watching,

        /// <summary>The rules put a question to the seat (a level up, a chest, the arena): one of its options is to be picked.</summary>
        Choosing,

        /// <summary>A stack of the seat's is to act in a battle.</summary>
        Fighting,

        /// <summary>The seat's turn on the map: its heroes move, its towns build, and the turn ends.</summary>
        Moving
    }

    /// <summary>
    /// What a remote control shows of a seat and may do for it, worked out from the game as every client holds it: a
    /// phone that plays as a remote of a shared screen runs the same game as every other client, so nothing has to be
    /// sent to it; its panel reads the seat's treasury, its hero in hand and the question or the battle the rules wait
    /// on, and sends the seat's commands the way the interface does. Unity-free, so the panel's reasoning is tested
    /// outside the editor.
    /// </summary>
    public static class RemoteView
    {
        /// <summary>What the remote of <paramref name="seat"/> offers now: nothing while the game waits for anybody else.</summary>
        public static RemoteMode ModeOf(HeroesGame game, int seat)
        {
            if (game == null || seat < 0 || game.IsOver || game.WaitingPlayer != seat)
            {
                return RemoteMode.Watching;
            }
            if (game.State.pending.Count > 0)
            {
                return RemoteMode.Choosing;
            }
            return game.InBattle ? RemoteMode.Fighting : RemoteMode.Moving;
        }

        /// <summary>
        /// The hero the panel of <paramref name="seat"/> shows: the hero in hand (<paramref name="selected"/>, a hero id)
        /// when he is the seat's and alive, else the seat's first hero with movement left, else its first hero; null for a
        /// seat without heroes.
        /// </summary>
        public static HeroState HeroInHand(GameState state, int seat, int selected)
        {
            PlayerState player = state?.Player(seat);
            if (player == null)
            {
                return null;
            }
            HeroState chosen = state.Hero(selected);
            if (chosen != null && chosen.alive && chosen.owner == seat)
            {
                return chosen;
            }
            HeroState first = null;
            foreach (int id in player.heroes)
            {
                HeroState hero = state.Hero(id);
                if (hero == null || !hero.alive)
                {
                    continue;
                }
                if (hero.movement > 0 && !hero.sleeping)
                {
                    return hero;
                }
                first ??= hero;
            }
            return first;
        }

        /// <summary>
        /// The army of a hero as a line, "12 Pikemen, 5 Archers"; <paramref name="translate"/> puts each name into the
        /// language shown (the names are English in the rules). Empty for an empty army.
        /// </summary>
        public static string ArmyLine(Army army, Func<string, string> translate = null)
        {
            var line = new StringBuilder();
            if (army == null)
            {
                return "";
            }
            foreach (ArmySlot slot in army.slots)
            {
                if (slot == null || slot.IsEmpty || slot.Def == null)
                {
                    continue;
                }
                if (line.Length > 0)
                {
                    line.Append(", ");
                }
                string name = slot.Def.NameFor(slot.count);
                line.Append(slot.count).Append(' ').Append(translate != null ? translate(name) : name);
            }
            return line.ToString();
        }

        /// <summary>
        /// The treasury of a realm as a line, every kind in its order, "1,250 Gold  20 Wood  10 Ore ..."; the names through
        /// <paramref name="translate"/>. Empty without a realm.
        /// </summary>
        public static string ResourcesLine(PlayerState player, Func<string, string> translate = null)
        {
            if (player == null || player.resources == null)
            {
                return "";
            }
            var line = new StringBuilder();
            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                if (line.Length > 0)
                {
                    line.Append("   ");
                }
                string name = Land.ResourceName((ResourceKind)i);
                line.Append(player.resources.values[i].ToString("N0", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(' ').Append(translate != null ? translate(name) : name);
            }
            return line.ToString();
        }

        /// <summary>
        /// The move auto combat makes for the stack of <paramref name="seat"/> that is to act: the computer's battle sense
        /// (<see cref="BattleAI"/>), except that it never retreats, which is the player's own call: then the stack defends.
        /// Null while no stack of the seat's acts.
        /// </summary>
        public static GameCommand AutoMove(HeroesGame game, int seat)
        {
            if (ModeOf(game, seat) != RemoteMode.Fighting)
            {
                return null;
            }
            BattleStack current = game.Battle.Current;
            if (current == null)
            {
                return null;
            }
            GameCommand move = BattleAI.Next(game);
            if (move == null || move.kind == CommandKind.BattleRetreat || move.player != seat)
            {
                return GameCommand.BattleDefend(seat, current.id);
            }
            return move;
        }

        /// <summary>
        /// Whether the stack of <paramref name="seat"/> that is to act may retreat: a hero leads it, and it is not the
        /// defender of a town, who holds it to the last (the battle bar's rule).
        /// </summary>
        public static bool MayRetreat(HeroesGame game, int seat)
        {
            if (ModeOf(game, seat) != RemoteMode.Fighting)
            {
                return false;
            }
            BattleState battle = game.Battle;
            BattleStack current = battle.Current;
            if (current == null)
            {
                return false;
            }
            HeroState hero = game.State.Hero(battle.HeroOf(current.side));
            return hero != null && !(current.side == 1 && battle.town >= 0);
        }

        /// <summary>Whether the stack of <paramref name="seat"/> that is to act may wait (once a round, and not after a morale strike).</summary>
        public static bool MayWait(HeroesGame game, int seat)
        {
            if (ModeOf(game, seat) != RemoteMode.Fighting)
            {
                return false;
            }
            BattleStack current = game.Battle.Current;
            return current != null && !current.waited && !current.moraleUsed;
        }

        /// <summary>The heroes of <paramref name="seat"/> that still have movement left today and are awake, in the realm's order.</summary>
        public static List<HeroState> HeroesWithMoves(GameState state, int seat)
        {
            var heroes = new List<HeroState>();
            PlayerState player = state?.Player(seat);
            if (player == null)
            {
                return heroes;
            }
            foreach (int id in player.heroes)
            {
                HeroState hero = state.Hero(id);
                if (hero != null && hero.alive && !hero.sleeping && hero.movement > 0)
                {
                    heroes.Add(hero);
                }
            }
            return heroes;
        }
    }
}
