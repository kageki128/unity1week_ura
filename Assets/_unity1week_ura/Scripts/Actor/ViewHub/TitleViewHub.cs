using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class TitleViewHub : AnimationViewBase
    {
        [SerializeField] SmartPhoneView smartPhoneView;

        public Observable<Unit> OnStartButtonClicked => smartPhoneView.OnStartButtonClicked;

        public override void Initialize()
        {
            smartPhoneView.Initialize();
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            await smartPhoneView.ShowSceneAsync(SceneType.Title, ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            await smartPhoneView.HideSceneAsync(SceneType.Title, ct);
            gameObject.SetActive(false);
        }
    }
}