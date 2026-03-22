using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ScreenTransitionViewHub : MonoBehaviour
    {
        [SerializeField] CircleWipeTransitionView circleWipeTransitionView;
        [SerializeField] WhiteFadeTransitionView whiteFadeTransitionView;
        [SerializeField] AppIconLaunchTransitionView appIconLaunchTransitionView;

        Dictionary<ScreenTransitionType, AnimationViewBase> transitionViews = new();
        readonly SemaphoreSlim transitionSemaphore = new(1, 1);

        AnimationViewBase currentTransitionView;
        ScreenTransitionType currentTransitionType;
        bool hasCurrentTransitionType;

        public void Initialize()
        {
            transitionViews.Clear();
            var appIconTransition = ResolveAppIconTransition();
            transitionViews = new Dictionary<ScreenTransitionType, AnimationViewBase>
            {
                { ScreenTransitionType.CircleWipe, circleWipeTransitionView },
                { ScreenTransitionType.WhiteFade, whiteFadeTransitionView },
                { ScreenTransitionType.AppIconLaunchPortrait, appIconTransition },
                { ScreenTransitionType.AppIconLaunchLandscape, appIconTransition },
            };

            foreach (var view in transitionViews)
            {
                view.Value.Initialize();
            }

            currentTransitionView = null;
            hasCurrentTransitionType = false;
        }

        public async UniTask ShowAsync(ScreenTransitionType type, CancellationToken ct)
        {
            await transitionSemaphore.WaitAsync(ct);
            try
            {
                if (!transitionViews.TryGetValue(type, out var view))
                {
                    throw new KeyNotFoundException($"Transition view for type {type} not found.");
                }

                ConfigureTransitionView(type, view);

                if (currentTransitionView == view && hasCurrentTransitionType && currentTransitionType == type)
                {
                    return;
                }

                if (currentTransitionView != null)
                {
                    await currentTransitionView.HideAsync(ct);
                }

                await view.ShowAsync(ct);
                currentTransitionView = view;
                currentTransitionType = type;
                hasCurrentTransitionType = true;
            }
            finally
            {
                transitionSemaphore.Release();
            }
        }

        public async UniTask HideAsync(CancellationToken ct)
        {
            await transitionSemaphore.WaitAsync(ct);
            try
            {
                if (currentTransitionView == null)
                {
                    return;
                }

                await currentTransitionView.HideAsync(ct);
                currentTransitionView = null;
                hasCurrentTransitionType = false;
            }
            finally
            {
                transitionSemaphore.Release();
            }
        }

        void OnDestroy()
        {
            transitionSemaphore.Dispose();
        }

        AppIconLaunchTransitionView ResolveAppIconTransition()
        {
            if (appIconLaunchTransitionView != null)
            {
                return appIconLaunchTransitionView;
            }

            if (circleWipeTransitionView == null)
            {
                throw new KeyNotFoundException("ScreenTransitionViewHub: circleWipeTransitionView is not assigned.");
            }

            appIconLaunchTransitionView = circleWipeTransitionView.GetComponent<AppIconLaunchTransitionView>();
            if (appIconLaunchTransitionView != null)
            {
                return appIconLaunchTransitionView;
            }

            throw new KeyNotFoundException("ScreenTransitionViewHub: appIconLaunchTransitionView is not assigned. Assign it in Inspector.");
        }

        void ConfigureTransitionView(ScreenTransitionType type, AnimationViewBase view)
        {
            if (view is not AppIconLaunchTransitionView appIconLaunchTransition)
            {
                return;
            }

            switch (type)
            {
                case ScreenTransitionType.AppIconLaunchLandscape:
                    appIconLaunchTransition.SetIconRotationOffset(-90f);
                    return;
                case ScreenTransitionType.AppIconLaunchPortrait:
                    appIconLaunchTransition.SetIconRotationOffset(0f);
                    return;
            }
        }
    }
}
