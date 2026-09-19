using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The score of a mission and what feeds it. Kills in quick succession build a combo that multiplies their points
    /// (x2 from 5 kills, up to x5 from 40); the combo breaks when no kill follows within <see cref="ComboWindow"/>
    /// seconds or when the ship is hit. Also keeps the statistics shown on the results screen.
    /// </summary>
    public class ScoreKeeper
    {
        public const float ComboWindow = 2.6f;

        public int Score { get; private set; }

        public int Combo { get; private set; }

        public int Multiplier => MultiplierFor(Combo);

        /// <summary>Seconds left before the combo breaks.</summary>
        public float ComboTime { get; private set; }

        public int MaxCombo { get; private set; }

        public int Kills { get; private set; }

        public int ShotsFired { get; private set; }

        public int ShotsLanded { get; private set; }

        public int Crystals { get; private set; }

        public float Accuracy => ShotsFired > 0 ? Mathf.Clamp01(ShotsLanded / (float)ShotsFired) : 0f;

        public static int MultiplierFor(int combo)
        {
            if (combo >= 40)
            {
                return 5;
            }
            if (combo >= 25)
            {
                return 4;
            }
            if (combo >= 12)
            {
                return 3;
            }
            return combo >= 5 ? 2 : 1;
        }

        /// <summary>Counts a kill worth <paramref name="points"/> and returns what it scored with the combo.</summary>
        public int AddKill(int points)
        {
            Kills++;
            Combo++;
            MaxCombo = Mathf.Max(MaxCombo, Combo);
            ComboTime = ComboWindow;
            int scored = points * Multiplier;
            Score += scored;
            return scored;
        }

        /// <summary>Points that do not feed the combo (pickups, bonuses).</summary>
        public int Add(int points)
        {
            Score += points;
            return points;
        }

        public void AddCrystal()
        {
            Crystals++;
        }

        public void ShotFired(int count = 1)
        {
            ShotsFired += count;
        }

        public void ShotLanded()
        {
            ShotsLanded++;
        }

        public void BreakCombo()
        {
            Combo = 0;
            ComboTime = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (Combo == 0)
            {
                return;
            }
            ComboTime -= deltaTime;
            if (ComboTime <= 0f)
            {
                BreakCombo();
            }
        }

        public void Reset(int score = 0)
        {
            Score = score;
            Combo = 0;
            ComboTime = 0f;
            MaxCombo = 0;
            Kills = 0;
            ShotsFired = 0;
            ShotsLanded = 0;
            Crystals = 0;
        }
    }
}
