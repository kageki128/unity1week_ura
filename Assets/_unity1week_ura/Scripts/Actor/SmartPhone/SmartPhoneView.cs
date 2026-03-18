using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class SmartPhoneView : MonoBehaviour
    {
        public Observable<Unit> OnStartButtonClicked => titlePhoneScreenView.OnStartButtonClicked;

        [SerializeField] TitlePhoneScreenView titlePhoneScreenView;
        [SerializeField] SelectPhoneScreenView selectPhoneScreenView;
        [SerializeField] GamePhoneScreenView gamePhoneScreenView;

        readonly Dictionary<SceneType, PhoneScreenViewBase> screenViews = new();

        void Awake()
        {
            screenViews.Add(SceneType.Title, titlePhoneScreenView);
            screenViews.Add(SceneType.Select, selectPhoneScreenView);
            screenViews.Add(SceneType.Game, gamePhoneScreenView);
        }

        public void Initialize()
        {
            foreach (var screenView in screenViews.Values)
            {
                screenView.Initialize();
            }
        }

        public async UniTask ShowScreenAsync(SceneType sceneType)
        {
            var screenView = GetScreenView(sceneType);
            await screenView.ShowAsync();
        }

        public async UniTask HideScreenAsync(SceneType sceneType)
        {
            var screenView = GetScreenView(sceneType);
            await screenView.HideAsync();
        }

        PhoneScreenViewBase GetScreenView(SceneType sceneType)
        {
            if (screenViews.TryGetValue(sceneType, out var screenView))
            {
                return screenView;
            }

            throw new KeyNotFoundException($"Screen view for scene type {sceneType} not found.");
        }
    }
}