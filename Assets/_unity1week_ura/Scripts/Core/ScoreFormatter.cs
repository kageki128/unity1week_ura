namespace Unity1Week_Ura.Core
{
    public static class ScoreFormatter
    {
        public const int MinScore = 0;
        public const int MaxScore = 999999;

        const string DisplayFormat = "D6";

        public static int Clamp(int score)
        {
            if (score < MinScore)
            {
                return MinScore;
            }

            if (score > MaxScore)
            {
                return MaxScore;
            }

            return score;
        }

        public static string Format(int score)
        {
            return Clamp(score).ToString(DisplayFormat);
        }
    }
}
