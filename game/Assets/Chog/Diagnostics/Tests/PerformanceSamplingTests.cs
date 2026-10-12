using System;
using NUnit.Framework;

namespace Chog.Diagnostics.Tests
{
    public sealed class PerformanceSamplingTests
    {
        [TestCase("http://192.168.1.85:8082/?perf=1", true)]
        [TestCase("http://192.168.1.85:8082/?perf", true)]
        [TestCase("http://192.168.1.85:8082/?a=2&PERF=true#top", true)]
        [TestCase("http://192.168.1.85:8082/?perf=0", false)]
        [TestCase("http://192.168.1.85:8082/?performance=1", false)]
        [TestCase("http://192.168.1.85:8082/#perf=1", false)]
        [TestCase("http://192.168.1.85:8082/", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void QueryFlagIsReadOnlyFromTheQueryString(string url, bool expected)
        {
            Assert.AreEqual(expected, PerformanceSampling.HasQueryFlag(url, "perf"));
        }

        [TestCase("http://h/?perf=1&hz=30", "hz", true, 30)]
        [TestCase("http://h/?fps=-1", "fps", true, -1)]
        [TestCase("http://h/?HZ=45#x", "hz", true, 45)]
        [TestCase("http://h/?hz=", "hz", false, 0)]
        [TestCase("http://h/?hz", "hz", false, 0)]
        [TestCase("http://h/?hz=fast", "hz", false, 0)]
        [TestCase("http://h/?hz=3.5", "hz", false, 0)]
        [TestCase("http://h/#hz=30", "hz", false, 0)]
        [TestCase("http://h/?checksum=0", "hz", false, 0)]
        public void QueryIntegerIsParsedStrictly(string url, string name, bool expectedFound, int expectedValue)
        {
            Assert.AreEqual(expectedFound, PerformanceSampling.TryGetQueryInt(url, name, out var value));
            Assert.AreEqual(expectedValue, value);
        }

        [Test]
        public void PercentileUsesNearestRankOverTheFilledRange()
        {
            var values = new float[8];
            for (var i = 0; i < 5; i++)
                values[i] = 5 - i; // 5, 4, 3, 2, 1, then unused zeros
            values[5] = 999f;

            Assert.AreEqual(3f, PerformanceSampling.Percentile(values, 5, 50f));
            Assert.AreEqual(5f, PerformanceSampling.Percentile(values, 5, 95f));
            Assert.AreEqual(1f, PerformanceSampling.Percentile(values, 5, 1f));
            Assert.AreEqual(999f, values[5], "Values past the filled range must stay untouched.");
        }

        [Test]
        public void PercentileOfOneHundredFramesMatchesTheExpectedRank()
        {
            var values = new float[100];
            for (var i = 0; i < values.Length; i++)
                values[i] = values.Length - i; // 100 .. 1

            Assert.AreEqual(95f, PerformanceSampling.Percentile(values, 100, 95f));
            Assert.AreEqual(99f, PerformanceSampling.Percentile(values, 100, 99f));
            Assert.AreEqual(50f, PerformanceSampling.Percentile(values, 100, 50f));
        }

        [Test]
        public void PercentileHandlesEmptyAndInvalidRanges()
        {
            Assert.AreEqual(0f, PerformanceSampling.Percentile(null, 3, 50f));
            Assert.AreEqual(0f, PerformanceSampling.Percentile(new float[3], 0, 50f));
            Assert.Throws<ArgumentOutOfRangeException>(() => PerformanceSampling.Percentile(new float[3], 4, 50f));
        }
    }
}
