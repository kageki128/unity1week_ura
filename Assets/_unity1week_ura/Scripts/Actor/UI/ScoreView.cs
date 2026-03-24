using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Actor
{
    public class ScoreView : AnimationViewBase
    {
        [SerializeField] StandardViewAnimator standardViewAnimator;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text scoreDeltaText;

        [Header("Score Change Colors")]
        [SerializeField] Color scoreGainColor = new Color32(0xEE, 0x5A, 0x7F, 0xFF);
        [SerializeField] Color scoreLossColor = new Color32(0x1D, 0xA1, 0xF2, 0xFF);

        [Header("Count Up")]
        [SerializeField, Min(0f)] float countDurationPerPoint = 0.015f;
        [SerializeField, Min(0.01f)] float minCountDuration = 0.12f;
        [SerializeField, Min(0.01f)] float maxCountDuration = 0.45f;

        [Header("Change Animation")]
        [SerializeField, Min(1f)] float scoreDigitPunchScale = 1.12f;
        [SerializeField, Min(0.01f)] float scoreDigitPunchDuration = 0.22f;
        [SerializeField, Min(0.01f)] float scoreColorFlashDuration = 0.26f;

        [Header("Delta Text Animation")]
        [SerializeField, Min(0f)] float scoreDeltaSlideDistance = 0.4f;
        [SerializeField, Min(0.01f)] float scoreDeltaEnterDuration = 0.14f;
        [SerializeField, Min(0f)] float scoreDeltaStayDuration = 0.2f;
        [SerializeField, Min(0.01f)] float scoreDeltaExitDuration = 0.2f;

        int targetScore;
        int displayedScore;
        Color baseScoreColor = Color.white;
        Color animatedScoreColor;
        float animatedEffectiveDigitScale = 1f;
        bool isScoreColorAnimating;
        bool isDigitScaleAnimating;
        Vector3 scoreDeltaBaseLocalPosition;
        Tween scoreTween;
        Sequence scoreColorSequence;
        Sequence scoreScaleSequence;
        Sequence scoreDeltaSequence;

        public override void Initialize()
        {
            if (scoreText != null)
            {
                baseScoreColor = scoreText.color;
                scoreText.richText = true;
            }

            ResolveScoreDeltaTextReference();
            InitializeScoreDeltaText();

            targetScore = 0;
            displayedScore = 0;
            animatedScoreColor = baseScoreColor;
            animatedEffectiveDigitScale = 1f;
            isScoreColorAnimating = false;
            isDigitScaleAnimating = false;
            RefreshScoreText();
            standardViewAnimator?.Initialize();
            gameObject.SetActive(false);
        }

        public override UniTask ShowAsync(CancellationToken ct)
        {
            RefreshScoreText();
            if (standardViewAnimator != null)
            {
                return standardViewAnimator.ShowAsync(ct);
            }

            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync(CancellationToken ct)
        {
            StopAnimations(resetVisualState: true);
            if (standardViewAnimator != null)
            {
                return standardViewAnimator.HideAsync(ct);
            }

            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        public void SetScore(int score)
        {
            var clampedScore = ScoreFormatter.Clamp(score);
            if (!gameObject.activeInHierarchy)
            {
                ApplyScoreWithoutAnimation(clampedScore);
                return;
            }

            var previousTargetScore = targetScore;
            var scoreDelta = clampedScore - previousTargetScore;
            targetScore = clampedScore;

            if (scoreDelta == 0)
            {
                RefreshScoreText();
                return;
            }

            PlayScoreChangeSE(scoreDelta);
            PlayCountTween(clampedScore);
            PlayScaleAnimation();
            PlayScoreColorAnimation(scoreDelta);
            PlayScoreDeltaAnimation(scoreDelta);
        }

        void PlayCountTween(int score)
        {
            scoreTween?.Kill();

            var delta = Mathf.Abs(score - displayedScore);
            var duration = Mathf.Clamp(delta * countDurationPerPoint, minCountDuration, maxCountDuration);

            scoreTween = DOTween.To(
                    () => displayedScore,
                    value =>
                    {
                        displayedScore = value;
                        RefreshScoreText();
                    },
                    score,
                    duration)
                .SetEase(Ease.OutCubic)
                .OnKill(() => scoreTween = null);
        }

        void PlayScaleAnimation()
        {
            scoreScaleSequence?.Kill();
            isDigitScaleAnimating = true;
            animatedEffectiveDigitScale = 1f;
            RefreshScoreText();

            scoreScaleSequence = DOTween.Sequence()
                .Append(
                    DOTween.To(
                            () => animatedEffectiveDigitScale,
                            value =>
                            {
                                animatedEffectiveDigitScale = value;
                                RefreshScoreText();
                            },
                            scoreDigitPunchScale,
                            scoreDigitPunchDuration * 0.45f)
                        .SetEase(Ease.OutQuad))
                .Append(
                    DOTween.To(
                            () => animatedEffectiveDigitScale,
                            value =>
                            {
                                animatedEffectiveDigitScale = value;
                                RefreshScoreText();
                            },
                            1f,
                            scoreDigitPunchDuration * 0.55f)
                        .SetEase(Ease.OutBack))
                .OnComplete(() =>
                {
                    isDigitScaleAnimating = false;
                    animatedEffectiveDigitScale = 1f;
                    RefreshScoreText();
                })
                .OnKill(() => scoreScaleSequence = null);
        }

        void PlayScoreColorAnimation(int scoreDelta)
        {
            scoreColorSequence?.Kill();
            isScoreColorAnimating = true;
            animatedScoreColor = baseScoreColor;
            var changedColor = GetChangedScoreColor(scoreDelta);
            RefreshScoreText();

            scoreColorSequence = DOTween.Sequence()
                .Append(
                    DOTween.To(
                            () => animatedScoreColor,
                            value =>
                            {
                                animatedScoreColor = value;
                                RefreshScoreText();
                            },
                            changedColor,
                            scoreColorFlashDuration * 0.45f)
                        .SetEase(Ease.OutQuad))
                .Append(
                    DOTween.To(
                            () => animatedScoreColor,
                            value =>
                            {
                                animatedScoreColor = value;
                                RefreshScoreText();
                            },
                            baseScoreColor,
                            scoreColorFlashDuration * 0.55f)
                        .SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    isScoreColorAnimating = false;
                    animatedScoreColor = baseScoreColor;
                    RefreshScoreText();
                })
                .OnKill(() => scoreColorSequence = null);
        }

        void PlayScoreDeltaAnimation(int scoreDelta)
        {
            if (scoreDeltaText == null)
            {
                return;
            }

            scoreDeltaSequence?.Kill();

            var changedColor = GetChangedScoreColor(scoreDelta);
            var visibleColor = changedColor;
            visibleColor.a = 1f;

            var hiddenColor = changedColor;
            hiddenColor.a = 0f;

            scoreDeltaText.text = BuildScoreDeltaText(scoreDelta);
            scoreDeltaText.color = hiddenColor;
            scoreDeltaText.rectTransform.localPosition = scoreDeltaBaseLocalPosition + Vector3.right * scoreDeltaSlideDistance;

            scoreDeltaSequence = DOTween.Sequence()
                .Append(scoreDeltaText.rectTransform.DOLocalMove(scoreDeltaBaseLocalPosition, scoreDeltaEnterDuration).SetEase(Ease.OutQuad))
                .Join(scoreDeltaText.DOColor(visibleColor, scoreDeltaEnterDuration).SetEase(Ease.OutQuad))
                .AppendInterval(scoreDeltaStayDuration)
                .Append(scoreDeltaText.DOColor(hiddenColor, scoreDeltaExitDuration).SetEase(Ease.InQuad))
                .OnComplete(ResetScoreDeltaText)
                .OnKill(() => scoreDeltaSequence = null);
        }

        void RefreshScoreText()
        {
            if (scoreText == null)
            {
                return;
            }

            scoreText.color = baseScoreColor;
            scoreText.text = BuildAnimatedScoreText(
                displayedScore,
                isScoreColorAnimating,
                animatedScoreColor,
                isDigitScaleAnimating,
                animatedEffectiveDigitScale);
        }

        void ResolveScoreDeltaTextReference()
        {
            if (scoreDeltaText != null)
            {
                return;
            }

            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                var candidate = texts[i];
                if (candidate == null || candidate == scoreText)
                {
                    continue;
                }

                var candidateName = candidate.name;
                if (candidateName.IndexOf("Sub", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    candidateName.IndexOf("Delta", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    candidateName.IndexOf("Diff", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    candidateName.IndexOf("Change", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    candidateName.IndexOf("差分", System.StringComparison.Ordinal) >= 0)
                {
                    scoreDeltaText = candidate;
                    return;
                }
            }
        }

        void InitializeScoreDeltaText()
        {
            if (scoreDeltaText == null)
            {
                return;
            }

            scoreDeltaBaseLocalPosition = scoreDeltaText.rectTransform.localPosition;
            scoreDeltaText.richText = false;
            ResetScoreDeltaText();
        }

        void ResetScoreDeltaText()
        {
            if (scoreDeltaText == null)
            {
                return;
            }

            scoreDeltaText.text = string.Empty;
            scoreDeltaText.rectTransform.localPosition = scoreDeltaBaseLocalPosition;
            var hiddenColor = scoreDeltaText.color;
            hiddenColor.a = 0f;
            scoreDeltaText.color = hiddenColor;
        }

        static string BuildAnimatedScoreText(int score, bool applyColor, Color validDigitColor, bool applyScale, float effectiveDigitScale)
        {
            var formattedScore = ScoreFormatter.Format(score);
            var validDigitStartIndex = GetValidDigitStartIndex(formattedScore);
            var leadingPadding = formattedScore.Substring(0, validDigitStartIndex);
            var effectiveDigits = formattedScore.Substring(validDigitStartIndex);

            if (applyScale)
            {
                var effectiveScalePercent = Mathf.Max(1, Mathf.RoundToInt(effectiveDigitScale * 100f));
                effectiveDigits = $"<size={effectiveScalePercent}%>{effectiveDigits}</size>";
            }

            if (applyColor)
            {
                var validDigitColorHex = ColorUtility.ToHtmlStringRGBA(validDigitColor);
                effectiveDigits = $"<color=#{validDigitColorHex}>{effectiveDigits}</color>";
            }

            return $"{leadingPadding}{effectiveDigits}";
        }

        static int GetValidDigitStartIndex(string formattedScore)
        {
            for (var i = 0; i < formattedScore.Length; i++)
            {
                if (formattedScore[i] != '0')
                {
                    return i;
                }
            }

            return Mathf.Max(formattedScore.Length - 1, 0);
        }

        static string BuildScoreDeltaText(int scoreDelta)
        {
            var sign = scoreDelta >= 0 ? "+" : "-";
            var amount = Mathf.Abs(scoreDelta);
            return $"{sign}{amount}";
        }

        Color GetChangedScoreColor(int scoreDelta)
        {
            return scoreDelta >= 0 ? scoreGainColor : scoreLossColor;
        }

        void PlayScoreChangeSE(int scoreDelta)
        {
            if (scoreDelta > 0)
            {
                AudioPlayer.Current?.PlaySE(SEType.Success);
                return;
            }

            if (scoreDelta < 0)
            {
                AudioPlayer.Current?.PlaySE(SEType.Ads);
            }
        }

        void ApplyScoreWithoutAnimation(int score)
        {
            StopAnimations(resetVisualState: false);
            targetScore = score;
            displayedScore = score;
            isScoreColorAnimating = false;
            isDigitScaleAnimating = false;
            animatedScoreColor = baseScoreColor;
            animatedEffectiveDigitScale = 1f;
            RefreshScoreText();
            ResetScoreDeltaText();
        }

        void StopAnimations(bool resetVisualState)
        {
            scoreTween?.Kill();
            scoreTween = null;

            scoreColorSequence?.Kill();
            scoreColorSequence = null;

            scoreScaleSequence?.Kill();
            scoreScaleSequence = null;

            scoreDeltaSequence?.Kill();
            scoreDeltaSequence = null;

            if (!resetVisualState || scoreText == null)
            {
                return;
            }

            displayedScore = targetScore;
            isScoreColorAnimating = false;
            isDigitScaleAnimating = false;
            animatedScoreColor = baseScoreColor;
            animatedEffectiveDigitScale = 1f;
            RefreshScoreText();
            ResetScoreDeltaText();
        }

        void OnDestroy()
        {
            StopAnimations(resetVisualState: false);
        }
    }
}
