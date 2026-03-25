using System.Collections.Generic;
using VContainer.Unity;
using System;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading;

using Unity1Week_Ura.Core;
using Unity1Week_Ura.Actor;
using UnityEngine;


namespace Unity1Week_Ura.Director
{
    public class MainEntryPoint : IAsyncStartable, ITickable, IDisposable
    {
        readonly SceneModel sceneModel;
        readonly TitleSceneDirector titleSceneDirector;
        readonly SelectSceneDirector selectSceneDirector;
        readonly GameSceneDirector gameSceneDirector;
        readonly ResultSceneDirector resultSceneDirector;

        readonly Dictionary<SceneType, ISceneDirector> sceneDirectors = new();   
        SceneType currentScene;

        readonly CompositeDisposable disposables = new();
        readonly CancellationTokenSource cts = new();

        public MainEntryPoint
        (
            SceneModel sceneModel,
            TitleSceneDirector titleSceneDirector,
            SelectSceneDirector selectSceneDirector,
            GameSceneDirector gameSceneDirector,
            ResultSceneDirector resultSceneDirector
        )
        {
            this.sceneModel = sceneModel;
            this.titleSceneDirector = titleSceneDirector;
            this.selectSceneDirector = selectSceneDirector;
            this.gameSceneDirector = gameSceneDirector;
            this.resultSceneDirector = resultSceneDirector;

            sceneDirectors[SceneType.Title] = titleSceneDirector;
            sceneDirectors[SceneType.Select] = selectSceneDirector;
            sceneDirectors[SceneType.Game] = gameSceneDirector;
            sceneDirectors[SceneType.Result] = resultSceneDirector;
        }

        public async UniTask StartAsync(CancellationToken ct)
        {
            foreach (var director in sceneDirectors.Values)
            {
                director.Initialize();
            }

            disposables.Clear();
            sceneModel.CurrentScene.Pairwise().Subscribe(pair =>
            {
                var (from, to) = pair;
                HandleSceneTransition(from, to).Forget();
            }).AddTo(disposables);

            sceneModel.OnSceneReload.Subscribe(scene =>
            {
                HandleSceneTransition(scene, scene).Forget();
            }).AddTo(disposables);

            // 初期シーンのDirectorのEnterAsyncを呼び出す
            currentScene = sceneModel.CurrentScene.CurrentValue;
            await GetCurrentSceneDirector(currentScene).EnterAsync(ct);
        }

        public void Tick()
        {
            GetCurrentSceneDirector(currentScene).Tick();
        }

        public void Dispose()
        {
            disposables.Dispose();
            cts.Cancel();
        }

        ISceneDirector GetCurrentSceneDirector(SceneType sceneType)
        {
            if (sceneDirectors.TryGetValue(sceneType, out var director))
            {
                return director;
            }
            throw new InvalidOperationException($"SceneDirector not found for SceneType: {sceneType}");
        }

        async UniTask HandleSceneTransition(SceneType from, SceneType to)
        {
            var fromDirector = GetCurrentSceneDirector(from);
            var toDirector = GetCurrentSceneDirector(to);
            Debug.Log($"[U1W-DIAG][TR-001] HandleSceneTransition start from={from} to={to}");

            try
            {
                Debug.Log($"[U1W-DIAG][TR-010] ExitAsync start scene={from}");
                await fromDirector.ExitAsync(cts.Token);
                Debug.Log($"[U1W-DIAG][TR-011] ExitAsync complete scene={from}");

                Debug.Log($"[U1W-DIAG][TR-020] EnterAsync start scene={to}");
                await toDirector.EnterAsync(cts.Token);
                Debug.Log($"[U1W-DIAG][TR-021] EnterAsync complete scene={to}");
                currentScene = to;
                Debug.Log($"[U1W-DIAG][TR-030] HandleSceneTransition complete currentScene={currentScene}");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[U1W-DIAG][TR-900] HandleSceneTransition canceled from={from} to={to}");
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[U1W-DIAG][TR-999] HandleSceneTransition failed from={from} to={to}\n{exception}");
                throw;
            }
            finally
            {
                sceneModel.ReleaseLock();
                Debug.Log($"[U1W-DIAG][TR-040] Scene lock released from={from} to={to}");
            }
        }
    }
}   