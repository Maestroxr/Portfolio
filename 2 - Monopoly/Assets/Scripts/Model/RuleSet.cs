using System;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The rules of a match: the classic rules by default, with the switches of the official variants (the speed die of
    /// the Mega Edition, the short game with dealt title deeds, a time limit) and the most popular house rules (the Free
    /// Parking jackpot, double salary on GO, no rent from jail, no auctions, party cards).
    /// </summary>
    [Serializable]
    public class RuleSet
    {
        public int startingCash = 1500;
        public int salary = 200;
        /// <summary>House rule: landing exactly on GO pays twice the salary.</summary>
        public bool doubleSalaryOnGo;
        /// <summary>House rule: taxes and fines go into a pot that the next player to land on Free Parking collects.</summary>
        public bool freeParkingJackpot;
        /// <summary>What the bank puts into the Free Parking pot at the start and every time it is won (jackpot only).</summary>
        public int jackpotSeed;
        /// <summary>A property its visitor does not buy goes to auction (official rule); otherwise it stays with the bank.</summary>
        public bool auctions = true;
        /// <summary>The speed die of the Mega Edition, used once a player has passed GO.</summary>
        public bool speedDie;
        /// <summary>Short game: title deeds dealt to every player at the start.</summary>
        public int dealtProperties;
        /// <summary>Whether the dealt title deeds are paid for at their printed price (official short game).</summary>
        public bool payForDealtProperties = true;
        /// <summary>Houses on every property of a set before a hotel: 4, or 3 in the short game.</summary>
        public int housesForHotel = 4;
        /// <summary>The match ends at this many bankruptcies and the richest player wins (0: play to the last player).</summary>
        public int bankruptciesToEnd;
        /// <summary>The match ends after this many rounds and the richest player wins (0: no limit).</summary>
        public int roundLimit;
        /// <summary>House rule: owners in jail collect no rent.</summary>
        public bool noRentInJail;
        /// <summary>A few extra Chance and Community Chest cards (Robin Hood, free house, charity gala).</summary>
        public bool partyCards;
        public int jailFine = 50;
        /// <summary>Attempts at doubles before the fine must be paid.</summary>
        public int maxJailTurns = 3;
        /// <summary>The bank's supply of houses and hotels is limited (32 and 12, housing shortage rules).</summary>
        public bool limitedBuildings = true;
        /// <summary>Houses must be built and sold evenly across a set (official rule).</summary>
        public bool evenBuilding = true;

        public RuleSet Clone()
        {
            return (RuleSet)MemberwiseClone();
        }

        public bool IsValid(out string message)
        {
            if (startingCash < 100 || startingCash > 100000)
            {
                message = $"Starting cash {startingCash} has to be between 100 and 100000";
                return false;
            }
            if (salary < 0 || salary > 10000)
            {
                message = $"Salary {salary} has to be between 0 and 10000";
                return false;
            }
            if (housesForHotel < 1 || housesForHotel > 4)
            {
                message = $"Houses before a hotel ({housesForHotel}) has to be between 1 and 4";
                return false;
            }
            if (dealtProperties < 0 || dealtProperties > 7)
            {
                message = $"Dealt properties ({dealtProperties}) has to be between 0 and 7";
                return false;
            }
            if (roundLimit < 0 || roundLimit > 500)
            {
                message = $"Round limit ({roundLimit}) has to be between 0 (none) and 500";
                return false;
            }
            if (bankruptciesToEnd < 0)
            {
                message = "Bankruptcies to end cannot be negative";
                return false;
            }
            if (jailFine < 0 || maxJailTurns < 1)
            {
                message = "The jail fine cannot be negative and a jailed player needs at least one attempt";
                return false;
            }
            if (jackpotSeed < 0)
            {
                message = "The jackpot seed cannot be negative";
                return false;
            }
            message = "OK";
            return true;
        }

        /// <summary>Whether the match is decided by net worth (time limit or short game) rather than by the last player standing.</summary>
        public bool DecidedByNetWorth => roundLimit > 0 || bankruptciesToEnd > 0;
    }
}
