using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ButtonAnimator : MonoBehaviour
    {
        enum HoverColorMode
        {
            CustomColor,
            Darken,
            Brighten,
        }

        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] SpriteRenderer[] spriteRenderers;
        [SerializeField] TMP_Text[] texts;

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
        [Header("Hover Sprite Color")]
        [SerializeField] HoverColorMode spriteHoverColorMode = HoverColorMode.Darken;
        [SerializeField] Color spriteHoverColor = new(0.92f, 0.92f, 0.92f, 1f);
        [SerializeField, Min(0f)] float spriteHoverDarkenAmount = 0.08f;
        [SerializeField, Min(0f)] float spriteHoverBrightenAmount = 0.08f;
        [Header("Hover Text Color")]
        [SerializeField] HoverColorMode textHoverColorMode = HoverColorMode.Darken;
        [SerializeField] Color textHoverColor = new(0.92f, 0.92f, 0.92f, 1f);
        [SerializeField, Min(0f)] float textHoverDarkenAmount = 0.08f;
        [SerializeField, Min(0f)] float textHoverBrightenAmount = 0.08f;
        [SerializeField, Min(0.01f)] float colorDuration = 0.08f;

        readonly CompositeDisposable disposables = new();

        Vector3 baseLocalScale;
        Vector3 baseLocalPosition;
        Color[] baseSpriteColors;
        Color[] baseTextColors;
        bool isHovered;
        bool isPressed;
        Tween idleTween;
        Tween scaleTween;
        Tween moveTween;
        Tween[] spriteColorTweens;
        Tween[] textColorTweens;

        void Awake()
        {
            baseLocalScale = transform.localScale;
            baseLocalPosition = transform.localPosition;
            CacheBaseColors();

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

            TweenToHoverColors();
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

            TweenToBaseColors();
        }

        void OnPointerDown()
        {
            isPressed = true;

            StopIdleAnimation();

            scaleTween?.Kill();
            scaleTween = transform.DOScale(GetPressedScale(), pressDuration).SetEase(Ease.OutQuad);

            moveTween?.Kill();
            moveTween = transform.DOLocalMove(GetPressedPosition(), pressDuration).SetEase(Ease.OutQuad);

            TweenToPressedColors();
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

            if (isHovered)
            {
                TweenToHoverColors();
            }
            else
            {
                TweenToBaseColors();
            }

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

        void CacheBaseColors()
        {
            baseSpriteColors = new Color[spriteRenderers.Length];
            spriteColorTweens = new Tween[spriteRenderers.Length];

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                var sprite = spriteRenderers[i];
                baseSpriteColors[i] = sprite == null ? Color.white : sprite.color;
            }

            baseTextColors = new Color[texts.Length];
            textColorTweens = new Tween[texts.Length];

            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                baseTextColors[i] = text == null ? Color.white : text.color;
            }
        }

        void TweenToHoverColors()
        {
            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                var sprite = spriteRenderers[i];
                if (sprite == null)
                {
                    continue;
                }

                var targetColor = GetHoverColor(baseSpriteColors[i], spriteHoverColorMode, spriteHoverColor, spriteHoverDarkenAmount, spriteHoverBrightenAmount);
                spriteColorTweens[i]?.Kill();
                spriteColorTweens[i] = sprite.DOColor(targetColor, colorDuration).SetEase(Ease.OutQuad);
            }

            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null)
                {
                    continue;
                }

                var targetColor = GetHoverColor(baseTextColors[i], textHoverColorMode, textHoverColor, textHoverDarkenAmount, textHoverBrightenAmount);
                textColorTweens[i]?.Kill();
                textColorTweens[i] = text.DOColor(targetColor, colorDuration).SetEase(Ease.OutQuad);
            }
        }

        void TweenToBaseColors()
        {
            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                var sprite = spriteRenderers[i];
                if (sprite == null)
                {
                    continue;
                }

                spriteColorTweens[i]?.Kill();
                spriteColorTweens[i] = sprite.DOColor(baseSpriteColors[i], colorDuration).SetEase(Ease.OutQuad);
            }

            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null)
                {
                    continue;
                }

                textColorTweens[i]?.Kill();
                textColorTweens[i] = text.DOColor(baseTextColors[i], colorDuration).SetEase(Ease.OutQuad);
            }
        }

        void TweenToPressedColors()
        {
            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                var sprite = spriteRenderers[i];
                if (sprite == null)
                {
                    continue;
                }

                var targetColor = GetDarkenedColor(baseSpriteColors[i], pressedDarkenAmount);
                spriteColorTweens[i]?.Kill();
                spriteColorTweens[i] = sprite.DOColor(targetColor, colorDuration).SetEase(Ease.OutQuad);
            }

            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null)
                {
                    continue;
                }

                var targetColor = GetDarkenedColor(baseTextColors[i], pressedDarkenAmount);
                textColorTweens[i]?.Kill();
                textColorTweens[i] = text.DOColor(targetColor, colorDuration).SetEase(Ease.OutQuad);
            }
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

        Color GetHoverColor(Color baseColor, HoverColorMode mode, Color customColor, float darkenAmount, float brightenAmount)
        {
            return mode switch
            {
                HoverColorMode.CustomColor => customColor,
                HoverColorMode.Darken => GetDarkenedColor(baseColor, darkenAmount),
                HoverColorMode.Brighten => GetBrightenedColor(baseColor, brightenAmount),
                _ => customColor,
            };
        }

        Color GetDarkenedColor(Color baseColor, float darkenAmount)
        {
            var brightness = Mathf.Clamp01(1f - darkenAmount);
            return new Color(baseColor.r * brightness, baseColor.g * brightness, baseColor.b * brightness, baseColor.a);
        }

        Color GetBrightenedColor(Color baseColor, float brightenAmount)
        {
            var amount = Mathf.Clamp01(brightenAmount);
            return Color.Lerp(baseColor, Color.white, amount);
        }

        void OnDestroy()
        {
            idleTween?.Kill();
            scaleTween?.Kill();
            moveTween?.Kill();

            for (var i = 0; i < spriteColorTweens.Length; i++)
            {
                spriteColorTweens[i]?.Kill();
            }

            for (var i = 0; i < textColorTweens.Length; i++)
            {
                textColorTweens[i]?.Kill();
            }

            disposables.Dispose();
        }
    }
}
