using DG.Tweening;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ViewArranger : MonoBehaviour
    {
        public float Width => GetLength(isXAxis: true);
        public float Height => GetLength(isXAxis: false);

        [SerializeField] Transform positionTarget;
        [SerializeField] SpriteRenderer sizeReferenceRenderer;
        [SerializeField] bool convertSizeToParentLocal = true;
        [SerializeField] float moveAnimationDuration = 0.16f;
        [SerializeField] Ease moveAnimationEase = Ease.OutCubic;

        Tween moveTween;

        public void SetPosition(float x, float y)
        {
            var target = positionTarget != null ? positionTarget : transform;
            var targetPosition = new Vector3(x, y, target.localPosition.z);

            if (moveAnimationDuration <= 0f)
            {
                moveTween?.Kill();
                target.localPosition = targetPosition;
                return;
            }

            moveTween?.Kill();
            moveTween = target
                .DOLocalMove(targetPosition, moveAnimationDuration)
                .SetEase(moveAnimationEase)
                .OnComplete(() => moveTween = null);
        }

        float GetLength(bool isXAxis)
        {
            if (sizeReferenceRenderer == null)
            {
                return 0f;
            }

            float worldLength = isXAxis ? sizeReferenceRenderer.bounds.size.x : sizeReferenceRenderer.bounds.size.y;
            if (!convertSizeToParentLocal)
            {
                return worldLength;
            }

            var target = positionTarget != null ? positionTarget : transform;
            var parent = target.parent;
            if (parent == null)
            {
                return worldLength;
            }

            var parentLossyScale = parent.lossyScale;
            float axisScale = isXAxis ? Mathf.Abs(parentLossyScale.x) : Mathf.Abs(parentLossyScale.y);
            if (axisScale <= Mathf.Epsilon)
            {
                return worldLength;
            }

            return worldLength / axisScale;
        }

        void Reset()
        {
            positionTarget = transform;
        }

        void OnDestroy()
        {
            moveTween?.Kill();
        }
    }
}
