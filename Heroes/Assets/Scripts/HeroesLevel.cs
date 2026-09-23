using Gamebox;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// A scenario: the recipe of its map (<see cref="MapSpec"/>: size, seats, riches, monsters, what the story places on
    /// purpose), how it is won, and the story told before and after. Campaign scenarios have one person against the
    /// computer; the skirmish and online maps take their seats from the setup screen or the room.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroesLevel", menuName = "Heroes/Level", order = 2)]
    public class HeroesLevel : GameLevel
    {
        [field: SerializeField] public MapSpec Map { get; private set; } = new MapSpec();

        [field: SerializeField, TextArea(3, 10)] public string Intro { get; private set; } = "";

        [field: SerializeField, TextArea(2, 6)] public string Outro { get; private set; } = "";

        [field: SerializeField] public string Goal { get; private set; } = "Defeat all enemies.";

        /// <summary>Win by these days for three and two stars (one for any win).</summary>
        [field: SerializeField] public int ThreeStarDays { get; private set; } = 60;

        [field: SerializeField] public int TwoStarDays { get; private set; } = 90;

        /// <summary>The scenario is a map for the skirmish and online play (its seats come from the setup) rather than a chapter of the campaign.</summary>
        [field: SerializeField] public bool IsSkirmish { get; private set; }

        /// <summary>Where the chapter sits on the campaign map, in thousandths of its width and height.</summary>
        [field: SerializeField] public Vector2 MapPosition { get; private set; } = new Vector2(500, 500);

        public override bool IsLevelValid(out string message)
        {
            if (Map == null || Map.players.Count < 1)
            {
                message = "The map has no players.";
                return false;
            }
            if (Map.columns < 20 || Map.rows < 20)
            {
                message = "The map is too small.";
                return false;
            }
            message = "OK";
            return true;
        }

        public int StarsFor(int days)
        {
            if (days <= ThreeStarDays)
            {
                return 3;
            }
            return days <= TwoStarDays ? 2 : 1;
        }

#if UNITY_EDITOR
        public void Configure(MapSpec map, string goal, string intro, string outro, int threeStars, int twoStars,
            bool skirmish, Vector2 position)
        {
            Map = map;
            Goal = goal;
            Intro = intro;
            Outro = outro;
            ThreeStarDays = threeStars;
            TwoStarDays = twoStars;
            IsSkirmish = skirmish;
            MapPosition = position;
        }
#endif
    }
}
