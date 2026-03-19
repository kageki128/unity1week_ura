using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ObservableCollections;
using R3;
using UnityEngine;

namespace Unity1Week_Ura.Core
{
    public class GameSession
    {
        public ReadOnlyReactiveProperty<float> RemainingTimeSeconds => remainingTimeSeconds;
        readonly ReactiveProperty<float> remainingTimeSeconds = new(0);

        public ReadOnlyReactiveProperty<int> Score => score;
        readonly ReactiveProperty<int> score = new(0);

        public ReadOnlyReactiveProperty<GameState> CurrentGameState => currentGameState;
        readonly ReactiveProperty<GameState> currentGameState = new(GameState.Ready);

        public IReadOnlyList<Account> PlayerAccounts => timeline.PlayerAccounts;
        public IReadOnlyObservableList<Post> PublishedPosts => timeline.PublishedPosts;
        public IReadOnlyObservableList<Post> DraftPosts => timeline.DraftPosts;

        readonly Timeline timeline;

        GameRuleSO gameRule;
        
        public GameSession(GameRuleSO defaultGameRule, IAccountRepository accountRepository, IPostRepository postRepository)
        {
            timeline = new Timeline(accountRepository, postRepository);
            gameRule = defaultGameRule;
        }

        public void SetNewGameRule(GameRuleSO newGameRule)
        {
            if(currentGameState.CurrentValue != GameState.Ready && currentGameState.CurrentValue != GameState.Finished)
            {
                return;
            }

            gameRule = newGameRule;
        }

        public async UniTask LoadNewGame(CancellationToken ct)
        {
            if(currentGameState.CurrentValue != GameState.Ready && currentGameState.CurrentValue != GameState.Finished)
            {
                return;
            }

            await timeline.LoadAsync(gameRule, ct);

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

            timeline.TrySupplyPost(gameRule, deltaTime);

            remainingTimeSeconds.Value = Mathf.Max(remainingTimeSeconds.Value - deltaTime, 0);
            if (remainingTimeSeconds.Value <= 0)
            {
                currentGameState.Value = GameState.Finished;
            }
        }
    }
}