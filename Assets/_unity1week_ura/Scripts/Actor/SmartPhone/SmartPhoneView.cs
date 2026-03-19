using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class SmartPhoneView : ViewBase
    {
        // Title
        public Observable<Unit> OnStartButtonClicked => titlePhoneScreenView.OnStartButtonClicked;

        // Select
        public Observable<GameRuleSO> OnDifficultyButtonClicked => selectPhoneScreenView.OnDifficultyButtonClicked;
        public Observable<Unit> OnBackToTitleButtonClicked => selectPhoneScreenView.OnBackToTitleButtonClicked;

        // Result
        public Observable<Unit> OnRetryButtonClicked => resultPhoneScreenView.OnRetryButtonClicked;
        public Observable<Unit> OnBackToSelectButtonClicked => resultPhoneScreenView.OnBackToSelectButtonClicked;
        public Observable<Unit> OnShareButtonClicked => resultPhoneScreenView.OnShareButtonClicked;


        [SerializeField] TitlePhoneScreenView titlePhoneScreenView;
        [SerializeField] SelectPhoneScreenView selectPhoneScreenView;
        [SerializeField] GamePhoneScreenView gamePhoneScreenView;
        [SerializeField] ResultPhoneScreenView resultPhoneScreenView;

        readonly Dictionary<SceneType, ViewBase> screenViews = new();

        public override void Initialize()
        {
            screenViews.Clear();
            screenViews.Add(SceneType.Title, titlePhoneScreenView);
            screenViews.Add(SceneType.Select, selectPhoneScreenView);
            screenViews.Add(SceneType.Game, gamePhoneScreenView);
            screenViews.Add(SceneType.Result, resultPhoneScreenView);

            foreach (var screenView in screenViews.Values)
            {
                screenView.Initialize();
            }
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            await UniTask.CompletedTask;
        }
        public override async UniTask HideAsync(CancellationToken ct)
        {
            gameObject.SetActive(false);
            await UniTask.CompletedTask;
        }

        public async UniTask ShowScreenAsync(SceneType sceneType, CancellationToken ct)
        {
            var screenView = GetScreenView(sceneType);
            await screenView.ShowAsync(ct);
        }

        public async UniTask HideScreenAsync(SceneType sceneType, CancellationToken ct)
        {
            var screenView = GetScreenView(sceneType);
            await screenView.HideAsync(ct);
        }

        public void AddPostToTimeline(Post post) => gamePhoneScreenView.AddPost(post);
        public void ClearTimeline() => gamePhoneScreenView.ClearPosts();

        ViewBase GetScreenView(SceneType sceneType)
        {
            if (screenViews.TryGetValue(sceneType, out var screenView))
            {
                return screenView;
            }

            throw new KeyNotFoundException($"Screen view for scene type {sceneType} not found.");
        }
    }
}