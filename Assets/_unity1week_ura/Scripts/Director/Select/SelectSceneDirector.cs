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
        readonly GameSessionModel gameSessionModel;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();

        public SelectSceneDirector(UIDirector uiDirector, GameSessionModel gameSessionModel, SceneModel sceneModel)
        {
            this.uiDirector = uiDirector;
            this.gameSessionModel = gameSessionModel;
            this.sceneModel = sceneModel;
        }

        public void Dispose()
        {
            disposables.Dispose();
        }

        public void Initialize()
        {
            uiDirector.Initialize();

            disposables.Clear();
            uiDirector.OnDifficultyButtonClicked.Subscribe(gameRule =>
            {
                DifficultyButtonHandlerAsync(gameRule);
            }).AddTo(disposables);
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            await uiDirector.ShowScreenAsync(SceneType.Select, ct);
        }

        public void Tick()
        {
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            disposables.Clear();
            await uiDirector.HideScreenAsync(SceneType.Select, ct);
        }

        public void DifficultyButtonHandlerAsync(GameRuleSO gameRule)
        {
            gameSessionModel.SetNewGameRule(gameRule);
            sceneModel.ChangeScene(SceneType.Game);
        }
    }
}
