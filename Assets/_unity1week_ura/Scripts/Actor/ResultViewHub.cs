using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ResultViewHub : ViewBase
    {
        public Observable<Unit> OnRetryButtonClicked => smartPhoneView.OnRetryButtonClicked;
        public Observable<Unit> OnBackToSelectButtonClicked => smartPhoneView.OnBackToSelectButtonClicked;
        public Observable<Unit> OnShareButtonClicked => smartPhoneView.OnShareButtonClicked;

        [SerializeField] SmartPhoneView smartPhoneView;

        public override void Initialize()
        {
            smartPhoneView.Initialize();
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            await smartPhoneView.ShowScreenAsync(SceneType.Result, ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            await smartPhoneView.HideScreenAsync(SceneType.Result, ct);
        }
    }
}