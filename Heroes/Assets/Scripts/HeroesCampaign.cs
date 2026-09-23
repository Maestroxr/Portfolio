using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The chapters of the campaign and the skirmish maps, in one list. A chapter opens when the one before it is won; a
    /// skirmish map is always open. Stars are earned by winning a chapter quickly.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroesCampaign", menuName = "Heroes/Campaign", order = 3)]
    public class HeroesCampaign : Campaign
    {
        [field: SerializeField] public string Title { get; private set; } = "The Shattered Crown";

        public HeroesLevel Scenario(int index)
        {
            return this[index] as HeroesLevel;
        }

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
            List<int> chapters = Chapters;
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

        /// <summary>The next chapter after <paramref name="index"/>, or -1.</summary>
        public int NextChapter(int index)
        {
            List<int> chapters = Chapters;
            int position = chapters.IndexOf(index);
            return position >= 0 && position + 1 < chapters.Count ? chapters[position + 1] : -1;
        }

#if UNITY_EDITOR
        public void Configure(string title, System.Collections.Generic.List<GameLevel> levels)
        {
            Title = title;
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
