using R3;

namespace Unity1Week_Ura.Core
{
    public class GameSessionModel
    {
        public ReadOnlyReactiveProperty<int> Score => score;
        readonly ReactiveProperty<int> score = new(0);

        GameRuleSO gameRule;

        public GameSessionModel(GameRuleSO defaultGameRule)
        {
            gameRule = defaultGameRule;
        }

        public void SetNewGame(GameRuleSO newGameRule)
        {
            gameRule = newGameRule;
            score.Value = 0;
        }
    }
}