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
        readonly SelectViewHub selectViewHub;
        readonly GameSession gameSession;
        readonly IHighScoreRepository highScoreRepository;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();

        public SelectSceneDirector(
            SelectViewHub selectViewHub,
            GameSession gameSession,
            IHighScoreRepository highScoreRepository,
            SceneModel sceneModel)
        {
            this.selectViewHub = selectViewHub;
            this.gameSession = gameSession;
            this.highScoreRepository = highScoreRepository;
            this.sceneModel = sceneModel;
        }

        public void Dispose()
        {
            disposables.Dispose();
        }

        public void Initialize()
        {
            selectViewHub.Initialize();
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            disposables.Clear();
            
            await selectViewHub.LoadHighScoresAsync(highScoreRepository, ct);
            await selectViewHub.ShowAsync(ct);

            selectViewHub.OnGameStartButtonClicked.Subscribe(StartButtonHandler).AddTo(disposables);
            selectViewHub.OnBackToTitleButtonClicked.Subscribe(_ =>
            {
                sceneModel.ChangeScene(SceneType.Title);
            }).AddTo(disposables);
        }

        public void Tick()
        {
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            disposables.Clear();
            await selectViewHub.HideAsync(ct);
        }

        void StartButtonHandler(GameRuleSO gameRule)
        {
            gameSession.SetNewGameRule(gameRule);
            sceneModel.ChangeScene(SceneType.Game);
        }
    }
}
