using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ResultViewHub : AnimationViewBase
    {
        public Observable<Unit> OnRetryButtonClicked => smartPhoneView.OnRetryButtonClicked;
        public Observable<Unit> OnBackToSelectButtonClicked => smartPhoneView.OnBackToSelectButtonClicked;
        public Observable<Unit> OnShareButtonClicked => smartPhoneView.OnShareButtonClicked;

        [SerializeField] SmartPhoneView smartPhoneView;
        [SerializeField] TimedViewAnimationPlayer timedViewAnimationPlayer;

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
            AudioPlayer.Current?.PlayBGM(BGMType.Result);

            if (timedViewAnimationPlayer == null || !timedViewAnimationPlayer.HasShowAnimations)
            {
                await smartPhoneView.ShowSceneAsync(SceneType.Result, ct);
                return;
            }

            await UniTask.WhenAll(
                smartPhoneView.ShowSceneAsync(SceneType.Result, ct),
                timedViewAnimationPlayer.PlayShowAsync(ct));
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            AudioPlayer.Current?.StopBGM();

            if (timedViewAnimationPlayer == null || !timedViewAnimationPlayer.HasHideAnimations)
            {
                await smartPhoneView.HideSceneAsync(SceneType.Result, ct);
                gameObject.SetActive(false);
                return;
            }

            await UniTask.WhenAll(
                smartPhoneView.HideSceneAsync(SceneType.Result, ct),
                timedViewAnimationPlayer.PlayHideAsync(ct));

            gameObject.SetActive(false);
        }

        public UniTask PlayShareComposeAsync(string shareText, CancellationToken ct)
        {
            return smartPhoneView.PlayResultShareComposeAsync(shareText, ct);
        }

        public void SetResultCharacterSprite(bool isSuccess)
        {
            smartPhoneView.SetResultCharacterSprite(isSuccess);
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
