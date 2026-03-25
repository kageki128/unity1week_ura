using NUnit.Framework;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Tests
{
    public class ScoreFormatterTests
    {
        [Test]
        public void Clamp_WhenScoreExceedsMaxScore_ReturnsMaxScore()
        {
            Assert.That(ScoreFormatter.Clamp(1234567), Is.EqualTo(ScoreFormatter.MaxScore));
        }

        [Test]
        public void ClampTotal_WhenScoreExceedsMaxTotalScore_ReturnsMaxTotalScore()
        {
            Assert.That(ScoreFormatter.ClampTotal(12345678), Is.EqualTo(ScoreFormatter.MaxTotalScore));
        }

        [Test]
        public void Format_UsesSixDigits()
        {
            Assert.That(ScoreFormatter.Format(123), Is.EqualTo("000123"));
        }

        [Test]
        public void FormatTotal_UsesSevenDigits()
        {
            Assert.That(ScoreFormatter.FormatTotal(123), Is.EqualTo("0000123"));
        }
    }
}
