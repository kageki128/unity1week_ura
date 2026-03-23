using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Actor;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Director
{
    public class ResultSceneDirector : ISceneDirector, IDisposable
    {
        readonly ResultViewHub resultViewHub;
        readonly GameSession gameSession;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();
        readonly CancellationTokenSource cts = new();

        public ResultSceneDirector(ResultViewHub resultViewHub, GameSession gameSession, SceneModel sceneModel)
        {
            this.resultViewHub = resultViewHub;
            this.gameSession = gameSession;
            this.sceneModel = sceneModel;
        }

        public void Dispose()
        {
            disposables.Dispose();
            cts.Cancel();
            cts.Dispose();
        }

        public void Initialize()
        {
            resultViewHub.Initialize();
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            disposables.Clear();

            var isSuccess = gameSession.CurrentFinishReason.CurrentValue == FinishReason.TimeUp;
            resultViewHub.SetResultCharacterSprite(isSuccess);

            await resultViewHub.ShowAsync(ct);

            var shareText = gameSession.GetResultShareText();
            await resultViewHub.PlayShareComposeAsync(shareText, ct);

            resultViewHub.OnRetryButtonClicked.Subscribe(_ =>
            {
                sceneModel.ChangeScene(SceneType.Game);
            }).AddTo(disposables);

            resultViewHub.OnBackToSelectButtonClicked.Subscribe(_ =>
            {
                sceneModel.ChangeScene(SceneType.Select);
            }).AddTo(disposables);

            resultViewHub.OnShareButtonClicked.Subscribe(_ =>
            {
                gameSession.ShareResultAsync(cts.Token).Forget();
            }).AddTo(disposables);
        }

        public void Tick()
        {
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            disposables.Clear();
            await resultViewHub.HideAsync(ct);
        }
    }
}
