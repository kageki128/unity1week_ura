using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

using Unity1Week_Ura.Actor;
using Unity1Week_Ura.Core;
using System;

namespace Unity1Week_Ura.Director
{
    public class TitleSceneDirector : ISceneDirector, IDisposable
    {
        readonly SmartPhoneView smartPhoneView;
        readonly SceneModel sceneModel;

        readonly CompositeDisposable disposables = new();

        public TitleSceneDirector(SmartPhoneView smartPhoneView, SceneModel sceneModel)
        {
            this.smartPhoneView = smartPhoneView;
            this.sceneModel = sceneModel;
        }

        public void Dispose()
        {
            disposables.Dispose();
        }

        public void Initialize()
        {
            smartPhoneView.Initialize();

            disposables.Clear();
            smartPhoneView.OnStartButtonClicked.Subscribe(_ => 
            {
                StartButtonHandler();
            }).AddTo(disposables);
        }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            await smartPhoneView.ShowScreenAsync(SceneType.Title, ct);
        }

        public void Tick()
        {
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            disposables.Clear();
            await smartPhoneView.HideScreenAsync(SceneType.Title, ct);
        }

        void StartButtonHandler()
        {
            sceneModel.ChangeScene(SceneType.Select);
        }
    }
}
