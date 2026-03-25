namespace Unity1Week_Ura.Core
{
    public static class ScoreFormatter
    {
        public const int MinScore = 0;
        public const int MaxScore = 999999;
        public const int MaxTotalScore = 9999999;

        const string DisplayFormat = "D6";
        const string TotalDisplayFormat = "D7";

        public static int Clamp(int score)
        {
            return ClampCore(score, MaxScore);
        }

        public static int ClampTotal(int score)
        {
            return ClampCore(score, MaxTotalScore);
        }

        static int ClampCore(int score, int maxScore)
        {
            if (score < MinScore)
            {
                return MinScore;
            }

            if (score > maxScore)
            {
                return maxScore;
            }

            return score;
        }

        public static string Format(int score)
        {
            return Clamp(score).ToString(DisplayFormat);
        }

        public static string FormatTotal(int score)
        {
            return ClampTotal(score).ToString(TotalDisplayFormat);
        }
    }
}
