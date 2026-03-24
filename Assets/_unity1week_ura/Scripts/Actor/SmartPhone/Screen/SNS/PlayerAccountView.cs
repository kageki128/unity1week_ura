using System;
using DG.Tweening;
using R3;
using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity1Week_Ura.Actor
{
    public class PlayerAccountView : MonoBehaviour
    {
        public float Width => viewArranger.Width;
        public float Height => viewArranger.Height;
        public Observable<Account> OnClicked => pointerEventObserver.OnClicked.Select(_ => account);
        public Account Account => account;
        public float SelectedIconScale => selectedIconScale;

        [FormerlySerializedAs("sizeCalculator")]
        [SerializeField] ViewArranger viewArranger;
        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] SpriteRenderer iconImage;
        [SerializeField] TMP_Text nameText;

        [SerializeField, Range(0f, 1f)] float unselectedBrightness = 0.45f;
        [SerializeField] float selectedIconScale = 1.2f;
        [SerializeField] float scaleAnimationDuration = 0.16f;
        [SerializeField] Ease scaleAnimationEase = Ease.OutCubic;
        [Header("Hover")]
        [SerializeField, Min(1f)] float hoverScaleMultiplier = 1.08f;
        [SerializeField, Min(0.01f)] float hoverScaleDuration = 0.08f;
        [Header("Hover Icon Color")]
        [SerializeField, Min(0f)] float spriteHoverBrightenAmount = 0.08f;
        [SerializeField, Min(0.01f)] float colorDuration = 0.08f;

        readonly CompositeDisposable disposables = new();
        Color normalColor;
        Tween scaleTween;
        Tween colorTween;
        bool isSelected;
        bool isHovered;
        float baseWidth;

        const float UnselectedIconScale = 1f;

        Account account;

        void Awake()
        {
            pointerEventObserver.OnPointerEntered.Subscribe(_ => OnPointerEnter()).AddTo(disposables);
            pointerEventObserver.OnPointerExited.Subscribe(_ => OnPointerExit()).AddTo(disposables);
            pointerEventObserver.OnClicked.Subscribe(_ => PlaySE(SEType.ButtonClick)).AddTo(disposables);
        }

        public void Initialize(Account account)
        {
            this.account = account;

            normalColor = iconImage.color;

            if (account.Icon != null)
            {
                iconImage.sprite = account.Icon;
            }
            
            nameText.text = account.PlayerAccountLabel;
            transform.localScale = Vector3.one * UnselectedIconScale;
            baseWidth = viewArranger.Width;
            SetSelected(false);
        }

        public void SetPosition(float x, float y)
        {
            viewArranger.SetPosition(x, y);
        }

        public void SetSelected(bool isSelected, Action onScaleAnimationCompleted = null)
        {
            this.isSelected = isSelected;
            if (isSelected)
            {
                isHovered = false;
            }

            colorTween?.Kill();
            iconImage.color = GetTargetColor();

            float targetScale = GetTargetScale();
            scaleTween?.Kill();

            if (Mathf.Approximately(transform.localScale.x, targetScale))
            {
                onScaleAnimationCompleted?.Invoke();
                return;
            }

            scaleTween = transform
                .DOScale(targetScale, scaleAnimationDuration)
                .SetEase(scaleAnimationEase)
                .OnComplete(() =>
                {
                    scaleTween = null;
                    onScaleAnimationCompleted?.Invoke();
                });
        }

        void OnPointerEnter()
        {
            if (isSelected)
            {
                return;
            }

            isHovered = true;
            PlaySE(SEType.ButtonHover);
            PlayHoverScaleAnimation();
        }

        void OnPointerExit()
        {
            isHovered = false;
            PlayHoverScaleAnimation();
        }

        void PlayHoverScaleAnimation()
        {
            scaleTween?.Kill();
            scaleTween = transform
                .DOScale(GetTargetScale(), GetEffectiveHoverScaleDuration())
                .SetEase(Ease.OutQuad)
                .OnComplete(() => scaleTween = null);

            colorTween?.Kill();
            colorTween = iconImage
                .DOColor(GetTargetColor(), colorDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => colorTween = null);
        }

        float GetTargetScale()
        {
            float baseScale = isSelected ? selectedIconScale : UnselectedIconScale;
            return !isSelected && isHovered ? baseScale * GetEffectiveHoverScaleMultiplier() : baseScale;
        }

        Color GetTargetColor()
        {
            if (isSelected)
            {
                return normalColor;
            }

            var baseUnselectedColor = normalColor * unselectedBrightness;
            baseUnselectedColor.a = normalColor.a;
            return isHovered ? GetBrightenedColor(baseUnselectedColor, spriteHoverBrightenAmount) : baseUnselectedColor;
        }

        float GetEffectiveHoverScaleMultiplier()
        {
            return Mathf.Max(1f, hoverScaleMultiplier);
        }

        float GetEffectiveHoverScaleDuration()
        {
            return Mathf.Max(0.01f, hoverScaleDuration);
        }

        Color GetBrightenedColor(Color baseColor, float brightenAmount)
        {
            var amount = Mathf.Clamp01(brightenAmount);
            return Color.Lerp(baseColor, Color.white, amount);
        }

        public float GetPredictedWidth(bool isSelected)
        {
            float targetScale = isSelected ? selectedIconScale : UnselectedIconScale;
            return baseWidth * targetScale;
        }

        void OnDestroy()
        {
            scaleTween?.Kill();
            colorTween?.Kill();
            disposables.Dispose();
        }

        void PlaySE(SEType seType)
        {
            AudioPlayer.Current?.PlaySE(seType);
        }
    }
}
