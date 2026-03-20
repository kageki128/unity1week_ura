using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class CircleWipeTransitionView : AnimationViewBase
    {
        [Header("Dependencies")]
        [SerializeField] Transform circleTransform;

        [Header("Show (Circle Expands)")]
        [SerializeField] Vector3 hiddenScale = new(0f, 0f, 1f);
        [SerializeField] Vector3 coveredScale = new(30f, 30f, 1f);
        [SerializeField, Min(0f)] float showDuration = 0.5f;
        [SerializeField] Ease showEase = Ease.InCubic;

        [Header("Hide (Circle Clears)")]
        [SerializeField, Min(0f)] float hideDuration = 0.5f;
        [SerializeField] Ease hideEase = Ease.OutCubic;

        Tween currentTween;

        public override void Initialize()
        {
            ValidateDependencies();
            KillCurrentTween();
            circleTransform.localScale = hiddenScale;
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            ValidateDependencies();
            circleTransform.localScale = hiddenScale;
            gameObject.SetActive(true);
            await PlayScaleAsync(coveredScale, showDuration, showEase, ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            ValidateDependencies();

            if (!gameObject.activeSelf)
            {
                return;
            }

            await PlayScaleAsync(hiddenScale, hideDuration, hideEase, ct);
            gameObject.SetActive(false);
        }

        async UniTask PlayScaleAsync(Vector3 targetScale, float duration, Ease ease, CancellationToken ct)
        {
            KillCurrentTween();

            if (duration <= 0f)
            {
                circleTransform.localScale = targetScale;
                return;
            }

            var tween = circleTransform.DOScale(targetScale, duration).SetEase(ease);
            currentTween = tween;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            try
            {
                await tween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                KillCurrentTween();
                throw;
            }
            finally
            {
                if (currentTween == tween)
                {
                    currentTween = null;
                }
            }
        }

        void ValidateDependencies()
        {
            if (circleTransform == null)
            {
                throw new System.InvalidOperationException("CircleWipeTransitionView: circleTransform is not assigned.");
            }
        }

        void KillCurrentTween()
        {
            if (currentTween == null)
            {
                return;
            }

            if (currentTween.IsActive())
            {
                currentTween.Kill();
            }

            currentTween = null;
        }

        void OnDestroy()
        {
            KillCurrentTween();
        }
    }
}