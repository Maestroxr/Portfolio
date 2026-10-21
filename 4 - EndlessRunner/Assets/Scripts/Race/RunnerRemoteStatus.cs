using System;
using System.Globalization;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>How the run of a seat is doing, as the shared screen tells its remote control.</summary>
    public enum RemoteRunState
    {
        /// <summary>The countdown, or the race has not begun.</summary>
        Ready = 0,
        Running = 1,
        /// <summary>Across the finish line.</summary>
        Finished = 2,
        /// <summary>Out of hearts, or left behind.</summary>
        Out = 3
    }


    /// <summary>
    /// What the shared screen tells the remote control of a seat it drives, and what the remote reads back
    /// (<see cref="OnlineGameController.PublishStatus"/>, <see cref="OnlineGameController.RemoteStatus"/>): the hearts,
    /// coins, score, distance and place of the runner, how its run is doing and the seconds left of each power-up. Numbers
    /// only, in the key=value form of the room options, so the panel words them in its own language
    /// (<see cref="RunnerRemotePanel"/>), and a device that takes its seat back starts from them (the power-ups, which no
    /// pose carries). Plain C#, covered by edit mode tests.
    /// </summary>
    public sealed class RunnerRemoteStatus
    {
        public const string HeartsKey = "h";
        public const string MaxHeartsKey = "m";
        public const string CoinsKey = "c";
        public const string ScoreKey = "s";
        public const string DistanceKey = "d";
        public const string LengthKey = "l";
        public const string PlaceKey = "p";
        public const string StateKey = "st";
        public const string PowerUpsKey = "pu";

        private readonly int[] powerUps = new int[PowerUps.Count];

        public int Hearts;

        public int MaxHearts;

        /// <summary>Coin value collected, as the run counts it.</summary>
        public int Coins;

        public int Score;

        /// <summary>Whole meters run.</summary>
        public int Distance;

        /// <summary>The length of the track in meters, 0 for the endless run.</summary>
        public int Length;

        /// <summary>The place in the race from 1, 0 while unknown.</summary>
        public int Place;

        public RemoteRunState State;

        /// <summary>The whole seconds <paramref name="type"/> still lasts, 0 for none.</summary>
        public float PowerUpSeconds(PowerUpType type)
        {
            return powerUps[(int)type];
        }

        public void SetPowerUpSeconds(PowerUpType type, float seconds)
        {
            powerUps[(int)type] = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        }

        /// <summary>The state of a run of the split screen, as the remote hears it.</summary>
        internal static RemoteRunState StateOf(RunPhase phase, bool finished)
        {
            switch (phase)
            {
                case RunPhase.Running: return RemoteRunState.Running;
                case RunPhase.Finishing: return RemoteRunState.Finished;
                case RunPhase.Dying: return RemoteRunState.Out;
                case RunPhase.Watching: return finished ? RemoteRunState.Finished : RemoteRunState.Out;
                default: return RemoteRunState.Ready;
            }
        }

        /// <summary>The status in the form the screen sends: <c>h=2;m=3;c=14;s=230;d=90;l=800;p=2;st=1;pu=0,7,0,0</c>.</summary>
        public string Write()
        {
            return RoomOptions.Write(HeartsKey, Hearts, MaxHeartsKey, MaxHearts, CoinsKey, Coins, ScoreKey, Score, DistanceKey, Distance,
                LengthKey, Length, PlaceKey, Place, StateKey, (int)State, PowerUpsKey, string.Join(",", powerUps));
        }

        /// <summary>The status a screen sent, or null for none (an empty text, or one of another game).</summary>
        public static RunnerRemoteStatus Parse(string data)
        {
            if (string.IsNullOrEmpty(data) || RoomOptions.Value(data, HeartsKey) == null)
            {
                return null;
            }
            var status = new RunnerRemoteStatus
            {
                Hearts = RoomOptions.Number(data, HeartsKey, 0),
                MaxHearts = RoomOptions.Number(data, MaxHeartsKey, 0),
                Coins = RoomOptions.Number(data, CoinsKey, 0),
                Score = RoomOptions.Number(data, ScoreKey, 0),
                Distance = RoomOptions.Number(data, DistanceKey, 0),
                Length = RoomOptions.Number(data, LengthKey, 0),
                Place = RoomOptions.Number(data, PlaceKey, 0),
                State = (RemoteRunState)Mathf.Clamp(RoomOptions.Number(data, StateKey, 0), 0, (int)RemoteRunState.Out)
            };
            string[] seconds = (RoomOptions.Value(data, PowerUpsKey) ?? string.Empty).Split(',');
            for (int i = 0; i < seconds.Length && i < status.powerUps.Length; i++)
            {
                if (int.TryParse(seconds[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int left))
                {
                    status.powerUps[i] = Math.Max(0, left);
                }
            }
            return status;
        }
    }
}
