using System.IO;
using System.Linq;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Round;
using BuyBidBlow.UI.Ticker;
using NUnit.Framework;

namespace BuyBidBlow.Dev.Tests.EditMode.Ticker
{
    public sealed class TickerScheduleTests
    {
        const string SchedulePath = "Assets/Project/Content/Props/TickerBoard/TickerSchedule.json";
        const double Prepare = 95, Start = 100;

        // Three 60-second rounds: R1 100-160, R2 160-220, R3 220-280.
        static TimelineSpan[] Spans() => MatchSchedule.Build(new MatchSettings
        {
            RoundCount = 3, RoundSeconds = 60, AuctionRounds = new int[0], AuctionRoundSeconds = 60,
        }, Prepare, Start).ToArray();

        static TickerSchedule Real() => TickerSchedule.FromJson(File.ReadAllText(SchedulePath));

        static string[] At(TickerSchedule schedule, double now, out TickerShot[] shots)
        {
            shots = new TickerShot[schedule.BoardCount];
            Assert.IsTrue(schedule.Resolve(Spans(), now, shots));
            return shots.Select(s => s.Channel?.id).ToArray();
        }

        [Test] public void ProjectScheduleIsValid()
        {
            CollectionAssert.IsEmpty(Real().Validate(new[] { "roundover", "transition" }));
        }

        [Test] public void NothingIsScheduledWithoutATimeline()
        {
            Assert.IsFalse(Real().Resolve(new TimelineSpan[0], 0, new TickerShot[3]));
        }

        [Test] public void CountdownBeforeTheStartIsMirrored()
        {
            CollectionAssert.AreEqual(new[] { "timer", "timer", "timer" }, At(Real(), 96, out _));
        }

        [Test] public void PlaylistRunsFromTheTimelineStart()
        {
            CollectionAssert.AreEqual(new[] { "company:cycle", "overview", "timer" }, At(Real(), 101, out var shots));
            Assert.AreEqual(Prepare, shots[0].Since);
            Assert.AreEqual(-1, shots[0].Slot);
            CollectionAssert.AreEqual(new[] { "overview", "overview", "overview" }, At(Real(), Prepare + 21, out _));
        }

        [Test] public void CycleChannelRotatesCompanies()
        {
            At(Real(), Prepare + 8.5, out var shots);
            Assert.AreEqual(1, shots[0].Company);
            At(Real(), Prepare + 16.5, out shots);
            Assert.AreEqual(2, shots[0].Company);
        }

        [Test] public void LastTenSecondsOfARoundShowTheTimerEverywhere()
        {
            CollectionAssert.AreEqual(new[] { "timer", "timer", "timer" }, At(Real(), 155, out _));
        }

        // The round-over and transition videos are not settled yet, so the project schedule leaves them out.
        [Test] public void RoundChangeAndMatchEndKeepTheRegularScreens()
        {
            var regular = new[] { "company:cycle", "overview", "timer", "company:0", "company:1", "company:2" };
            foreach (double now in new[] { 160.5, 165.5, 281 })
                Assert.IsTrue(At(Real(), now, out _).All(regular.Contains), $"regular screens at {now}");
        }

        // Sequence rules stay supported for when the videos return.
        static TickerSchedule Sequenced() => TickerSchedule.FromJson(@"{
            ""boards"": [""L"", ""C"", ""R""], ""cutSeconds"": 0, ""companySeconds"": 8,
            ""channels"": [{ ""id"": ""overview"", ""screen"": ""Overview"" }, { ""id"": ""wide"", ""video"": ""roundover"", ""wide"": true },
                           { ""id"": ""pig"", ""video"": ""transition"" }, { ""id"": ""company:0"", ""screen"": ""Company"", ""company"": 0 },
                           { ""id"": ""company:1"", ""screen"": ""Company"", ""company"": 1 }, { ""id"": ""company:2"", ""screen"": ""Company"", ""company"": 2 }],
            ""playlist"": [{ ""layout"": ""mirror"", ""channel"": ""overview"", ""seconds"": 10 }],
            ""rules"": [{ ""span"": ""Round"", ""edge"": ""start"", ""from"": 0, ""priority"": 90, ""skipFirst"": true, ""sequence"": [
                            { ""layout"": ""span"", ""channel"": ""wide"", ""seconds"": 5 }, { ""layout"": ""mirror"", ""channel"": ""pig"", ""seconds"": 3 },
                            { ""layout"": ""split"", ""channels"": [""company:0"", ""company:1"", ""company:2""], ""seconds"": 8 }] },
                        { ""span"": ""Match"", ""edge"": ""end"", ""from"": 0, ""to"": 5, ""priority"": 90, ""layout"": ""span"", ""channel"": ""wide"" }]
        }");

        [Test] public void RoundChangePlaysTheSequence()
        {
            CollectionAssert.IsEmpty(Sequenced().Validate(new[] { "roundover", "transition" }));
            CollectionAssert.AreEqual(new[] { "wide", "wide", "wide" }, At(Sequenced(), 160.5, out var shots));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, shots.Select(s => s.Slot).ToArray(), "span gives each board its slice");
            Assert.AreEqual(160, shots[1].Since);
            CollectionAssert.AreEqual(new[] { "pig", "pig", "pig" }, At(Sequenced(), 165.5, out shots));
            Assert.AreEqual(165, shots[0].Since);
            CollectionAssert.AreEqual(new[] { "company:0", "company:1", "company:2" }, At(Sequenced(), 169, out _));
        }

        [Test] public void FirstRoundOpeningIsNotARoundChange()
        {
            CollectionAssert.DoesNotContain(At(Sequenced(), 100.5, out _), "wide");
        }

        [Test] public void MatchEndPlaysTheSequence()
        {
            CollectionAssert.AreEqual(new[] { "wide", "wide", "wide" }, At(Sequenced(), 281, out _));
        }

        [Test] public void EmptySplitEntriesFallThroughToThePlaylist()
        {
            var schedule = TickerSchedule.FromJson(@"{
                ""boards"": [""L"", ""C"", ""R""], ""cutSeconds"": 0, ""companySeconds"": 8,
                ""channels"": [{ ""id"": ""timer"", ""screen"": ""Timer"" }, { ""id"": ""overview"", ""screen"": ""Overview"" }],
                ""playlist"": [{ ""layout"": ""mirror"", ""channel"": ""timer"", ""seconds"": 10 }],
                ""rules"": [{ ""span"": ""Round"", ""edge"": ""start"", ""from"": 0, ""to"": 5, ""priority"": 1,
                              ""layout"": ""split"", ""channels"": ["""", ""overview"", """"] }]
            }");
            CollectionAssert.IsEmpty(schedule.Validate());
            CollectionAssert.AreEqual(new[] { "timer", "overview", "timer" }, At(schedule, 101, out _));
        }

        [Test] public void ValidationNamesBrokenReferences()
        {
            var schedule = TickerSchedule.FromJson(@"{
                ""boards"": [""L"", ""C"", ""R""], ""companySeconds"": 8,
                ""channels"": [{ ""id"": ""movie"", ""video"": ""missing"" }],
                ""playlist"": [{ ""layout"": ""split"", ""channels"": [""movie""], ""seconds"": 5 }],
                ""rules"": [{ ""span"": ""Lunch"", ""edge"": ""middle"", ""from"": 0, ""to"": 5, ""layout"": ""mirror"", ""channel"": ""nope"" }]
            }");
            var errors = string.Join("\n", schedule.Validate(new[] { "roundover" }));
            StringAssert.Contains("no clip for video missing", errors);
            StringAssert.Contains("split needs 3 channels", errors);
            StringAssert.Contains("unknown span", errors);
            StringAssert.Contains("edge must be start or end", errors);
            StringAssert.Contains("unknown channel nope", errors);
        }
    }
}
