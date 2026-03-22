using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ScrollBarView : MonoBehaviour
    {
        [SerializeField] Transform thumb;
        [SerializeField] float minThumbLength = 0.4f;
        [SerializeField] bool hideWhenNotScrollable = true;

        bool isCached;
        Vector3 thumbBaseLocalScale;
        float thumbBaseCenterY;
        float thumbMovableAreaLength;

        public void Initialize()
        {
            CacheBaseStateIfNeeded();
        }

        public void UpdateVisual(float contentHeight, float viewportHeight, float scrollOffsetY)
        {
            if (thumb == null)
            {
                return;
            }

            CacheBaseStateIfNeeded();

            bool validViewport = viewportHeight > Mathf.Epsilon;
            bool scrollable = validViewport && contentHeight > viewportHeight + Mathf.Epsilon;

            if (!scrollable && hideWhenNotScrollable)
            {
                thumb.gameObject.SetActive(false);
                return;
            }

            thumb.gameObject.SetActive(true);

            float safeMovableAreaLength = Mathf.Max(thumbMovableAreaLength, Mathf.Epsilon);
            float maxOffset = Mathf.Max(0f, contentHeight - viewportHeight);

            float thumbLength = safeMovableAreaLength;
            if (scrollable)
            {
                float visibleRatio = Mathf.Clamp01(viewportHeight / contentHeight);
                thumbLength = Mathf.Clamp(safeMovableAreaLength * visibleRatio, minThumbLength, safeMovableAreaLength);
            }

            var scale = thumbBaseLocalScale;
            // 初期Thumbの縦サイズを可動領域の最大サイズとして扱う
            scale.y = thumbBaseLocalScale.y * (thumbLength / safeMovableAreaLength);
            thumb.localScale = scale;

            float normalizedOffset = maxOffset <= Mathf.Epsilon
                ? 0f
                : Mathf.Clamp01(scrollOffsetY / maxOffset);

            float topLimitY = thumbBaseCenterY + safeMovableAreaLength * 0.5f;
            float movableRange = Mathf.Max(0f, safeMovableAreaLength - thumbLength);
            float centerOffsetFromTop = thumbLength * 0.5f + movableRange * normalizedOffset;
            float y = topLimitY - centerOffsetFromTop;

            var localPosition = thumb.localPosition;
            localPosition.y = y;
            thumb.localPosition = localPosition;
        }

        void CacheBaseStateIfNeeded()
        {
            if (isCached)
            {
                return;
            }

            thumbBaseLocalScale = thumb.localScale;
            thumbBaseCenterY = thumb.localPosition.y;
            thumbMovableAreaLength = Mathf.Abs(thumbBaseLocalScale.y);
            isCached = true;
        }
    }
}
