using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity1Week_Ura.Actor
{
    // 使い方:
    // 1) Show/Hide を共通化したい GameObject にこのコンポーネントを付与する。
    // 2) 初期化時に Initialize()、表示時に ShowAsync(ct)、非表示時に HideAsync(ct) を呼ぶ。
    // 3) 専用 View から委譲する場合は、専用 View 側で StandardViewAnimator を SerializeField 参照し、
    //    ShowAsync/HideAsync をそのまま転送する。
    // 4) レイアウト変更後に表示基準位置を更新したい場合は RefreshShownPositionFromCurrent() を呼ぶ。
    [DisallowMultipleComponent]
    public class StandardViewAnimator : AnimationViewBase
    {
        enum MoveDirection
        {
            [InspectorName("動かない")] None,
            [InspectorName("上")] Up,
            [InspectorName("下")] Down,
            [InspectorName("左")] Left,
            [InspectorName("右")] Right
        }

        [Header("References")]
        // 移動アニメーションを適用する対象。未指定時はこの GameObject の transform。
        [Tooltip("移動アニメーションを適用する対象。未指定時はこの GameObject の Transform を使います。")]
        [SerializeField] Transform moveTarget;
        // CanvasGroup を使ってフェードする対象。指定時は Sprite/TMP/Graphic 個別フェードより優先される。
        [Tooltip("CanvasGroup でフェードする対象。指定時は Sprite/TMP/Graphic 個別フェードより優先されます。")]
        [SerializeField] CanvasGroup canvasGroupTarget;
        // true の場合、初期化時に子階層の SpriteRenderer/TMP_Text/Graphic を自動収集する。
        [Tooltip("true の場合、初期化時に子階層の SpriteRenderer/TMP_Text/Graphic を自動収集します。")]
        [SerializeField] bool autoCollectVisualTargets = true;
        // CanvasGroup を使わない場合にフェード対象となる SpriteRenderer 一覧。
        [Tooltip("CanvasGroup を使わない場合にフェード対象となる SpriteRenderer 一覧です。")]
        [SerializeField] SpriteRenderer[] spriteRenderers;
        // CanvasGroup を使わない場合にフェード対象となる TMP_Text 一覧。
        [Tooltip("CanvasGroup を使わない場合にフェード対象となる TMP_Text 一覧です。")]
        [SerializeField] TMP_Text[] texts;
        // CanvasGroup を使わない場合にフェード対象となる UI Graphic 一覧。
        [Tooltip("CanvasGroup を使わない場合にフェード対象となる UI Graphic 一覧です。")]
        [SerializeField] Graphic[] graphics;

        [Header("Show")]
        // Show 時にどの方向から入ってくるか。
        [Tooltip("Show 時にどの方向から入ってくるかを指定します。")]
        [SerializeField] MoveDirection showDirection = MoveDirection.Left;
        // Show 開始位置のオフセット量。
        [Tooltip("Show 開始位置のオフセット量です。")]
        [SerializeField, Min(0f)] float showMoveDistance = 3f;
        // Show アニメーション時間。0 の場合は即時反映。
        [Tooltip("Show アニメーション時間。0 の場合は即時反映します。")]
        [SerializeField, Min(0f)] float showDuration = 0.3f;
        // Show 時のイージング。
        [Tooltip("Show 時のイージングです。")]
        [SerializeField] Ease showEase = Ease.OutCubic;

        [Header("Hide")]
        // Hide 時にどの方向へ抜けるか。
        [Tooltip("Hide 時にどの方向へ抜けるかを指定します。")]
        [SerializeField] MoveDirection hideDirection = MoveDirection.Left;
        // Hide 終了位置のオフセット量。
        [Tooltip("Hide 終了位置のオフセット量です。")]
        [SerializeField, Min(0f)] float hideMoveDistance = 3f;
        // Hide アニメーション時間。0 の場合は即時反映。
        [Tooltip("Hide アニメーション時間。0 の場合は即時反映します。")]
        [SerializeField, Min(0f)] float hideDuration = 0.3f;
        // Hide 時のイージング。
        [Tooltip("Hide 時のイージングです。")]
        [SerializeField] Ease hideEase = Ease.OutCubic;
        // Hide 完了後に GameObject を非アクティブ化するか。
        [Tooltip("Hide 完了後に GameObject を非アクティブ化するかを指定します。")]
        [SerializeField] bool deactivateOnHide = true;

        [Header("Fade")]
        // 表示状態での目標アルファ値。
        [Tooltip("表示状態での目標アルファ値です。")]
        [SerializeField, Range(0f, 1f)] float visibleAlpha = 1f;
        // 非表示状態での目標アルファ値。
        [Tooltip("非表示状態での目標アルファ値です。")]
        [SerializeField, Range(0f, 1f)] float hiddenAlpha = 0f;

        [Header("Initialize")]
        // Initialize 時に hiddenAlpha を適用して非表示開始にするか。
        [Tooltip("Initialize 時に hiddenAlpha を適用して非表示開始にするかを指定します。")]
        [SerializeField] bool hideOnInitialize = true;

        Vector3 shownLocalPosition;
        Tween activeTween;
        bool isInitialized;
        bool isPlayingAnimation;
        IAnimationSuspendable[] animationSuspendables = Array.Empty<IAnimationSuspendable>();
        int suspendDepth;

        public override void Initialize()
        {
            EnsureTargetsResolved();
            shownLocalPosition = moveTarget.localPosition;
            isInitialized = true;

            if (!hideOnInitialize)
            {
                SetVisualAlpha(visibleAlpha);
                return;
            }

            SetVisualAlpha(hiddenAlpha);
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            EnsureInitializedForRuntime();
            KillActiveTween();
            RefreshVisualTargetsForPlayback();
            RefreshAnimationSuspendablesForPlayback();

            gameObject.SetActive(true);
            SuspendAnimationSuspendables();

            try
            {
                var startPosition = shownLocalPosition + ToDirectionVector(showDirection) * showMoveDistance;
                var clampedVisibleAlpha = Mathf.Clamp01(visibleAlpha);
                var clampedHiddenAlpha = Mathf.Clamp01(hiddenAlpha);

                if (showDuration <= 0f)
                {
                    moveTarget.localPosition = shownLocalPosition;
                    SetVisualAlpha(clampedVisibleAlpha);
                    return;
                }

                moveTarget.localPosition = startPosition;
                SetVisualAlpha(clampedHiddenAlpha);
                await PlayAnimationAsync(
                    startPosition,
                    shownLocalPosition,
                    clampedHiddenAlpha,
                    clampedVisibleAlpha,
                    showDuration,
                    showEase,
                    ct);
            }
            finally
            {
                ResumeAnimationSuspendables();
            }
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            EnsureInitializedForRuntime();
            KillActiveTween();
            RefreshVisualTargetsForPlayback();

            if (!gameObject.activeSelf && deactivateOnHide)
            {
                moveTarget.localPosition = shownLocalPosition;
                SetVisualAlpha(Mathf.Clamp01(hiddenAlpha));
                return;
            }

            RefreshAnimationSuspendablesForPlayback();
            SuspendAnimationSuspendables();

            try
            {
                shownLocalPosition = moveTarget.localPosition;

                var hidePosition = shownLocalPosition + ToDirectionVector(hideDirection) * hideMoveDistance;
                var clampedVisibleAlpha = Mathf.Clamp01(visibleAlpha);
                var clampedHiddenAlpha = Mathf.Clamp01(hiddenAlpha);

                if (hideDuration <= 0f)
                {
                    moveTarget.localPosition = hidePosition;
                    SetVisualAlpha(clampedHiddenAlpha);
                    FinalizeHideState();
                    return;
                }

                await PlayAnimationAsync(
                    shownLocalPosition,
                    hidePosition,
                    clampedVisibleAlpha,
                    clampedHiddenAlpha,
                    hideDuration,
                    hideEase,
                    ct);
                FinalizeHideState();
            }
            finally
            {
                ResumeAnimationSuspendables();
            }
        }

        // 現在位置を「表示時の基準位置」として再記録する。
        public void RefreshShownPositionFromCurrent()
        {
            EnsureTargetsResolved();
            shownLocalPosition = moveTarget.localPosition;
            isInitialized = true;
        }

        async UniTask PlayAnimationAsync(
            Vector3 fromPosition,
            Vector3 toPosition,
            float fromAlpha,
            float toAlpha,
            float duration,
            Ease ease,
            CancellationToken ct)
        {
            moveTarget.localPosition = fromPosition;
            SetVisualAlpha(fromAlpha);
            isPlayingAnimation = true;

            var animatedAlpha = fromAlpha;
            var sequence = DOTween.Sequence()
                .Join(moveTarget.DOLocalMove(toPosition, duration).SetEase(ease))
                .Join(DOTween.To(
                        () => animatedAlpha,
                        value =>
                        {
                            animatedAlpha = value;
                            SetVisualAlpha(animatedAlpha);
                        },
                        toAlpha,
                        duration)
                    .SetEase(ease));

            activeTween = sequence;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            try
            {
                await sequence.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                KillActiveTween();
                throw;
            }
            finally
            {
                isPlayingAnimation = false;
                if (activeTween == sequence)
                {
                    activeTween = null;
                }
            }
        }

        void EnsureInitializedForRuntime()
        {
            if (isInitialized)
            {
                return;
            }

            EnsureTargetsResolved();
            shownLocalPosition = moveTarget.localPosition;
            isInitialized = true;
        }

        void EnsureTargetsResolved()
        {
            if (moveTarget == null)
            {
                moveTarget = transform;
            }

            if (canvasGroupTarget == null)
            {
                canvasGroupTarget = GetComponent<CanvasGroup>();
            }

            RefreshAutoCollectedVisualTargets();
            RefreshAnimationSuspendablesForPlayback();
        }

        void RefreshVisualTargetsForPlayback()
        {
            RefreshAutoCollectedVisualTargets();
        }

        void RefreshAnimationSuspendablesForPlayback()
        {
            var behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            if (behaviours == null || behaviours.Length == 0)
            {
                animationSuspendables = Array.Empty<IAnimationSuspendable>();
                return;
            }

            var count = 0;
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IAnimationSuspendable)
                {
                    count++;
                }
            }

            if (count <= 0)
            {
                animationSuspendables = Array.Empty<IAnimationSuspendable>();
                return;
            }

            if (animationSuspendables.Length != count)
            {
                animationSuspendables = new IAnimationSuspendable[count];
            }

            var index = 0;
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IAnimationSuspendable suspendable)
                {
                    continue;
                }

                animationSuspendables[index] = suspendable;
                index++;
            }
        }

        void SuspendAnimationSuspendables()
        {
            suspendDepth++;
            if (suspendDepth != 1)
            {
                return;
            }

            for (var i = 0; i < animationSuspendables.Length; i++)
            {
                animationSuspendables[i].SuspendAnimation();
            }
        }

        void ResumeAnimationSuspendables()
        {
            if (suspendDepth <= 0)
            {
                return;
            }

            suspendDepth--;
            if (suspendDepth > 0)
            {
                return;
            }

            for (var i = 0; i < animationSuspendables.Length; i++)
            {
                animationSuspendables[i].ResumeAnimation();
            }
        }

        void RefreshAutoCollectedVisualTargets()
        {
            if (!autoCollectVisualTargets)
            {
                return;
            }

            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            texts = GetComponentsInChildren<TMP_Text>(true);
            graphics = GetComponentsInChildren<Graphic>(true);
        }

        void SetVisualAlpha(float alpha)
        {
            var clampedAlpha = Mathf.Clamp01(alpha);
            if (isPlayingAnimation && autoCollectVisualTargets && canvasGroupTarget == null)
            {
                RefreshAutoCollectedVisualTargets();
            }

            if (canvasGroupTarget != null)
            {
                canvasGroupTarget.alpha = clampedAlpha;
                return;
            }

            if (spriteRenderers != null)
            {
                for (var i = 0; i < spriteRenderers.Length; i++)
                {
                    var renderer = spriteRenderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    var color = renderer.color;
                    color.a = clampedAlpha;
                    renderer.color = color;
                }
            }

            if (texts != null)
            {
                for (var i = 0; i < texts.Length; i++)
                {
                    var text = texts[i];
                    if (text == null)
                    {
                        continue;
                    }

                    var color = text.color;
                    color.a = clampedAlpha;
                    text.color = color;
                }
            }

            if (graphics != null)
            {
                for (var i = 0; i < graphics.Length; i++)
                {
                    var graphic = graphics[i];
                    if (graphic == null)
                    {
                        continue;
                    }

                    var color = graphic.color;
                    color.a = clampedAlpha;
                    graphic.color = color;
                }
            }
        }

        void FinalizeHideState()
        {
            if (!deactivateOnHide)
            {
                return;
            }

            moveTarget.localPosition = shownLocalPosition;
            gameObject.SetActive(false);
        }

        Vector3 ToDirectionVector(MoveDirection direction)
        {
            return direction switch
            {
                MoveDirection.Up => Vector3.up,
                MoveDirection.Down => Vector3.down,
                MoveDirection.Left => Vector3.left,
                MoveDirection.Right => Vector3.right,
                _ => Vector3.zero
            };
        }

        void KillActiveTween()
        {
            if (activeTween == null)
            {
                return;
            }

            if (activeTween.IsActive())
            {
                activeTween.Kill();
            }

            activeTween = null;
        }

        void Reset()
        {
            moveTarget = transform;
            canvasGroupTarget = GetComponent<CanvasGroup>();
        }

        void OnDestroy()
        {
            KillActiveTween();
        }
    }
}
