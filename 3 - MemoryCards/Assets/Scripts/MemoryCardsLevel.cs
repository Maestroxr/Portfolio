using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>What kind of entry of the campaign a level is.</summary>
    public enum LevelKind
    {
        /// <summary>A level of the campaign with three stars to earn.</summary>
        Campaign,
        /// <summary>Board after board against one running clock; keeps a record instead of stars.</summary>
        Endless,
        /// <summary>A board dealt with the free play settings (default or custom, see the settings panel).</summary>
        FreePlay
    }


    /// <summary>How a level is played, for its badge in the level select and the HUD.</summary>
    public enum LevelMode
    {
        Classic,
        TimeAttack,
        Survival,
        MoveLimit,
        Parade,
        Endless,
        FreePlay
    }


    /// <summary>
    /// A level of the Memory Cards campaign: the world it belongs to, the board rules (its settings), the story shown
    /// in the level select and the goals of its three stars.
    /// </summary>
    [CreateAssetMenu(fileName = "MemoryCardsLevel", menuName = "Memory Cards/Level", order = 2)]
    public class MemoryCardsLevel : GameLevel
    {
        [field: SerializeField]
        public MemoryCardsSettings Settings { get; private set; }

        [SerializeField] internal LevelKind kind = LevelKind.Campaign;
        [Tooltip("Index of the world in the campaign.")]
        [SerializeField] internal int world;
        [SerializeField, TextArea] internal string description;
        [Tooltip("The twist the level introduces, shown as a NEW badge and as a tip when the level starts.")]
        [SerializeField] internal string introduces;
        [SerializeField, TextArea] internal string tip;
        [SerializeField] internal StarGoals goals = new StarGoals();

        public LevelKind Kind => kind;
        public int World => world;
        public string Description => description;
        public string Introduces => introduces;
        public string Tip => tip;
        public StarGoals Goals => goals;
        public bool IsCampaign => kind == LevelKind.Campaign;
        public bool IsEndless => kind == LevelKind.Endless;
        public bool IsFreePlay => kind == LevelKind.FreePlay;

        /// <summary>The badge of the level: what limits it or how it is played.</summary>
        public LevelMode Mode => ModeOf(kind, Settings != null ? Settings.ToRules() : null);

        public static LevelMode ModeOf(LevelKind kind, RoundRules rules)
        {
            if (kind == LevelKind.Endless)
            {
                return LevelMode.Endless;
            }
            if (kind == LevelKind.FreePlay || rules == null)
            {
                return LevelMode.FreePlay;
            }
            if (rules.Parade)
            {
                return LevelMode.Parade;
            }
            if (rules.HasTimeLimit)
            {
                return LevelMode.TimeAttack;
            }
            if (rules.Hearts > 0)
            {
                return LevelMode.Survival;
            }
            if (rules.MoveLimit > 0)
            {
                return LevelMode.MoveLimit;
            }
            return LevelMode.Classic;
        }

        /// <summary>Short names of the twists of <paramref name="rules"/>, for the level details.</summary>
        public static List<string> Twists(RoundRules rules)
        {
            var twists = new List<string>();
            if (rules == null)
            {
                return twists;
            }
            if (rules.PreviewTime > 0f)
            {
                twists.Add("Memorize");
            }
            if (rules.MatchSize == 3)
            {
                twists.Add("Triplets");
            }
            else if (rules.MatchSize > 3)
            {
                twists.Add($"Sets of {rules.MatchSize}");
            }
            if (rules.Parade)
            {
                twists.Add("Parade");
            }
            if (rules.ShuffleEvery > 0)
            {
                twists.Add("Shuffle");
            }
            if (rules.Wilds > 0)
            {
                twists.Add("Wild");
            }
            if (rules.Bombs > 0)
            {
                twists.Add("Bombs");
            }
            if (rules.Clocks > 0)
            {
                twists.Add("Clocks");
            }
            if (rules.Peeks > 0)
            {
                twists.Add("Peek");
            }
            if (rules.Frozen > 0)
            {
                twists.Add("Ice");
            }
            if (rules.Hearts > 0)
            {
                twists.Add("Hearts");
            }
            if (rules.MoveLimit > 0)
            {
                twists.Add("Moves");
            }
            if (rules.HasTimeLimit)
            {
                twists.Add("Timer");
            }
            return twists;
        }

        public override bool IsLevelValid(out string message)
        {
            if (kind != LevelKind.Campaign)
            {
                message = "OK";
                return true;
            }
            if (Settings == null)
            {
                message = "The level has no card settings.";
                return false;
            }
            return Settings.AreSettingsValid(out message);
        }
    }
}
