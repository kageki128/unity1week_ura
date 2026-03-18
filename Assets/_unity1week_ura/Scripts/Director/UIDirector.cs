using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

using Unity1Week_Ura.Actor;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Director
{
    public class UIDirector
    {
        readonly SmartPhoneView smartPhoneView;

        public Observable<Unit> OnStartButtonClicked => smartPhoneView.OnStartButtonClicked;
        public Observable<GameRuleSO> OnDifficultyButtonClicked => smartPhoneView.OnDifficultyButtonClicked;

        public UIDirector(SmartPhoneView smartPhoneView)
        {
            this.smartPhoneView = smartPhoneView;
        }

        public void Initialize()
        {
            smartPhoneView.Initialize();
        }

        public async UniTask ShowScreenAsync(SceneType sceneType, CancellationToken ct)
        {
            await smartPhoneView.ShowScreenAsync(sceneType, ct);
        }

        public async UniTask HideScreenAsync(SceneType sceneType, CancellationToken ct)
        {
            await smartPhoneView.HideScreenAsync(sceneType, ct);
        }
    }
}