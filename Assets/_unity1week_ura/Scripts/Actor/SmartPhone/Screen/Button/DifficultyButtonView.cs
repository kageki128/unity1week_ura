using R3;
using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class DifficultyButtonView : MonoBehaviour
    {
        public Observable<GameRuleSO> OnClicked => buttonCollider.OnClicked.Select(_ => gameRule);
        public GameRuleSO GameRule => gameRule;
        public bool IsSelected => isSelected;

        [SerializeField] PointerEventObserver buttonCollider;
        [SerializeField] GameRuleSO gameRule;
        [SerializeField] SpriteRenderer[] targetSpriteRenderers;
        [SerializeField] TMP_Text[] targetTexts;
        [SerializeField] Color selectedSpriteColor = new(0.93333334f, 0.3529412f, 0.49803922f, 1f);
        [SerializeField] Color selectedTextColor = new(0.93333334f, 0.3529412f, 0.49803922f, 1f);
        [SerializeField] ButtonAnimator buttonAnimator;

        Color[] defaultSpriteColors;
        Color[] defaultTextColors;
        bool isSelected;
        bool isInitialized;

        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            ResolveTargetsIfNeeded();
            CacheDefaultColors();
            ApplyVisualState(false);
            isInitialized = true;
        }

        public void SetSelected(bool selected)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            isSelected = selected;
            ApplyVisualState(selected);
        }

        void ResolveTargetsIfNeeded()
        {
            if (buttonAnimator == null)
            {
                TryGetComponent(out buttonAnimator);
            }

            if (targetSpriteRenderers == null || targetSpriteRenderers.Length == 0)
            {
                targetSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            if (targetTexts == null || targetTexts.Length == 0)
            {
                targetTexts = GetComponentsInChildren<TMP_Text>(true);
            }
        }

        void CacheDefaultColors()
        {
            defaultSpriteColors = new Color[targetSpriteRenderers.Length];
            for (int i = 0; i < targetSpriteRenderers.Length; i++)
            {
                var spriteRenderer = targetSpriteRenderers[i];
                defaultSpriteColors[i] = spriteRenderer == null ? Color.white : spriteRenderer.color;
            }

            defaultTextColors = new Color[targetTexts.Length];
            for (int i = 0; i < targetTexts.Length; i++)
            {
                var text = targetTexts[i];
                defaultTextColors[i] = text == null ? Color.white : text.color;
            }
        }

        void ApplyVisualState(bool selected)
        {
            for (int i = 0; i < targetSpriteRenderers.Length; i++)
            {
                var spriteRenderer = targetSpriteRenderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                var defaultColor = i < defaultSpriteColors.Length ? defaultSpriteColors[i] : Color.white;
                spriteRenderer.color = selected ? selectedSpriteColor : defaultColor;
            }

            for (int i = 0; i < targetTexts.Length; i++)
            {
                var text = targetTexts[i];
                if (text == null)
                {
                    continue;
                }

                var defaultColor = i < defaultTextColors.Length ? defaultTextColors[i] : Color.white;
                text.color = selected ? selectedTextColor : defaultColor;
            }

            buttonAnimator?.RefreshBaseColorsFromCurrent();
        }
    }
}
