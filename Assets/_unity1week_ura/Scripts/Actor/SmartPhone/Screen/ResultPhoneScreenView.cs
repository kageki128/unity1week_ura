using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ResultPhoneScreenView : PhoneScreenViewBase
    {
        public Observable<Unit> OnRetryButtonClicked => RetryButtonView.OnClicked;
        public Observable<Unit> OnBackToSelectButtonClicked => BackToSelectButtonView.OnClicked;
        public Observable<Unit> OnShareButtonClicked => ShareButtonView.OnClicked;

        [SerializeField] ButtonView RetryButtonView;
        [SerializeField] ButtonView BackToSelectButtonView;
        [SerializeField] ButtonView ShareButtonView;

        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);

            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            await screenTransitionViewHub.HideAsync(ct);
        }

        public override UniTask HideAsync(CancellationToken ct)
        {
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }
    }
}