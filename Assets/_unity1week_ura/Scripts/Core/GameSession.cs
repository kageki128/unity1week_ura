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

        public Observable<Unit> OnGameCanceled => onGameCanceled;
        readonly Subject<Unit> onGameCanceled = new();

        public Observable<Unit> OnGameRestarted => onGameRestarted;
        readonly Subject<Unit> onGameRestarted = new();

        public Observable<Unit> OnGameFinished => onGameFinished;
        readonly Subject<Unit> onGameFinished = new();

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
            if (currentGameState.CurrentValue != GameState.Preparing && currentGameState.CurrentValue != GameState.Finished)
            {
                throw new InvalidOperationException("Cannot change game rule while the game is running.");
            }

            gameRule = newGameRule;
        }

        public async UniTask LoadNewGame(CancellationToken ct)
        {
            if (currentGameState.CurrentValue != GameState.Preparing && currentGameState.CurrentValue != GameState.Finished)
            {
                throw new InvalidOperationException("Cannot load a new game while the current game is running.");
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

        public void CancelGame()
        {
            if (currentGameState.CurrentValue != GameState.Playing && currentGameState.CurrentValue != GameState.Pause)
            {
                return;
            }

            onGameCanceled.OnNext(Unit.Default);
            currentGameState.Value = GameState.Finished;
        }

        public void RestartGame()
        {
            if (currentGameState.CurrentValue != GameState.Playing && currentGameState.CurrentValue != GameState.Pause)
            {
                return;
            }

            onGameRestarted.OnNext(Unit.Default);
            currentGameState.Value = GameState.Finished;
        }

        void FinishGame()
        {
            if (currentGameState.CurrentValue == GameState.Finished)
            {
                return;
            }

            currentGameState.Value = GameState.Finished;
            onGameFinished.OnNext(Unit.Default);
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
                FinishGame();
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
                score.Value += post.IsReply ? gameConfig.ReplyPoint : gameConfig.PostPoint;
            }
            else
            {
                FinishGame();
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
                FinishGame();
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
                FinishGame();
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
            var shareText = GetResultShareText();
            await socialSharePort.ShareResultTextAsync(shareText, ct);
        }

        public string GetResultShareText()
        {
            var state = currentGameState.CurrentValue;
            if (state != GameState.Finished && state != GameState.Preparing)
            {
                throw new InvalidOperationException("Cannot share result text while the game is playing or paused.");
            }

            GameResult gameResult = new(
                score: score.Value,
                gameRule: gameRule
            );
            return socialSharePort.BuildResultShareText(gameResult);
        }
    }
}
