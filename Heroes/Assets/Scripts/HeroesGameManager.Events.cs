using System.Collections;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Playing out what the rules recorded. Every event of the engine becomes something on the screen: a hero walking
    /// his path, a banner going up over a mine, a line in the log, an army fighting on the hexagons of the map. The
    /// director waits for each one, so the map never runs ahead of the story it is telling.
    /// </summary>
    public partial class HeroesGameManager
    {
        private IEnumerator Play(GameEvent what)
        {
            switch (what.kind)
            {
                case EventKind.Message:
                    ui.Log(what.text, what.player);
                    break;

                // ---------------------------------------------------------- the turn
                case EventKind.DayBegan:
                    ui.Refresh();
                    break;
                case EventKind.WeekBegan:
                    sound?.Play(Sfx.NewWeek);
                    ui.Announce($"Week {what.a}", "A new week begins.");
                    yield return Beat(0.7f);
                    break;
                case EventKind.TurnBegan:
                    yield return TurnBegan(what);
                    break;

                // ---------------------------------------------------------- heroes on the map
                case EventKind.HeroMoved:
                    yield return Moved(what);
                    break;
                case EventKind.HeroStopped:
                    Map.Warp(what.a, what.b);
                    ui.Refresh();
                    break;
                case EventKind.Revealed:
                    if (what.player == Viewer)
                    {
                        Map.Fog.MarkDirty();
                    }
                    break;
                case EventKind.HeroDefeated:
                    yield return Defeated(what);
                    break;
                case EventKind.HeroHired:
                    Map.Sync();
                    sound?.Play(Sfx.Recruit);
                    ui.Log(what.text, what.player);
                    ui.Refresh();
                    break;

                // ---------------------------------------------------------- what a hero finds
                case EventKind.ResourcesGained:
                    yield return Gained(what);
                    break;
                case EventKind.ArtifactFound:
                    Say(what, Sfx.Artifact, new Color(0.85f, 0.6f, 1f));
                    yield return Beat(0.45f);
                    break;
                case EventKind.SpellLearned:
                    Say(what, Sfx.Page, new Color(0.6f, 0.8f, 1f));
                    yield return Beat(0.35f);
                    break;
                case EventKind.ObjectVisited:
                    ui.Log(what.text, what.player);
                    if (what.c != 0)
                    {
                        sound?.Play(Sfx.Treasure, 0.7f);
                    }
                    break;
                case EventKind.ObjectCaptured:
                    Captured(what);
                    break;
                case EventKind.ObjectRemoved:
                    Map.Sync();
                    break;
                case EventKind.ExperienceGained:
                    ui.Log(what.text, what.player);
                    break;
                case EventKind.HeroLeveled:
                    yield return Leveled(what);
                    break;
                case EventKind.StatRaised:
                    Say(what, Sfx.LevelUp, new Color(1f, 0.9f, 0.5f));
                    break;

                // ---------------------------------------------------------- towns
                case EventKind.TownBuilt:
                    sound?.Play(Sfx.Build);
                    ui.Log(what.text, what.player);
                    ui.Refresh();
                    break;
                case EventKind.CreaturesRecruited:
                    sound?.Play(Sfx.Recruit);
                    ui.Refresh();
                    break;
                case EventKind.TownCaptured:
                    yield return Taken(what);
                    break;
                case EventKind.ArmyChanged:
                    ui.Refresh();
                    break;

                // ---------------------------------------------------------- battle
                case EventKind.BattleStarted:
                    yield return BattleStarted(what);
                    break;
                case EventKind.BattleEnded:
                    yield return BattleEnded(what);
                    break;
                case EventKind.RoundBegan:
                    ui.SetBattleRound(what.a);
                    break;
                case EventKind.CreaturesRaised when !Game.InBattle:
                    ui.Log(what.text, what.player);
                    break;

                // ---------------------------------------------------------- the end
                case EventKind.PlayerEliminated:
                    ui.Announce("Defeated", what.text);
                    yield return Beat(0.8f);
                    break;
                case EventKind.GameOver:
                    ui.Log(what.text, -1);
                    break;
                case EventKind.ChoiceNeeded:
                    // The choice itself is put to the player by the director, once everything else has played.
                    break;

                default:
                    if (Battle.Running || Game.InBattle)
                    {
                        yield return Battle.Play(what, Game.Battle);
                    }
                    break;
            }
        }

        private WaitForSeconds Beat(float seconds)
        {
            return new WaitForSeconds(seconds / Speed);
        }

        private void Say(GameEvent what, Sfx sfx, Color color)
        {
            ui.Log(what.text, what.player);
            sound?.Play(sfx);
            HeroState hero = Game.State.Hero(what.a);
            if (hero != null && hero.cell >= 0 && !string.IsNullOrEmpty(what.text))
            {
                Effects.Float(Map.Over(hero.cell), Short(what.text), color, 2.8f);
            }
        }

        /// <summary>The part of a line that fits over a hero's head.</summary>
        private static string Short(string text)
        {
            int stop = text.IndexOf(':');
            string cut = stop > 0 ? text.Substring(stop + 1).Trim() : text;
            return cut.Length > 28 ? cut.Substring(0, 27) + "…" : cut;
        }

        // ------------------------------------------------------------------ the turn

        private IEnumerator TurnBegan(GameEvent what)
        {
            ui.Refresh();
            PlayerState player = Game.State.Player(what.player);
            if (player == null)
            {
                yield break;
            }
            if (player.human && !InSession)
            {
                Viewer = player.index;
                Map.Fog.Show(player);
            }
            if (player.human)
            {
                sound?.Play(Sfx.NewDay, 0.6f);
                HeroState hero = NextHero() ?? (player.heroes.Count > 0 ? Game.State.Hero(player.heroes[0]) : null);
                if (hero != null)
                {
                    Select(hero);
                }
                ui.Announce($"Day {Game.State.DayOfWeek}, Week {Game.State.Week}", player.name);
                yield return Beat(0.5f);
            }
        }

        // ------------------------------------------------------------------ heroes

        private IEnumerator Moved(GameEvent what)
        {
            HeroState hero = Game.State.Hero(what.a);
            if (hero == null)
            {
                yield break;
            }
            HeroView view = Map.Hero(hero.id);
            if (view == null)
            {
                Map.Sync();
                yield break;
            }
            bool watched = Game.State.Player(Viewer) != null && Game.State.Player(Viewer).Explored(what.c);
            if (!watched && !Options.showEnemyMoves)
            {
                Map.Warp(hero.id, what.c);
                yield break;
            }
            if (Viewer == what.player && cameraRig != null && !cameraRig.IsVisible(Map.Point(what.c)))
            {
                cameraRig.Focus(Map.Point(what.c));
            }
            sound?.Play(Sfx.Horse, 0.25f);
            yield return view.WalkTo(Map.Point(what.c), 2.6f * Speed);
            view.Cell = what.c;
            Map.Fog.MarkDirty();
            ui.Refresh();
        }

        private IEnumerator Defeated(GameEvent what)
        {
            HeroState hero = Game.State.Hero(what.a);
            HeroView view = hero != null ? Map.Hero(hero.id) : null;
            if (view != null)
            {
                Effects.Burst(view.Middle, new Color(0.6f, 0.1f, 0.1f), 40, 3.5f);
            }
            sound?.Play(Sfx.Defeat, 0.6f);
            ui.Log(what.text, what.player);
            Map.Sync();
            yield return Beat(0.4f);
        }

        private IEnumerator Gained(GameEvent what)
        {
            var kind = (ResourceKind)what.b;
            sound?.Play(kind == ResourceKind.Gold ? Sfx.Gold : Sfx.Resource);
            if (what.a >= 0)
            {
                Effects.Float(Map.Over(what.a), $"+{what.c} {Land.ResourceName(kind)}", new Color(1f, 0.9f, 0.5f), 3f);
                Effects.Rise(Map.Point(what.a, 0.6f), new Color(1f, 0.85f, 0.45f));
            }
            ui.Log(what.text, what.player);
            ui.Refresh();
            yield return Beat(0.25f);
        }

        private IEnumerator Leveled(GameEvent what)
        {
            HeroState hero = Game.State.Hero(what.a);
            sound?.Play(Sfx.LevelUp);
            ui.Log(what.text, what.player);
            if (hero != null && hero.cell >= 0)
            {
                Effects.Rise(Map.Point(hero.cell, 0.5f), new Color(1f, 0.92f, 0.5f), 45);
                Effects.Float(Map.Over(hero.cell), $"Level {what.b}", new Color(1f, 0.92f, 0.5f), 3.4f);
            }
            yield return Beat(0.5f);
        }

        private void Captured(GameEvent what)
        {
            sound?.Play(Sfx.Flag);
            ui.Log(what.text, what.player);
            Map.Sync();
            ui.Refresh();
        }

        private IEnumerator Taken(GameEvent what)
        {
            TownState town = Game.State.Town(what.a);
            sound?.Play(Sfx.Flag);
            ui.Log(what.text, what.player);
            Map.Sync();
            ui.Refresh();
            if (town != null && Viewer == what.player)
            {
                ui.Announce(town.name, what.text);
                yield return Beat(0.7f);
            }
        }

        // ------------------------------------------------------------------ battle

        private IEnumerator BattleStarted(GameEvent what)
        {
            sound?.Play(Sfx.BattleStart);
            sound?.PlayBattleMusic();
            Path.Clear();
            Battle.Begin(Game.Battle);
            ui.BeginBattle(Game.Battle);
            if (cameraRig != null)
            {
                float radius = (BattleState.HalfWidth + 2f) * Map.Layout.CellWidth;
                cameraRig.Frame(new Bounds(Map.Point(what.a), new Vector3(radius * 2f, 1f, radius * 1.4f)));
            }
            yield return Beat(0.8f);
        }

        private IEnumerator BattleEnded(GameEvent what)
        {
            var result = (BattleResult)what.a;
            yield return Beat(0.7f);
            Battle.End();
            ui.EndBattle(result, Game.Battle);
            sound?.PlayAdventureMusic();
            Map.Sync();
            ui.Refresh();
            yield return Beat(0.4f);
        }
    }
}
