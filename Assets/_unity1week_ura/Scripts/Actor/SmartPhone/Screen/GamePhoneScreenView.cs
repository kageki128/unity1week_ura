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

        [SerializeField] TimelineGameSubScreenView timelineGameSubScreenView;

        readonly Dictionary<GameScreenType, PhoneScreenViewBase> subScreens = new();
        readonly SemaphoreSlim screenSemaphore = new(1, 1);

        PhoneScreenViewBase currentScreen;

        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);

            subScreens.Clear();
            subScreens.Add(GameScreenType.Timeline, timelineGameSubScreenView);

            foreach (var screen in subScreens.Values)
            {
                screen.Initialize(screenTransitionViewHub);
            }

            currentScreen = GetScreenView(GameScreenType.Timeline);

            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            await currentScreen.ShowAsync(ct);
            await screenTransitionViewHub.HideAsync(ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            await screenTransitionViewHub.ShowAsync(ScreenTransitionType.CircleWipe, ct);
            gameObject.SetActive(false);
        }

        async UniTask ChangeScreenAsync(GameScreenType targetType, CancellationToken ct)
        {
            await screenSemaphore.WaitAsync(ct);
            try
            {
                var nextScreen = GetScreenView(targetType);

                if (currentScreen == nextScreen)
                {
                    return;
                }

                if (currentScreen != null)
                {
                    await currentScreen.HideAsync(ct);
                }

                currentScreen = nextScreen;
                await currentScreen.ShowAsync(ct);
            }
            finally
            {
                screenSemaphore.Release();
            }
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
