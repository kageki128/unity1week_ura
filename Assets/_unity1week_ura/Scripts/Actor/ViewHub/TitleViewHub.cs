using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class TitleViewHub : ViewBase
    {
        [SerializeField] SmartPhoneView smartPhoneView;

        public Observable<Unit> OnStartButtonClicked => smartPhoneView.OnStartButtonClicked;

        public override void Initialize()
        {
            smartPhoneView.Initialize();
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            
            await smartPhoneView.ShowScreenAsync(SceneType.Title, ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            
            await smartPhoneView.HideScreenAsync(SceneType.Title, ct);
        }
    }
}