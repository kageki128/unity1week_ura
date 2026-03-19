using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using System;

using Unity1Week_Ura.Core;
using Unity1Week_Ura.Actor;

namespace Unity1Week_Ura.Director
{
    public class SelectSceneDirector : ISceneDirector, IDisposable
    {
        readonly ActorHub actorHub;
        readonly GameSession gameSession;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();

        public SelectSceneDirector(ActorHub actorHub, GameSession gameSession, SceneModel sceneModel)
        {
            this.actorHub = actorHub;
            this.gameSession = gameSession;
            this.sceneModel = sceneModel;
        }

        public void Dispose()
        {
            disposables.Dispose();
        }

        public void Initialize()
        {
            disposables.Clear();
            actorHub.OnDifficultyButtonClicked.Subscribe(gameRule =>
            {
                DifficultyButtonHandlerAsync(gameRule);
            }).AddTo(disposables);
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            await actorHub.EnterAsync(SceneType.Select, ct);
        }

        public void Tick()
        {
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            disposables.Clear();
            await actorHub.ExitAsync(SceneType.Select, ct);
        }

        public void DifficultyButtonHandlerAsync(GameRuleSO gameRule)
        {
            gameSession.SetNewGameRule(gameRule);
            sceneModel.ChangeScene(SceneType.Game);
        }
    }
}
