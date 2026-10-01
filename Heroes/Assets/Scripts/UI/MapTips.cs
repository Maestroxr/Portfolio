using UnityEngine;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// What the tooltip of the map says about a cell: a hero with his portrait and army, a town with its picture, a
    /// wandering army with its creature, how hard a fight it looks for the hero in hand, a mine with what it yields, a
    /// pile with what lies in it, a place with what it gives. Bare ground says nothing.
    /// </summary>
    public static class MapTips
    {
        public static bool Describe(HeroesGameManager manager, int cell, out string title, out string body, out Sprite picture)
        {
            title = null;
            body = null;
            picture = null;
            HeroesGame game = manager.Game;
            if (game == null || cell < 0)
            {
                return false;
            }
            GameState state = game.State;
            PlayerState viewer = manager.ViewerState;
            if (viewer != null && !viewer.Explored(cell))
            {
                return false;
            }
            HeroesArt art = manager.Art;
            HeroState hero = state.HeroAt(cell);
            if (hero != null)
            {
                HeroClass heroClass = hero.Def != null ? hero.Def.Class : HeroClass.Knight;
                PlayerState owner = state.Player(hero.owner);
                title = Words.Name(hero.Name);
                picture = art.HeroPortrait(heroClass);
                bool mine = hero.owner == manager.Viewer;
                string className = Words.T(HeroData.Class(heroClass).Name);
                body = (owner == null ? Words.F("Level {0} {1}", hero.level, className)
                           : owner.index == manager.Viewer ? Words.F("Level {0} {1} of you", hero.level, className)
                           : Words.F("Level {0} {1} of {2}", hero.level, className, Words.Name(owner.name))) + "\n" +
                       (mine
                           ? $"{UIKit.Glyph("movement")} {hero.movement} / {hero.maxMovement}   {UIKit.Glyph("mana")} {hero.mana}"
                           : Words.F("An army of {0}", Headcount(hero.army.TotalCreatures)) + Threat(manager, game.Strength(hero)));
                return true;
            }
            MapObject what = state.ObjectAt(cell);
            if (what == null || what.removed)
            {
                return false;
            }
            switch (what.kind)
            {
                case ObjectKind.Town:
                {
                    TownState town = state.Town(what.subtype);
                    if (town == null)
                    {
                        return false;
                    }
                    PlayerState owner = state.Player(town.owner);
                    title = Words.Name(town.name);
                    picture = art.TownPortrait(town.faction, owner != null ? (int)owner.color : 4);
                    string faction = Words.T(Land.FactionName(town.faction));
                    body = (owner == null ? Words.F("A {0} town, held by no one", faction)
                               : owner.index == manager.Viewer ? Words.F("A {0} town, held by you", faction)
                               : Words.F("A {0} town, held by {1}", faction, Words.Name(owner.name))) +
                           (town.owner != manager.Viewer && !town.garrison.IsEmpty
                               ? "\n" + Words.F("A garrison of {0}", Headcount(town.garrison.TotalCreatures)) + Threat(manager, game.Strength(town.garrison))
                               : "");
                    return true;
                }
                case ObjectKind.Monster:
                {
                    CreatureDef def = Creatures.Get(what.subtype);
                    if (def == null)
                    {
                        return false;
                    }
                    title = what.amount < 10
                        ? $"{what.amount} {Words.Name(what.amount == 1 ? def.Name : def.Plural)}"
                        : Words.F("About {0} {1}", Rough(what.amount), Words.Name(def.Plural));
                    picture = art.Portrait(def.Id);
                    body = $"{UIKit.Glyph("attack")} {def.Attack}  {UIKit.Glyph("defense")} {def.Defense}  " +
                           $"{UIKit.Glyph("damage")} {def.MinDamage}-{def.MaxDamage}  {UIKit.Glyph("health")} {def.Health}" +
                           Threat(manager, def.Value * what.amount);
                    return true;
                }
                case ObjectKind.Mine:
                {
                    var kind = (ResourceKind)Mathf.Clamp(what.subtype, 0, ResourceSet.Kinds - 1);
                    PlayerState owner = state.Player(what.owner);
                    title = Words.T(MapObjects.MineName(kind));
                    body = Words.F("{0} a day to its owner", $"{UIKit.Glyph(kind)} {MapObjects.MineYield(kind)}") + "\n" +
                           (owner == null ? Words.T("Held by no one")
                               : owner.index == manager.Viewer ? Words.T("Held by you")
                               : Words.F("Held by {0}", Words.Name(owner.name)));
                    return true;
                }
                case ObjectKind.Resource:
                {
                    var kind = (ResourceKind)Mathf.Clamp(what.subtype, 0, ResourceSet.Kinds - 1);
                    title = Words.T(Land.ResourceName(kind));
                    body = Words.F("{0} to pick up", $"{UIKit.Glyph(kind)} {what.amount}");
                    return true;
                }
                case ObjectKind.Artifact:
                {
                    ArtifactDef def = Artifacts.Get((ArtifactId)what.subtype);
                    title = Words.T(def != null ? def.Name : "Artifact");
                    picture = art.Artifact((ArtifactId)what.subtype);
                    body = def != null ? Words.T(def.Description) : "";
                    return true;
                }
                case ObjectKind.Dwelling:
                {
                    CreatureDef def = Creatures.Get(what.subtype);
                    title = def != null ? Words.F("Dwelling of the {0}", Words.Name(def.Plural)) : Words.T(MapObjects.Name(what.kind));
                    picture = def != null ? art.Portrait(def.Id) : null;
                    body = Words.T(MapObjects.Hint(what.kind)) + (def != null ? "\n" + Words.F("{0} waiting, {1} each", what.amount, UIKit.Cost(def.Cost)) : "");
                    return true;
                }
                default:
                {
                    title = Words.T(MapObjects.Name(what.kind));
                    body = Words.T(MapObjects.Hint(what.kind));
                    HeroState selected = manager.Selected;
                    if (selected != null && MapObjects.OncePerHero(what.kind) && what.visitedBy.Contains(selected.id))
                    {
                        body += "\n<i>" + Words.F("{0} has been here.", Words.Name(selected.Name)) + "</i>";
                    }
                    return true;
                }
            }
        }

        /// <summary>How many creatures, the way a scout would put it: "1 creature", "7 creatures", "about 40 creatures".</summary>
        private static string Headcount(int count)
        {
            return count == 1 ? Words.T("1 creature") : count < 10 ? Words.F("{0} creatures", count) : Words.F("about {0} creatures", Rough(count));
        }

        /// <summary>How many, the way a scout would put it: exact while few, rounded as they grow.</summary>
        private static string Rough(int count)
        {
            if (count < 10)
            {
                return count.ToString();
            }
            if (count < 50)
            {
                return (count / 5 * 5).ToString();
            }
            return (count / 10 * 10).ToString();
        }

        /// <summary>How the fight would look to the hero in hand: an easy one, a fair one, a hard one, a deadly one.</summary>
        private static string Threat(HeroesGameManager manager, int strength)
        {
            HeroState hero = manager.Selected;
            if (hero == null || manager.Game == null || strength <= 0)
            {
                return "";
            }
            int mine = Mathf.Max(1, manager.Game.Strength(hero));
            float ratio = strength / (float)mine;
            string look = ratio < 0.5f ? "<color=#8CE07E>" + Words.T("An easy fight") + "</color>"
                : ratio < 0.9f ? "<color=#D8D07A>" + Words.T("A fair fight") + "</color>"
                : ratio < 1.5f ? "<color=#E8A064>" + Words.T("A hard fight") + "</color>"
                : "<color=#EE7A66>" + Words.T("A deadly fight") + "</color>";
            return "\n" + Words.F("{0} for {1}", look, Words.Name(hero.Name));
        }
    }
}
