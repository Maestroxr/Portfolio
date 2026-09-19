using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The Memory Cards campaign: the levels of the base <see cref="Campaign"/> grouped into worlds. On top of the base
    /// rule (a level opens once the campaign level before it is completed) every world is gated behind a number of
    /// stars. The endless run opens once <see cref="endlessAfter"/> campaign levels are done, and free play is always
    /// open. Only campaign levels earn stars.
    /// </summary>
    [CreateAssetMenu(fileName = "MemoryCardsCampaign", menuName = "Memory Cards/Campaign", order = 3)]
    public class MemoryCardsCampaign : Campaign
    {
        [Serializable]
        public class World
        {
            public string title;
            public CardWorld theme;
            [Tooltip("Stars from the whole campaign needed to enter the world.")]
            public int starsRequired;
        }

        [SerializeField] internal World[] worlds = new World[0];
        [Tooltip("Every animal of the game; save games refer to animals by their index here.")]
        [SerializeField] internal List<Sprite> animals = new List<Sprite>();
        [Tooltip("Campaign levels to complete before the endless run opens.")]
        [SerializeField] internal int endlessAfter = 6;

        public int WorldCount => worlds != null ? worlds.Length : 0;

        public IReadOnlyList<Sprite> Animals => animals;

        public World GetWorld(int index)
        {
            return worlds != null && index >= 0 && index < worlds.Length ? worlds[index] : null;
        }

        public CardWorld Theme(int world)
        {
            return GetWorld(world)?.theme;
        }

        public MemoryCardsLevel Level(int index)
        {
            return this[index] as MemoryCardsLevel;
        }

        public bool IsCampaignLevel(int index)
        {
            MemoryCardsLevel level = Level(index);
            return level != null && level.IsCampaign;
        }

        /// <summary>Indices of the levels of <paramref name="world"/>, in campaign order.</summary>
        public List<int> LevelsOf(int world)
        {
            var levels = new List<int>();
            for (int i = 0; i < Count; i++)
            {
                if (Level(i) != null && Level(i).World == world)
                {
                    levels.Add(i);
                }
            }
            return levels;
        }

        /// <summary>Index of the first level of <paramref name="kind"/>, or -1.</summary>
        public int IndexOf(LevelKind kind)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Level(i) != null && Level(i).Kind == kind)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// The animals a level deals from: its settings' own animals, else the animals of its world, else every
        /// animal of the game.
        /// </summary>
        public IReadOnlyList<Sprite> AnimalPool(MemoryCardsLevel level, MemoryCardsSettings settings = null)
        {
            MemoryCardsSettings rules = settings != null ? settings : level != null ? level.Settings : null;
            if (rules != null && rules.CardTypes != null && rules.CardTypes.Count > 0)
            {
                return rules.CardTypes;
            }
            CardWorld theme = level != null ? Theme(level.World) : null;
            if (theme != null && theme.animals != null && theme.animals.Count > 0)
            {
                return theme.animals;
            }
            return animals;
        }

        /// <summary>Stars are earned on campaign levels only.</summary>
        public override int MaxStars
        {
            get
            {
                int levels = 0;
                for (int i = 0; i < Count; i++)
                {
                    if (IsCampaignLevel(i))
                    {
                        levels++;
                    }
                }
                return levels * MaxStarsPerLevel;
            }
        }

        public override int TotalStars(CampaignProgress progress)
        {
            if (progress == null)
            {
                return 0;
            }
            int total = 0;
            for (int i = 0; i < Count; i++)
            {
                if (IsCampaignLevel(i))
                {
                    total += progress.Stars(i);
                }
            }
            return total;
        }

        /// <summary>Stars needed to enter the world of the level at <paramref name="index"/>.</summary>
        public int StarsRequired(int index)
        {
            MemoryCardsLevel level = Level(index);
            if (level == null || !level.IsCampaign)
            {
                return 0;
            }
            World world = GetWorld(level.World);
            return world != null ? world.starsRequired : 0;
        }

        /// <summary>Whether any level of <paramref name="world"/> is open (for campaign worlds: its first level).</summary>
        public bool IsWorldUnlocked(int world, CampaignProgress progress)
        {
            foreach (int index in LevelsOf(world))
            {
                if (IsUnlocked(index, progress))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool IsUnlocked(int index, CampaignProgress progress)
        {
            MemoryCardsLevel level = Level(index);
            if (level == null)
            {
                return false;
            }
            if (progress == null || level.IsFreePlay)
            {
                return true;
            }
            if (level.IsEndless)
            {
                return CompletedLevels(progress) >= endlessAfter;
            }
            int previous = PreviousLevel(index);
            bool previousDone = previous < 0 || progress.IsCompleted(previous);
            return previousDone && TotalStars(progress) >= StarsRequired(index);
        }

        /// <summary>The campaign level after <paramref name="index"/>; -1 after the last.</summary>
        public int NextLevel(int index)
        {
            for (int i = index + 1; i < Count; i++)
            {
                if (IsCampaignLevel(i))
                {
                    return i;
                }
            }
            return -1;
        }

        public int CompletedLevels(CampaignProgress progress)
        {
            int done = 0;
            for (int i = 0; i < Count; i++)
            {
                if (IsCampaignLevel(i) && progress != null && progress.IsCompleted(i))
                {
                    done++;
                }
            }
            return done;
        }

        /// <summary>Checks every level against the animals of its world. Returns the problems found.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            for (int i = 0; i < Count; i++)
            {
                MemoryCardsLevel level = Level(i);
                if (level == null)
                {
                    problems.Add($"Level {i} is missing or not a Memory Cards level.");
                    continue;
                }
                if (!level.IsCampaign)
                {
                    continue;
                }
                if (level.Settings == null)
                {
                    problems.Add($"{level.name}: no settings.");
                    continue;
                }
                if (GetWorld(level.World) == null)
                {
                    problems.Add($"{level.name}: world {level.World} does not exist.");
                }
                int pool = AnimalPool(level).Count;
                if (!level.Settings.AreSettingsValid(pool, out string message))
                {
                    problems.Add($"{level.name}: {message}");
                }
            }
            return problems;
        }

        private int PreviousLevel(int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                if (IsCampaignLevel(i))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
