using System.Threading;
using Cysharp.Threading.Tasks;

using Unity1Week_Ura.Actor;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Director
{
    public class SelectSceneDirector : ISceneDirector
    {
        readonly SmartPhoneView smartPhoneView;

        public SelectSceneDirector(SmartPhoneView smartPhoneView)
        {
            this.smartPhoneView = smartPhoneView;
        }

        public void Initialize()
        {
            smartPhoneView.Initialize();
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            await smartPhoneView.ShowScreenAsync(SceneType.Select, ct);
        }

        public void Tick()
        {
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            await smartPhoneView.HideScreenAsync(SceneType.Select, ct);
        }
    }
}
