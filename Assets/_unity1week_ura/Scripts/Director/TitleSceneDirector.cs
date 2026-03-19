using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

using Unity1Week_Ura.Core;
using System;
using Unity1Week_Ura.Actor;

namespace Unity1Week_Ura.Director
{
    public class TitleSceneDirector : ISceneDirector, IDisposable
    {
        readonly TitleViewHub titleViewHub;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();

        public TitleSceneDirector(TitleViewHub titleViewHub, SceneModel sceneModel)
        {
            this.titleViewHub = titleViewHub;
            this.sceneModel = sceneModel;
        }

        public void Dispose()
        {
            disposables.Dispose();
        }

        public void Initialize()
        {
            titleViewHub.Initialize();
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            disposables.Clear();

            await titleViewHub.ShowAsync(ct);

            titleViewHub.OnStartButtonClicked.Subscribe(_ =>
            {
                StartButtonHandler();
            }).AddTo(disposables);
        }

        public void Tick()
        {
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            disposables.Clear();
            await titleViewHub.HideAsync(ct);
        }

        void StartButtonHandler()
        {
            sceneModel.ChangeScene(SceneType.Select);
        }
    }
}
