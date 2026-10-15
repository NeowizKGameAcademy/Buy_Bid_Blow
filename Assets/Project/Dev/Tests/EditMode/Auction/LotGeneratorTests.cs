using System;
using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Systems.Auction;
using BuyBidBlow.Systems.Types;
using NUnit.Framework;

namespace BuyBidBlow.Dev.Tests.EditMode.Auction
{
    public sealed class LotGeneratorTests
    {
        private static readonly int[] AuctionRounds = { 2, 6, 10, 14 };

        [Test]
        public void EachAuctionCoversItsFourRoundsWithoutOverlap()
        {
            var generator = new LotGenerator(AuctionRounds, RandomPrices(17, 1), new Random(7));

            foreach (int start in AuctionRounds)
            {
                Lot first = generator.Next();
                Lot second = generator.Next();
                int[] rounds = { first.First.Round, first.Second.Round, second.First.Round, second.Second.Round };

                CollectionAssert.AreEquivalent(Enumerable.Range(start, 4), rounds);
                Assert.Less(first.First.Round, first.Second.Round);
                Assert.Less(second.First.Round, second.Second.Round);
            }

            Assert.AreEqual(0, generator.Remaining);
        }

        [Test]
        public void DirectionComparesWithPreviousClose()
        {
            // Straw falls, Timber is pinned at the floor, Brick rises - every round.
            var prices = new List<int[]>();
            for (int round = 0; round <= 5; round++)
                prices.Add(new[] { 30 - round, 5, 20 + round });

            var generator = new LotGenerator(new[] { 2 }, prices, new Random(3));
            var expected = new Dictionary<Company, Direction>
            {
                [Company.Straw] = Direction.Fall,
                [Company.Timber] = Direction.Flat,
                [Company.Brick] = Direction.Rise
            };

            for (int i = 0; i < 2; i++)
            {
                Lot lot = generator.Next();
                foreach (Info info in new[] { lot.First, lot.Second })
                {
                    Assert.AreEqual(expected[info.CompanyA], info.DirectionA);
                    Assert.AreEqual(expected[info.CompanyB], info.DirectionB);
                }
            }
        }

        [Test]
        public void EachRoundNamesTwoDifferentCompaniesInOrder()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var generator = new LotGenerator(AuctionRounds, RandomPrices(17, seed), new Random(seed));
                while (generator.Remaining > 0)
                {
                    Lot lot = generator.Next();
                    Assert.Less(lot.First.CompanyA, lot.First.CompanyB);
                    Assert.Less(lot.Second.CompanyA, lot.Second.CompanyB);
                }
            }
        }

        [Test]
        public void SameSeedGivesSameLots()
        {
            List<int[]> prices = RandomPrices(17, 1);
            var a = new LotGenerator(AuctionRounds, prices, new Random(42));
            var b = new LotGenerator(AuctionRounds, prices, new Random(42));

            while (a.Remaining > 0)
                Assert.AreEqual(a.Next().ToString(), b.Next().ToString());
        }

        [Test]
        public void ThrowsWhenPricesEndBeforeLastTargetRound()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LotGenerator(new[] { 14 }, RandomPrices(16, 1), new Random(1)));
        }

        private static List<int[]> RandomPrices(int roundCount, int seed)
        {
            var random = new Random(seed);
            var prices = new List<int[]>();
            for (int round = 0; round <= roundCount; round++)
                prices.Add(new[] { random.Next(5, 40), random.Next(5, 40), random.Next(5, 40) });
            return prices;
        }
    }
}
