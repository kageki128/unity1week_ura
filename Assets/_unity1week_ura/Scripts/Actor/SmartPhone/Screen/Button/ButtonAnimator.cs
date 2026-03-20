using DG.Tweening;
using R3;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ButtonAnimator : MonoBehaviour
    {
        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] SpriteRenderer spriteRenderer;

        [Header("Idle")]
        [SerializeField] bool playIdleAnimation = true;
        [SerializeField, Min(0f)] float idleScaleAmplitude = 0.02f;
        [SerializeField, Min(0.01f)] float idleDuration = 0.9f;

        [Header("Press")]
        [SerializeField] Vector2 pressedScale = new(1.06f, 0.92f);
        [SerializeField] float pressedOffsetY = -0.03f;
        [SerializeField, Min(0.01f)] float pressDuration = 0.06f;
        [SerializeField, Min(0f)] float pressedDarkenAmount = 0.12f;

        [Header("Release")]
        [SerializeField] Vector2 releaseOvershootScale = new(0.96f, 1.04f);
        [SerializeField, Min(0.01f)] float releaseDuration = 0.12f;

        [Header("Hover")]
        [SerializeField, Min(1f)] float hoverScaleMultiplier = 1.04f;
        [SerializeField, Min(0.01f)] float hoverScaleDuration = 0.08f;
        [SerializeField, Min(0f)] float hoverDarkenAmount = 0.08f;
        [SerializeField, Min(0.01f)] float colorDuration = 0.08f;

        readonly CompositeDisposable disposables = new();

        Vector3 baseLocalScale;
        Vector3 baseLocalPosition;
        Color baseColor;
        bool isHovered;
        bool isPressed;
        Tween idleTween;
        Tween scaleTween;
        Tween moveTween;
        Tween colorTween;

        void Awake()
        {
            baseLocalScale = transform.localScale;
            baseLocalPosition = transform.localPosition;
            baseColor = spriteRenderer.color;

            pointerEventObserver.OnPointerEntered.Subscribe(_ => OnPointerEnter()).AddTo(disposables);
            pointerEventObserver.OnPointerExited.Subscribe(_ => OnPointerExit()).AddTo(disposables);
            pointerEventObserver.OnPointerDowned.Subscribe(_ => OnPointerDown()).AddTo(disposables);
            pointerEventObserver.OnPointerUpped.Subscribe(_ => OnPointerUp()).AddTo(disposables);

            PlayIdleAnimationIfNeeded();
        }

        void OnPointerEnter()
        {
            isHovered = true;

            StopIdleAnimation();

            if (isPressed)
            {
                return;
            }

            scaleTween?.Kill();
            scaleTween = transform.DOScale(GetHoverScale(), hoverScaleDuration).SetEase(Ease.OutQuad);

            TweenColor(GetHoverColor());
        }

        void OnPointerExit()
        {
            isHovered = false;

            if (isPressed)
            {
                return;
            }

            scaleTween?.Kill();
            scaleTween = transform.DOScale(baseLocalScale, hoverScaleDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(PlayIdleAnimationIfNeeded);

            TweenColor(baseColor);
        }

        void OnPointerDown()
        {
            isPressed = true;

            StopIdleAnimation();

            scaleTween?.Kill();
            scaleTween = transform.DOScale(GetPressedScale(), pressDuration).SetEase(Ease.OutQuad);

            moveTween?.Kill();
            moveTween = transform.DOLocalMove(GetPressedPosition(), pressDuration).SetEase(Ease.OutQuad);

            TweenColor(GetPressedColor());
        }

        void OnPointerUp()
        {
            isPressed = false;

            var targetScale = isHovered ? GetHoverScale() : baseLocalScale;

            scaleTween?.Kill();
            scaleTween = DOTween.Sequence()
                .Append(transform.DOScale(GetReleaseOvershootScale(targetScale), releaseDuration * 0.5f).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(targetScale, releaseDuration * 0.5f).SetEase(Ease.OutBack));

            moveTween?.Kill();
            moveTween = transform.DOLocalMove(baseLocalPosition, releaseDuration).SetEase(Ease.OutBack);

            var targetColor = isHovered ? GetHoverColor() : baseColor;
            TweenColor(targetColor);

            if (!isHovered)
            {
                PlayIdleAnimationIfNeeded();
            }
        }

        void PlayIdleAnimationIfNeeded()
        {
            if (!playIdleAnimation || isPressed || isHovered)
            {
                return;
            }

            idleTween?.Kill();
            var maxScale = baseLocalScale * (1f + idleScaleAmplitude);
            idleTween = transform.DOScale(maxScale, idleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        void StopIdleAnimation()
        {
            idleTween?.Kill();
            idleTween = null;
        }

        void TweenColor(Color targetColor)
        {
            colorTween?.Kill();
            colorTween = spriteRenderer.DOColor(targetColor, colorDuration).SetEase(Ease.OutQuad);
        }

        Vector3 GetPressedScale()
        {
            return new Vector3(baseLocalScale.x * pressedScale.x, baseLocalScale.y * pressedScale.y, baseLocalScale.z);
        }

        Vector3 GetHoverScale()
        {
            return baseLocalScale * hoverScaleMultiplier;
        }

        Vector3 GetReleaseOvershootScale(Vector3 targetScale)
        {
            return new Vector3(targetScale.x * releaseOvershootScale.x, targetScale.y * releaseOvershootScale.y, targetScale.z);
        }

        Vector3 GetPressedPosition()
        {
            return baseLocalPosition + Vector3.up * pressedOffsetY;
        }

        Color GetHoverColor()
        {
            return GetDarkenedColor(hoverDarkenAmount);
        }

        Color GetPressedColor()
        {
            return GetDarkenedColor(Mathf.Max(hoverDarkenAmount, pressedDarkenAmount));
        }

        Color GetDarkenedColor(float darkenAmount)
        {
            var brightness = Mathf.Clamp01(1f - darkenAmount);
            return new Color(baseColor.r * brightness, baseColor.g * brightness, baseColor.b * brightness, baseColor.a);
        }

        void OnDestroy()
        {
            idleTween?.Kill();
            scaleTween?.Kill();
            moveTween?.Kill();
            colorTween?.Kill();
            disposables.Dispose();
        }
    }
}
