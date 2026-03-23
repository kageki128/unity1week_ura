using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class SelectPhoneScreenView : PhoneScreenViewBase
    {
        public Observable<GameRuleSO> OnDifficultyButtonClicked => onDifficultyButtonClicked;
        public Observable<Unit> OnBackToTitleButtonClicked =>backToTitleButton.OnClicked;
        Observable<GameRuleSO> onDifficultyButtonClicked;

        [SerializeField] List<DifficultyButtonView> difficultyButtons;
        [SerializeField] ButtonView backToTitleButton;
        [SerializeField] TimedViewAnimationPlayer timedViewAnimationPlayer;
        
        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);

            onDifficultyButtonClicked = Observable.Merge(difficultyButtons.Select(button => button.OnClicked).ToArray());
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
                await screenTransitionViewHub.ShowAsync(ScreenTransitionType.AppIconLaunchPortrait, ct);
                gameObject.SetActive(false);
                return;
            }

            await UniTask.WhenAll(
                screenTransitionViewHub.ShowAsync(ScreenTransitionType.AppIconLaunchPortrait, ct),
                timedViewAnimationPlayer.PlayHideAsync(ct));

            gameObject.SetActive(false);
        }
    }
}
