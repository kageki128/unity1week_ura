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

        public override void Initialize()
        {
            smartPhoneView.Initialize();
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            await smartPhoneView.ShowSceneAsync(SceneType.Result, ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            await smartPhoneView.HideSceneAsync(SceneType.Result, ct);
            gameObject.SetActive(false);
        }

        public UniTask PlayShareComposeAsync(string shareText, CancellationToken ct)
        {
            return smartPhoneView.PlayResultShareComposeAsync(shareText, ct);
        }
    }
}
