using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class GamePhoneScreenView : PhoneScreenViewBase
    {
        public Observable<Post> OnNormalDraftDroppedToPublish => onNormalDraftDroppedToPublish;
        public Observable<ReplyDraftPublishRequest> OnReplyDraftDroppedToPublish => onReplyDraftDroppedToPublish;
        public Observable<Account> OnPlayerAccountClicked => timelineGameSubScreenView.OnPlayerAccountClicked;
        public Observable<Post> OnLikedByPlayer => onLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => onRepostedByPlayer;
        public Observable<Unit> OnSettingOpened => onSettingOpened;
        public Observable<Unit> OnSettingClosed => onSettingClosed;
        public Observable<Unit> OnSelectSceneButtonClicked => settingGameSubScreenView.OnSelectSceneButtonClicked;
        public Observable<Unit> OnRestartButtonClicked => settingGameSubScreenView.OnRestartButtonClicked;

        [SerializeField] TimelineGameSubScreenView timelineGameSubScreenView;
        [SerializeField] SettingGameSubScreenView settingGameSubScreenView;
        [SerializeField] FocusGameSubScreenView focusGameSubScreenView;
        [SerializeField] DraftListView draftListView;

        readonly Dictionary<GameScreenType, PhoneScreenViewBase> subScreens = new();
        readonly SemaphoreSlim screenSemaphore = new(1, 1);
        readonly CompositeDisposable disposables = new();
        Observable<Post> onNormalDraftDroppedToPublish;
        Observable<ReplyDraftPublishRequest> onReplyDraftDroppedToPublish;
        Observable<Post> onLikedByPlayer;
        Observable<Post> onRepostedByPlayer;
        readonly Subject<Unit> onSettingOpened = new();
        readonly Subject<Unit> onSettingClosed = new();

        GameScreenType currentScreenType = GameScreenType.Timeline;
        bool isBackFromFocusProcessing;
        bool isSubScreenTransitionEnabled;

        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);
            disposables.Clear();

            subScreens.Clear();
            subScreens.Add(GameScreenType.Timeline, timelineGameSubScreenView);
            subScreens.Add(GameScreenType.Setting, settingGameSubScreenView);
            subScreens.Add(GameScreenType.Focus, focusGameSubScreenView);

            isSubScreenTransitionEnabled = false;

            timelineGameSubScreenView.OnSettingButtonClicked
                .Where(_ => isSubScreenTransitionEnabled)
                .Subscribe(_ => ChangeScreenAsync(GameScreenType.Setting, destroyCancellationToken).Forget())
                .AddTo(disposables);
            settingGameSubScreenView.OnTimelineButtonClicked
                .Where(_ => isSubScreenTransitionEnabled)
                .Subscribe(_ => ChangeScreenAsync(GameScreenType.Timeline, destroyCancellationToken).Forget())
                .AddTo(disposables);
            timelineGameSubScreenView.OnPostClicked
                .Where(_ => isSubScreenTransitionEnabled)
                .Subscribe(post => GoToFocusScreenAsync(post, destroyCancellationToken).Forget())
                .AddTo(disposables);
            focusGameSubScreenView.OnPostClicked.Subscribe(post => FocusCurrentPostAsync(post, destroyCancellationToken).Forget()).AddTo(disposables);
            focusGameSubScreenView.OnTimelineButtonClicked
                .Where(_ => isSubScreenTransitionEnabled)
                .Subscribe(_ => BackFromFocusAsync(destroyCancellationToken).Forget())
                .AddTo(disposables);

            onNormalDraftDroppedToPublish = timelineGameSubScreenView.OnNormalDraftDroppedToPublish;
            onReplyDraftDroppedToPublish = focusGameSubScreenView.OnReplyDraftDroppedToPublish;
            onLikedByPlayer = Observable.Merge(new[]
            {
                timelineGameSubScreenView.OnLikedByPlayer,
                focusGameSubScreenView.OnLikedByPlayer
            });
            onRepostedByPlayer = Observable.Merge(new[]
            {
                timelineGameSubScreenView.OnRepostedByPlayer,
                focusGameSubScreenView.OnRepostedByPlayer
            });

            foreach (var screen in subScreens.Values)
            {
                screen.Initialize(screenTransitionViewHub);
            }

            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            isSubScreenTransitionEnabled = false;
            if (draftListView != null)
            {
                await draftListView.SetVisible(currentScreenType != GameScreenType.Setting, ct);
            }

            var currentScreen = GetScreenView(currentScreenType);
            await currentScreen.ShowAsync(ct);
            await screenTransitionViewHub.HideAsync(ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            isSubScreenTransitionEnabled = false;
            await screenTransitionViewHub.ShowAsync(ScreenTransitionType.AppIconLaunchPortrait, ct);

            foreach (var screen in subScreens.Values)
            {
                screen.gameObject.SetActive(false);
            }
            
            currentScreenType = GameScreenType.Timeline;
            gameObject.SetActive(false);
        }

        async UniTask ChangeScreenAsync(GameScreenType targetType, CancellationToken ct)
        {
            if (!isSubScreenTransitionEnabled)
            {
                return;
            }

            await screenSemaphore.WaitAsync(ct);
            try
            {
                var previousScreenType = currentScreenType;
                var currentScreen = GetScreenView(currentScreenType);
                var nextScreen = GetScreenView(targetType);

                if (currentScreenType == targetType)
                {
                    return;
                }

                NotifySettingScreenTransition(previousScreenType, targetType);
                UniTask draftListVisibilityTask = UniTask.CompletedTask;
                if (draftListView != null)
                {
                    if (targetType == GameScreenType.Setting)
                    {
                        draftListVisibilityTask = draftListView.SetVisible(false, ct);
                    }
                    else if (previousScreenType == GameScreenType.Setting)
                    {
                        draftListVisibilityTask = draftListView.SetVisible(true, ct);
                    }
                }

                await UniTask.WhenAll(
                    currentScreen.HideAsync(ct),
                    draftListVisibilityTask);
                await nextScreen.ShowAsync(ct);
                currentScreenType = targetType;
            }
            finally
            {
                screenSemaphore.Release();
            }
        }

        void NotifySettingScreenTransition(GameScreenType from, GameScreenType to)
        {
            if (from != GameScreenType.Setting && to == GameScreenType.Setting)
            {
                onSettingOpened.OnNext(Unit.Default);
                return;
            }

            if (from == GameScreenType.Setting && to != GameScreenType.Setting)
            {
                onSettingClosed.OnNext(Unit.Default);
            }
        }

        async UniTask GoToFocusScreenAsync(Post post, CancellationToken ct)
        {
            if (post == null)
            {
                return;
            }

            focusGameSubScreenView.SetPost(post);
            await ChangeScreenAsync(GameScreenType.Focus, ct);
        }

        async UniTask FocusCurrentPostAsync(Post post, CancellationToken ct)
        {
            if (post == null)
            {
                return;
            }

            await screenSemaphore.WaitAsync(ct);
            try
            {
                if (currentScreenType != GameScreenType.Focus)
                {
                    return;
                }

                await focusGameSubScreenView.FocusPostAsync(post, ct);
            }
            finally
            {
                screenSemaphore.Release();
            }
        }

        async UniTask BackFromFocusAsync(CancellationToken ct)
        {
            if (isBackFromFocusProcessing)
            {
                return;
            }

            isBackFromFocusProcessing = true;
            bool movedToPreviousFocus = false;

            try
            {
                await screenSemaphore.WaitAsync(ct);
                try
                {
                    if (currentScreenType != GameScreenType.Focus)
                    {
                        return;
                    }

                    movedToPreviousFocus = await focusGameSubScreenView.BackToPreviousFocusAsync(ct);
                }
                finally
                {
                    screenSemaphore.Release();
                }

                if (movedToPreviousFocus)
                {
                    return;
                }

                await ChangeScreenAsync(GameScreenType.Timeline, ct);
            }
            finally
            {
                isBackFromFocusProcessing = false;
            }
        }

        public void AddPost(Post post)
        {
            timelineGameSubScreenView.AddPost(post);
            focusGameSubScreenView.AddPublishedPost(post);
        }

        public void ClearPosts() => timelineGameSubScreenView.ClearPosts();
        public void SetPlayerAccounts(IReadOnlyList<Account> accounts, Account selectedAccount = null)
        {
            Account initialAccount = selectedAccount;
            if (initialAccount == null && accounts != null && accounts.Count > 0)
            {
                initialAccount = accounts[0];
            }

            timelineGameSubScreenView.SetPlayerAccounts(accounts, initialAccount);
            focusGameSubScreenView.SetCurrentPlayerAccount(initialAccount);
        }
        public void SetSelectedPlayerAccount(Account account)
        {
            timelineGameSubScreenView.SetSelectedPlayerAccount(account);
            focusGameSubScreenView.SetCurrentPlayerAccount(account);
        }
        public void SetSubScreenTransitionEnabled(bool isEnabled) => isSubScreenTransitionEnabled = isEnabled;

        PhoneScreenViewBase GetScreenView(GameScreenType screenType)
        {
            if (subScreens.TryGetValue(screenType, out var screenView))
            {
                return screenView;
            }

            throw new KeyNotFoundException($"Screen view for game screen type {screenType} not found.");
        }

        void OnDestroy()
        {
            disposables.Dispose();
            screenSemaphore.Dispose();
            onSettingOpened.OnCompleted();
            onSettingClosed.OnCompleted();
            onSettingOpened.Dispose();
            onSettingClosed.Dispose();
        }
    }
}
