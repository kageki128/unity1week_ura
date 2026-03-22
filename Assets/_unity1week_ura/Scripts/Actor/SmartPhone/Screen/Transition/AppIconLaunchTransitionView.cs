using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class AppIconLaunchTransitionView : AnimationViewBase
    {
        [Header("Dependencies")]
        [SerializeField] GameObject iconObject;
        [SerializeField] GameObject coverObject;
        [SerializeField] GameObject flashObject;
        [SerializeField] Sprite appIconSprite;
        [SerializeField, Min(0.1f)] float animationDurationMultiplier = 2f;
        [SerializeField, Min(0.1f)] float iconScaleMultiplier = 3f;

        [Header("Show (Icon)")]
        [SerializeField] Vector3 iconStandbyScale = new(0.9f, 0.9f, 1f);
        [SerializeField] Vector3 iconPressedScale = new(0.8f, 0.8f, 1f);
        [SerializeField] Vector3 iconPopScale = new(1.08f, 1.08f, 1f);
        [SerializeField] Vector3 iconFadeScale = new(1.22f, 1.22f, 1f);
        [SerializeField, Min(0f)] float pressDuration = 0.05f;
        [SerializeField] Ease pressEase = Ease.OutQuad;
        [SerializeField, Min(0f)] float popDuration = 0.15f;
        [SerializeField] Ease popEase = Ease.OutBack;
        [SerializeField, Min(0f)] float iconFadeDuration = 0.15f;
        [SerializeField] Ease iconFadeEase = Ease.InCubic;
        [SerializeField, Range(-45f, 45f)] float standbyTilt = -8f;
        [SerializeField, Range(-45f, 45f)] float pressedTilt = -2f;

        [Header("Show (Cover)")]
        [SerializeField] Vector3 coverStandbyScale = new(0.96f, 0.96f, 1f);
        [SerializeField] Vector3 coverOvershootScale = new(1.03f, 1.03f, 1f);
        [SerializeField] Vector3 coverSettledScale = new(1f, 1f, 1f);
        [SerializeField, Min(0f)] float coverInDuration = 0.16f;
        [SerializeField] Ease coverInEase = Ease.OutCubic;
        [SerializeField, Min(0f)] float coverSettleDuration = 0.08f;
        [SerializeField] Ease coverSettleEase = Ease.OutCubic;

        [Header("Show (Flash)")]
        [SerializeField, Range(0f, 1f)] float flashPeakAlpha = 0.16f;
        [SerializeField, Min(0f)] float flashInDuration = 0.03f;
        [SerializeField, Min(0f)] float flashOutDuration = 0.08f;
        [SerializeField] Ease flashEase = Ease.OutQuad;

        [Header("Hide (Content Reveal)")]
        [SerializeField, Min(0f)] float hideDuration = 0.2f;
        [SerializeField] Ease hideEase = Ease.InCubic;
        [SerializeField] Vector3 hideCoverScale = new(0.98f, 0.98f, 1f);

        Tween currentTween;
        Transform iconTransform;
        Transform coverTransform;
        Transform flashTransform;
        SpriteRenderer iconRenderer;
        SpriteRenderer coverRenderer;
        SpriteRenderer flashRenderer;
        Vector3 baseIconScale;
        Vector3 baseCoverScale;
        Vector3 baseFlashScale = Vector3.one;
        bool hasCachedBaseScale;
        float iconRotationOffset;

        public override void Initialize()
        {
            ResolveDependencies();
            CacheBaseScalesIfNeeded();
            KillCurrentTween();
            ApplyIconSpriteIfNeeded();
            ResetVisualState();
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            ResolveDependencies();
            CacheBaseScalesIfNeeded();
            ApplyIconSpriteIfNeeded();
            KillCurrentTween();
            ResetVisualState();
            gameObject.SetActive(true);
            await PlayShowAsync(ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            ResolveDependencies();

            if (!gameObject.activeSelf)
            {
                return;
            }

            await PlayHideAsync(ct);
            ResetVisualState();
            gameObject.SetActive(false);
        }

        async UniTask PlayShowAsync(CancellationToken ct)
        {
            var scaledPressDuration = GetDuration(pressDuration);
            var scaledPopDuration = GetDuration(popDuration);
            var scaledIconFadeDuration = GetDuration(iconFadeDuration);
            var scaledCoverInDuration = GetDuration(coverInDuration);
            var scaledCoverSettleDuration = GetDuration(coverSettleDuration);
            var scaledFlashInDuration = GetDuration(flashInDuration);
            var scaledFlashOutDuration = GetDuration(flashOutDuration);
            var coverStartTime = Mathf.Max(0f, scaledPressDuration * 0.5f);
            var sequence = DOTween.Sequence();

            if (scaledPressDuration > 0f)
            {
                _ = sequence.Append(iconTransform.DOScale(GetAbsoluteIconScale(iconPressedScale), scaledPressDuration).SetEase(pressEase));
                _ = sequence.Join(iconTransform.DOLocalRotate(GetIconRotationEuler(pressedTilt), scaledPressDuration).SetEase(pressEase));
            }
            else
            {
                SetIconTransform(iconPressedScale, pressedTilt);
            }

            if (scaledPopDuration > 0f)
            {
                _ = sequence.Append(iconTransform.DOScale(GetAbsoluteIconScale(iconPopScale), scaledPopDuration).SetEase(popEase));
                _ = sequence.Join(iconTransform.DOLocalRotate(GetIconRotationEuler(0f), scaledPopDuration).SetEase(popEase));
            }
            else
            {
                SetIconTransform(iconPopScale, 0f);
            }

            if (scaledIconFadeDuration > 0f)
            {
                _ = sequence.Append(iconTransform.DOScale(GetAbsoluteIconScale(iconFadeScale), scaledIconFadeDuration).SetEase(iconFadeEase));
                _ = sequence.Join(iconRenderer.DOFade(0f, scaledIconFadeDuration).SetEase(iconFadeEase));
            }
            else
            {
                SetIconAlpha(0f);
                SetIconTransform(iconFadeScale, 0f);
            }

            if (scaledCoverInDuration > 0f)
            {
                _ = sequence.Insert(coverStartTime, coverRenderer.DOFade(1f, scaledCoverInDuration).SetEase(coverInEase));
                _ = sequence.Insert(coverStartTime, coverTransform.DOScale(ScaleBy(baseCoverScale, coverOvershootScale), scaledCoverInDuration).SetEase(coverInEase));
                if (scaledCoverSettleDuration > 0f)
                {
                    var settleStart = coverStartTime + scaledCoverInDuration;
                    _ = sequence.Insert(settleStart, coverTransform.DOScale(ScaleBy(baseCoverScale, coverSettledScale), scaledCoverSettleDuration).SetEase(coverSettleEase));
                }
            }
            else
            {
                SetCoverAlpha(1f);
                coverTransform.localScale = ScaleBy(baseCoverScale, coverSettledScale);
            }

            if (flashRenderer != null && flashPeakAlpha > 0f)
            {
                if (scaledFlashInDuration > 0f)
                {
                    _ = sequence.Insert(coverStartTime, flashRenderer.DOFade(flashPeakAlpha, scaledFlashInDuration).SetEase(flashEase));
                }
                else
                {
                    SetFlashAlpha(flashPeakAlpha);
                }

                if (scaledFlashOutDuration > 0f)
                {
                    _ = sequence.Insert(coverStartTime + scaledFlashInDuration, flashRenderer.DOFade(0f, scaledFlashOutDuration).SetEase(flashEase));
                }
                else
                {
                    SetFlashAlpha(0f);
                }
            }

            await AwaitTweenAsync(sequence, ct);
        }

        async UniTask PlayHideAsync(CancellationToken ct)
        {
            var scaledHideDuration = GetDuration(hideDuration);
            if (scaledHideDuration <= 0f)
            {
                SetCoverAlpha(0f);
                SetFlashAlpha(0f);
                return;
            }

            var sequence = DOTween.Sequence();
            _ = sequence.Join(coverRenderer.DOFade(0f, scaledHideDuration).SetEase(hideEase));
            _ = sequence.Join(coverTransform.DOScale(ScaleBy(baseCoverScale, hideCoverScale), scaledHideDuration).SetEase(hideEase));
            if (flashRenderer != null)
            {
                _ = sequence.Join(flashRenderer.DOFade(0f, scaledHideDuration).SetEase(hideEase));
            }

            await AwaitTweenAsync(sequence, ct);
        }

        async UniTask AwaitTweenAsync(Tween tween, CancellationToken ct)
        {
            KillCurrentTween();
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

        void ResolveDependencies()
        {
            if (iconObject == null)
            {
                var renderer = GetComponentInChildren<SpriteRenderer>(true);
                if (renderer != null)
                {
                    iconRenderer = renderer;
                    iconObject = renderer.gameObject;
                    iconTransform = renderer.transform;
                }
            }

            if (iconObject == null)
            {
                throw new InvalidOperationException("AppIconLaunchTransitionView: iconObject is not assigned.");
            }

            if (iconTransform == null)
            {
                iconTransform = iconObject.transform;
            }

            if (iconRenderer == null)
            {
                iconRenderer = iconObject.GetComponent<SpriteRenderer>();
            }

            if (iconRenderer == null)
            {
                throw new InvalidOperationException("AppIconLaunchTransitionView: SpriteRenderer on iconObject is not assigned.");
            }

            if (coverObject == null)
            {
                throw new InvalidOperationException("AppIconLaunchTransitionView: coverObject is not assigned.");
            }

            if (coverTransform == null)
            {
                coverTransform = coverObject.transform;
            }

            if (coverRenderer == null)
            {
                coverRenderer = coverObject.GetComponent<SpriteRenderer>();
            }

            if (coverRenderer == null)
            {
                throw new InvalidOperationException("AppIconLaunchTransitionView: SpriteRenderer on coverObject is not assigned.");
            }

            if (flashObject == null)
            {
                flashTransform = null;
                flashRenderer = null;
                return;
            }

            if (flashTransform == null)
            {
                flashTransform = flashObject.transform;
            }

            if (flashRenderer == null)
            {
                flashRenderer = flashObject.GetComponent<SpriteRenderer>();
            }

            if (flashRenderer == null)
            {
                throw new InvalidOperationException("AppIconLaunchTransitionView: SpriteRenderer on flashObject is not assigned.");
            }
        }

        void ApplyIconSpriteIfNeeded()
        {
            if (appIconSprite != null && iconRenderer.sprite != appIconSprite)
            {
                iconRenderer.sprite = appIconSprite;
            }
        }

        void SetIconTransform(Vector3 scale, float zRotation)
        {
            iconTransform.localScale = ScaleBy(baseIconScale, GetIconScale(scale));
            iconTransform.localRotation = Quaternion.Euler(GetIconRotationEuler(zRotation));
        }

        void SetIconAlpha(float alpha)
        {
            var color = iconRenderer.color;
            color.a = alpha;
            iconRenderer.color = color;
        }

        void SetCoverAlpha(float alpha)
        {
            var color = coverRenderer.color;
            color.a = alpha;
            coverRenderer.color = color;
        }

        void SetFlashAlpha(float alpha)
        {
            if (flashRenderer == null)
            {
                return;
            }

            var color = flashRenderer.color;
            color.a = alpha;
            flashRenderer.color = color;
        }

        float GetDuration(float duration)
        {
            if (duration <= 0f)
            {
                return 0f;
            }

            return duration * animationDurationMultiplier;
        }

        Vector3 GetIconScale(Vector3 scale)
        {
            return new Vector3(
                scale.x * iconScaleMultiplier,
                scale.y * iconScaleMultiplier,
                scale.z);
        }

        Vector3 GetAbsoluteIconScale(Vector3 scale)
        {
            return ScaleBy(baseIconScale, GetIconScale(scale));
        }

        Vector3 GetIconRotationEuler(float zRotation)
        {
            return new Vector3(0f, 0f, zRotation + iconRotationOffset);
        }

        void CacheBaseScalesIfNeeded()
        {
            if (hasCachedBaseScale)
            {
                return;
            }

            baseIconScale = iconTransform.localScale;
            baseCoverScale = coverTransform.localScale;
            if (flashTransform != null)
            {
                baseFlashScale = flashTransform.localScale;
            }

            hasCachedBaseScale = true;
        }

        Vector3 ScaleBy(Vector3 baseScale, Vector3 multiplier)
        {
            return new Vector3(
                baseScale.x * multiplier.x,
                baseScale.y * multiplier.y,
                baseScale.z * multiplier.z);
        }

        public void SetIconRotationOffset(float zRotation)
        {
            iconRotationOffset = zRotation;
        }

        void ResetVisualState()
        {
            SetIconTransform(iconStandbyScale, standbyTilt);
            SetIconAlpha(1f);
            coverTransform.localScale = ScaleBy(baseCoverScale, coverStandbyScale);
            SetCoverAlpha(0f);
            if (flashTransform != null)
            {
                flashTransform.localScale = baseFlashScale;
            }
            SetFlashAlpha(0f);
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
