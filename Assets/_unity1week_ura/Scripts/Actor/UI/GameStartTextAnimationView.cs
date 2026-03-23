using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    [DisallowMultipleComponent]
    public class GameStartTextAnimationView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] TMP_Text messageText;
        [SerializeField] RectTransform textTransform;

        [Header("Text")]
        [SerializeField] string readyText = "READY...?";
        [SerializeField] string startText = "START!";
        [SerializeField] Color readyColor = new Color32(0xEE, 0x5A, 0x7F, 0xFF);
        [SerializeField] Color startColor = new Color32(0xEE, 0x5A, 0x7F, 0xFF);

        [Header("Ready Phase")]
        [SerializeField, Min(0.01f)] float readyIntroDuration = 0.56f;
        [SerializeField, Min(0.01f)] float readyPulseDuration = 0.3f;
        [SerializeField, Min(0f)] float readyHoldDuration = 0.32f;
        [SerializeField, Min(0.01f)] float readyFadeOutDuration = 0.4f;
        [SerializeField, Min(0.1f)] float readyStartScale = 1.42f;
        [SerializeField, Min(0.1f)] float readyPulseScale = 1.045f;
        [SerializeField] float readyStartTiltZ = -8f;
        [SerializeField, Min(0f)] float readyStartOffsetY = 34f;
        [SerializeField, Min(0f)] float readyFadeOutDropY = 22f;
        [SerializeField] float readyStartCharacterSpacing = 9f;
        [SerializeField] float readyBaseCharacterSpacing = 1.2f;

        [Header("Start Phase")]
        [SerializeField, Min(0.01f)] float startChargeDuration = 0.24f;
        [SerializeField, Min(0.01f)] float startImpactDuration = 0.28f;
        [SerializeField, Min(0.01f)] float startSettleDuration = 0.3f;
        [SerializeField, Min(0.01f)] float startRecoverDuration = 0.18f;
        [SerializeField, Min(0f)] float startHoldDuration = 0.32f;
        [SerializeField, Min(0.01f)] float startFadeOutDuration = 0.28f;
        [SerializeField, Min(0.1f)] float startInitialScale = 3.05f;
        [SerializeField, Min(0.1f)] float startChargeScale = 3.22f;
        [SerializeField, Min(0.1f)] float startImpactScale = 0.82f;
        [SerializeField, Min(0.1f)] float startSettleScale = 1.08f;
        [SerializeField] float startInitialOffsetY = 72f;
        [SerializeField] float startImpactOvershootY = -10f;
        [SerializeField] float startInitialTiltZ = -3.5f;
        [SerializeField] float startImpactTiltZ = 1.6f;
        [SerializeField] float startCharacterSpacingStart = 12f;
        [SerializeField] float startCharacterSpacingImpact = -3f;
        [SerializeField] float startCharacterSpacingSettle = 0.5f;

        [Header("Behavior")]
        [SerializeField] bool hideOnInitialize = true;
        [SerializeField] bool deactivateOnComplete = true;

        Vector3 baseLocalPosition;
        Vector3 baseLocalScale = Vector3.one;
        Quaternion baseLocalRotation = Quaternion.identity;
        Tween activeTween;
        float currentAlpha;
        float baseCharacterSpacing;
        Color currentMessageColor = Color.white;
        bool isInitialized;

        public void Initialize()
        {
            ResolveReferences();
            CacheBaseTransform();
            isInitialized = true;

            if (hideOnInitialize)
            {
                SetHiddenInstant();
                gameObject.SetActive(false);
                return;
            }

            SetAlpha(0f);
        }

        public async UniTask PlayAsync(CancellationToken ct)
        {
            EnsureInitialized();
            if (!CanPlayAnimation())
            {
                return;
            }

            KillActiveTween();
            gameObject.SetActive(true);

            try
            {
                await PlayReadyPhaseAsync(ct);
                await PlayStartPhaseAsync(ct);
            }
            finally
            {
                SetHiddenInstant();
                if (deactivateOnComplete)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        async UniTask PlayReadyPhaseAsync(CancellationToken ct)
        {
            SetMessage(readyText, readyColor);
            SetAlpha(0f);
            ResetTransform();

            textTransform.localScale = baseLocalScale * readyStartScale;
            textTransform.localPosition = baseLocalPosition + Vector3.up * readyStartOffsetY;
            textTransform.localRotation = Quaternion.Euler(0f, 0f, readyStartTiltZ);
            SetCharacterSpacing(readyStartCharacterSpacing);

            var fadeOutTarget = baseLocalPosition + Vector3.down * readyFadeOutDropY;
            var sequence = DOTween.Sequence()
                .Append(DOTween.To(GetAlpha, SetAlpha, 1f, readyIntroDuration).SetEase(Ease.OutCubic))
                .Join(textTransform.DOLocalMove(baseLocalPosition, readyIntroDuration).SetEase(Ease.OutCubic))
                .Join(textTransform.DOScale(baseLocalScale, readyIntroDuration).SetEase(Ease.OutBack))
                .Join(textTransform.DOLocalRotate(baseLocalRotation.eulerAngles, readyIntroDuration).SetEase(Ease.OutCubic))
                .Join(DOTween.To(GetCharacterSpacing, SetCharacterSpacing, readyBaseCharacterSpacing, readyIntroDuration).SetEase(Ease.OutCubic))
                .Append(textTransform.DOScale(baseLocalScale * readyPulseScale, readyPulseDuration * 0.45f).SetEase(Ease.OutQuad))
                .Append(textTransform.DOScale(baseLocalScale, readyPulseDuration * 0.55f).SetEase(Ease.OutBack))
                .AppendInterval(readyHoldDuration)
                .Append(DOTween.To(GetAlpha, SetAlpha, 0f, readyFadeOutDuration).SetEase(Ease.InCubic))
                .Join(textTransform.DOLocalMove(fadeOutTarget, readyFadeOutDuration).SetEase(Ease.InCubic))
                .Join(DOTween.To(GetCharacterSpacing, SetCharacterSpacing, readyStartCharacterSpacing, readyFadeOutDuration).SetEase(Ease.InCubic));

            await AwaitTweenAsync(sequence, ct);
        }

        async UniTask PlayStartPhaseAsync(CancellationToken ct)
        {
            SetMessage(startText, startColor);
            SetAlpha(0f);
            ResetTransform();

            textTransform.localPosition = baseLocalPosition + Vector3.up * startInitialOffsetY;
            textTransform.localScale = baseLocalScale * startInitialScale;
            textTransform.localRotation = Quaternion.Euler(0f, 0f, startInitialTiltZ);
            SetCharacterSpacing(startCharacterSpacingStart);

            var impactPosition = baseLocalPosition + Vector3.up * startImpactOvershootY;
            var sequence = DOTween.Sequence()
                .Append(DOTween.To(GetAlpha, SetAlpha, 1f, startChargeDuration * 0.7f).SetEase(Ease.OutQuad))
                .Join(textTransform.DOScale(baseLocalScale * startChargeScale, startChargeDuration).SetEase(Ease.OutCubic))
                .Join(textTransform.DOLocalRotate(Vector3.zero, startChargeDuration).SetEase(Ease.OutCubic))
                .Append(textTransform.DOScale(baseLocalScale * startImpactScale, startImpactDuration).SetEase(Ease.InExpo))
                .Join(textTransform.DOLocalMove(impactPosition, startImpactDuration).SetEase(Ease.InExpo))
                .Join(textTransform.DOLocalRotate(new Vector3(0f, 0f, startImpactTiltZ), startImpactDuration).SetEase(Ease.InExpo))
                .Join(DOTween.To(GetCharacterSpacing, SetCharacterSpacing, startCharacterSpacingImpact, startImpactDuration).SetEase(Ease.InExpo))
                .Append(textTransform.DOScale(baseLocalScale * startSettleScale, startSettleDuration).SetEase(Ease.OutBack))
                .Join(textTransform.DOLocalMove(baseLocalPosition, startSettleDuration).SetEase(Ease.OutBack))
                .Join(textTransform.DOLocalRotate(Vector3.zero, startSettleDuration).SetEase(Ease.OutBack))
                .Join(DOTween.To(GetCharacterSpacing, SetCharacterSpacing, startCharacterSpacingSettle, startSettleDuration).SetEase(Ease.OutBack))
                .Append(textTransform.DOScale(baseLocalScale, startRecoverDuration).SetEase(Ease.OutQuad))
                .Join(DOTween.To(GetCharacterSpacing, SetCharacterSpacing, baseCharacterSpacing, startRecoverDuration).SetEase(Ease.OutQuad))
                .AppendInterval(startHoldDuration)
                .Append(DOTween.To(GetAlpha, SetAlpha, 0f, startFadeOutDuration).SetEase(Ease.InCubic));

            await AwaitTweenAsync(sequence, ct);
        }

        async UniTask AwaitTweenAsync(Tween tween, CancellationToken ct)
        {
            activeTween = tween;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            try
            {
                await tween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                KillActiveTween();
                throw;
            }
            finally
            {
                if (activeTween == tween)
                {
                    activeTween = null;
                }
            }
        }

        void ResolveReferences()
        {
            if (messageText == null)
            {
                messageText = GetComponentInChildren<TMP_Text>(true);
            }

            if (textTransform == null && messageText != null)
            {
                textTransform = messageText.rectTransform;
            }

            if (textTransform == null)
            {
                textTransform = transform as RectTransform;
            }
        }

        void CacheBaseTransform()
        {
            if (textTransform == null)
            {
                return;
            }

            baseLocalPosition = textTransform.localPosition;
            baseLocalScale = textTransform.localScale;
            baseLocalRotation = textTransform.localRotation;
            if (messageText != null)
            {
                baseCharacterSpacing = messageText.characterSpacing;
            }
        }

        void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            Initialize();
        }

        bool CanPlayAnimation()
        {
            return messageText != null && textTransform != null;
        }

        void SetMessage(string message, Color color)
        {
            if (messageText == null)
            {
                return;
            }

            currentMessageColor = color;
            messageText.text = message;
            ApplyMessageColor();
        }

        float GetAlpha()
        {
            return currentAlpha;
        }

        float GetCharacterSpacing()
        {
            return messageText != null ? messageText.characterSpacing : 0f;
        }

        void SetAlpha(float value)
        {
            currentAlpha = Mathf.Clamp01(value);
            if (messageText == null)
            {
                return;
            }

            ApplyMessageColor();
        }

        void SetCharacterSpacing(float value)
        {
            if (messageText == null)
            {
                return;
            }

            messageText.characterSpacing = value;
        }

        void ApplyMessageColor()
        {
            if (messageText == null)
            {
                return;
            }

            var color = currentMessageColor;
            color.a = currentAlpha;
            messageText.color = color;
        }

        void ResetTransform()
        {
            if (textTransform == null)
            {
                return;
            }

            textTransform.localPosition = baseLocalPosition;
            textTransform.localScale = baseLocalScale;
            textTransform.localRotation = baseLocalRotation;
        }

        void SetHiddenInstant()
        {
            ResetTransform();
            SetAlpha(0f);
            SetCharacterSpacing(baseCharacterSpacing);
        }

        void KillActiveTween()
        {
            if (activeTween == null)
            {
                return;
            }

            if (activeTween.IsActive())
            {
                activeTween.Kill();
            }

            activeTween = null;
        }

        void OnDestroy()
        {
            KillActiveTween();
        }
    }
}
