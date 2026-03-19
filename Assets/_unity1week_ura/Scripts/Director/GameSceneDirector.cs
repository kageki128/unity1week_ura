using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ObservableCollections;
using R3;

using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Director
{
    public class GameSceneDirector : ISceneDirector, IDisposable
    {
        readonly UIDirector uiDirector;
        readonly GameSession gameSession;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();

        public GameSceneDirector(UIDirector uiDirector, GameSession gameSession, SceneModel sceneModel)
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
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            await gameSession.LoadNewGame(ct);

            // 投稿されたポストを購読
            gameSession.PublishedPosts.ObserveAdd().Subscribe(addEvent =>
            {
                uiDirector.AddPostToTimeline(addEvent.Value);
            }).AddTo(disposables);

            await uiDirector.EnterAsync(SceneType.Game, ct);

            gameSession.Play();
        }

        public void Tick()
        {
            gameSession.Proceed(Time.deltaTime);
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            disposables.Clear();
            await uiDirector.ExitAsync(SceneType.Game, ct);
        }
    }
}
