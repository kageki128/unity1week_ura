namespace Unity1Week_Ura.Core
{
    public class GameResult
    {
        public int Score { get; }
        public GameRuleSO GameRule { get; }
        
        public GameResult(int score, GameRuleSO gameRule)
        {
            Score = score;
            GameRule = gameRule;
        }
    }
}