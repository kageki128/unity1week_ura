using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ObservableCollections;
using R3;
using Unity1Week_Ura.Actor;

using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Director
{
    public class GameSceneDirector : ISceneDirector, IDisposable
    {
        readonly GameViewHub gameViewHub;
        readonly GameSession gameSession;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();

        public GameSceneDirector(GameViewHub gameViewHub, GameSession gameSession, SceneModel sceneModel)
        {
            this.gameViewHub = gameViewHub;
            this.gameSession = gameSession;
            this.sceneModel = sceneModel;
        }

        public void Dispose()
        {
            disposables.Dispose();
        }

        public void Initialize()
        {
            gameViewHub.Initialize();
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            disposables.Clear();

            await gameSession.LoadNewGame(ct);
            await gameViewHub.ShowAsync(ct);

            // 投稿されたポストを購読
            gameSession.PublishedPosts.ObserveAdd().Subscribe(addEvent =>
            {
                gameViewHub.AddPostToTimeline(addEvent.Value);
            }).AddTo(disposables);
            // UI
            gameSession.Score.Subscribe(score =>
            {
                gameViewHub.SetScore(score);
            }).AddTo(disposables);
            gameSession.RemainingTimeSeconds.Subscribe(remainingTime =>
            {
                gameViewHub.SetRemainingTime(remainingTime);
            }).AddTo(disposables);
            // ゲーム終了を購読
            gameSession.CurrentGameState.Where(state => state == GameState.Finished).Take(1).Subscribe(_ =>
            {
                sceneModel.ChangeScene(SceneType.Result);
            }).AddTo(disposables);

            gameSession.Play();
        }

        public void Tick()
        {
            gameSession.Proceed(Time.deltaTime);
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            disposables.Clear();
            await gameViewHub.HideAsync(ct);
        }
    }
}
