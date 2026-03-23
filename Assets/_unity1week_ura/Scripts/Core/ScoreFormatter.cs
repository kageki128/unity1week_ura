namespace Unity1Week_Ura.Core
{
    public static class ScoreFormatter
    {
        const string DisplayFormat = "D6";

        public static string Format(int score)
        {
            return score.ToString(DisplayFormat);
        }
    }
}
