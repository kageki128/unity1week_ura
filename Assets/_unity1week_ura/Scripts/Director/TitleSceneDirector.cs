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
        readonly ActorHub actorHub;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();

        public TitleSceneDirector(ActorHub actorHub, SceneModel sceneModel)
        {
            this.actorHub = actorHub;
            this.sceneModel = sceneModel;
        }

        public void Dispose()
        {
            disposables.Dispose();
        }

        public void Initialize()
        {
            disposables.Clear();
            actorHub.OnStartButtonClicked.Subscribe(_ => 
            {
                StartButtonHandler();
            }).AddTo(disposables);
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            await actorHub.EnterAsync(SceneType.Title, ct);
        }

        public void Tick()
        {
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            disposables.Clear();
            await actorHub.ExitAsync(SceneType.Title, ct);
        }

        void StartButtonHandler()
        {
            sceneModel.ChangeScene(SceneType.Select);
        }
    }
}
