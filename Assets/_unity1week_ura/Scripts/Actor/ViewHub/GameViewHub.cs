using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] GameCharacterView gameCharacterView;
        [SerializeField] HowToPlayOverlayView howToPlayOverlayView;
        [FormerlySerializedAs("gameStartTextAnimationView")]
        [SerializeField] GamePhaseTextAnimationView gamePhaseTextAnimationView;
        [SerializeField] TimedViewAnimationPlayer timedViewAnimationPlayer;
        IPlayerProgressRepository playerProgressRepository;

        public override void Initialize()
        {
            InitializeViews();
            gameCharacterView?.Initialize();
            draftListView.Initialize();
            if (gamePhaseTextAnimationView == null)
            {
                gamePhaseTextAnimationView = GetComponentInChildren<GamePhaseTextAnimationView>(true);
            }
            gamePhaseTextAnimationView?.Initialize();
            smartPhoneView.SetGameSubScreenTransitionEnabled(false);
            gameObject.SetActive(false);
        }

        public void SetPlayerProgressRepository(IPlayerProgressRepository repository)
        {
            playerProgressRepository = repository ?? throw new ArgumentNullException(nameof(repository));
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

            
            if (playerProgressRepository == null)
            {
                throw new InvalidOperationException($"{nameof(IPlayerProgressRepository)} is not set.");
            }

            var shouldShowHowToPlay = !await playerProgressRepository.HasSeenHowToPlayAsync(ct);
            Debug.Log($"[U1W-DIAG][GV-010] ShouldShowHowToPlay={shouldShowHowToPlay}");
            if (shouldShowHowToPlay)
            {
                Debug.Log("[U1W-DIAG][GV-011] HowToPlay overlay show/wait start");
                await ShowHowToPlayOverlayAndWaitUntilClosedAsync(ct);
                Debug.Log("[U1W-DIAG][GV-012] HowToPlay overlay closed");
                await playerProgressRepository.MarkHowToPlayAsSeenAsync(ct);
                Debug.Log("[U1W-DIAG][GV-013] HowToPlay seen flag saved");
            }

            AudioPlayer.Current?.PlayBGM(BGMType.Game);

            if (gamePhaseTextAnimationView != null)
            {
                await gamePhaseTextAnimationView.PlayAsync(ct);
            }
            
            smartPhoneView.SetGameSubScreenTransitionEnabled(true);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            AudioPlayer.Current?.StopBGM();
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
        public void SetPlayerAccounts(IReadOnlyList<Account> accounts, Account selectedAccount = null)
        {
            smartPhoneView.SetPlayerAccounts(accounts, selectedAccount);
            gameCharacterView?.SetSelectedPlayerAccount(selectedAccount);
        }
        public void SetSelectedPlayerAccount(Account account)
        {
            smartPhoneView.SetSelectedPlayerAccount(account);
            gameCharacterView?.SetSelectedPlayerAccount(account);
        }

        public void AddDraft(Post post) => draftListView.AddDraft(post);
        public void RemoveDraft(Post post) => draftListView.RemoveDraft(post);
        public void ClearDrafts() => draftListView.ClearDrafts();

        public void SetScore(int score)
        {
            scoreView.SetScore(score);
            gameCharacterView?.SetScore(score);
        }

        public void SetGamePaused(bool isPaused) => gameCharacterView?.SetPaused(isPaused);

        public void SetRemainingTime(float remainingTime) => remainingTimeView.SetRemainingTime(remainingTime);
        public void SetGameSubScreenTransitionEnabled(bool isEnabled) => smartPhoneView.SetGameSubScreenTransitionEnabled(isEnabled);
        public void PrepareForNewGame() => gameCharacterView?.PrepareForNewGame();
        public async UniTask PlayGameFinishedAnimationAsync(FinishReason finishReason, CancellationToken ct)
        {
            gameCharacterView?.SetFinishReason(finishReason);

            var isTimeUp = finishReason == FinishReason.TimeUp;
            if (!isTimeUp)
            {
                var audioPlayer = AudioPlayer.Current;
                if (audioPlayer != null)
                {
                    audioPlayer.StopBGM(true);
                    audioPlayer.PlaySE(SEType.Failure);
                }
            }

            if (gamePhaseTextAnimationView == null)
            {
                return;
            }

            await gamePhaseTextAnimationView.PlayFinishAsync(finishReason, ct);
        }

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

        async UniTask ShowHowToPlayOverlayAndWaitUntilClosedAsync(CancellationToken ct)
        {
            if (howToPlayOverlayView == null)
            {
                throw new InvalidOperationException($"{nameof(howToPlayOverlayView)} is not set.");
            }

            Debug.Log("[U1W-DIAG][GV-100] HowToPlayOverlayView.ShowAsync start");
            await howToPlayOverlayView.ShowAsync(ct);
            Debug.Log("[U1W-DIAG][GV-101] HowToPlayOverlayView.ShowAsync complete");
            Debug.Log("[U1W-DIAG][GV-102] Waiting OnHidden.FirstAsync");
            await howToPlayOverlayView.OnHidden.FirstAsync(ct);
            Debug.Log("[U1W-DIAG][GV-103] OnHidden.FirstAsync complete");
        }

    }
}
