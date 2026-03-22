using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class SettingGameSubScreenView : PhoneScreenViewBase
    {
        public Observable<Unit> OnTimelineButtonClicked => timelineButtonView.OnClicked;
        public Observable<Unit> OnSelectSceneButtonClicked => selectSceneButtonView.OnClicked;
        public Observable<Unit> OnRestartButtonClicked => restartButtonView.OnClicked;

        [SerializeField] ButtonView timelineButtonView;
        [SerializeField] ButtonView selectSceneButtonView;
        [SerializeField] ButtonView restartButtonView;

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

        public override async UniTask HideAsync(CancellationToken ct)
        {
            await screenTransitionViewHub.ShowAsync(ScreenTransitionType.WhiteFade, ct);
            gameObject.SetActive(false);
        }
    }
}
