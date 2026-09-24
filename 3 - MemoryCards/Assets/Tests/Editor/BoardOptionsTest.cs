using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Portfolio.MemoryCards.Tests
{
    /// <summary>The board of a room in its options: what the host's client writes is what the server reads back.</summary>
    public class BoardOptionsTest
    {
        [Test]
        public void WhatIsWrittenIsReadBack()
        {
            var board = new RoundRules
            {
                Cards = 24, Columns = 6, MatchSize = 3, Wilds = 1, Bombs = 1, Clocks = 1, Frozen = 3, PreviewTime = 2.5f,
                Hearts = 3, TimeLimit = 90f
            };
            object[] pairs = BoardOptions.Pairs(VersusMatch.RulesFor(board), 12, "turn", 20);
            var options = new Dictionary<string, int>();
            for (int i = 0; i < pairs.Length; i += 2)
            {
                options[(string)pairs[i]] = Convert.ToInt32(pairs[i + 1]);
            }
            Assert.AreEqual(20, options["turn"], "the pairs given first come first");

            RoundRules read = BoardOptions.Read((key, fallback) => options.TryGetValue(key, out int value) ? value : fallback, out int animals);
            Assert.AreEqual(12, animals);
            Assert.AreEqual(24, read.Cards);
            Assert.AreEqual(6, read.Columns);
            Assert.AreEqual(3, read.MatchSize);
            Assert.AreEqual(1, read.Wilds);
            Assert.AreEqual(1, read.Bombs);
            Assert.AreEqual(1, read.Peeks, "the clock card of the level is dealt as a peek card");
            Assert.AreEqual(3, read.Frozen);
            Assert.AreEqual(2.5f, read.PreviewTime, 0.001f);
            Assert.AreEqual(0, read.Hearts, "what limits a single player does not travel");
            Assert.AreEqual(0f, read.TimeLimit);
            Assert.IsTrue(read.IsValid(animals, out string message), message);
        }

        [Test]
        public void MissingKeysMakeAPlainBoardAndNumbersStayWithinReason()
        {
            RoundRules read = BoardOptions.Read((key, fallback) => key == BoardOptions.CardsKey ? 5000 : key == BoardOptions.BombsKey ? -4 : fallback,
                out int animals);
            Assert.AreEqual(BoardOptions.MaxValue, read.Cards);
            Assert.AreEqual(0, read.Bombs);
            Assert.AreEqual(4, read.Columns);
            Assert.AreEqual(2, read.MatchSize);
            Assert.AreEqual(30, animals);
            Assert.IsFalse(read.IsValid(animals, out _), "and the check of the rules refuses what cannot be dealt");
        }
    }
}
