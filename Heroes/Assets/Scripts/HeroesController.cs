using Gamebox;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The local controller: it takes what the interface asks for in the name of the player at this device, checks that
    /// the rules would even listen right now, and hands the command to the engine of the running game. Starting a game
    /// goes through the base <see cref="OfflineGameController"/> flow into <see cref="HeroesGameManager.StartGame"/>.
    /// In an online game the commands go to the server instead, through <see cref="HeroesOnlineController"/>.
    /// </summary>
    public class HeroesController : OfflineGameController, IHeroesCommands
    {
        public HeroesGameManager Heroes => BaseManager as HeroesGameManager;

        private HeroesGame Game => Heroes != null ? Heroes.Game : null;

        /// <summary>The seat this device plays. Offline it is whoever's turn it is, as long as a human sits there.</summary>
        protected virtual int Seat => Game != null ? Game.WaitingPlayer : -1;

        /// <summary>Whether the engine is waiting for this device and has nothing left to play out first.</summary>
        protected bool Ready
        {
            get
            {
                HeroesGame game = Game;
                return game != null && Heroes.IsGameRunning && !game.HasEvents && Seat >= 0 && !game.IsComputer(Seat);
            }
        }

        protected virtual void Send(GameCommand command)
        {
            Heroes.Submit(command);
        }

        private void Do(CommandKind kind, int a = 0, int b = 0, int c = 0, int d = 0, int e = 0)
        {
            if (Ready)
            {
                Send(GameCommand.Of(kind, Seat, a, b, c, d, e));
            }
        }

        // ------------------------------------------------------------------ the adventure map

        public void MoveHero(int heroId, int cell)
        {
            Do(CommandKind.MoveHero, heroId, cell);
        }

        public void EndTurn()
        {
            Do(CommandKind.EndTurn);
        }

        public void Build(int townId, BuildingId building)
        {
            Do(CommandKind.Build, townId, (int)building);
        }

        public void Recruit(int townId, int tier, int count, bool toHero)
        {
            Do(CommandKind.Recruit, townId, tier, count, toHero ? 1 : 0);
        }

        public void RecruitAt(int objectId, int heroId, int count)
        {
            Do(CommandKind.Recruit, objectId, 0, count, 2, heroId);
        }

        public void Hire(int townId, int slot)
        {
            Do(CommandKind.HireHero, townId, slot);
        }

        public void MoveArmy(int from, int fromSlot, int to, int toSlot, int count)
        {
            Do(CommandKind.MoveArmy, from, fromSlot, to, toSlot, count);
        }

        public void Trade(ResourceKind give, ResourceKind take, int amount)
        {
            Do(CommandKind.Trade, (int)give, (int)take, amount);
        }

        public void Choose(int option)
        {
            // A choice is answered by the player it is waiting on, whoever's turn it is.
            HeroesGame game = Game;
            if (game != null && Heroes.IsGameRunning && game.State.pending.Count > 0)
            {
                int who = game.State.pending[0].player;
                if (!game.IsComputer(who))
                {
                    Send(GameCommand.Of(CommandKind.Choose, who, option));
                }
            }
        }

        public void Dismiss(int holder, int slot)
        {
            Do(CommandKind.DismissStack, holder, slot);
        }

        public void Sleep(int heroId, bool sleeping)
        {
            Do(CommandKind.SleepHero, heroId, sleeping ? 1 : 0);
        }

        // ------------------------------------------------------------------ battle

        public void BattleMove(int stackId, int cell)
        {
            Do(CommandKind.BattleMove, stackId, cell);
        }

        public void BattleAttack(int stackId, int targetStack, int fromCell)
        {
            Do(CommandKind.BattleAttack, stackId, targetStack, fromCell);
        }

        public void BattleShoot(int stackId, int targetStack)
        {
            Do(CommandKind.BattleShoot, stackId, targetStack);
        }

        public void BattleWait(int stackId)
        {
            Do(CommandKind.BattleWait, stackId);
        }

        public void BattleDefend(int stackId)
        {
            Do(CommandKind.BattleDefend, stackId);
        }

        public void BattleCast(SpellId spell, int cell)
        {
            Do(CommandKind.BattleCast, (int)spell, cell);
        }

        public void BattleRetreat()
        {
            Do(CommandKind.BattleRetreat);
        }
    }
}
