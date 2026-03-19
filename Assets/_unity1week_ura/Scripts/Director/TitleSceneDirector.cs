using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

using Unity1Week_Ura.Core;
using System;

namespace Unity1Week_Ura.Director
{
    public class TitleSceneDirector : ISceneDirector, IDisposable
    {
        readonly UIDirector uiDirector;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();

        public TitleSceneDirector(UIDirector uiDirector, SceneModel sceneModel)
        {
            this.uiDirector = uiDirector;
            this.sceneModel = sceneModel;
        }

        public void Dispose()
        {
            disposables.Dispose();
        }

        public void Initialize()
        {
            disposables.Clear();
            uiDirector.OnStartButtonClicked.Subscribe(_ => 
            {
                StartButtonHandler();
            }).AddTo(disposables);
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            await uiDirector.EnterAsync(SceneType.Title, ct);
        }

        public void Tick()
        {
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            disposables.Clear();
            await uiDirector.ExitAsync(SceneType.Title, ct);
        }

        void StartButtonHandler()
        {
            sceneModel.ChangeScene(SceneType.Select);
        }
    }
}
