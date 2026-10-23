using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class RankRulesTests
    {
        // { humans, points before, won (1) or lost (0), change }, computed by the services' own scoring on
        // 23 October 2026: deltaFor(scoringFor(humans), before, outcome) of server/src/ranks.js.
        private static readonly int[][] Services =
        {
            new[] {1, 0, 1, 3}, new[] {1, 0, 0, 0}, new[] {1, 2, 1, 3}, new[] {1, 2, 0, -2}, new[] {1, 97, 1, 3},
            new[] {1, 97, 0, -2}, new[] {1, 99, 1, 1}, new[] {1, 99, 0, -2}, new[] {1, 100, 1, 0}, new[] {1, 100, 0, 0},
            new[] {1, 250, 1, 0}, new[] {1, 700, 0, 0}, new[] {2, 0, 1, 6}, new[] {2, 2, 0, -2}, new[] {2, 97, 0, -4},
            new[] {2, 240, 1, 6}, new[] {2, 250, 1, 5}, new[] {2, 250, 0, -5}, new[] {2, 499, 0, -5}, new[] {2, 500, 1, 3},
            new[] {2, 700, 0, -6}, new[] {3, 0, 1, 10}, new[] {3, 97, 0, -6}, new[] {3, 260, 1, 8}, new[] {3, 260, 0, -8},
            new[] {3, 500, 1, 5}, new[] {3, 700, 0, -9}, new[] {6, 0, 1, 20}, new[] {6, 99, 0, -12}, new[] {6, 250, 1, 15},
            new[] {6, 499, 0, -15}, new[] {6, 500, 1, 10}, new[] {6, 700, 0, -18},
        };

        [Test]
        public void TheChangeIsTheServicesOwn()
        {
            foreach (var row in Services)
                Assert.AreEqual(row[3], RankRules.DeltaFor(row[0], row[1], row[2] == 1),
                    "humans " + row[0] + ", before " + row[1] + ", " + (row[2] == 1 ? "win" : "loss"));
        }

        [Test]
        public void ADrawOrAnImpossibleCountChangesNothing()
        {
            Assert.AreEqual(0, RankRules.DeltaFor(1, 40, null));
            Assert.AreEqual(0, RankRules.DeltaFor(0, 40, true));
            Assert.AreEqual(0, RankRules.DeltaFor(7, 40, true));
        }

        [Test]
        public void TheSeasonReadsInAFewWords()
        {
            Assert.AreEqual("SEASON 1 ENDS 6 NOV",
                PlayerRanks.SeasonLine(new PlayerSeason { id = "season-1", endsAt = "2026-11-06T00:00:00.000Z" }));
            Assert.AreEqual("SEASON 1 ENDED", PlayerRanks.SeasonLine(new PlayerSeason { id = "season-1", ended = true }));
            Assert.AreEqual("", PlayerRanks.SeasonLine((PlayerSeason)null));
        }

        [TestCase(0, "BRONZE", 100)]
        [TestCase(99, "BRONZE", 100)]
        [TestCase(100, "SILVER", 250)]
        [TestCase(250, "GOLD", 500)]
        [TestCase(500, "STAR", 0)]
        public void TiersStartWhereTheServicesSay(int points, string tier, int next)
        {
            Assert.AreEqual(tier, RankRules.TierFor(points));
            Assert.AreEqual(next, RankRules.NextFor(points));
        }
    }
}
