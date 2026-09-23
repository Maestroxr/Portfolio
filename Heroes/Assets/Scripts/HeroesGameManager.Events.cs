using System.Collections;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Playing out what the rules recorded. Every event of the engine becomes something on the screen: a hero walking
    /// his path, a banner going up over a mine, a line in the log, an army fighting on the hexagons of the map. The
    /// director waits for each one, so the map never runs ahead of the story it is telling. The chronicle and the
    /// sounds are the viewer's: what another realm finds, learns or builds is its own affair, and shows only on the
    /// part of the map the viewer can see.
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
                    if (Own(what))
                    {
                        sound?.Play(Sfx.Recruit);
                        ui.Log(what.text, what.player);
                    }
                    ui.Refresh();
                    break;

                // ---------------------------------------------------------- what a hero finds
                case EventKind.ResourcesGained:
                    yield return Gained(what);
                    break;
                case EventKind.ArtifactFound:
                    if (Say(what, Sfx.Artifact, new Color(0.85f, 0.6f, 1f)))
                    {
                        yield return Beat(0.45f);
                    }
                    break;
                case EventKind.SpellLearned:
                    if (Say(what, Sfx.Page, new Color(0.6f, 0.8f, 1f)))
                    {
                        yield return Beat(0.35f);
                    }
                    break;
                case EventKind.ObjectVisited:
                    if (Own(what))
                    {
                        ui.Log(what.text, what.player);
                        if (what.c != 0)
                        {
                            sound?.Play(Sfx.Treasure, 0.7f);
                        }
                    }
                    break;
                case EventKind.ObjectCaptured:
                    Captured(what);
                    break;
                case EventKind.ObjectRemoved:
                    Map.Sync();
                    break;
                case EventKind.ExperienceGained:
                    if (Own(what))
                    {
                        ui.Log(what.text, what.player);
                    }
                    break;
                case EventKind.HeroLeveled:
                    yield return Leveled(what);
                    break;
                case EventKind.StatRaised:
                    Say(what, Sfx.LevelUp, new Color(1f, 0.9f, 0.5f));
                    break;

                // ---------------------------------------------------------- towns
                case EventKind.TownBuilt:
                    if (Own(what))
                    {
                        sound?.Play(Sfx.Build);
                        ui.Log(what.text, what.player);
                    }
                    ui.Refresh();
                    break;
                case EventKind.CreaturesRecruited:
                    if (Own(what))
                    {
                        sound?.Play(Sfx.Recruit);
                    }
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
                    if (battleShown && Battle.Running)
                    {
                        yield return Battle.Play(what);
                        ui.SetBattleRound(what.a);
                    }
                    break;
                case EventKind.CreaturesRaised:
                    // Necromancy, after a battle: the spells that raise the dead in one tell it with StackHealed.
                    if (Own(what))
                    {
                        ui.Log(what.text, what.player);
                    }
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
                    // The blows, shots and spells of a battle on the screen; one nobody here watches passes in no time.
                    if (battleShown && Battle.Running)
                    {
                        yield return Battle.Play(what);
                    }
                    break;
            }
        }

        private WaitForSeconds Beat(float seconds)
        {
            return new WaitForSeconds(seconds / Speed);
        }

        /// <summary>
        /// What a hero found or learned: in the log and heard when the hero is the viewer's, over his head when the
        /// viewer can see him. False when none of it showed.
        /// </summary>
        private bool Say(GameEvent what, Sfx sfx, Color color)
        {
            bool own = Own(what);
            if (own)
            {
                ui.Log(what.text, what.player);
                sound?.Play(sfx);
            }
            HeroState hero = Game.State.Hero(what.a);
            bool seen = hero != null && (own || InSight(hero.cell));
            if (seen && hero.cell >= 0 && !string.IsNullOrEmpty(what.text))
            {
                Effects.Float(Map.Over(hero.cell), Short(what.text), color, 2.8f);
            }
            return own || seen;
        }

        // ------------------------------------------------------------------ who is told

        /// <summary>
        /// Whether an event is the business of the player at this device: one of their own realm, or of no realm in
        /// particular. Only those are written in the chronicle (whose lines are in the second person, "joins your
        /// cause") and heard.
        /// </summary>
        private bool Own(GameEvent what)
        {
            return what.player < 0 || what.player == Viewer;
        }

        /// <summary>Whether the player at this device can see <paramref name="cell"/>, so what happens there shows on the map.</summary>
        private bool InSight(int cell)
        {
            PlayerState viewer = ViewerState;
            return cell >= 0 && (viewer == null || viewer.Explored(cell));
        }

        /// <summary>
        /// Whether a battle that is not played out on this screen is told in the chronicle: one of the viewer's own, or
        /// one fought where the viewer can see (the computer realms' battles in the shroud stay their own business).
        /// </summary>
        private bool InSight(BattleState battle)
        {
            return battle != null && (battle.attackerPlayer == Viewer || battle.defenderPlayer == Viewer ||
                                      InSight(battle.mapCell) || InSight(battle.attackerCell));
        }

        /// <summary>The name of the realm of <paramref name="player"/>, for a line about another realm.</summary>
        private string RealmName(int player)
        {
            PlayerState realm = Game.State.Player(player);
            return realm != null ? realm.name : "Another realm";
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
                // Online the camera stays with the player at this device on the turns of the others.
                if (hero != null && player.index == Viewer)
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

        /// <summary>A hero falls or flees: told when he was the viewer's or fell where the viewer can see.</summary>
        private IEnumerator Defeated(GameEvent what)
        {
            HeroState hero = Game.State.Hero(what.a);
            HeroView view = hero != null ? Map.Hero(hero.id) : null;
            // The rules have taken him off the map already; his figure still stands where he fell.
            bool seen = Own(what) || view != null && InSight(view.Cell);
            if (!seen)
            {
                Map.Sync();
                yield break;
            }
            if (view != null)
            {
                Effects.Burst(view.Middle, new Color(0.6f, 0.1f, 0.1f), 40, 3.5f);
            }
            sound?.Play(Sfx.Defeat, 0.6f);
            ui.Log(what.text, what.player);
            Map.Sync();
            if (selected != null && selected.id == what.a)
            {
                // The hero in hand is gone: another of the player's takes his place in the panels (the next with moves
                // left on the player's own turn), and the camera stays where the battle was.
                PlayerState viewer = ViewerState;
                HeroState next = viewer != null && Game.State.currentPlayer == viewer.index ? NextHero() : null;
                if (next == null && viewer != null && viewer.heroes.Count > 0)
                {
                    next = Game.State.Hero(viewer.heroes[0]);
                }
                selected = next;
                Path?.Clear();
                ui.Refresh();
            }
            yield return Beat(0.4f);
        }

        /// <summary>
        /// Resources come in: the viewer's are heard and written down (a day's income too), and a pile or a chest that
        /// is picked up where the viewer can see it rises from the map, whoever takes it.
        /// </summary>
        private IEnumerator Gained(GameEvent what)
        {
            bool own = Own(what);
            bool seen = what.a >= 0 && (own || InSight(what.a));
            if (!own && !seen)
            {
                yield break;
            }
            var kind = (ResourceKind)what.b;
            if (seen)
            {
                Effects.Float(Map.Over(what.a), $"+{what.c} {Land.ResourceName(kind)}", new Color(1f, 0.9f, 0.5f), 3f);
                Effects.Rise(Map.Point(what.a, 0.6f), new Color(1f, 0.85f, 0.45f));
            }
            if (own)
            {
                sound?.Play(kind == ResourceKind.Gold ? Sfx.Gold : Sfx.Resource);
                ui.Log(what.text, what.player);
                ui.Refresh();
            }
            yield return Beat(0.25f);
        }

        private IEnumerator Leveled(GameEvent what)
        {
            HeroState hero = Game.State.Hero(what.a);
            bool own = Own(what);
            bool seen = hero != null && hero.cell >= 0 && (own || InSight(hero.cell));
            if (own)
            {
                sound?.Play(Sfx.LevelUp);
                ui.Log(what.text, what.player);
            }
            if (seen)
            {
                Effects.Rise(Map.Point(hero.cell, 0.5f), new Color(1f, 0.92f, 0.5f), 45);
                Effects.Float(Map.Over(hero.cell), $"Level {what.b}", new Color(1f, 0.92f, 0.5f), 3.4f);
            }
            if (own || seen)
            {
                yield return Beat(0.5f);
            }
        }

        /// <summary>
        /// A mine changes hands. The rules' line ("now flies your flag") is the taker's; when another realm takes a mine
        /// the viewer can see (one of the viewer's own among them), the chronicle says whose flag it is now.
        /// </summary>
        private void Captured(GameEvent what)
        {
            Map.Sync();
            ui.Refresh();
            if (what.player < 0)
            {
                // The mines of a realm that is out of the game stand without a flag; the map shows it.
                return;
            }
            if (Own(what))
            {
                sound?.Play(Sfx.Flag);
                ui.Log(what.text, what.player);
                return;
            }
            MapObject mine = Game.State.Object(what.a);
            if (mine != null && mine.kind == ObjectKind.Mine && InSight(mine.cell))
            {
                sound?.Play(Sfx.Flag, 0.6f);
                ui.Log($"The {MapObjects.MineName((ResourceKind)mine.subtype)} now flies the flag of {RealmName(what.player)}.", what.player);
            }
        }

        /// <summary>A town changes hands: told to its new and its old owner, and to anyone who can see it.</summary>
        private IEnumerator Taken(GameEvent what)
        {
            TownState town = Game.State.Town(what.a);
            Map.Sync();
            ui.Refresh();
            bool told = Own(what) || what.b == Viewer || town != null && InSight(town.cell);
            if (!told)
            {
                yield break;
            }
            sound?.Play(Sfx.Flag);
            ui.Log(what.text, what.player);
            if (town != null && Viewer == what.player)
            {
                ui.Announce(town.name, what.text);
                yield return Beat(0.7f);
            }
        }
    }
}
