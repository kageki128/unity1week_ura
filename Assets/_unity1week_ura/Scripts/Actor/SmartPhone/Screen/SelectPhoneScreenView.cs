using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        
        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);

            onDifficultyButtonClicked = Observable.Merge(difficultyButtons.Select(button => button.OnClicked).ToArray());
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            await screenTransitionViewHub.HideAsync(ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            await screenTransitionViewHub.ShowAsync(ScreenTransitionType.AppIconLaunchPortrait, ct);
            gameObject.SetActive(false);
        }
    }
}
