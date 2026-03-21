using System;
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
        readonly ReactiveProperty<GameState> currentGameState = new(GameState.Preparing);

        public IReadOnlyList<Account> PlayerAccounts => timeline.PlayerAccounts;
        public ReadOnlyReactiveProperty<Account> SelectedPlayerAccount => timeline.SelectedPlayerAccount;
        public IReadOnlyObservableList<Post> PublishedPosts => timeline.PublishedPosts;
        public IReadOnlyObservableList<Post> DraftPosts => timeline.DraftPosts;

        readonly Timeline timeline;
        readonly ISocialSharePort socialSharePort;
        readonly GameConfigSO gameConfig;
        readonly HashSet<string> likeScoredPostIds = new(StringComparer.Ordinal);
        readonly HashSet<string> repostScoredPostIds = new(StringComparer.Ordinal);
        GameRuleSO gameRule;
        
        public GameSession(GameConfigSO gameConfig, IAccountRepository accountRepository, IPostRepository postRepository, ISocialSharePort socialSharePort)
        {
            timeline = new Timeline(accountRepository, postRepository);
            this.socialSharePort = socialSharePort;
            this.gameConfig = gameConfig;
            gameRule = gameConfig.InitialGameRule;
        }

        public void SetNewGameRule(GameRuleSO newGameRule)
        {
            if(currentGameState.CurrentValue != GameState.Preparing && currentGameState.CurrentValue != GameState.Finished)
            {
                throw new InvalidOperationException("ゲーム中はルールを変更できません。");
            }

            gameRule = newGameRule;
        }

        public async UniTask LoadNewGame(CancellationToken ct)
        {
            if(currentGameState.CurrentValue != GameState.Preparing && currentGameState.CurrentValue != GameState.Finished)
            {
                throw new InvalidOperationException("ゲーム中は新しいゲームをロードできません。");
            }

            await timeline.LoadAsync(gameRule, ct);

            likeScoredPostIds.Clear();
            repostScoredPostIds.Clear();
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

        public void TryPublishDraft(Post post)
        {
            if (currentGameState.CurrentValue != GameState.Playing)
            {
                return;
            }

            if (timeline.TryPublishDraft(post))
            {
                score.Value += post.ScoreInfo.PublishPoint;
            }
            else
            {
                // ゲームオーバー
                currentGameState.Value = GameState.Finished;
            }
        }

        public void SetCurrentPlayerAccount(Account account) => timeline.SetCurrentPlayerAccount(account);

        public void LikePostByPlayer(Post post)
        {
            if (currentGameState.CurrentValue != GameState.Playing)
            {
                return;
            }

            if (post == null)
            {
                return;
            }

            if (!timeline.IsCurrentPlayerAccountCorrectForAction(post))
            {
                currentGameState.Value = GameState.Finished;
                return;
            }

            bool isActive = post.ToggleLikeByPlayer();
            if (!isActive)
            {
                return;
            }

            if (likeScoredPostIds.Add(post.Property.Id))
            {
                score.Value += gameConfig.LikePoint;
            }
        }

        public void RepostByPlayer(Post post)
        {
            if (currentGameState.CurrentValue != GameState.Playing)
            {
                return;
            }

            if (post == null)
            {
                return;
            }

            if (!timeline.IsCurrentPlayerAccountCorrectForAction(post))
            {
                currentGameState.Value = GameState.Finished;
                return;
            }

            bool isActive = post.ToggleRepostByPlayer();
            if (!isActive)
            {
                return;
            }

            if (repostScoredPostIds.Add(post.Property.Id))
            {
                score.Value += gameConfig.RepostPoint;
                timeline.AddRepostToTimeline(post);
            }
        }

        public async UniTask ShareResultAsync(CancellationToken ct)
        {
            if (currentGameState.CurrentValue != GameState.Finished)
            {
                throw new InvalidOperationException("ゲームが終了していないため、結果を共有できません。");
            }

            GameResult gameResult = new(
                score: score.Value,
                gameRule: gameRule
            );
            await socialSharePort.ShareResultAsync(gameResult, ct);
        }
    }
}