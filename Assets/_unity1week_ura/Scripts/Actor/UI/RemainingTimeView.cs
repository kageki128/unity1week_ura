using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class RemainingTimeView : AnimationViewBase
    {
        [SerializeField] StandardViewAnimator standardViewAnimator;
        [SerializeField] TMP_Text remainingTimeText;

        const string Format = @"mm\:ss";

        [Header("Warning")]
        [SerializeField, Min(0f)] float warningThresholdSeconds = 10f;
        [SerializeField] Color warningColor = new Color32(0xEE, 0x5A, 0x7F, 0xFF);
        [SerializeField, Min(1f)] float warningDigitScale = 1.12f;
        [SerializeField, Min(0.01f)] float warningAnimationDuration = 0.24f;

        Color baseTimeColor = Color.white;
        Color animatedWarningColor = Color.white;
        float animatedWarningScale = 1f;
        bool isWarningAnimating;
        string displayedTimeText = string.Empty;
        Vector3 baseTimeScale = Vector3.one;
        Sequence warningSequence;

        public override void Initialize()
        {
            if (remainingTimeText != null)
            {
                baseTimeColor = remainingTimeText.color;
                remainingTimeText.richText = true;
                baseTimeScale = remainingTimeText.rectTransform.localScale;
            }

            displayedTimeText = TimeSpan.Zero.ToString(Format);
            RefreshTimeText();
            standardViewAnimator?.Initialize();
            gameObject.SetActive(false);
        }

        public override UniTask ShowAsync(CancellationToken ct)
        {
            RefreshTimeText();
            if (standardViewAnimator != null)
            {
                return standardViewAnimator.ShowAsync(ct);
            }

            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync(CancellationToken ct)
        {
            StopWarningAnimation(resetVisualState: true);
            if (standardViewAnimator != null)
            {
                return standardViewAnimator.HideAsync(ct);
            }

            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        public void SetRemainingTime(float remainingTime)
        {
            var clampedRemainingTime = Mathf.Max(remainingTime, 0f);
            var displayedRemainingSeconds = Mathf.CeilToInt(clampedRemainingTime);
            var timeSpan = TimeSpan.FromSeconds(displayedRemainingSeconds);
            var nextDisplayedText = timeSpan.ToString(Format);
            var warningThresholdDisplaySeconds = Mathf.FloorToInt(warningThresholdSeconds);
            var isTextChanged = !string.Equals(displayedTimeText, nextDisplayedText, StringComparison.Ordinal);

            displayedTimeText = nextDisplayedText;
            if (!isTextChanged)
            {
                return;
            }

            if (displayedRemainingSeconds <= warningThresholdDisplaySeconds)
            {
                PlayWarningAnimation();
                PlayTimerSE(displayedRemainingSeconds);
                return;
            }

            StopWarningAnimation(resetVisualState: true);
            RefreshTimeText();
        }

        void PlayWarningAnimation()
        {
            warningSequence?.Kill();

            isWarningAnimating = true;
            animatedWarningColor = baseTimeColor;
            animatedWarningScale = 1f;
            RefreshTimeText();

            warningSequence = DOTween.Sequence()
                .Append(
                    DOTween.To(
                            () => animatedWarningColor,
                            value =>
                            {
                                animatedWarningColor = value;
                                RefreshTimeText();
                            },
                            warningColor,
                            warningAnimationDuration * 0.45f)
                        .SetEase(Ease.OutQuad))
                .Join(
                    DOTween.To(
                            () => animatedWarningScale,
                            value =>
                            {
                                animatedWarningScale = value;
                                RefreshTimeText();
                            },
                            warningDigitScale,
                            warningAnimationDuration * 0.45f)
                        .SetEase(Ease.OutQuad))
                .Append(
                    DOTween.To(
                            () => animatedWarningColor,
                            value =>
                            {
                                animatedWarningColor = value;
                                RefreshTimeText();
                            },
                            baseTimeColor,
                            warningAnimationDuration * 0.55f)
                        .SetEase(Ease.InQuad))
                .Join(
                    DOTween.To(
                            () => animatedWarningScale,
                            value =>
                            {
                                animatedWarningScale = value;
                                RefreshTimeText();
                            },
                            1f,
                            warningAnimationDuration * 0.55f)
                        .SetEase(Ease.OutBack))
                .OnComplete(() =>
                {
                    isWarningAnimating = false;
                    animatedWarningColor = baseTimeColor;
                    animatedWarningScale = 1f;
                    RefreshTimeText();
                })
                .OnKill(() => warningSequence = null);
        }

        void PlayTimerSE(int displayedRemainingSeconds)
        {
            if (displayedRemainingSeconds <= 0)
            {
                return;
            }

            AudioPlayer.Current?.PlaySE(SEType.Timer);
        }

        void RefreshTimeText()
        {
            if (remainingTimeText == null)
            {
                return;
            }

            remainingTimeText.color = isWarningAnimating ? animatedWarningColor : baseTimeColor;
            remainingTimeText.text = BuildAnimatedTimeText(displayedTimeText);
            remainingTimeText.rectTransform.localScale = isWarningAnimating
                ? baseTimeScale * animatedWarningScale
                : baseTimeScale;
        }

        static string BuildAnimatedTimeText(string formattedTime)
        {
            return formattedTime;
        }

        void StopWarningAnimation(bool resetVisualState)
        {
            warningSequence?.Kill();
            warningSequence = null;

            if (!resetVisualState)
            {
                return;
            }

            isWarningAnimating = false;
            animatedWarningColor = baseTimeColor;
            animatedWarningScale = 1f;
            RefreshTimeText();
        }

        void OnDestroy()
        {
            StopWarningAnimation(resetVisualState: false);
        }
    }
}
