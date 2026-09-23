# Portfolio - Heroes

A turn based strategy of heroes, towns and armies in the spirit of Heroes of Might and Magic III, on a hexagonal map
seen from above. One thing is deliberately different from the original: **there is no separate battlefield**. Armies
fight where they meet, on the map itself — the hexagonal grid is hidden while heroes travel and is drawn on the ground
only for the length of a battle, around the cells the two armies stand on, with the castles, mines and treasures of
that stretch of land still in place.

Up to four realms play: people sharing one device, computer players, or people on devices of their own in a room of
the game's server.

## Opening the project

The hexagons of the map are drawn by **Terrain Grid System 2** (Kronnect), a paid Asset Store package, so it is not in
this repository. Import it into `Assets/TerrainGridSystem` before opening the project; everything else, including the
art, comes with it. The game only uses the package's `Kronnect.TerrainGridSystem` assembly.

## Playing

- **New game** opens the campaign, *The Shattered Crown*: eight chapters that unlock one after the other, each with
  the stars it was won with, and four skirmish maps that can be played straight away.
- **The map**: click a hero to pick him up, then a cell to send him there. The trail of arrows shows the road, white
  for as far as today's movement carries him and red for the days after. Clicking a hero again opens his book; a town
  of yours opens its screen. The right hand column lists your heroes and towns; the buttons under it are next hero,
  sleep, the hero's book, the town and the menu, with **End Turn** below them.
- **A town** builds one thing a day, recruits from the dwellings it has built, trades in its marketplace, hires heroes
  in its tavern and teaches spells in its mage guild. The garrison holds the walls; creatures are dragged between it
  and a visiting hero by clicking one slot and then another.
- **A hero** carries his primary skills, his secondary skills, artifacts in eight slots, a spellbook, and an army of up
  to seven stacks. Movement, mana and morale all come from what he has learned and what he carries.
- **A battle** starts when a hero walks into an enemy army, a wandering army or a town that is defended. The grid
  appears, the stacks take the cells they stand on, and the bar along the bottom shows whose turn it is, the order of
  the round and what that stack may do: walk, strike, shoot, wait, defend, cast a spell or retreat.
- A scenario saves at the start of every day and continues from the title screen. Winning earns stars: three for
  winning inside the chapter's quick day count, two inside the slower one, one for winning at all.

## Rules

The rules are in `Scripts/Model`, which is an assembly of its own that knows nothing of Unity: the same code decides a
game on every device.

- **The map** is a hexagonal grid of nine terrains, with obstacles (forest, rocks, mountains, lakes, dead trees) that
  block the way and roads that speed it up. The generator lays out a map from a seed: zones per player, borders with
  guarded gates between them, a town per player, neutral towns, mines, dwellings, treasures, artifacts, the buildings
  a hero visits, and wandering armies that guard what is worth guarding.
- **Resources**: gold, wood, ore, mercury, sulfur, crystal and gems. Towns give gold a day, mines give their resource,
  and the marketplace trades at a rate that improves with every marketplace you hold.
- **Three factions** (Castle, Necropolis, Stronghold), seven creature tiers each, plus the creatures of the wilds.
  Every creature has attack, defense, damage, health, speed, weekly growth and a price, and some have abilities:
  flying, shooting, double attack, no retaliation, life drain, undead, breath, charge, regeneration.
- **Heroes** have six classes, four primary skills, thirteen secondary skills, seventeen spells and twenty four
  artifacts. Experience comes from battles and from the map; a level raises one primary skill and offers a choice of
  two secondary ones.
- **A battle** is fought on the cells of the map around the two armies, in rounds, in the order of each stack's speed.
  Damage counts attack against defense, morale gives a stack another turn and bad morale takes one away, luck doubles
  a blow, a shooter at close quarters strikes at half strength, and a stack that is struck strikes back once.
- **Victory** is set per scenario: defeat everyone, take a town, slay a monster, gather gold or find an artifact.

## Multiplayer

An online game is played in step, which is what the deterministic rules are for. Everybody in the room lays out the
same map from the room's seed, and every client runs the same rules over the same commands: what a player does is sent
to the action log of the base server, the server puts the actions of a room in one order and stamps each with a random
number, and every client applies them in that order and seeds the chances of that action with that number. The server
knows no rules of Heroes: it seats the table (`Server/Lib.cs`, one `HeroesSeat` row per seat), checks who may act for
which seat, keeps the clock, hands the realm of a player who leaves to the computer, and keeps statistics. The host's
client works out the moves of the computer players and of the wandering armies, and sends them like any other action.

The development player can play a game against itself to prove it: `-heroes-online host <folder>` and
`-heroes-online join <folder>` write a checksum of the whole game state after every five actions, and the two logs
carry the same numbers line for line.

## Code

| Folder | What is in it |
| --- | --- |
| `Scripts/Model` | The rules: the grid, the content tables, the state, the commands, the rules engine, the map generator, the computer players and the lockstep wrapper. No Unity. |
| `Scripts/View` | What is seen: the terrain built from the cells, the hexagonal grid (Terrain Grid System 2), the fog of war, the map, the battle, the paths, the puppets that animate the models. |
| `Scripts/UI` | The interface, built from code when a scenario starts: the resource bar, the heroes and towns, the log, the minimap, the campaign, the town, the hero's book, the battle bar and the dialogs. |
| `Scripts/Server` | The generated bindings of the game's server and the client stamped for them. |
| `Editor` | The builders: the art (models, materials, terrain layers, prefabs, interface sprites, fonts, sounds), the scenarios, the scene and the menu that runs them all. |
| `Tests/Editor` | The rules under test: the grid, the generator, paths, turns, battles, and that the same commands always give the same game. |
| `Server` | The server module: the seats of a room and the checks around the action log, on top of the Gamebox base server. |

Building everything: **Heroes > Build Everything** in the editor, or in batch mode
`-executeMethod Portfolio.Heroes.EditorTools.HeroesBuildMenu.BuildEverything`. The art builder writes every material,
prefab and sprite in place, so a rebuild keeps the references the scene already has.

## Art

Everything is drawn or downloaded under licences that allow it (see `Art/Licenses`): KayKit's character, dungeon,
halloween and medieval hexagon packs and Quaternius's characters, monsters, animals and items (CC0), textures from
ambientCG (CC0), icons from game-icons.net (CC BY 3.0), the fonts Cinzel and Alegreya (OFL), music by Kevin MacLeod
(CC BY) and sounds from OpenGameArt and Kenney (CC0). The interface itself — the framed panels, the buttons, the
slots, the bars and the stars — is drawn from code by the art builder, so it has no source files at all.
