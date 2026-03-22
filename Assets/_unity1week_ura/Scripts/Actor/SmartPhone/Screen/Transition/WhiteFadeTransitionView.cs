using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class WhiteFadeTransitionView : AnimationViewBase
    {
        [Header("Dependencies")]
        [SerializeField] GameObject fadeObject;

        [Header("Show (Fade In to white)")]
        [SerializeField, Min(0f)] float showDuration = 0.15f;
        [SerializeField] Ease showEase = Ease.OutCubic;

        [Header("Hide (Fade Out to clear)")]
        [SerializeField, Min(0f)] float hideDuration = 0.15f;
        [SerializeField] Ease hideEase = Ease.InCubic;

        Tween currentTween;
        SpriteRenderer fadeRenderer;

        public override void Initialize()
        {
            ValidateDependencies();
            KillCurrentTween();
            SetFadeAlpha(0f);
            gameObject.SetActive(false);
        }

        // ShowAsync in Transition means the transition effect appears (cover screen with white)
        public override async UniTask ShowAsync(CancellationToken ct)
        {
            ValidateDependencies();
            gameObject.SetActive(true);
            SetFadeAlpha(0f);
            await PlayFadeAsync(1f, showDuration, showEase, ct);
        }

        // HideAsync in Transition means the transition effect disappears (uncover screen)
        public override async UniTask HideAsync(CancellationToken ct)
        {
            ValidateDependencies();

            if (!gameObject.activeSelf)
            {
                return;
            }

            await PlayFadeAsync(0f, hideDuration, hideEase, ct);
            gameObject.SetActive(false);
        }

        async UniTask PlayFadeAsync(float targetAlpha, float duration, Ease ease, CancellationToken ct)
        {
            KillCurrentTween();

            if (duration <= 0f)
            {
                SetFadeAlpha(targetAlpha);
                return;
            }

            var tween = fadeRenderer.DOFade(targetAlpha, duration).SetEase(ease);
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
            if (fadeObject == null)
            {
                throw new InvalidOperationException("WhiteFadeTransitionView: fadeObject is not assigned.");
            }

            if (fadeRenderer == null)
            {
                fadeRenderer = fadeObject.GetComponent<SpriteRenderer>();
            }

            if (fadeRenderer == null)
            {
                throw new InvalidOperationException("WhiteFadeTransitionView: SpriteRenderer on fadeObject is not assigned.");
            }
        }

        void SetFadeAlpha(float alpha)
        {
            var color = fadeRenderer.color;
            color.a = alpha;
            fadeRenderer.color = color;
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
