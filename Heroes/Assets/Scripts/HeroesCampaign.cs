using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The campaigns of the game and the skirmish maps, in one list. Each chapter belongs to one of the campaigns
    /// (<see cref="Arcs"/>, by <see cref="HeroesLevel.Arc"/>), which are stories of their own, each played as one realm
    /// and all open from the start. Within a campaign a chapter opens when the one before it is won; a skirmish map is
    /// always open. Stars are earned by winning a chapter quickly. A chapter keeps its place in the list for good (saved
    /// games and the progress refer to it by it), so new chapters only ever come at the end.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroesCampaign", menuName = "Heroes/Campaign", order = 3)]
    public class HeroesCampaign : Campaign
    {
        /// <summary>A campaign: a story told in chapters, the realm it is played as, and the short name on its tab.</summary>
        [System.Serializable]
        public sealed class Arc
        {
            public string title = "";
            /// <summary>The short name on the campaign's tab ("The Crown").</summary>
            public string tab = "";
            [TextArea(2, 5)] public string description = "";
            /// <summary>The realm the player leads through it, whose town is the picture of its tab.</summary>
            public Faction faction;
        }

        /// <summary>The title of the first campaign, which the game is named after.</summary>
        [field: SerializeField] public string Title { get; private set; } = "The Shattered Crown";

        [SerializeField] private List<Arc> arcs = new List<Arc>();

        public HeroesLevel Scenario(int index)
        {
            return this[index] as HeroesLevel;
        }

        /// <summary>The campaigns, in the order their tabs show. An asset from before there were several has one, the first.</summary>
        public IReadOnlyList<Arc> Arcs => arcs != null && arcs.Count > 0 ? arcs : new List<Arc> { new Arc { title = Title, tab = Title } };

        public int ArcCount => Arcs.Count;

        /// <summary>The campaign <paramref name="arc"/>, the nearest there is for a number out of range.</summary>
        public Arc GetArc(int arc)
        {
            IReadOnlyList<Arc> all = Arcs;
            return all[Mathf.Clamp(arc, 0, all.Count - 1)];
        }

        /// <summary>The campaign the chapter at <paramref name="index"/> belongs to (0 for anything else).</summary>
        public int ArcOf(int index)
        {
            HeroesLevel level = Scenario(index);
            return level != null && !level.IsSkirmish ? Mathf.Clamp(level.Arc, 0, ArcCount - 1) : 0;
        }

        /// <summary>Every chapter of every campaign, in the order of the list.</summary>
        public List<int> Chapters
        {
            get
            {
                var list = new List<int>();
                for (int i = 0; i < Count; i++)
                {
                    if (Scenario(i) != null && !Scenario(i).IsSkirmish)
                    {
                        list.Add(i);
                    }
                }
                return list;
            }
        }

        /// <summary>The chapters of the campaign <paramref name="arc"/>, in the order they are played.</summary>
        public List<int> ChaptersOf(int arc)
        {
            var list = new List<int>();
            foreach (int index in Chapters)
            {
                if (ArcOf(index) == arc)
                {
                    list.Add(index);
                }
            }
            return list;
        }

        public List<int> Skirmishes
        {
            get
            {
                var list = new List<int>();
                for (int i = 0; i < Count; i++)
                {
                    if (Scenario(i) != null && Scenario(i).IsSkirmish)
                    {
                        list.Add(i);
                    }
                }
                return list;
            }
        }

        public override int MaxStars => Chapters.Count * MaxStarsPerLevel;

        public int MaxStarsOf(int arc)
        {
            return ChaptersOf(arc).Count * MaxStarsPerLevel;
        }

        /// <summary>
        /// A skirmish map is always open, and so is the first chapter of every campaign; any other chapter once the one
        /// before it in its campaign is won.
        /// </summary>
        public override bool IsUnlocked(int index, CampaignProgress progress)
        {
            HeroesLevel level = Scenario(index);
            if (level == null)
            {
                return false;
            }
            if (level.IsSkirmish || progress == null)
            {
                return true;
            }
            List<int> chapters = ChaptersOf(ArcOf(index));
            int position = chapters.IndexOf(index);
            return position <= 0 || progress.IsCompleted(chapters[position - 1]);
        }

        public override int TotalStars(CampaignProgress progress)
        {
            if (progress == null)
            {
                return 0;
            }
            int total = 0;
            foreach (int chapter in Chapters)
            {
                total += progress.Stars(chapter);
            }
            return total;
        }

        /// <summary>The stars earned in the chapters of the campaign <paramref name="arc"/>.</summary>
        public int StarsOf(int arc, CampaignProgress progress)
        {
            int total = 0;
            if (progress != null)
            {
                foreach (int chapter in ChaptersOf(arc))
                {
                    total += progress.Stars(chapter);
                }
            }
            return total;
        }

        /// <summary>The next chapter after <paramref name="index"/> in its campaign, or -1 after its last.</summary>
        public int NextChapter(int index)
        {
            HeroesLevel level = Scenario(index);
            if (level == null || level.IsSkirmish)
            {
                return -1;
            }
            List<int> chapters = ChaptersOf(ArcOf(index));
            int position = chapters.IndexOf(index);
            return position >= 0 && position + 1 < chapters.Count ? chapters[position + 1] : -1;
        }

        /// <summary>The chapter of the campaign <paramref name="arc"/> to play next: the first open one not yet won, else its first.</summary>
        public int ChapterToPlay(int arc, CampaignProgress progress)
        {
            List<int> chapters = ChaptersOf(arc);
            foreach (int index in chapters)
            {
                if (IsUnlocked(index, progress) && (progress == null || progress.Stars(index) == 0))
                {
                    return index;
                }
            }
            return chapters.Count > 0 ? chapters[0] : -1;
        }

#if UNITY_EDITOR
        public void Configure(List<Arc> campaigns, List<GameLevel> levels)
        {
            arcs = new List<Arc>(campaigns);
            Title = arcs.Count > 0 ? arcs[0].title : Title;
            LevelList.Clear();
            LevelList.AddRange(levels);
        }
#endif
    }

    /// <summary>The campaign's records beyond the stars: scenarios won and lost, the fastest win and the most creatures slain.</summary>
    public class HeroesProgress : CampaignProgress
    {
        public const string WinsKey = "Wins";
        public const string LossesKey = "Losses";
        public const string SlainKey = "Slain";
        public const string ChapterKey = "Chapter";

        public HeroesProgress(IStorageStrategy storage, GameType type) : base(storage, type)
        {
        }

        public int Wins => Value(WinsKey);
        public int Losses => Value(LossesKey);
        public int MostSlain => (int)Record(SlainKey);
        /// <summary>The chapter begun last, whose campaign the campaign screen opens on.</summary>
        public int LastChapter => Value(ChapterKey, 0);

        public bool RecordWin(int level, int stars, int score, int slain)
        {
            SetValue(WinsKey, Wins + 1);
            RecordMax(SlainKey, slain);
            return RecordLevel(level, stars, score);
        }

        public void RecordLoss()
        {
            SetValue(LossesKey, Losses + 1);
        }

        public void RememberChapter(int level)
        {
            SetValue(ChapterKey, level);
        }

        public void ResetAll(int levels)
        {
            Reset(levels, new[] { SlainKey }, new[] { WinsKey, LossesKey, ChapterKey });
        }
    }
}
