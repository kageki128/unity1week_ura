using R3;
using UnityEngine;

namespace Unity1Week_Ura.Core
{
    public class GameSessionModel
    {
        public ReadOnlyReactiveProperty<float> RemainingTimeSeconds => remainingTimeSeconds;
        readonly ReactiveProperty<float> remainingTimeSeconds = new(0);

        public ReadOnlyReactiveProperty<int> Score => score;
        readonly ReactiveProperty<int> score = new(0);

        public ReadOnlyReactiveProperty<GameState> CurrentGameState => currentGameState;
        readonly ReactiveProperty<GameState> currentGameState = new(GameState.Ready);

        GameRuleSO gameRule;

        public GameSessionModel(GameRuleSO defaultGameRule)
        {
            gameRule = defaultGameRule;
        }

        public void SetNewGame(GameRuleSO newGameRule)
        {
            if(currentGameState.CurrentValue != GameState.Ready && currentGameState.CurrentValue != GameState.Finished)
            {
                return;
            }

            gameRule = newGameRule;
            remainingTimeSeconds.Value = gameRule.TimeLimitSeconds;
            score.Value = 0;
            currentGameState.Value = GameState.Pause;
        }

        public void Play()
        {
            if (currentGameState.CurrentValue != GameState.Pause)
            {
                return;
            }

            currentGameState.Value = GameState.Playing;
        }

        public void Pause()
        {
            if (currentGameState.CurrentValue != GameState.Playing)
            {
                return;
            }

            currentGameState.Value = GameState.Pause;
        }

        public void Proceed(float deltaTime)
        {
            if (currentGameState.CurrentValue != GameState.Playing)
            {
                return;
            }

            remainingTimeSeconds.Value = Mathf.Max(remainingTimeSeconds.Value - deltaTime, 0);
            if (remainingTimeSeconds.Value <= 0)
            {
                currentGameState.Value = GameState.Finished;
            }
        }
    }
}