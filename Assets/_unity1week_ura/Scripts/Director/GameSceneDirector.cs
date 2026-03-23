using System;
using System.Collections.Generic;
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
            gameViewHub.ClearTimeline();
            gameViewHub.ClearDrafts();

            await gameSession.LoadNewGame(ct);
            
            gameViewHub.SetPlayerAccounts(gameSession.PlayerAccounts);
            gameViewHub.SetScore(gameSession.Score.CurrentValue);
            gameViewHub.SetRemainingTime(gameSession.RemainingTimeSeconds.CurrentValue);
            await gameViewHub.ShowAsync(ct);

            // 投稿されたポストを購読
            gameSession.PublishedPosts.ObserveAdd().Subscribe(addEvent =>
            {
                gameViewHub.AddPostToTimeline(addEvent.Value);
            }).AddTo(disposables);
            gameSession.DraftPosts.ObserveAdd().Subscribe(addEvent =>
            {
                gameViewHub.AddDraft(addEvent.Value);
            }).AddTo(disposables);
            gameSession.DraftPosts.ObserveRemove().Subscribe(removeEvent =>
            {
                gameViewHub.RemoveDraft(removeEvent.Value);
            }).AddTo(disposables);
            gameViewHub.OnNormalDraftDroppedToPublish.Subscribe(gameSession.TryPublishNormalDraft).AddTo(disposables);
            gameViewHub.OnReplyDraftDroppedToPublish.Subscribe(gameSession.TryPublishReplyDraft).AddTo(disposables);
            gameViewHub.OnPlayerAccountClicked.Subscribe(gameSession.SetCurrentPlayerAccount).AddTo(disposables);
            gameViewHub.OnLikedByPlayer.Subscribe(gameSession.LikePostByPlayer).AddTo(disposables);
            gameViewHub.OnRepostedByPlayer.Subscribe(gameSession.RepostByPlayer).AddTo(disposables);
            
            gameViewHub.OnSelectSceneButtonClicked.Subscribe(_ => gameSession.CancelGame()).AddTo(disposables);
            gameViewHub.OnRestartButtonClicked.Subscribe(_ => gameSession.RestartGame()).AddTo(disposables);
            
            // UI
            gameSession.SelectedPlayerAccount.Subscribe(gameViewHub.SetSelectedPlayerAccount).AddTo(disposables);
            gameSession.Score.Subscribe(gameViewHub.SetScore).AddTo(disposables);
            gameSession.RemainingTimeSeconds.Subscribe(gameViewHub.SetRemainingTime).AddTo(disposables);
            
            // ゲーム終了を購読
            gameSession.OnGameCanceled.Subscribe(_ =>
            {
                sceneModel.ChangeScene(SceneType.Select);
            }).AddTo(disposables);

            gameSession.OnGameRestarted.Subscribe(_ =>
            {
                sceneModel.ReloadCurrentScene();
            }).AddTo(disposables);
            gameSession.OnGameFinished.Take(1).Subscribe(_ =>
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
