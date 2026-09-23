using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    public sealed partial class HeroesGame
    {
        /// <summary>
        /// Starts the game: the first day begins for the first player. Call once on a freshly generated state (a loaded
        /// game goes on where it was).
        /// </summary>
        public void Begin()
        {
            foreach (PlayerState player in State.players)
            {
                RevealAll(player.index);
                foreach (int id in player.heroes)
                {
                    HeroState hero = State.Hero(id);
                    hero.maxMovement = DailyMovement(hero);
                    hero.movement = hero.maxMovement;
                    hero.mana = Math.Max(hero.mana, MaxMana(hero));
                }
                RefreshTavern(player);
            }
            State.currentPlayer = FirstAlive(0);
            Emit(EventKind.DayBegan, -1, State.day, State.Week, State.Month);
            Emit(EventKind.TurnBegan, State.currentPlayer, State.day);
            LearnGuildSpellsInTowns(State.currentPlayer);
        }

        private int FirstAlive(int from)
        {
            for (int i = 0; i < State.players.Count; i++)
            {
                int index = (from + i) % State.players.Count;
                if (State.players[index].alive)
                {
                    return index;
                }
            }
            return 0;
        }

        private bool EndTurn(int player)
        {
            if (player != State.currentPlayer)
            {
                return false;
            }
            int next = -1;
            for (int i = player + 1; i < State.players.Count; i++)
            {
                if (State.players[i].alive)
                {
                    next = i;
                    break;
                }
            }
            if (next < 0)
            {
                NewDay();
                if (State.over)
                {
                    return true;
                }
                next = FirstAlive(0);
            }
            State.currentPlayer = next;
            foreach (int id in State.players[next].heroes)
            {
                HeroState hero = State.Hero(id);
                if (hero != null)
                {
                    hero.sleeping = false;
                }
            }
            Emit(EventKind.TurnBegan, next, State.day);
            LearnGuildSpellsInTowns(next);
            return true;
        }

        private void NewDay()
        {
            State.day++;
            bool newWeek = State.DayOfWeek == 1;
            Emit(EventKind.DayBegan, -1, State.day, State.Week, State.Month);
            if (newWeek)
            {
                NewWeek();
            }
            foreach (TownState town in State.towns)
            {
                town.builtToday = false;
            }
            foreach (PlayerState player in State.players)
            {
                if (!player.alive)
                {
                    continue;
                }
                ResourceSet income = Income(player.index);
                player.resources.Add(income);
                if (!income.IsEmpty)
                {
                    Emit(EventKind.ResourcesGained, player.index, -1, 0, text: income.ToString());
                }
                foreach (int id in player.heroes)
                {
                    HeroState hero = State.Hero(id);
                    if (hero == null || !hero.alive)
                    {
                        continue;
                    }
                    hero.maxMovement = DailyMovement(hero);
                    hero.movement = hero.maxMovement;
                    int regen = 1 + 2 * hero.SkillLevel(SkillId.Mysticism);
                    TownState town = TownAtGate(hero.cell);
                    if (town != null && town.owner == player.index && Buildings.MageGuildLevel(town) > 0)
                    {
                        regen = MaxMana(hero);
                    }
                    hero.mana = Math.Min(MaxMana(hero), hero.mana + regen);
                }
                if (player.towns.Count == 0)
                {
                    player.daysWithoutTown++;
                }
                else
                {
                    player.daysWithoutTown = 0;
                }
            }
            CheckElimination();
            CheckVictory();
        }

        private void NewWeek()
        {
            Emit(EventKind.WeekBegan, -1, State.Week);
            foreach (TownState town in State.towns)
            {
                int bonus = Buildings.GrowthBonus(town);
                for (int tier = 1; tier <= 7; tier++)
                {
                    if (!town.Has(Buildings.DwellingOf(tier)))
                    {
                        continue;
                    }
                    CreatureDef def = Creatures.Get(Creatures.OfTier(town.faction, tier));
                    int growth = def.Growth + def.Growth * bonus / 100;
                    town.available[tier - 1] += growth;
                }
            }
            foreach (MapObject obj in State.objects)
            {
                if (obj.removed)
                {
                    continue;
                }
                switch (obj.kind)
                {
                    case ObjectKind.Dwelling:
                        obj.amount = Math.Max(obj.amount, Creatures.Get(obj.subtype).Growth);
                        break;
                    case ObjectKind.Monster:
                        // Wandering monsters gather: a tenth more every week, at least one.
                        obj.amount += Math.Max(1, obj.amount / 10);
                        break;
                    case ObjectKind.Windmill:
                    case ObjectKind.WaterWheel:
                        obj.visitedBy.Clear();
                        break;
                }
            }
            foreach (PlayerState player in State.players)
            {
                RefreshTavern(player);
            }
        }

        /// <summary>What the player earns a day: towns, mines, and the heroes' estates and purses.</summary>
        public ResourceSet Income(int player)
        {
            var income = new ResourceSet();
            PlayerState state = State.Player(player);
            if (state == null)
            {
                return income;
            }
            foreach (int id in state.towns)
            {
                TownState town = State.Town(id);
                income[ResourceKind.Gold] += Buildings.Income(town);
                if (town.Has(BuildingId.Silo))
                {
                    income[Land.RareOf(town.faction)] += 1;
                }
            }
            foreach (MapObject obj in State.objects)
            {
                if (!obj.removed && obj.kind == ObjectKind.Mine && obj.owner == player)
                {
                    income[(ResourceKind)obj.subtype] += MapObjects.MineYield((ResourceKind)obj.subtype);
                }
            }
            foreach (int id in state.heroes)
            {
                HeroState hero = State.Hero(id);
                if (hero == null || !hero.alive)
                {
                    continue;
                }
                int estates = hero.SkillLevel(SkillId.Estates);
                income[ResourceKind.Gold] += estates == 3 ? 500 : estates * 125;
                foreach (ArtifactDef artifact in Worn(hero))
                {
                    income[ResourceKind.Gold] += artifact.Gold;
                }
            }
            // The computer's difficulty: an easy one earns less, a hard one more.
            if (!state.human)
            {
                int percent = state.aiLevel == 0 ? 80 : state.aiLevel == 2 ? 125 : 100;
                for (int i = 0; i < ResourceSet.Kinds; i++)
                {
                    income.values[i] = income.values[i] * percent / 100;
                }
            }
            return income;
        }

        private void RefreshTavern(PlayerState player)
        {
            // Two heroes of the land wait in every player's taverns: one of the player's faction when there is one.
            for (int slot = 0; slot < 2; slot++)
            {
                int current = player.tavern[slot];
                if (current >= 0 && State.freeHeroes.Contains(current))
                {
                    continue;
                }
                player.tavern[slot] = PickFreeHero(player, slot == 0 ? player.faction : (Faction?)null, player.tavern[1 - slot]);
            }
        }

        private int PickFreeHero(PlayerState player, Faction? faction, int exclude)
        {
            var candidates = new List<int>();
            foreach (int id in State.freeHeroes)
            {
                if (id == exclude || IsInOtherTavern(player.index, id))
                {
                    continue;
                }
                if (faction == null || HeroData.Hero(id).Faction == faction.Value)
                {
                    candidates.Add(id);
                }
            }
            if (candidates.Count == 0 && faction != null)
            {
                return PickFreeHero(player, null, exclude);
            }
            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : -1;
        }

        private bool IsInOtherTavern(int player, int hero)
        {
            foreach (PlayerState other in State.players)
            {
                if (other.index != player && (other.tavern[0] == hero || other.tavern[1] == hero))
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ winning and losing

        private void CheckElimination()
        {
            foreach (PlayerState player in State.players)
            {
                if (!player.alive)
                {
                    continue;
                }
                bool noHeroes = true;
                foreach (int id in player.heroes)
                {
                    HeroState hero = State.Hero(id);
                    if (hero != null && hero.alive)
                    {
                        noHeroes = false;
                        break;
                    }
                }
                bool lost = (noHeroes && player.towns.Count == 0) || player.daysWithoutTown >= 7;
                if (player.human && State.rules.loss == LossKind.LoseHero && State.rules.lossValue >= 0)
                {
                    HeroState hero = State.Hero(State.rules.lossValue);
                    if (hero != null && hero.owner == player.index && !hero.alive)
                    {
                        lost = true;
                    }
                }
                if (lost)
                {
                    Eliminate(player);
                }
            }
        }

        private void Eliminate(PlayerState player)
        {
            player.alive = false;
            foreach (int id in new List<int>(player.heroes))
            {
                HeroState hero = State.Hero(id);
                if (hero != null && hero.alive)
                {
                    RemoveHero(hero);
                }
            }
            foreach (int id in new List<int>(player.towns))
            {
                TownState town = State.Town(id);
                SetTownOwner(town, -1);
            }
            foreach (MapObject obj in State.objects)
            {
                if (obj.owner == player.index && obj.kind == ObjectKind.Mine)
                {
                    obj.owner = -1;
                    Emit(EventKind.ObjectCaptured, -1, obj.id, -1);
                }
            }
            Emit(EventKind.PlayerEliminated, player.index, text: $"{player.name} has been defeated.");
        }

        /// <summary>Checks the scenario's goals; ends the game when somebody won or every human lost.</summary>
        private void CheckVictory()
        {
            if (State.over)
            {
                return;
            }
            var alive = new List<PlayerState>();
            foreach (PlayerState player in State.players)
            {
                if (player.alive)
                {
                    alive.Add(player);
                }
            }
            // Everybody left standing is on one team: they win.
            if (alive.Count > 0)
            {
                int team = alive[0].team;
                bool oneTeam = true;
                foreach (PlayerState player in alive)
                {
                    oneTeam &= player.team == team;
                }
                if (oneTeam)
                {
                    Finish(alive[0].index);
                    return;
                }
            }
            else
            {
                Finish(-1);
                return;
            }
            bool anyHuman = false;
            foreach (PlayerState player in alive)
            {
                anyHuman |= player.human;
            }
            bool hadHumans = false;
            foreach (PlayerState player in State.players)
            {
                hadHumans |= player.human;
            }
            if (hadHumans && !anyHuman)
            {
                // Every person at the table lost: the computer won. Online too: a seat the computer plays on for somebody
                // who left is no person's any more, and a game only the computer players are left in would go on for
                // nobody (they need not ever settle it among themselves).
                Finish(alive[0].index);
                return;
            }
            ScenarioRules rules = State.rules;
            if (rules.dayLimit > 0 && State.day > rules.dayLimit && rules.loss == LossKind.TimeLimit)
            {
                // Out of time: the first computer player standing takes the game.
                foreach (PlayerState player in alive)
                {
                    if (!player.human)
                    {
                        Finish(player.index);
                        return;
                    }
                }
            }
            foreach (PlayerState player in alive)
            {
                if (MetGoal(player))
                {
                    Finish(player.index);
                    return;
                }
            }
        }

        private bool MetGoal(PlayerState player)
        {
            ScenarioRules rules = State.rules;
            switch (rules.victory)
            {
                case VictoryKind.CaptureTown:
                    TownState town = State.Town(rules.victoryValue);
                    return town != null && town.owner == player.index && player.human;
                case VictoryKind.DefeatMonster:
                    foreach (MapObject obj in State.objects)
                    {
                        if (obj.tag == rules.victoryTag && obj.kind == ObjectKind.Monster)
                        {
                            return obj.removed && obj.owner == player.index;
                        }
                    }
                    return false;
                case VictoryKind.GatherGold:
                    return player.resources.Gold >= rules.victoryValue && player.human;
                case VictoryKind.FindArtifact:
                    foreach (int id in player.heroes)
                    {
                        HeroState hero = State.Hero(id);
                        if (hero != null && hero.alive && (Array.IndexOf(hero.equipped, rules.victoryValue) >= 0 || hero.backpack.Contains(rules.victoryValue)))
                        {
                            return player.human;
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }

        private void Finish(int winner)
        {
            State.over = true;
            State.winner = winner;
            PlayerState player = State.Player(winner);
            Emit(EventKind.GameOver, winner, State.day, text: player != null ? $"{player.name} is victorious!" : "Nobody is left standing.");
        }

        /// <summary>For a scenario won by a goal other than conquest: checks it right after any change.</summary>
        private void CheckGoalsNow()
        {
            if (!State.over && State.rules.victory != VictoryKind.DefeatAll)
            {
                CheckVictory();
            }
        }
    }
}
