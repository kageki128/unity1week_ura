using System.Threading;
using Cysharp.Threading.Tasks;

using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Director
{
    public class GameSceneDirector : ISceneDirector
    {
        readonly UIDirector uiDirector;
        readonly GameSession gameSession;
        readonly SceneModel sceneModel;

        public GameSceneDirector(UIDirector uiDirector, GameSession gameSession, SceneModel sceneModel)
        {
            this.uiDirector = uiDirector;
            this.gameSession = gameSession;
            this.sceneModel = sceneModel;
        }

        public void Initialize()
        {
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            await gameSession.LoadNewGame(ct);

            // test
            foreach (var post in gameSession.BeforeAppearingPosts)
            {
                uiDirector.AddPostToTimeline(post);
            }

            await uiDirector.ShowScreenAsync(SceneType.Game, ct);

            gameSession.Play();
        }

        public void Tick()
        {
            gameSession.Proceed(Time.deltaTime);
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            await uiDirector.HideScreenAsync(SceneType.Game, ct);
        }
    }
}
