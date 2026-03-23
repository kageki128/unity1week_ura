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
        public Observable<Unit> OnSelectSceneButtonClicked => settingGameSubScreenView.OnSelectSceneButtonClicked;
        public Observable<Unit> OnRestartButtonClicked => settingGameSubScreenView.OnRestartButtonClicked;

        [SerializeField] TimelineGameSubScreenView timelineGameSubScreenView;
        [SerializeField] SettingGameSubScreenView settingGameSubScreenView;
        [SerializeField] FocusGameSubScreenView focusGameSubScreenView;

        readonly Dictionary<GameScreenType, PhoneScreenViewBase> subScreens = new();
        readonly SemaphoreSlim screenSemaphore = new(1, 1);
        readonly CompositeDisposable disposables = new();
        Observable<Post> onNormalDraftDroppedToPublish;
        Observable<ReplyDraftPublishRequest> onReplyDraftDroppedToPublish;
        Observable<Post> onLikedByPlayer;
        Observable<Post> onRepostedByPlayer;

        GameScreenType currentScreenType = GameScreenType.Timeline;
        bool isBackFromFocusProcessing;

        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);
            disposables.Clear();

            subScreens.Clear();
            subScreens.Add(GameScreenType.Timeline, timelineGameSubScreenView);
            subScreens.Add(GameScreenType.Setting, settingGameSubScreenView);
            subScreens.Add(GameScreenType.Focus, focusGameSubScreenView);

            timelineGameSubScreenView.OnSettingButtonClicked.Subscribe(_ => ChangeScreenAsync(GameScreenType.Setting, destroyCancellationToken).Forget()).AddTo(disposables);
            settingGameSubScreenView.OnTimelineButtonClicked.Subscribe(_ => ChangeScreenAsync(GameScreenType.Timeline, destroyCancellationToken).Forget()).AddTo(disposables);
            timelineGameSubScreenView.OnPostClicked.Subscribe(post => GoToFocusScreenAsync(post, destroyCancellationToken).Forget()).AddTo(disposables);
            focusGameSubScreenView.OnPostClicked.Subscribe(post => FocusCurrentPostAsync(post, destroyCancellationToken).Forget()).AddTo(disposables);
            focusGameSubScreenView.OnTimelineButtonClicked.Subscribe(_ => BackFromFocusAsync(destroyCancellationToken).Forget()).AddTo(disposables);

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

            var currentScreen = GetScreenView(currentScreenType);
            await currentScreen.ShowAsync(ct);
            await screenTransitionViewHub.HideAsync(ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
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
            await screenSemaphore.WaitAsync(ct);
            try
            {
                var currentScreen = GetScreenView(currentScreenType);
                var nextScreen = GetScreenView(targetType);

                if (currentScreenType == targetType)
                {
                    return;
                }

                await currentScreen.HideAsync(ct);
                await nextScreen.ShowAsync(ct);
                currentScreenType = targetType;
            }
            finally
            {
                screenSemaphore.Release();
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
        public void SetPlayerAccounts(IReadOnlyList<Account> accounts)
        {
            timelineGameSubScreenView.SetPlayerAccounts(accounts);
            Account initialAccount = accounts != null && accounts.Count > 0 ? accounts[0] : null;
            focusGameSubScreenView.SetCurrentPlayerAccount(initialAccount);
        }
        public void SetSelectedPlayerAccount(Account account)
        {
            timelineGameSubScreenView.SetSelectedPlayerAccount(account);
            focusGameSubScreenView.SetCurrentPlayerAccount(account);
        }

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
        }
    }
}
