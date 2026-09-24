using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>What flipping a card did.</summary>
    public enum FlipOutcome
    {
        /// <summary>Nothing: the card was not hidden or the round is over.</summary>
        Ignored,
        /// <summary>The card was frozen; the tap cracked the ice.</summary>
        Cracked,
        /// <summary>The card turned face up and waits for the rest of its set.</summary>
        Revealed,
        /// <summary>The card completed a set.</summary>
        Matched,
        /// <summary>The face up cards do not belong together; they flip back after the unflip delay.</summary>
        Mismatched,
        /// <summary>A complete set, but not the animal the parade asked for; it flips back.</summary>
        WrongOrder,
        /// <summary>A wild card matched every card of an animal.</summary>
        WildMatched,
        Bomb,
        Clock,
        Peek,

        // What the server of an online game sends besides flips (its FlipEvent), in the same field; a round never
        // produces them.

        /// <summary>Online: a mistake, or the cards of a player whose turn ended, turned back face down.</summary>
        FlippedBack = 20,
        /// <summary>Online: the player ran out of time; the cards they had face up turned back.</summary>
        TimedOut = 21,
        /// <summary>Online: the board is shown to everybody before the first turn (memorize).</summary>
        Preview = 22
    }


    /// <summary>Why a round ended.</summary>
    public enum RoundEnd
    {
        None,
        Cleared,
        OutOfTime,
        OutOfHearts,
        OutOfMoves
    }


    /// <summary>Everything a flip changed, for the view to animate.</summary>
    public sealed class FlipResult
    {
        public FlipOutcome Outcome;

        /// <summary>The card that was tapped.</summary>
        public MemoryCard Card;

        /// <summary>The set that matched or the face up cards that did not match.</summary>
        public readonly List<MemoryCard> Cards = new List<MemoryCard>();

        /// <summary>Cards of an earlier mismatch that flipped back because this card was tapped before they did.</summary>
        public readonly List<MemoryCard> FlippedBack = new List<MemoryCard>();

        /// <summary>Score change (negative for bombs).</summary>
        public int Points;

        /// <summary>Combo after the flip.</summary>
        public int Combo;

        /// <summary>Change of the clock in seconds (positive adds to a countdown or to the elapsed time).</summary>
        public float TimeChange;

        public bool HeartLost;

        /// <summary>The mistake count reached the shuffle interval: the hidden cards swap places once the mismatch is hidden.</summary>
        public bool Shuffle;

        /// <summary>A set of animals was completed (two wild cards that cancel out score, but complete none).</summary>
        public bool SetCompleted;

        public bool Ignored => Outcome == FlipOutcome.Ignored;

        public bool IsMatch => Outcome == FlipOutcome.Matched || Outcome == FlipOutcome.WildMatched;

        public bool IsMistake => Outcome == FlipOutcome.Mismatched || Outcome == FlipOutcome.WrongOrder;
    }


    /// <summary>
    /// The rules engine of one board. It knows nothing of the view: the manager passes the tapped card to
    /// <see cref="Flip"/>, animates the returned <see cref="FlipResult"/>, calls <see cref="HideMismatch"/> after the
    /// unflip delay and <see cref="Tick"/> every frame the clock runs. The server of an online game runs the same
    /// engine on a board it rebuilds from its tables for every flip (this file is compiled into the server module):
    /// cards that are face up in the deal are the set the player is turning.
    /// </summary>
    public sealed class MemoryRound
    {
        public const int PointsPerPair = 100;
        public const int PointsPerTriple = 160;
        public const int MaxComboMultiplier = 5;
        public const int WildBonus = 50;
        public const int ClockPoints = 25;
        public const int PeekPoints = 25;
        public const int BombPoints = 50;
        public const int PointsPerSecondLeft = 10;
        public const int PointsPerHeartLeft = 100;
        public const int PointsPerMoveLeft = 20;

        private readonly List<MemoryCard> cards;
        private readonly List<MemoryCard> revealed = new List<MemoryCard>();
        private readonly List<int> parade;
        private readonly int[] slots;
        private int mistakesSinceShuffle;

        public MemoryRound(RoundRules rules, Deal deal, int score = 0, int combo = 0)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            if (deal == null)
            {
                throw new ArgumentNullException(nameof(deal));
            }
            cards = deal.Cards;
            Animals = deal.Animals;
            parade = deal.Parade;
            slots = new int[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                slots[cards[i].Slot] = i;
                if (cards[i].State == CardState.Revealed)
                {
                    revealed.Add(cards[i]);
                }
            }
            Sets = 0;
            var counted = new HashSet<int>();
            foreach (MemoryCard card in cards)
            {
                if (card.IsAnimal && counted.Add(card.Animal))
                {
                    Sets++;
                    // A board rebuilt in the middle of a game: the animals already matched are the sets found.
                    if (IsAnimalMatched(card.Animal))
                    {
                        MatchedSets++;
                    }
                }
            }
            Score = score;
            Combo = combo;
            HeartsLeft = rules.Hearts;
            Clock = rules.HasTimeLimit ? rules.TimeLimit : 0f;
        }

        public RoundRules Rules { get; }

        public IReadOnlyList<MemoryCard> Cards => cards;

        /// <summary>Pool index of every animal of the round.</summary>
        public IReadOnlyList<int> Animals { get; }

        public IReadOnlyList<int> ParadeOrder => parade;

        /// <summary>Face up cards waiting for the rest of their set, or a mismatch waiting to flip back.</summary>
        public IReadOnlyList<MemoryCard> Revealed => revealed;

        public int Sets { get; }

        public int MatchedSets { get; private set; }

        public int Score { get; private set; }

        public int Combo { get; private set; }

        public int BestCombo { get; private set; }

        public int Mistakes { get; private set; }

        /// <summary>Attempts: every completed set and every mistake.</summary>
        public int Moves { get; private set; }

        public int HeartsLeft { get; private set; }

        public int BombsHit { get; private set; }

        /// <summary>Seconds left on a timed round, or the seconds counted so far (penalties and bonuses included).</summary>
        public float Clock { get; private set; }

        /// <summary>Seconds actually played.</summary>
        public float Elapsed { get; private set; }

        public bool HasCountdown => Rules.HasTimeLimit;

        /// <summary>The face up cards are a mismatch that flips back after the unflip delay.</summary>
        public bool MismatchShowing { get; private set; }

        public RoundEnd End { get; private set; }

        public bool IsOver => End != RoundEnd.None;

        public bool IsCleared => End == RoundEnd.Cleared;

        /// <summary>Moves left under a move limit, or -1 without one.</summary>
        public int MovesLeft => Rules.MoveLimit > 0 ? Mathf.Max(0, Rules.MoveLimit - Moves) : -1;

        /// <summary>Position in the parade: how many animals of it were matched.</summary>
        public int ParadeIndex { get; private set; }

        /// <summary>The animal (round index) the parade asks for next, or -1.</summary>
        public int ParadeTarget => Rules.Parade && ParadeIndex < parade.Count ? parade[ParadeIndex] : -1;

        public int ComboMultiplier => Mathf.Clamp(Combo, 1, MaxComboMultiplier);

        /// <summary>The card lying in <paramref name="slot"/>.</summary>
        public MemoryCard CardInSlot(int slot)
        {
            return slot >= 0 && slot < slots.Length ? cards[slots[slot]] : null;
        }

        /// <summary>Whether every card of an animal is matched.</summary>
        public bool IsAnimalMatched(int animal)
        {
            foreach (MemoryCard card in cards)
            {
                if (card.IsAnimal && card.Animal == animal && card.State != CardState.Matched)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Taps a card. A mismatch still showing flips back first.</summary>
        public FlipResult Flip(MemoryCard card)
        {
            var result = new FlipResult { Card = card, Combo = Combo };
            if (card == null || IsOver || card.State != CardState.Hidden)
            {
                result.Outcome = FlipOutcome.Ignored;
                return result;
            }
            if (MismatchShowing)
            {
                result.FlippedBack.AddRange(HideMismatch());
            }
            if (card.Frozen)
            {
                card.Frozen = false;
                result.Outcome = FlipOutcome.Cracked;
                return result;
            }
            switch (card.Kind)
            {
                case CardKind.Bomb:
                    HitBomb(card, result);
                    break;
                case CardKind.Clock:
                    card.State = CardState.Spent;
                    result.Outcome = FlipOutcome.Clock;
                    result.Points = ClockPoints;
                    result.TimeChange = HasCountdown ? Rules.ClockBonus : -Mathf.Min(Clock, Rules.ClockBonus);
                    Clock += result.TimeChange;
                    Score += result.Points;
                    break;
                case CardKind.Peek:
                    card.State = CardState.Spent;
                    result.Outcome = FlipOutcome.Peek;
                    result.Points = PeekPoints;
                    Score += result.Points;
                    break;
                default:
                    card.State = CardState.Revealed;
                    revealed.Add(card);
                    Evaluate(result);
                    break;
            }
            result.Combo = Combo;
            return result;
        }

        /// <summary>Flips a showing mismatch back. Returns the cards that turned face down.</summary>
        public List<MemoryCard> HideMismatch()
        {
            var hidden = new List<MemoryCard>();
            if (!MismatchShowing)
            {
                return hidden;
            }
            foreach (MemoryCard card in revealed)
            {
                if (card.State == CardState.Revealed)
                {
                    card.State = CardState.Hidden;
                    hidden.Add(card);
                }
            }
            revealed.Clear();
            MismatchShowing = false;
            return hidden;
        }

        /// <summary>
        /// Turns every face up card that waits for the rest of its set back down (a mistake still showing included)
        /// and ends the combo: the player ran out of time in a versus game. Returns the cards that turned face down.
        /// </summary>
        public List<MemoryCard> HideRevealed()
        {
            var hidden = new List<MemoryCard>();
            foreach (MemoryCard card in revealed)
            {
                if (card.State == CardState.Revealed)
                {
                    card.State = CardState.Hidden;
                    hidden.Add(card);
                }
            }
            revealed.Clear();
            MismatchShowing = false;
            Combo = 0;
            return hidden;
        }

        /// <summary>Runs the clock; a countdown that reaches zero ends the round.</summary>
        public void Tick(float deltaTime)
        {
            if (IsOver || deltaTime <= 0f)
            {
                return;
            }
            Elapsed += deltaTime;
            if (HasCountdown)
            {
                Clock -= deltaTime;
                if (Clock <= 0f)
                {
                    Clock = 0f;
                    End = RoundEnd.OutOfTime;
                }
            }
            else
            {
                Clock += deltaTime;
            }
        }

        /// <summary>
        /// Swaps the hidden cards around (face up and finished cards stay put). Returns the cards that moved; their
        /// <see cref="MemoryCard.Slot"/> holds the new slot.
        /// </summary>
        public List<MemoryCard> Shuffle(System.Random random)
        {
            var moving = new List<MemoryCard>();
            foreach (MemoryCard card in cards)
            {
                if (card.State == CardState.Hidden)
                {
                    moving.Add(card);
                }
            }
            var moved = new List<MemoryCard>();
            if (moving.Count < 2)
            {
                return moved;
            }
            var targets = new List<int>();
            foreach (MemoryCard card in moving)
            {
                targets.Add(card.Slot);
            }
            // Keep shuffling until at least half of the cards changed place, so a shuffle always shows.
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Dealer.Shuffle(targets, random);
                int changed = 0;
                for (int i = 0; i < moving.Count; i++)
                {
                    if (targets[i] != moving[i].Slot)
                    {
                        changed++;
                    }
                }
                if (changed * 2 >= moving.Count)
                {
                    break;
                }
            }
            for (int i = 0; i < moving.Count; i++)
            {
                if (moving[i].Slot != targets[i])
                {
                    moving[i].Slot = targets[i];
                    moved.Add(moving[i]);
                }
                slots[targets[i]] = moving[i].Id;
            }
            return moved;
        }

        /// <summary>Points a cleared board earns for the time, hearts and moves left.</summary>
        public int FinishBonus()
        {
            if (!IsCleared)
            {
                return 0;
            }
            int bonus = 0;
            if (HasCountdown)
            {
                bonus += Mathf.FloorToInt(Clock) * PointsPerSecondLeft;
            }
            if (Rules.Hearts > 0)
            {
                bonus += HeartsLeft * PointsPerHeartLeft;
            }
            if (Rules.MoveLimit > 0)
            {
                bonus += MovesLeft * PointsPerMoveLeft;
            }
            return bonus;
        }

        /// <summary>Adds the finish bonus to the score once the board is cleared. Returns the bonus.</summary>
        public int ApplyFinishBonus()
        {
            int bonus = FinishBonus();
            Score += bonus;
            return bonus;
        }

        /// <summary>
        /// Restores a saved round: card states, ice and slots come from the deal, the counters from here. A saved
        /// mismatch is not restored; its cards come back hidden.
        /// </summary>
        public void Restore(int matchedSets, int score, int combo, int bestCombo, int mistakes, int moves, int heartsLeft,
            float clock, float elapsed, int paradeIndex, int mistakesToShuffle, int bombsHit)
        {
            MatchedSets = matchedSets;
            Score = score;
            Combo = combo;
            BestCombo = bestCombo;
            Mistakes = mistakes;
            Moves = moves;
            HeartsLeft = heartsLeft;
            Clock = clock;
            Elapsed = elapsed;
            ParadeIndex = paradeIndex;
            mistakesSinceShuffle = mistakesToShuffle;
            BombsHit = bombsHit;
            revealed.Clear();
            foreach (MemoryCard card in cards)
            {
                if (card.State == CardState.Revealed)
                {
                    card.State = CardState.Hidden;
                }
                slots[card.Slot] = card.Id;
            }
            MismatchShowing = false;
        }

        /// <summary>Mistakes made since the last shuffle (saved with the round).</summary>
        public int MistakesSinceShuffle => mistakesSinceShuffle;

        private void HitBomb(MemoryCard card, FlipResult result)
        {
            card.State = CardState.Spent;
            BombsHit++;
            result.Outcome = FlipOutcome.Bomb;
            result.Points = -Mathf.Min(Score, BombPoints);
            Score += result.Points;
            Combo = 0;
            result.TimeChange = HasCountdown ? -Mathf.Min(Clock, Rules.BombPenalty) : Rules.BombPenalty;
            Clock += result.TimeChange;
            if (Rules.Hearts > 0)
            {
                LoseHeart(result);
            }
            if (HasCountdown && Clock <= 0f && End == RoundEnd.None)
            {
                Clock = 0f;
                End = RoundEnd.OutOfTime;
            }
        }

        private void Evaluate(FlipResult result)
        {
            int animal = -1;
            bool mixed = false;
            int wilds = 0;
            int animals = 0;
            foreach (MemoryCard card in revealed)
            {
                if (card.Kind == CardKind.Wild)
                {
                    wilds++;
                    continue;
                }
                animals++;
                if (animal < 0)
                {
                    animal = card.Animal;
                }
                else if (card.Animal != animal)
                {
                    mixed = true;
                }
            }

            if (mixed)
            {
                Miss(result, FlipOutcome.Mismatched);
                return;
            }
            if (wilds > 0)
            {
                if (animals == 0)
                {
                    if (wilds < 2)
                    {
                        result.Outcome = FlipOutcome.Revealed;
                        return;
                    }
                    // Two wild cards together: they cancel out for a bonus.
                    result.Cards.AddRange(revealed);
                    foreach (MemoryCard card in revealed)
                    {
                        card.State = CardState.Matched;
                    }
                    revealed.Clear();
                    result.Outcome = FlipOutcome.WildMatched;
                    result.Points = WildBonus * 2;
                    Score += result.Points;
                    return;
                }
                if (Rules.Parade && animal != ParadeTarget)
                {
                    Miss(result, FlipOutcome.WrongOrder);
                    return;
                }
                // The wild card takes every card of the animal off the board, hidden ones included.
                result.Cards.AddRange(revealed);
                foreach (MemoryCard card in cards)
                {
                    if (card.IsAnimal && card.Animal == animal && card.State == CardState.Hidden)
                    {
                        card.Frozen = false;
                        result.Cards.Add(card);
                    }
                }
                Complete(result, FlipOutcome.WildMatched, WildBonus);
                return;
            }
            if (animals < Rules.MatchSize)
            {
                result.Outcome = FlipOutcome.Revealed;
                return;
            }
            if (Rules.Parade && animal != ParadeTarget)
            {
                Miss(result, FlipOutcome.WrongOrder);
                return;
            }
            result.Cards.AddRange(revealed);
            Complete(result, FlipOutcome.Matched, 0);
        }

        private void Complete(FlipResult result, FlipOutcome outcome, int bonus)
        {
            foreach (MemoryCard card in result.Cards)
            {
                card.State = CardState.Matched;
            }
            revealed.Clear();
            MatchedSets++;
            Moves++;
            Combo++;
            BestCombo = Mathf.Max(BestCombo, Combo);
            int basePoints = Rules.MatchSize >= 3 ? PointsPerTriple : PointsPerPair;
            result.SetCompleted = true;
            result.Outcome = outcome;
            result.Points = basePoints * ComboMultiplier + bonus;
            Score += result.Points;
            if (HasCountdown && Rules.MatchTimeBonus > 0f)
            {
                result.TimeChange = Rules.MatchTimeBonus;
                Clock += Rules.MatchTimeBonus;
            }
            if (Rules.Parade)
            {
                AdvanceParade();
            }
            if (MatchedSets >= Sets)
            {
                End = RoundEnd.Cleared;
            }
            else if (Rules.MoveLimit > 0 && Moves >= Rules.MoveLimit)
            {
                End = RoundEnd.OutOfMoves;
            }
        }

        private void Miss(FlipResult result, FlipOutcome outcome)
        {
            result.Outcome = outcome;
            result.Cards.AddRange(revealed);
            MismatchShowing = true;
            Mistakes++;
            Moves++;
            Combo = 0;
            if (Rules.Hearts > 0)
            {
                LoseHeart(result);
            }
            if (Rules.ShuffleEvery > 0)
            {
                mistakesSinceShuffle++;
                if (mistakesSinceShuffle >= Rules.ShuffleEvery)
                {
                    mistakesSinceShuffle = 0;
                    result.Shuffle = true;
                }
            }
            if (End == RoundEnd.None && Rules.MoveLimit > 0 && Moves >= Rules.MoveLimit)
            {
                End = RoundEnd.OutOfMoves;
            }
        }

        private void LoseHeart(FlipResult result)
        {
            if (HeartsLeft <= 0)
            {
                return;
            }
            HeartsLeft--;
            result.HeartLost = true;
            if (HeartsLeft == 0 && End == RoundEnd.None)
            {
                End = RoundEnd.OutOfHearts;
            }
        }

        private void AdvanceParade()
        {
            while (ParadeIndex < parade.Count && IsAnimalMatched(parade[ParadeIndex]))
            {
                ParadeIndex++;
            }
        }
    }
}
