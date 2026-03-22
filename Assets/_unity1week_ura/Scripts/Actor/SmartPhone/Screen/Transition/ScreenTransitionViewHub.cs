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

        Dictionary<ScreenTransitionType, AnimationViewBase> transitionViews = new();
        readonly SemaphoreSlim transitionSemaphore = new(1, 1);

        AnimationViewBase currentTransitionView;

        public void Initialize()
        {
            transitionViews.Clear();
            transitionViews = new Dictionary<ScreenTransitionType, AnimationViewBase>
            {
                { ScreenTransitionType.CircleWipe, circleWipeTransitionView },
                { ScreenTransitionType.WhiteFade, whiteFadeTransitionView },
            };

            foreach (var view in transitionViews)
            {
                view.Value.Initialize();
            }

            currentTransitionView = null;
        }

        public async UniTask ShowAsync(ScreenTransitionType type, CancellationToken ct)
        {
            await transitionSemaphore.WaitAsync(ct);
            try
            {
                var view = transitionViews[type];

                if (currentTransitionView == view)
                {
                    return;
                }

                if (currentTransitionView != null)
                {
                    await currentTransitionView.HideAsync(ct);
                }

                await view.ShowAsync(ct);
                currentTransitionView = view;
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
    }
}