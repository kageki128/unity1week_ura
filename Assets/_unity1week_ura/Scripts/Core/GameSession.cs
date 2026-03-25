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
        public GameRuleSO CurrentGameRule => gameRule;

        public ReadOnlyReactiveProperty<GameState> CurrentGameState => currentGameState;
        readonly ReactiveProperty<GameState> currentGameState = new(GameState.Preparing);
        public ReadOnlyReactiveProperty<FinishReason> CurrentFinishReason => finishReason;
        readonly ReactiveProperty<FinishReason> finishReason = new(FinishReason.None);

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
        bool hasSuppliedInitialPost;

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

            Debug.Log($"[U1W-DIAG][GM-001] LoadNewGame start rule={(gameRule != null ? gameRule.name : "null")}");
            Debug.Log("[U1W-DIAG][GM-010] Timeline.LoadAsync start");
            await timeline.LoadAsync(gameRule, ct);
            Debug.Log("[U1W-DIAG][GM-011] Timeline.LoadAsync complete");

            likeScoredPostIds.Clear();
            repostScoredPostIds.Clear();
            remainingTimeSeconds.Value = gameRule.TimeLimitSeconds;
            score.Value = 0;
            finishReason.Value = FinishReason.None;
            hasSuppliedInitialPost = false;
            currentGameState.Value = GameState.Pause;
            Debug.Log("[U1W-DIAG][GM-020] LoadNewGame complete state=Pause");
        }

        public void Play()
        {
            if (currentGameState.CurrentValue != GameState.Pause)
            {
                return;
            }

            currentGameState.Value = GameState.Playing;

            if (!hasSuppliedInitialPost)
            {
                hasSuppliedInitialPost = timeline.TrySupplyPostGuaranteed(gameRule);
            }
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

        void FinishGame(FinishReason reason)
        {
            if (currentGameState.CurrentValue == GameState.Finished)
            {
                return;
            }

            finishReason.Value = reason;
            currentGameState.Value = GameState.Finished;
            onGameFinished.OnNext(Unit.Default);
        }

        public void Proceed(float deltaTime)
        {
            if (currentGameState.CurrentValue != GameState.Playing)
            {
                return;
            }

            bool hasNoPosts = timeline.DraftPosts.Count == 0 && timeline.PublishedPosts.Count == 0;
            if (hasNoPosts)
            {
                bool supplied = timeline.TrySupplyPostGuaranteed(gameRule);
                hasSuppliedInitialPost = hasSuppliedInitialPost || supplied;
            }
            else
            {
                hasSuppliedInitialPost = true;
                timeline.TrySupplyPost(gameRule, deltaTime);
            }

            remainingTimeSeconds.Value = Mathf.Max(remainingTimeSeconds.Value - deltaTime, 0);
            if (remainingTimeSeconds.Value <= 0)
            {
                FinishGame(FinishReason.TimeUp);
            }
        }

        public void TryPublishNormalDraft(Post post)
        {
            if (currentGameState.CurrentValue != GameState.Playing)
            {
                return;
            }

            if (post == null || post.Type != PostType.Normal)
            {
                FinishGame(FinishReason.InvalidNormalDraftPublish);
                return;
            }

            if (timeline.TryPublishDraft(post))
            {
                AddScore(gameConfig.PostPoint);
            }
            else
            {
                FinishGame(FinishReason.WrongAccountNormalPost);
            }
        }

        public void TryPublishReplyDraft(ReplyDraftPublishRequest request)
        {
            if (currentGameState.CurrentValue != GameState.Playing)
            {
                return;
            }

            if (request == null)
            {
                FinishGame(FinishReason.InvalidReplyDraftPublish);
                return;
            }

            var replyDraft = request.ReplyDraft;
            if (replyDraft == null || replyDraft.Type != PostType.Reply)
            {
                FinishGame(FinishReason.InvalidReplyDraftPublish);
                return;
            }

            bool isReplyTargetMatched = IsFocusedPostMatchedReplyTarget(replyDraft, request.FocusedPost);

            if (timeline.TryPublishDraft(replyDraft, request.FocusedPost))
            {
                if (!isReplyTargetMatched)
                {
                    FinishGame(FinishReason.WrongReplyTarget);
                    return;
                }

                AddScore(GetActionScoreDelta(request.FocusedPost, gameConfig.ReplyPoint));
            }
            else
            {
                if (!isReplyTargetMatched)
                {
                    FinishGame(FinishReason.WrongReplyTarget);
                    return;
                }

                FinishGame(FinishReason.WrongAccountReplyPost);
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
                FinishGame(FinishReason.WrongAccountLike);
                return;
            }

            bool isActive = post.ToggleLikeByPlayer();
            if (!isActive)
            {
                return;
            }

            if (likeScoredPostIds.Add(post.Property.Id))
            {
                AddScore(GetActionScoreDelta(post, gameConfig.LikePoint));
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
                FinishGame(FinishReason.WrongAccountRepost);
                return;
            }

            bool isActive = post.ToggleRepostByPlayer();
            if (!isActive)
            {
                return;
            }

            if (repostScoredPostIds.Add(post.Property.Id))
            {
                AddScore(GetActionScoreDelta(post, gameConfig.RepostPoint));
                timeline.AddRepostToTimeline(post);
            }
        }

        void AddScore(int delta)
        {
            score.Value = ScoreFormatter.Clamp(score.Value + delta);
        }

        public async UniTask ShareResultAsync(CancellationToken ct)
        {
            var shareText = GetResultShareText();
            await socialSharePort.ShareResultTextAsync(shareText, ct);
        }

        bool IsFocusedPostMatchedReplyTarget(Post replyDraft, Post focusedPost)
        {
            if (replyDraft == null || focusedPost == null)
            {
                return false;
            }

            var replyTargetPostId = replyDraft.Property.ParentPostId;
            var focusedPostId = focusedPost.Property.Id;
            if (string.IsNullOrEmpty(replyTargetPostId) || string.IsNullOrEmpty(focusedPostId))
            {
                return false;
            }

            return string.Equals(replyTargetPostId, focusedPostId, StringComparison.Ordinal);
        }

        static int GetActionScoreDelta(Post targetPost, int score)
        {
            if (targetPost?.Property?.Author?.Type == AccountType.Advertise)
            {
                return -score;
            }

            return score;
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
                gameRule: gameRule,
                finishReason: finishReason.CurrentValue
            );
            return socialSharePort.BuildResultShareText(gameResult);
        }
    }
}
