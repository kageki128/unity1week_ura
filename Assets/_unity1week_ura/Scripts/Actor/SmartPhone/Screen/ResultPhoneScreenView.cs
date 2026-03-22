using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ResultPhoneScreenView : PhoneScreenViewBase
    {
        public Observable<Unit> OnRetryButtonClicked => RetryButtonView.OnClicked;
        public Observable<Unit> OnBackToSelectButtonClicked => BackToSelectButtonView.OnClicked;
        public Observable<Unit> OnShareButtonClicked => ShareButtonView.OnClicked;

        [Header("Buttons")]
        [SerializeField] ButtonView RetryButtonView;
        [SerializeField] ButtonView BackToSelectButtonView;
        [SerializeField] ButtonView ShareButtonView;

        [Header("Share Compose")]
        [SerializeField] TMP_Text shareContentText;
        [SerializeField] RectTransform shareContentRect;
        [SerializeField] Transform shareFieldFrame;
        [SerializeField] Transform shareFieldTopLine;
        [SerializeField] Transform shareFieldBottomLine;
        [SerializeField] Transform shareFieldIconsRoot;
        [SerializeField] Transform shareButtonRoot;

        [Header("Placeholder")]
        [SerializeField] string placeholderText = "\u3044\u307e\u3069\u3046\u3057\u3066\u308b\uFF1F";
        [SerializeField] Color placeholderTextColor = new(0.3254902f, 0.39215687f, 0.44313726f, 1f);
        [SerializeField] Color typingTextColor = new(0.06666667f, 0.078431375f, 0.09803922f, 1f);

        [Header("Typing")]
        [SerializeField, Min(0f)] float minTypingInterval = 0.03f;
        [SerializeField, Min(0f)] float maxTypingInterval = 0.1f;
        [SerializeField, Min(0f)] float punctuationPause = 0.08f;
        [SerializeField, Min(0f)] float lineBreakPause = 0.14f;
        [SerializeField, Range(0f, 1f)] float flickBurstChance = 0.2f;

        [Header("Share Button Fade")]
        [SerializeField, Min(0f)] float shareButtonFadeDuration = 0.25f;
        [SerializeField] Ease shareButtonFadeEase = Ease.OutCubic;

        SpriteRenderer[] shareButtonSpriteRenderers = Array.Empty<SpriteRenderer>();
        TMP_Text[] shareButtonTexts = Array.Empty<TMP_Text>();
        ButtonAnimator[] shareButtonAnimators = Array.Empty<ButtonAnimator>();
        Tween shareButtonFadeTween;

        bool hasCachedBaseLayout;
        float baseContentRenderedHeight;
        float baseFrameScaleY;
        float baseFrameLocalY;
        float baseTopLineLocalY;
        float baseBottomLineLocalY;
        float baseContentLocalY;
        float baseContentHeight;
        float baseIconsLocalY;
        float baseShareButtonLocalY;
        float topAnchorLocalY;
        float bottomToButtonOffset;
        float bottomToIconsOffset;

        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);

            ResolveReferencesIfNeeded();
            CacheBaseLayoutIfNeeded();
            ResetShareComposePresentation();

            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            ResetShareComposePresentation();
            gameObject.SetActive(true);
            await screenTransitionViewHub.HideAsync(ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            StopShareComposeAnimations();
            await screenTransitionViewHub.ShowAsync(ScreenTransitionType.CircleWipe, ct);
            ResetShareComposePresentation();
            gameObject.SetActive(false);
        }

        public async UniTask PlayShareComposeAsync(string shareText, CancellationToken ct)
        {
            ResolveReferencesIfNeeded();
            CacheBaseLayoutIfNeeded();
            ResetShareComposePresentation();

            if (shareContentText == null)
            {
                return;
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            var linkedToken = linkedCts.Token;

            await PlayTypingAsync(shareText ?? string.Empty, linkedToken);
            await FadeInShareButtonAsync(linkedToken);
        }

        void ResolveReferencesIfNeeded()
        {
            if (shareButtonRoot == null && ShareButtonView != null)
            {
                shareButtonRoot = ShareButtonView.transform;
            }

            if (shareButtonRoot == null)
            {
                return;
            }

            var fieldRoot = shareButtonRoot.parent;
            if (fieldRoot == null)
            {
                return;
            }

            if (shareFieldFrame == null)
            {
                shareFieldFrame = fieldRoot.Find("Frame");
            }

            if (shareFieldTopLine == null)
            {
                shareFieldTopLine = fieldRoot.Find("TopLine");
            }

            if (shareFieldBottomLine == null)
            {
                shareFieldBottomLine = fieldRoot.Find("BottomLine");
            }

            if (shareFieldIconsRoot == null)
            {
                shareFieldIconsRoot = fieldRoot.Find("Icons");
            }

            if (shareContentText == null || shareContentRect == null)
            {
                var content = fieldRoot.Find("Content");
                if (content != null)
                {
                    if (shareContentText == null)
                    {
                        shareContentText = content.GetComponent<TMP_Text>();
                    }

                    if (shareContentRect == null)
                    {
                        shareContentRect = content.GetComponent<RectTransform>();
                    }
                }
            }

            if (shareContentRect == null && shareContentText != null)
            {
                shareContentRect = shareContentText.rectTransform;
            }

            shareButtonSpriteRenderers = shareButtonRoot.GetComponentsInChildren<SpriteRenderer>(true);
            shareButtonTexts = shareButtonRoot.GetComponentsInChildren<TMP_Text>(true);
            shareButtonAnimators = shareButtonRoot.GetComponentsInChildren<ButtonAnimator>(true);
        }

        void CacheBaseLayoutIfNeeded()
        {
            if (hasCachedBaseLayout)
            {
                return;
            }

            baseContentRenderedHeight = 0f;
            if (shareContentText != null)
            {
                shareContentText.ForceMeshUpdate();
                baseContentRenderedHeight = Mathf.Max(shareContentText.renderedHeight, 0f);
            }

            if (shareContentRect != null)
            {
                baseContentLocalY = shareContentRect.anchoredPosition.y;
                baseContentHeight = shareContentRect.sizeDelta.y;
                if (baseContentRenderedHeight <= Mathf.Epsilon)
                {
                    baseContentRenderedHeight = baseContentHeight;
                }
            }

            if (shareFieldFrame != null)
            {
                baseFrameScaleY = shareFieldFrame.localScale.y;
                baseFrameLocalY = shareFieldFrame.localPosition.y;
            }

            if (shareFieldTopLine != null)
            {
                baseTopLineLocalY = shareFieldTopLine.localPosition.y;
                topAnchorLocalY = baseTopLineLocalY;
            }
            else
            {
                topAnchorLocalY = baseFrameLocalY + (baseFrameScaleY * 0.5f);
            }

            if (shareFieldBottomLine != null)
            {
                baseBottomLineLocalY = shareFieldBottomLine.localPosition.y;
            }

            if (shareButtonRoot != null)
            {
                baseShareButtonLocalY = shareButtonRoot.localPosition.y;
            }

            if (shareFieldIconsRoot != null)
            {
                baseIconsLocalY = shareFieldIconsRoot.localPosition.y;
            }

            bottomToButtonOffset = baseShareButtonLocalY - baseBottomLineLocalY;
            bottomToIconsOffset = baseIconsLocalY - baseBottomLineLocalY;
            hasCachedBaseLayout = true;
        }

        void ResetShareComposePresentation()
        {
            StopShareComposeAnimations();

            if (shareContentText != null)
            {
                shareContentText.text = placeholderText;
                shareContentText.color = placeholderTextColor;
            }

            ApplyComposeLayout(CalculateExtraHeight());
            SetShareButtonAlpha(0f);
            ShareButtonView?.SetInteractable(false);
        }

        async UniTask PlayTypingAsync(string fullText, CancellationToken ct)
        {
            if (shareContentText == null)
            {
                return;
            }

            var builder = new StringBuilder(Mathf.Max(fullText.Length, 16));
            var textLength = fullText.Length;
            var index = 0;
            if (textLength > 0)
            {
                shareContentText.color = typingTextColor;
            }

            while (index < textLength)
            {
                ct.ThrowIfCancellationRequested();

                var burstCount = DetermineBurstCount(fullText, index);
                for (var i = 0; i < burstCount && index < textLength; i++, index++)
                {
                    builder.Append(fullText[index]);
                }

                shareContentText.text = builder.ToString();
                ApplyComposeLayout(CalculateExtraHeight());

                if (index >= textLength)
                {
                    break;
                }

                var delaySeconds = CalculateTypingDelay(fullText[index - 1]);
                if (delaySeconds <= 0f)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    continue;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), DelayType.DeltaTime, PlayerLoopTiming.Update, ct);
            }
        }

        int DetermineBurstCount(string fullText, int currentIndex)
        {
            if (currentIndex >= fullText.Length - 1)
            {
                return 1;
            }

            var current = fullText[currentIndex];
            var next = fullText[currentIndex + 1];
            if (current == '\n' || next == '\n' || char.IsWhiteSpace(current) || char.IsWhiteSpace(next))
            {
                return 1;
            }

            return UnityEngine.Random.value <= flickBurstChance ? 2 : 1;
        }

        float CalculateTypingDelay(char lastTypedCharacter)
        {
            var minDelay = Mathf.Min(minTypingInterval, maxTypingInterval);
            var maxDelay = Mathf.Max(minTypingInterval, maxTypingInterval);
            var delay = UnityEngine.Random.Range(minDelay, maxDelay);

            if (lastTypedCharacter == '\n')
            {
                delay += lineBreakPause;
            }
            else if (lastTypedCharacter == '\u3002' || lastTypedCharacter == '\uFF01' || lastTypedCharacter == '\uFF1F' || lastTypedCharacter == '!' || lastTypedCharacter == '?')
            {
                delay += punctuationPause;
            }
            else if (char.IsWhiteSpace(lastTypedCharacter))
            {
                delay *= 0.5f;
            }

            return delay;
        }

        float CalculateExtraHeight()
        {
            if (shareContentText == null)
            {
                return 0f;
            }

            shareContentText.ForceMeshUpdate();
            var renderedHeight = shareContentText.renderedHeight;
            return Mathf.Max(0f, renderedHeight - baseContentRenderedHeight);
        }

        void ApplyComposeLayout(float extraHeight)
        {
            if (!hasCachedBaseLayout)
            {
                return;
            }

            if (shareFieldFrame != null)
            {
                var frameScale = shareFieldFrame.localScale;
                frameScale.y = baseFrameScaleY + extraHeight;
                shareFieldFrame.localScale = frameScale;

                var framePosition = shareFieldFrame.localPosition;
                framePosition.y = topAnchorLocalY - (frameScale.y * 0.5f);
                shareFieldFrame.localPosition = framePosition;
            }

            if (shareFieldTopLine != null)
            {
                var topPosition = shareFieldTopLine.localPosition;
                topPosition.y = baseTopLineLocalY;
                shareFieldTopLine.localPosition = topPosition;
            }

            var bottomY = baseBottomLineLocalY - extraHeight;
            if (shareFieldBottomLine != null)
            {
                var bottomPosition = shareFieldBottomLine.localPosition;
                bottomPosition.y = bottomY;
                shareFieldBottomLine.localPosition = bottomPosition;
            }

            if (shareContentRect != null)
            {
                var sizeDelta = shareContentRect.sizeDelta;
                sizeDelta.y = baseContentHeight + extraHeight;
                shareContentRect.sizeDelta = sizeDelta;

                var anchoredPosition = shareContentRect.anchoredPosition;
                anchoredPosition.y = baseContentLocalY - (extraHeight * 0.5f);
                shareContentRect.anchoredPosition = anchoredPosition;
            }

            if (shareButtonRoot != null)
            {
                var buttonPosition = shareButtonRoot.localPosition;
                var nextButtonY = bottomY + bottomToButtonOffset;
                if (!Mathf.Approximately(buttonPosition.y, nextButtonY))
                {
                    buttonPosition.y = nextButtonY;
                    shareButtonRoot.localPosition = buttonPosition;
                    RefreshShareButtonAnimatorBaseTransforms();
                }
            }

            if (shareFieldIconsRoot != null)
            {
                var iconsPosition = shareFieldIconsRoot.localPosition;
                iconsPosition.y = bottomY + bottomToIconsOffset;
                shareFieldIconsRoot.localPosition = iconsPosition;
            }
        }

        async UniTask FadeInShareButtonAsync(CancellationToken ct)
        {
            if (ShareButtonView == null)
            {
                return;
            }

            if (shareButtonFadeDuration <= 0f)
            {
                SetShareButtonAlpha(1f);
                ShareButtonView.SetInteractable(true);
                return;
            }

            var currentAlpha = 0f;
            shareButtonFadeTween = DOTween.To(
                    () => currentAlpha,
                    value =>
                    {
                        currentAlpha = value;
                        SetShareButtonAlpha(value);
                    },
                    1f,
                    shareButtonFadeDuration)
                .SetEase(shareButtonFadeEase);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            try
            {
                await shareButtonFadeTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                if (shareButtonFadeTween != null && shareButtonFadeTween.IsActive())
                {
                    shareButtonFadeTween.Kill();
                }

                throw;
            }
            finally
            {
                shareButtonFadeTween = null;
            }

            SetShareButtonAlpha(1f);
            ShareButtonView.SetInteractable(true);
        }

        void SetShareButtonAlpha(float alpha)
        {
            var clampedAlpha = Mathf.Clamp01(alpha);

            for (var i = 0; i < shareButtonSpriteRenderers.Length; i++)
            {
                var renderer = shareButtonSpriteRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var color = renderer.color;
                color.a = clampedAlpha;
                renderer.color = color;
            }

            for (var i = 0; i < shareButtonTexts.Length; i++)
            {
                var text = shareButtonTexts[i];
                if (text == null)
                {
                    continue;
                }

                var color = text.color;
                color.a = clampedAlpha;
                text.color = color;
            }
        }

        void RefreshShareButtonAnimatorBaseTransforms()
        {
            for (var i = 0; i < shareButtonAnimators.Length; i++)
            {
                var animator = shareButtonAnimators[i];
                if (animator == null)
                {
                    continue;
                }

                animator.RefreshBaseTransformFromCurrent();
            }
        }

        void StopShareComposeAnimations()
        {
            if (shareButtonFadeTween == null)
            {
                return;
            }

            if (shareButtonFadeTween.IsActive())
            {
                shareButtonFadeTween.Kill();
            }

            shareButtonFadeTween = null;
        }

        void OnDestroy()
        {
            StopShareComposeAnimations();
        }
    }
}
