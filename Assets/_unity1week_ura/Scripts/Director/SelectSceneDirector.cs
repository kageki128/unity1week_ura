using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using System;

using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Director
{
    public class SelectSceneDirector : ISceneDirector, IDisposable
    {
        readonly UIDirector uiDirector;
        readonly GameSession gameSession;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();

        public SelectSceneDirector(UIDirector uiDirector, GameSession gameSession, SceneModel sceneModel)
        {
            this.uiDirector = uiDirector;
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
            uiDirector.OnDifficultyButtonClicked.Subscribe(gameRule =>
            {
                DifficultyButtonHandlerAsync(gameRule);
            }).AddTo(disposables);
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            await uiDirector.EnterAsync(SceneType.Select, ct);
        }

        public void Tick()
        {
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            disposables.Clear();
            await uiDirector.ExitAsync(SceneType.Select, ct);
        }

        public void DifficultyButtonHandlerAsync(GameRuleSO gameRule)
        {
            gameSession.SetNewGameRule(gameRule);
            sceneModel.ChangeScene(SceneType.Game);
        }
    }
}
