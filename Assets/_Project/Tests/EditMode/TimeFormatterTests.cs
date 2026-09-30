using ArenaSurvival.UI;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    public class TimeFormatterTests
    {
        [TestCase(0f, "00:00")]
        [TestCase(5.9f, "00:05")]   // làm tròn xuống: chưa đủ giây thì chưa tính
        [TestCase(75.4f, "01:15")]
        [TestCase(600f, "10:00")]
        [TestCase(3599f, "59:59")]
        public void ToClock_FormatsMinutesAndSeconds(float seconds, string expected)
        {
            Assert.AreEqual(expected, TimeFormatter.ToClock(seconds));
        }
    }
}
