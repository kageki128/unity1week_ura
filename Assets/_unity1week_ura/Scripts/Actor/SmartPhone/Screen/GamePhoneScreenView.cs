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
        public Observable<Post> OnDraftDroppedToPublish => timelineGameSubScreenView.OnDraftDroppedToPublish;
        public Observable<Account> OnPlayerAccountClicked => timelineGameSubScreenView.OnPlayerAccountClicked;
        public Observable<Post> OnLikedByPlayer => timelineGameSubScreenView.OnLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => timelineGameSubScreenView.OnRepostedByPlayer;
        public Observable<Unit> OnSelectSceneButtonClicked => settingGameSubScreenView.OnSelectSceneButtonClicked;
        public Observable<Unit> OnRestartButtonClicked => settingGameSubScreenView.OnRestartButtonClicked;

        [SerializeField] TimelineGameSubScreenView timelineGameSubScreenView;
        [SerializeField] SettingGameSubScreenView settingGameSubScreenView;
        [SerializeField] FocusGameSubScreenView focusGameSubScreenView;

        readonly Dictionary<GameScreenType, PhoneScreenViewBase> subScreens = new();
        readonly SemaphoreSlim screenSemaphore = new(1, 1);

        GameScreenType currentScreenType = GameScreenType.Timeline;

        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);

            subScreens.Clear();
            subScreens.Add(GameScreenType.Timeline, timelineGameSubScreenView);
            subScreens.Add(GameScreenType.Setting, settingGameSubScreenView);
            subScreens.Add(GameScreenType.Focus, focusGameSubScreenView);

            timelineGameSubScreenView.OnSettingButtonClicked.Subscribe(_ => ChangeScreenAsync(GameScreenType.Setting, destroyCancellationToken).Forget()).AddTo(this);
            settingGameSubScreenView.OnTimelineButtonClicked.Subscribe(_ => ChangeScreenAsync(GameScreenType.Timeline, destroyCancellationToken).Forget()).AddTo(this);
            timelineGameSubScreenView.OnPostClicked.Subscribe(post => GoToFocusScreenAsync(post, destroyCancellationToken).Forget()).AddTo(this);
            focusGameSubScreenView.OnTimelineButtonClicked.Subscribe(_ => ChangeScreenAsync(GameScreenType.Timeline, destroyCancellationToken).Forget()).AddTo(this);

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
            await screenTransitionViewHub.ShowAsync(ScreenTransitionType.CircleWipe, ct);

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
            await focusGameSubScreenView.SetPost(post, ct);
            await ChangeScreenAsync(GameScreenType.Focus, ct);
        }

        public void AddPost(Post post) => timelineGameSubScreenView.AddPost(post);
        public void ClearPosts() => timelineGameSubScreenView.ClearPosts();
        public void SetPlayerAccounts(IReadOnlyList<Account> accounts) => timelineGameSubScreenView.SetPlayerAccounts(accounts);
        public void SetSelectedPlayerAccount(Account account) => timelineGameSubScreenView.SetSelectedPlayerAccount(account);

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
            screenSemaphore.Dispose();
        }
    }
}
