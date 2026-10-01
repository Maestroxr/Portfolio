using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>Where a run, or the game around it, is.</summary>
    internal enum RunPhase
    {
        Menu,
        Countdown,
        Running,
        Finishing,
        Dying,
        /// <summary>The run is over while others still run (a race); its player watches the ones who are out there.</summary>
        Watching
    }


    /// <summary>
    /// The run of one runner: how far it got, its coins and the points they are worth, its hearts and its power-ups. The
    /// game alone has one (the runner of the scene); a local race has one per seat, each with its own runner, camera and
    /// HUD, and its own phase (running, over the line, out of hearts, watching).
    /// </summary>
    internal sealed class RunnerRun
    {
        private readonly float[] powerUps = new float[PowerUps.Count];

        public RunnerRun(RunnerPlayer runner, RunnerCamera camera)
        {
            Runner = runner;
            Camera = camera;
        }

        public RunnerPlayer Runner { get; }

        /// <summary>The camera that films the runner (and whoever it watches once the run is over).</summary>
        public RunnerCamera Camera { get; }

        /// <summary>The phase of the runner in a local race; the game alone goes by the manager's.</summary>
        public RunPhase Phase { get; set; } = RunPhase.Menu;

        /// <summary>Seconds since <see cref="Phase"/> began.</summary>
        public float PhaseTime { get; set; }

        /// <summary>The farthest the runner got, in meters.</summary>
        public float Distance { get; private set; }

        /// <summary>Where the runner is along the track now (behind the line at the start of a race).</summary>
        public float Z { get; set; }

        /// <summary>Coin value collected (gems count more, double coins twice).</summary>
        public int Coins { get; private set; }

        /// <summary>The points the coins are worth.</summary>
        public float CoinPoints { get; private set; }

        public int Hearts { get; private set; }

        public int MaxHearts { get; private set; }

        public int HeartsLost { get; private set; }

        /// <summary>The endless run's record was announced in this run.</summary>
        public bool RecordAnnounced { get; set; }

        /// <summary>A point a meter, and the points of the coins.</summary>
        public float Score => Mathf.Floor(Distance) + CoinPoints;

        /// <summary>The seat that plays the run in a local race, or null.</summary>
        public LocalSeat Seat { get; set; }

        /// <summary>The run on the scoreboard of a local race, or null.</summary>
        public Racer Racer { get; set; }

        /// <summary>The run's HUD over its part of the screen in a local race, or null.</summary>
        public RunnerSeatHud Hud { get; set; }

        /// <summary>The run is over (across the line or out of hearts) and its player watches the others.</summary>
        public bool Done => Phase == RunPhase.Watching;

        /// <summary>The run reached the finish line.</summary>
        public bool Finished { get; set; }

        /// <summary>A new run with <paramref name="hearts"/> hearts: nothing collected, nothing lost, no power-ups.</summary>
        public void Reset(int hearts)
        {
            Distance = 0f;
            Z = 0f;
            Coins = 0;
            CoinPoints = 0f;
            MaxHearts = Mathf.Max(1, hearts);
            Hearts = MaxHearts;
            HeartsLost = 0;
            RecordAnnounced = false;
            Finished = false;
            PhaseTime = 0f;
            ClearPowerUps();
        }

        /// <summary>The runner is at <paramref name="z"/>: the distance is the farthest it got.</summary>
        public void RunTo(float z)
        {
            Z = z;
            Distance = Mathf.Max(Distance, z);
        }

        /// <summary>A coin of <paramref name="value"/> taken: double coins count it twice.</summary>
        public void Collect(int value, int pointsPerCoin)
        {
            int multiplier = IsPowerUpActive(PowerUpType.Multiplier) ? 2 : 1;
            AddCoins(value * multiplier, pointsPerCoin);
        }

        /// <summary>Coin value that counts as it is (a race's server already doubled it).</summary>
        public void AddCoins(int coins, int pointsPerCoin)
        {
            Coins += coins;
            CoinPoints += coins * pointsPerCoin;
        }

        /// <summary>A heart is gone; true when it was the last one.</summary>
        public bool LoseHeart()
        {
            Hearts = Mathf.Max(0, Hearts - 1);
            HeartsLost++;
            return Hearts <= 0;
        }

        public bool IsPowerUpActive(PowerUpType type)
        {
            return powerUps[(int)type] > 0f;
        }

        /// <summary>The seconds <paramref name="type"/> still lasts.</summary>
        public float PowerUpLeft(PowerUpType type)
        {
            return powerUps[(int)type];
        }

        public void GivePowerUp(PowerUpType type, float seconds)
        {
            powerUps[(int)type] = Mathf.Max(0f, seconds);
        }

        /// <summary>The power-ups run down by <paramref name="deltaTime"/>.</summary>
        public void TickPowerUps(float deltaTime)
        {
            for (int i = 0; i < powerUps.Length; i++)
            {
                if (powerUps[i] > 0f)
                {
                    powerUps[i] = Mathf.Max(0f, powerUps[i] - deltaTime);
                }
            }
        }

        public void ClearPowerUps()
        {
            for (int i = 0; i < powerUps.Length; i++)
            {
                powerUps[i] = 0f;
            }
        }

        /// <summary>The run enters <paramref name="phase"/> now.</summary>
        public void Enter(RunPhase phase)
        {
            Phase = phase;
            PhaseTime = 0f;
        }
    }
}
