using System;
using System.IO;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Types;
using NUnit.Framework;
using UnityEngine;

namespace BuyBidBlow.Dev.Tests.EditMode.Auction
{
    public sealed class ExpansionBalanceTests
    {
        private static MatchSettings Rules() => JsonUtility.FromJson<MatchSettings>(File.ReadAllText("Assets/Project/Content/Match/MatchSettings.json"));

        [Test]
        public void 초기경제_현금과재고가증가하고시작가만정확히두배가된다()
        {
            var rules = Rules(); rules.Validate();
            Assert.AreEqual(400, rules.StartingCash);
            Assert.AreEqual(0, rules.StartingShares);
            Assert.AreEqual(40, rules.GetInitialCompanySupply(3));
            Assert.AreEqual(70, rules.GetInitialCompanySupply(6));
            for (int seed = 0; seed < 100; seed++)
            {
                rules.StartingPriceMultiplier = 1;
                var before = rules.RollStartingPrices(new System.Random(seed));
                rules.StartingPriceMultiplier = 2;
                var after = rules.RollStartingPrices(new System.Random(seed));
                for (int company = 0; company < 3; company++) Assert.AreEqual(before[company] * 2, after[company]);
            }
            CollectionAssert.AreEqual(new[] { .35f, .5f, .75f }, rules.PriceChange.RiseChances);
        }

        [Test]
        public void 구간확률과상승확률_설정한분포를따른다()
        {
            var settings = Rules().PriceChange;
            var random = new System.Random(20261030);
            const int count = 30000;
            var bands = new int[3, 3]; var rises = new int[3];
            for (int sample = 0; sample < count; sample++)
                foreach (var change in PriceChangeGenerator.Generate(settings, random))
                {
                    int c = (int)change.Company, magnitude = Math.Abs(change.Amount);
                    Assert.That(magnitude, Is.InRange(1, 12));
                    bands[c, magnitude <= 3 ? 0 : magnitude <= 7 ? 1 : 2]++;
                    if (change.Amount > 0) rises[c]++;
                }
            for (int c = 0; c < 3; c++)
            {
                var w = settings.CompanyWeights[c]; var expected = new[] { w.Small, w.Medium, w.Large };
                for (int band = 0; band < 3; band++) Assert.AreEqual(expected[band], bands[c, band] / (double)count, .015, $"company {c} band {band}");
                Assert.AreEqual(settings.RiseChances[c], rises[c] / (double)count, .015, $"company {c} direction");
            }
        }

        [Test]
        public void 방향추첨_변동구간선택과독립적이다()
        {
            var settings = Rules().PriceChange;
            settings.CompanyWeights = new[] { new MagnitudeWeights { Small = 1 }, new MagnitudeWeights { Medium = 1 }, new MagnitudeWeights { Large = 1 } };
            for (int sign = 0; sign <= 1; sign++)
            {
                settings.RiseChances = new[] { (float)sign, (float)sign, (float)sign };
                for (int seed = 0; seed < 100; seed++)
                {
                    var changes = PriceChangeGenerator.Generate(settings, new System.Random(seed));
                    for (int c = 0; c < 3; c++)
                    {
                        Assert.AreEqual(sign == 1 ? 1 : -1, Math.Sign(changes[c].Amount));
                        Assert.That(Math.Abs(changes[c].Amount), Is.InRange(settings.AmountBands[c].Min, settings.AmountBands[c].Max));
                    }
                }
            }
        }

        [Test]
        public void 잘못된분포_경기생성전에거절한다()
        {
            var rules = Rules(); rules.PriceChange.CompanyWeights[0].Small = .9f;
            Assert.Throws<FormatException>(() => rules.Validate());
            rules = Rules(); rules.PriceChange.CompanyWeights[0].Small = float.NaN;
            Assert.Throws<FormatException>(() => rules.Validate());
            rules = Rules(); rules.PriceChange.AmountBands[1].Min = 2;
            Assert.Throws<FormatException>(() => rules.Validate());
            rules = Rules(); rules.PriceChange.RiseChances[0] = float.NaN;
            Assert.Throws<FormatException>(() => rules.Validate());
        }

        [Test]
        public void 여러시드_하한가와공개차트범위를유지하며완주한다()
        {
            var rules = Rules(); var id = new PlayerId("tester");
            for (int seed = 0; seed < 100; seed++)
            {
                var game = new MatchEngine(rules, seed, new[] { id }, 0);
                Assert.AreEqual(PriceChangeGenerator.HistoryRoundCount + 1, game.PublishedPriceCount);
                for (int r = 1; r <= rules.RoundCount; r++)
                {
                    foreach (Company c in Enum.GetValues(typeof(Company))) Assert.GreaterOrEqual(game.Price(c), rules.MinPrice);
                    game.Tick(game.Deadline);
                }
                Assert.IsTrue(game.Complete);
                Assert.AreEqual(rules.StartingCash, game.Total(id), "idle player keeps starting cash");
                Assert.AreEqual(0, game.Snapshot(id).Reserved);
            }
        }
    }
}
