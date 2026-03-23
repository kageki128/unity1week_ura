using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class GameViewHub : AnimationViewBase
    {
        public Observable<Post> OnNormalDraftDroppedToPublish => smartPhoneView.OnNormalDraftDroppedToPublish;
        public Observable<ReplyDraftPublishRequest> OnReplyDraftDroppedToPublish => smartPhoneView.OnReplyDraftDroppedToPublish;
        public Observable<Account> OnPlayerAccountClicked => smartPhoneView.OnPlayerAccountClicked;
        public Observable<Post> OnLikedByPlayer => smartPhoneView.OnLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => smartPhoneView.OnRepostedByPlayer;
        public Observable<Unit> OnSettingOpened => smartPhoneView.OnGameSettingOpened;
        public Observable<Unit> OnSettingClosed => smartPhoneView.OnGameSettingClosed;
        public Observable<Unit> OnSelectSceneButtonClicked => smartPhoneView.OnSelectSceneButtonClicked;
        public Observable<Unit> OnRestartButtonClicked => smartPhoneView.OnRestartButtonClicked;

        [SerializeField] SmartPhoneView smartPhoneView;
        [SerializeField] DraftListView draftListView;
        [SerializeField] ScoreView scoreView;
        [SerializeField] RemainingTimeView remainingTimeView;
        [SerializeField] GameStartTextAnimationView gameStartTextAnimationView;
        [SerializeField] TimedViewAnimationPlayer timedViewAnimationPlayer;

        public override void Initialize()
        {
            InitializeViews();
            draftListView.Initialize();
            if (gameStartTextAnimationView == null)
            {
                gameStartTextAnimationView = GetComponentInChildren<GameStartTextAnimationView>(true);
            }
            gameStartTextAnimationView?.Initialize();
            smartPhoneView.SetGameSubScreenTransitionEnabled(false);
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            smartPhoneView.SetGameSubScreenTransitionEnabled(false);

            if (timedViewAnimationPlayer == null || !timedViewAnimationPlayer.HasShowAnimations)
            {
                await smartPhoneView.ShowSceneAsync(SceneType.Game, ct);
                await scoreView.ShowAsync(ct);
                await remainingTimeView.ShowAsync(ct);
            }
            else
            {
                await UniTask.WhenAll(
                    smartPhoneView.ShowSceneAsync(SceneType.Game, ct),
                    timedViewAnimationPlayer.PlayShowAsync(ct));
            }
            
            if (gameStartTextAnimationView != null)
            {
                await gameStartTextAnimationView.PlayAsync(ct);
            }

            smartPhoneView.SetGameSubScreenTransitionEnabled(true);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            smartPhoneView.SetGameSubScreenTransitionEnabled(false);

            if (timedViewAnimationPlayer == null || !timedViewAnimationPlayer.HasHideAnimations)
            {
                await smartPhoneView.HideSceneAsync(SceneType.Game, ct);
                await scoreView.HideAsync(ct);
                await remainingTimeView.HideAsync(ct);
                gameObject.SetActive(false);
                return;
            }

            await UniTask.WhenAll(
                smartPhoneView.HideSceneAsync(SceneType.Game, ct),
                timedViewAnimationPlayer.PlayHideAsync(ct));
                
            gameObject.SetActive(false);
        }

        public void AddPostToTimeline(Post post) => smartPhoneView.AddPostToTimeline(post);
        public void ClearTimeline() => smartPhoneView.ClearTimeline();
        public void SetPlayerAccounts(IReadOnlyList<Account> accounts) => smartPhoneView.SetPlayerAccounts(accounts);
        public void SetSelectedPlayerAccount(Account account) => smartPhoneView.SetSelectedPlayerAccount(account);

        public void AddDraft(Post post) => draftListView.AddDraft(post);
        public void RemoveDraft(Post post) => draftListView.RemoveDraft(post);
        public void ClearDrafts() => draftListView.ClearDrafts();

        public void SetScore(int score) => scoreView.SetScore(score);
        public void SetRemainingTime(float remainingTime) => remainingTimeView.SetRemainingTime(remainingTime);

        void InitializeViews()
        {
            var initializedViews = new HashSet<AnimationViewBase>();
            TryInitializeView(smartPhoneView, initializedViews);
            TryInitializeView(scoreView, initializedViews);
            TryInitializeView(remainingTimeView, initializedViews);
            timedViewAnimationPlayer?.InitializeRegisteredViews(initializedViews);
        }

        static void TryInitializeView(AnimationViewBase view, ISet<AnimationViewBase> initializedViews)
        {
            if (view == null || initializedViews.Contains(view))
            {
                return;
            }

            view.Initialize();
            initializedViews.Add(view);
        }

    }
}
