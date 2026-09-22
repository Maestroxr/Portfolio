using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>What the level select shows about a world.</summary>
    public struct WorldSummary
    {
        public int Index;
        public string Title;
        public string Tagline;
        public bool Unlocked;
        public int StarsRequired;
        public int Stars;
        public int MaxStars;
        public Sprite Mascot;
        public Color Accent;
        public Color AccentDark;
    }


    /// <summary>What the level select shows about a level.</summary>
    public struct LevelSummary
    {
        public int Index;
        /// <summary>Number of the level in the campaign (1 based); 0 for the endless run and free play.</summary>
        public int Number;
        public string Title;
        public string Description;
        public string Introduces;
        public LevelKind Kind;
        public LevelMode Mode;
        public string Twists;
        public string Board;
        public bool Unlocked;
        public string LockReason;
        public int Stars;
        public int BestScore;
        public string[] Goals;
        public int EndlessBoards;
        public int EndlessScore;
        public Sprite CardBack;
        public Color Accent;
        public Color AccentDark;
        public string World;
    }


    /// <summary>How the HUD is set up for a round.</summary>
    public struct RoundHud
    {
        public string Title;
        public LevelMode Mode;
        public bool Countdown;
        public int Hearts;
        public int MoveLimit;
        public int Sets;
        public int MatchSize;
        public bool Parade;
        public bool Endless;
        public int Board;
        public Color Accent;
        /// <summary>Several players: the scoreboard shows in place of the score and the clock.</summary>
        public bool Versus;
        /// <summary>What the HUD says under the title of a versus game ("VERSUS - 3 PLAYERS").</summary>
        public string VersusLabel;
    }


    /// <summary>What the results screen shows about a finished game.</summary>
    public struct RoundResult
    {
        public string LevelTitle;
        public LevelKind Kind;
        public bool Victory;
        public RoundEnd End;
        public int Stars;
        public string[] Goals;
        public bool[] GoalsReached;
        public int Score;
        public int Bonus;
        public float Time;
        public bool Countdown;
        public int Mistakes;
        public int Moves;
        public int BestCombo;
        public int Boards;
        public bool NewBest;
        public int Best;
        public bool HasNext;
        public Color Accent;
        /// <summary>A versus game: the title and the standings below take the place of the stars and the goals.</summary>
        public bool Versus;
        /// <summary>An online game: the next one starts from the room, not from the results.</summary>
        public bool Online;
        public string VersusTitle;
        public string VersusStandings;
    }
}
