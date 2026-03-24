using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class SelectViewHub : AnimationViewBase
    {
        [SerializeField] SmartPhoneView smartPhoneView;
        [SerializeField] TimedViewAnimationPlayer timedViewAnimationPlayer;

        public Observable<GameRuleSO> OnGameStartButtonClicked => smartPhoneView.OnGameStartButtonClicked;
        public Observable<Unit> OnBackToTitleButtonClicked => smartPhoneView.OnBackToTitleButtonClicked;

        public override void Initialize()
        {
            var initializedViews = new HashSet<AnimationViewBase>();
            TryInitializeView(smartPhoneView, initializedViews);
            timedViewAnimationPlayer?.InitializeRegisteredViews(initializedViews);
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            AudioPlayer.Current?.PlayBGM(BGMType.Select);

            if (timedViewAnimationPlayer == null || !timedViewAnimationPlayer.HasShowAnimations)
            {
                await smartPhoneView.ShowSceneAsync(SceneType.Select, ct);
                return;
            }

            await UniTask.WhenAll(
                smartPhoneView.ShowSceneAsync(SceneType.Select, ct),
                timedViewAnimationPlayer.PlayShowAsync(ct));
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            AudioPlayer.Current?.StopBGM();

            if (timedViewAnimationPlayer == null || !timedViewAnimationPlayer.HasHideAnimations)
            {
                await smartPhoneView.HideSceneAsync(SceneType.Select, ct);
                gameObject.SetActive(false);
                return;
            }

            await UniTask.WhenAll(
                smartPhoneView.HideSceneAsync(SceneType.Select, ct),
                timedViewAnimationPlayer.PlayHideAsync(ct));

            gameObject.SetActive(false);
        }

        public UniTask LoadHighScoresAsync(IHighScoreRepository highScoreRepository, CancellationToken ct)
        {
            return smartPhoneView.LoadSelectHighScoresAsync(highScoreRepository, ct);
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
