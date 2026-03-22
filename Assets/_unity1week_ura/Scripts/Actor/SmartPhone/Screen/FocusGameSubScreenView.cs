using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class FocusGameSubScreenView : PhoneScreenViewBase
    {
        public Observable<Unit> OnTimelineButtonClicked => timelineButtonView.OnClicked;

        [SerializeField] ButtonView timelineButtonView;
        [SerializeField] FocusView focusView;

        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);
            focusView.Initialize();
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

        public async UniTask SetPost(Post post, CancellationToken ct)
        {
            await focusView.SetupAsync(post, ct);
        }
    }
}
