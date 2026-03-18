using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using System;

using Unity1Week_Ura.Actor;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Director
{
    public class SelectSceneDirector : ISceneDirector, IDisposable
    {
        readonly SmartPhoneView smartPhoneView;
        readonly GameSessionModel gameSessionModel;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();

        public SelectSceneDirector(SmartPhoneView smartPhoneView, GameSessionModel gameSessionModel, SceneModel sceneModel)
        {
            this.smartPhoneView = smartPhoneView;
            this.gameSessionModel = gameSessionModel;
            this.sceneModel = sceneModel;
        }

        public void Dispose()
        {
            disposables.Dispose();
        }

        public void Initialize()
        {
            smartPhoneView.Initialize();

            disposables.Clear();
            smartPhoneView.OnDifficultyButtonClicked.Subscribe(gameRule =>
            {
                DifficultyButtonHandler(gameRule);
            }).AddTo(disposables);
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
            disposables.Clear();
            await smartPhoneView.HideScreenAsync(SceneType.Select, ct);
        }

        void DifficultyButtonHandler(GameRuleSO gameRule)
        {
            gameSessionModel.SetNewGame(gameRule);
            sceneModel.ChangeScene(SceneType.Game);
        }
    }
}
