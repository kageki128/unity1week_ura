using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class TitlePhoneScreenView : PhoneScreenViewBase
    {
        public Observable<Unit> OnStartButtonClicked => startButton.OnClicked;
        [SerializeField] ButtonView startButton;
        [SerializeField] TimedViewAnimationPlayer timedViewAnimationPlayer;

        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);
            timedViewAnimationPlayer?.InitializeRegisteredViews();
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);

            if (timedViewAnimationPlayer == null || !timedViewAnimationPlayer.HasShowAnimations)
            {
                await screenTransitionViewHub.HideAsync(ct);
                return;
            }

            await UniTask.WhenAll(
                screenTransitionViewHub.HideAsync(ct),
                timedViewAnimationPlayer.PlayShowAsync(ct));
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            if (timedViewAnimationPlayer == null || !timedViewAnimationPlayer.HasHideAnimations)
            {
                await screenTransitionViewHub.ShowAsync(ScreenTransitionType.AppIconLaunchLandscape, ct);
                gameObject.SetActive(false);
                return;
            }

            await UniTask.WhenAll(
                screenTransitionViewHub.ShowAsync(ScreenTransitionType.AppIconLaunchLandscape, ct),
                timedViewAnimationPlayer.PlayHideAsync(ct));

            gameObject.SetActive(false);
        }
    }
}
