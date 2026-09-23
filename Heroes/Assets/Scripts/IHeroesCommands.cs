namespace Portfolio.Heroes
{
    /// <summary>
    /// What the interface asks for in the name of the player whose turn it is: moving a hero, building and recruiting
    /// in a town, trading, and every move of a battle. <see cref="HeroesController"/> hands the commands straight to
    /// the rules engine of a game played at one device; in an online game <see cref="HeroesOnlineController"/> sends
    /// them to the server, and they count once they come back in the action log, in their place among the commands of
    /// the others. The interface reaches whichever is in charge through <see cref="HeroesGameManager.Commands"/>.
    /// </summary>
    public interface IHeroesCommands
    {
        // ------------------------------------------------------------------ the adventure map

        /// <summary>Sends a hero toward a cell; he walks as far as his movement of the day carries him.</summary>
        void MoveHero(int heroId, int cell);

        void EndTurn();

        void Build(int townId, BuildingId building);

        /// <summary>Recruits from a town's dwelling, into the garrison or into the hero standing there.</summary>
        void Recruit(int townId, int tier, int count, bool toHero);

        /// <summary>Recruits from a dwelling out on the map into the hero visiting it.</summary>
        void RecruitAt(int objectId, int heroId, int count);

        /// <summary>Takes one of the two heroes the tavern of a town offers.</summary>
        void Hire(int townId, int slot);

        /// <summary>Moves creatures between two armies, addressed by <see cref="Holder"/>.</summary>
        void MoveArmy(int from, int fromSlot, int to, int toSlot, int count);

        void Trade(ResourceKind give, ResourceKind take, int amount);

        /// <summary>Answers whatever the rules are waiting to be told (a skill on levelling, a prize to take).</summary>
        void Choose(int option);

        void Dismiss(int holder, int slot);

        void Sleep(int heroId, bool sleeping);

        // ------------------------------------------------------------------ battle

        void BattleMove(int stackId, int cell);

        /// <summary>Strikes a stack: <paramref name="fromCell"/> is the cell to strike it from.</summary>
        void BattleAttack(int stackId, int targetCell, int fromCell);

        void BattleShoot(int stackId, int targetCell);

        void BattleWait(int stackId);

        void BattleDefend(int stackId);

        void BattleCast(SpellId spell, int cell);

        void BattleRetreat();
    }

    /// <summary>How an army is addressed in a command: a hero by his own number, a town's garrison by a negative one.</summary>
    public static class Holder
    {
        public static int Hero(int heroId)
        {
            return heroId;
        }

        public static int Garrison(int townId)
        {
            return -townId - 1;
        }

        public static bool IsGarrison(int holder)
        {
            return holder < 0;
        }

        public static int TownOf(int holder)
        {
            return -holder - 1;
        }
    }
}
