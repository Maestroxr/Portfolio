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
                title = hero.Name;
                picture = art.HeroPortrait(heroClass);
                bool mine = hero.owner == manager.Viewer;
                body = $"Level {hero.level} {HeroData.Class(heroClass).Name}" + (owner != null ? $" of {Owner(owner, manager)}" : "") + "\n" +
                       (mine
                           ? $"{UIKit.Glyph("movement")} {hero.movement} / {hero.maxMovement}   {UIKit.Glyph("mana")} {hero.mana}"
                           : $"An army of {Headcount(hero.army.TotalCreatures)}" + Threat(manager, game.Strength(hero)));
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
                    title = town.name;
                    picture = art.TownPortrait(town.faction, owner != null ? (int)owner.color : 4);
                    body = $"A {Land.FactionName(town.faction)} town, " + (owner != null ? $"held by {Owner(owner, manager)}" : "held by no one") +
                           (town.owner != manager.Viewer && !town.garrison.IsEmpty
                               ? $"\nA garrison of {Headcount(town.garrison.TotalCreatures)}" + Threat(manager, game.Strength(town.garrison))
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
                        ? $"{what.amount} {(what.amount == 1 ? def.Name : def.Plural)}"
                        : $"About {Rough(what.amount)} {def.Plural}";
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
                    title = MapObjects.MineName(kind);
                    body = $"{UIKit.Glyph(kind)} {MapObjects.MineYield(kind)} a day to its owner\n" +
                           (owner != null ? $"Held by {Owner(owner, manager)}" : "Held by no one");
                    return true;
                }
                case ObjectKind.Resource:
                {
                    var kind = (ResourceKind)Mathf.Clamp(what.subtype, 0, ResourceSet.Kinds - 1);
                    title = Land.ResourceName(kind);
                    body = $"{UIKit.Glyph(kind)} {what.amount} to pick up";
                    return true;
                }
                case ObjectKind.Artifact:
                {
                    ArtifactDef def = Artifacts.Get((ArtifactId)what.subtype);
                    title = def != null ? def.Name : "Artifact";
                    picture = art.Artifact((ArtifactId)what.subtype);
                    body = def != null ? def.Description : "";
                    return true;
                }
                case ObjectKind.Dwelling:
                {
                    CreatureDef def = Creatures.Get(what.subtype);
                    title = def != null ? $"Dwelling of the {def.Plural}" : MapObjects.Name(what.kind);
                    picture = def != null ? art.Portrait(def.Id) : null;
                    body = MapObjects.Hint(what.kind) + (def != null ? $"\n{what.amount} waiting, {UIKit.Cost(def.Cost)} each" : "");
                    return true;
                }
                default:
                {
                    title = MapObjects.Name(what.kind);
                    body = MapObjects.Hint(what.kind);
                    HeroState selected = manager.Selected;
                    if (selected != null && MapObjects.OncePerHero(what.kind) && what.visitedBy.Contains(selected.id))
                    {
                        body += $"\n<i>{selected.Name} has been here.</i>";
                    }
                    return true;
                }
            }
        }

        /// <summary>The name of a realm, or "you" for the player at this device.</summary>
        private static string Owner(PlayerState owner, HeroesGameManager manager)
        {
            return owner.index == manager.Viewer ? "you" : owner.name;
        }

        /// <summary>How many creatures, the way a scout would put it: "1 creature", "7 creatures", "about 40 creatures".</summary>
        private static string Headcount(int count)
        {
            return count < 10 ? $"{count} {(count == 1 ? "creature" : "creatures")}" : $"about {Rough(count)} creatures";
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
            string look = ratio < 0.5f ? "<color=#8CE07E>An easy fight</color>"
                : ratio < 0.9f ? "<color=#D8D07A>A fair fight</color>"
                : ratio < 1.5f ? "<color=#E8A064>A hard fight</color>"
                : "<color=#EE7A66>A deadly fight</color>";
            return $"\n{look} for {hero.Name}";
        }
    }
}
