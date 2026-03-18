using System.Threading;
using Cysharp.Threading.Tasks;

using Unity1Week_Ura.Actor;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Director
{
    public class GameSceneDirector : ISceneDirector
    {
        readonly SmartPhoneView smartPhoneView;
        readonly GameSessionModel gameSessionModel;
        readonly SceneModel sceneModel;

        public GameSceneDirector(SmartPhoneView smartPhoneView, GameSessionModel gameSessionModel, SceneModel sceneModel)
        {
            this.smartPhoneView = smartPhoneView;
            this.gameSessionModel = gameSessionModel;
            this.sceneModel = sceneModel;
        }

        public void Initialize()
        {
            smartPhoneView.Initialize();
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            await smartPhoneView.ShowScreenAsync(SceneType.Game, ct);
            gameSessionModel.Play();
        }

        public void Tick()
        {
            gameSessionModel.Proceed(Time.deltaTime);
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            await smartPhoneView.HideScreenAsync(SceneType.Game, ct);
        }
    }
}
