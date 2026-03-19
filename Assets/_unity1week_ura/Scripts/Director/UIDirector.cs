using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

using Unity1Week_Ura.Actor;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Director
{
    public class UIDirector
    {
        readonly SmartPhoneView smartPhoneView;

        public Observable<Unit> OnStartButtonClicked => smartPhoneView.OnStartButtonClicked;
        public Observable<GameRuleSO> OnDifficultyButtonClicked => smartPhoneView.OnDifficultyButtonClicked;

        public UIDirector(SmartPhoneView smartPhoneView)
        {
            this.smartPhoneView = smartPhoneView;
        }

        public void Initialize()
        {
            smartPhoneView.Initialize();
        }

        public UniTask EnterAsync(SceneType sceneType, CancellationToken ct)
        {
            return sceneType switch
            {
                SceneType.Title => EnterTitleScreenAsync(ct),
                SceneType.Select => EnterSelectScreenAsync(ct),
                SceneType.Game => EnterGameScreenAsync(ct),
                _ => UniTask.FromException(new ArgumentOutOfRangeException(nameof(sceneType), sceneType, null)),
            };
        }
        public UniTask ExitAsync(SceneType sceneType, CancellationToken ct)
        {
            return sceneType switch
            {
                SceneType.Title => ExitTitleScreenAsync(ct),
                SceneType.Select => ExitSelectScreenAsync(ct),
                SceneType.Game => ExitGameScreenAsync(ct),
                _ => UniTask.FromException(new ArgumentOutOfRangeException(nameof(sceneType), sceneType, null)),
            };
        }

        async UniTask EnterTitleScreenAsync(CancellationToken ct)
        {
            await smartPhoneView.ShowScreenAsync(SceneType.Title, ct);
        }
        async UniTask ExitTitleScreenAsync(CancellationToken ct)
        {
            await smartPhoneView.HideScreenAsync(SceneType.Title, ct);
        }

        async UniTask EnterSelectScreenAsync(CancellationToken ct)
        {
            await smartPhoneView.ShowScreenAsync(SceneType.Select, ct);
        }
        async UniTask ExitSelectScreenAsync(CancellationToken ct)
        {
            await smartPhoneView.HideScreenAsync(SceneType.Select, ct);
        }

        async UniTask EnterGameScreenAsync(CancellationToken ct)
        {
            await smartPhoneView.ShowScreenAsync(SceneType.Game, ct);
        }
        async UniTask ExitGameScreenAsync(CancellationToken ct)
        {
            await smartPhoneView.HideScreenAsync(SceneType.Game, ct);
        }

        public void AddPostToTimeline(Post post)
        {
            smartPhoneView.AddPostToTimeline(post);
        }
    }
}