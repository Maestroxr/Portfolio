using System.Globalization;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// How the game writes its numbers: money with thousands separators, race and lap times, rank changes, a car's
    /// health as a percentage. Plain text with no words in it (the windows and the HUD put the words around them), so it
    /// reads the same in every language and is cheap to test.
    /// </summary>
    public static class RaceFormat
    {
        /// <summary>The time shown before there is one (no lap driven yet).</summary>
        public const string NoTime = "-:--.--";

        /// <summary>"$12,300"; a negative amount "-$450".</summary>
        public static string Money(long amount)
        {
            string digits = System.Math.Abs(amount).ToString("N0", CultureInfo.InvariantCulture);
            return amount < 0 ? "-$" + digits : "$" + digits;
        }

        /// <summary>"1:02.34" for minutes, seconds and hundredths; <see cref="NoTime"/> for none.</summary>
        public static string Time(float seconds)
        {
            if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
            {
                return NoTime;
            }
            int hundredths = Mathf.RoundToInt(seconds * 100f);
            int minutes = hundredths / 6000;
            int rest = hundredths % 6000;
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}.{2:00}", minutes, rest / 100, rest % 100);
        }

        /// <summary>How far a driver moved on the ladder: "+3" up, "-2" down, "0" not at all.</summary>
        public static string RankChange(int before, int after)
        {
            int change = before - after;
            return change > 0 ? "+" + change.ToString(CultureInfo.InvariantCulture) : change.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>"#7".</summary>
        public static string Rank(int rank)
        {
            return "#" + rank.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>A share as a whole percentage: 0.456 is "46%".</summary>
        public static string Percent(float share)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(share) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>A count out of a most: "3/8".</summary>
        public static string OutOf(int count, int most)
        {
            return count.ToString(CultureInfo.InvariantCulture) + "/" + most.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>A whole number as it is written in every language of the game.</summary>
        public static string Number(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>Writes "1:02.34" into <paramref name="buffer"/> without making a string; returns the length.</summary>
        public static int TimeChars(float seconds, char[] buffer)
        {
            int hundredths = Mathf.Max(0, Mathf.RoundToInt(seconds * 100f));
            int minutes = Mathf.Min(hundredths / 6000, 99);
            int rest = hundredths % 6000;
            int length = 0;
            if (minutes >= 10)
            {
                buffer[length++] = (char)('0' + minutes / 10);
            }
            buffer[length++] = (char)('0' + minutes % 10);
            buffer[length++] = ':';
            buffer[length++] = (char)('0' + rest / 1000);
            buffer[length++] = (char)('0' + rest / 100 % 10);
            buffer[length++] = '.';
            buffer[length++] = (char)('0' + rest / 10 % 10);
            buffer[length++] = (char)('0' + rest % 10);
            return length;
        }
    }
}
