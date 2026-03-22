using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ViewArranger : MonoBehaviour
    {
        enum AppearDirection
        {
            None,
            Up,
            Down,
            Left,
            Right
        }

        public float Width => GetLength(isXAxis: true);
        public float Height => GetLength(isXAxis: false);

        [Header("References")]
        [SerializeField] Transform positionTarget;
        [SerializeField] SpriteRenderer sizeReferenceRenderer;

        [Header("Layout")]
        [SerializeField] bool convertSizeToParentLocal = true;

        [Header("Move Animation")]
        [SerializeField] float moveAnimationDuration = 0.3f;
        [SerializeField] Ease moveAnimationEase = Ease.OutCubic;

        [Header("Appear Animation")]
        [SerializeField] bool enableAppearAnimation = true;
        [SerializeField] float appearAnimationDuration = 0.3f;
        [SerializeField] float appearMoveDistance = 1f;
        [SerializeField] Ease appearAnimationEase = Ease.OutCubic;
        [SerializeField] AppearDirection appearDirection = AppearDirection.None;

        Tween moveTween;
        Tween appearTween;
        SpriteRenderer[] spriteRenderers;
        TMP_Text[] texts;
        bool hasAppeared;

        public void StopAnimations()
        {
            moveTween?.Kill();
            moveTween = null;
            appearTween?.Kill();
            appearTween = null;
        }

        public void SetPosition(float x, float y)
        {
            var target = positionTarget != null ? positionTarget : transform;
            var targetPosition = new Vector3(x, y, target.localPosition.z);

            if (enableAppearAnimation && !hasAppeared)
            {
                if (appearAnimationDuration <= 0f)
                {
                    hasAppeared = true;
                    SetVisualAlpha(1f);
                }
                else
                {
                    PlayAppearAnimation(target, targetPosition);
                    return;
                }
            }

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

        void PlayAppearAnimation(Transform target, Vector3 targetPosition)
        {
            moveTween?.Kill();
            appearTween?.Kill();

            hasAppeared = true;
            EnsureVisualTargets();

            target.localPosition = targetPosition + GetAppearOffset();
            SetVisualAlpha(0f);

            float alpha = 0f;
            appearTween = DOTween.Sequence()
                .Join(target.DOLocalMove(targetPosition, appearAnimationDuration).SetEase(appearAnimationEase))
                .Join(DOTween.To(() => alpha, value =>
                {
                    alpha = value;
                    SetVisualAlpha(alpha);
                }, 1f, appearAnimationDuration).SetEase(appearAnimationEase))
                .OnComplete(() =>
                {
                    SetVisualAlpha(1f);
                    appearTween = null;
                });
        }

        Vector3 GetAppearOffset()
        {
            return appearDirection switch
            {
                AppearDirection.Up => new Vector3(0f, appearMoveDistance, 0f),
                AppearDirection.Down => new Vector3(0f, -appearMoveDistance, 0f),
                AppearDirection.Left => new Vector3(-appearMoveDistance, 0f, 0f),
                AppearDirection.Right => new Vector3(appearMoveDistance, 0f, 0f),
                _ => Vector3.zero
            };
        }

        void EnsureVisualTargets()
        {
            if (spriteRenderers == null)
            {
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            if (texts == null)
            {
                texts = GetComponentsInChildren<TMP_Text>(true);
            }
        }

        void SetVisualAlpha(float alpha)
        {
            EnsureVisualTargets();

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                var renderer = spriteRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }

            for (int i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null)
                {
                    continue;
                }

                var color = text.color;
                color.a = alpha;
                text.color = color;
            }
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
            StopAnimations();
        }
    }
}
