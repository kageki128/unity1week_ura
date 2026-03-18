using System.Threading;
using Cysharp.Threading.Tasks;

using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Director
{
    public class GameSceneDirector : ISceneDirector
    {
        readonly UIDirector uiDirector;
        readonly GameSessionModel gameSessionModel;
        readonly SceneModel sceneModel;

        public GameSceneDirector(UIDirector uiDirector, GameSessionModel gameSessionModel, SceneModel sceneModel)
        {
            this.uiDirector = uiDirector;
            this.gameSessionModel = gameSessionModel;
            this.sceneModel = sceneModel;
        }

        public void Initialize()
        {
            uiDirector.Initialize();
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            await gameSessionModel.LoadNewGame(ct);
            await uiDirector.ShowScreenAsync(SceneType.Game, ct);
            gameSessionModel.Play();
        }

        public void Tick()
        {
            gameSessionModel.Proceed(Time.deltaTime);
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            await uiDirector.HideScreenAsync(SceneType.Game, ct);
        }
    }
}
