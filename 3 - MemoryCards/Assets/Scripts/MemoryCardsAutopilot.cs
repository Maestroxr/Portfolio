using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Plays Memory Cards by itself: add it to any object in play mode to play the running level end to end. It
    /// remembers every card, so it only misses on purpose (<see cref="mistakeRate"/>); it follows the parade, cracks
    /// ice, plays the wild card and the clocks and, unless told otherwise, stays away from bombs.
    /// </summary>
    public class MemoryCardsAutopilot : MonoBehaviour
    {
        [SerializeField] internal MemoryCardsGameManager manager;
        [Range(0f, 1f)]
        [SerializeField] internal float mistakeRate = 0.15f;
        [Tooltip("Seconds between two clicks.")]
        [SerializeField] internal float interval = 0.4f;
        [SerializeField] internal bool avoidBombs = true;
        [SerializeField] internal bool useSpecials = true;
        [Tooltip("Starts a set with the wild card when it is still on the board.")]
        [SerializeField] internal bool wildFirst;

        private float wait;

        public MemoryCardsGameManager Manager => manager;

        private void Update()
        {
            if (manager == null)
            {
                manager = FindAnyObjectByType<MemoryCardsGameManager>();
            }
            if (manager == null || !manager.IsPlaying)
            {
                return;
            }
            wait -= Time.deltaTime;
            if (wait > 0f)
            {
                return;
            }
            wait = interval;
            MemoryRound round = manager.Round;
            if (round.MismatchShowing)
            {
                // Click a face up card to flip the mistake back.
                if (round.Revealed.Count > 0)
                {
                    manager.ClickCard(manager.ViewOf(round.Revealed[0]));
                }
                return;
            }
            MemoryCard next = Choose(round);
            if (next != null)
            {
                manager.ClickCard(manager.ViewOf(next));
            }
        }

        /// <summary>The card to click next.</summary>
        internal MemoryCard Choose(MemoryRound round)
        {
            int animal = -1;
            bool wildShowing = false;
            foreach (MemoryCard card in round.Revealed)
            {
                if (card.IsAnimal)
                {
                    animal = card.Animal;
                }
                else if (card.Kind == CardKind.Wild)
                {
                    wildShowing = true;
                }
            }
            int target = round.Rules.Parade ? round.ParadeTarget : -1;
            if (wildShowing && animal < 0)
            {
                return Pick(round, card => card.IsAnimal && (target < 0 || card.Animal == target), false);
            }
            if (animal >= 0)
            {
                if (Random.value < mistakeRate)
                {
                    MemoryCard wrong = Pick(round, card => card.IsAnimal && card.Animal != animal, true);
                    if (wrong != null)
                    {
                        return wrong;
                    }
                }
                MemoryCard partner = Pick(round, card => card.IsAnimal && card.Animal == animal, false);
                if (partner != null)
                {
                    return partner;
                }
            }
            if (wildFirst)
            {
                MemoryCard wild = Pick(round, card => card.Kind == CardKind.Wild, false);
                if (wild != null)
                {
                    return wild;
                }
            }
            if (!avoidBombs)
            {
                MemoryCard bomb = Pick(round, card => card.Kind == CardKind.Bomb, false);
                if (bomb != null)
                {
                    return bomb;
                }
            }
            if (useSpecials && Random.value < 0.25f)
            {
                MemoryCard special = Pick(round, card => card.Kind == CardKind.Clock || card.Kind == CardKind.Peek || card.Kind == CardKind.Wild, true);
                if (special != null)
                {
                    return special;
                }
            }
            return Pick(round, card => card.IsAnimal && (target < 0 || card.Animal == target), true)
                   ?? Pick(round, card => card.IsAnimal, true)
                   ?? Pick(round, card => card.Kind != CardKind.Bomb, true)
                   ?? Pick(round, card => true, true);
        }

        /// <summary>A hidden card that passes <paramref name="filter"/>: the first one, or a random one.</summary>
        private static MemoryCard Pick(MemoryRound round, Func<MemoryCard, bool> filter, bool randomly)
        {
            MemoryCard chosen = null;
            int seen = 0;
            foreach (MemoryCard card in round.Cards)
            {
                if (card.State != CardState.Hidden || !filter(card))
                {
                    continue;
                }
                if (!randomly)
                {
                    return card;
                }
                seen++;
                if (Random.Range(0, seen) == 0)
                {
                    chosen = card;
                }
            }
            return chosen;
        }
    }
}
