using System.Threading;
using R3;

namespace Unity1Week_Ura.Core
{
    public class SceneModel
    {
        public ReadOnlyReactiveProperty<SceneType> CurrentScene => currentScene;
        readonly ReactiveProperty<SceneType> currentScene = new(SceneType.Title);

        public Observable<SceneType> OnSceneReload => onSceneReload;
        readonly Subject<SceneType> onSceneReload = new();

        readonly SemaphoreSlim sceneTransitionSemaphore = new(1, 1);

        readonly GameConfigSO gameConfig;

        public SceneModel(GameConfigSO gameConfig)
        {
            this.gameConfig = gameConfig;
            currentScene.Value = gameConfig.InitialSceneType;
        }

        public bool ChangeScene(SceneType sceneType)
        {
            if (currentScene.Value == sceneType)
            {
                return false;
            }

            if (!sceneTransitionSemaphore.Wait(0))
            {
                // シーン遷移中の場合は無視する
                return false;
            }

            currentScene.Value = sceneType;
            return true;
        }

        public void ReleaseLock()
        {
            sceneTransitionSemaphore.Release();
        }

        public void ReloadCurrentScene()
        {
            if (!sceneTransitionSemaphore.Wait(0))
            {
                // シーン遷移中の場合は無視する
                return;
            }

            onSceneReload.OnNext(currentScene.Value);
        }
    }
}