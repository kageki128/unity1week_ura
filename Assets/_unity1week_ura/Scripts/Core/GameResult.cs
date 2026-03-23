namespace Unity1Week_Ura.Core
{
    public class GameResult
    {
        public int Score { get; }
        public GameRuleSO GameRule { get; }
        public FinishReason FinishReason { get; }
        
        public GameResult(int score, GameRuleSO gameRule, FinishReason finishReason)
        {
            Score = score;
            GameRule = gameRule;
            FinishReason = finishReason;
        }
    }
}
