using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class SelectViewHub : AnimationViewBase
    {
        [SerializeField] SmartPhoneView smartPhoneView;

        public Observable<GameRuleSO> OnDifficultyButtonClicked => smartPhoneView.OnDifficultyButtonClicked;
        public Observable<Unit> OnBackToTitleButtonClicked => smartPhoneView.OnBackToTitleButtonClicked;

        public override void Initialize()
        {
            smartPhoneView.Initialize();
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            
            await smartPhoneView.ShowSceneAsync(SceneType.Select, ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            
            await smartPhoneView.HideSceneAsync(SceneType.Select, ct);
        }
    }
}