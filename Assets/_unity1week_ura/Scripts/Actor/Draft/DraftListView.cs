using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    public class DraftListView : MonoBehaviour
    {
        [SerializeField] DraftView draftViewPrefab;
        [SerializeField] Transform draftParent;
        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] Collider2D viewportCollider;
        [SerializeField] ScrollBarView scrollBarView;
        [SerializeField] StandardViewAnimator standardViewAnimator;
        [SerializeField] float wheelScrollStep = 0.2f;
        [SerializeField] float bottomSpacingAtMaxScroll = 1f;

        readonly List<DraftView> draftViews = new();
        readonly CompositeDisposable disposables = new();
        readonly Dictionary<DraftView, IDisposable> draftViewScrollSubscriptions = new();
        readonly SemaphoreSlim visibilitySemaphore = new(1, 1);
        float scrollOffsetY;
        bool isCurrentVisible = true;
        bool suppressDraftAnimations;

        public void Initialize()
        {
            disposables.Clear();
            ClearDraftViewScrollSubscriptions();
            scrollBarView?.Initialize();
            EnsureStandardViewAnimatorResolved();
            isCurrentVisible = gameObject.activeSelf;
            suppressDraftAnimations = !isCurrentVisible;

            if (pointerEventObserver == null)
            {
                return;
            }

            if (viewportCollider == null)
            {
                pointerEventObserver.TryGetComponent(out viewportCollider);
            }

            pointerEventObserver.OnScrolled.Subscribe(OnScrolled).AddTo(disposables);
            UpdateScrollBar(0f, GetViewportHeight(), 0f);
        }

        public async UniTask SetVisible(bool visible, CancellationToken ct = default)
        {
            await visibilitySemaphore.WaitAsync(ct);
            try
            {
                EnsureStandardViewAnimatorResolved();
                if (isCurrentVisible == visible)
                {
                    suppressDraftAnimations = !isCurrentVisible;
                    return;
                }

                suppressDraftAnimations = true;
                ArrangeDrafts(useAnimation: false);

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
                if (standardViewAnimator == null)
                {
                    gameObject.SetActive(visible);
                }
                else if (visible)
                {
                    await standardViewAnimator.ShowAsync(linkedCts.Token);
                }
                else
                {
                    await standardViewAnimator.HideAsync(linkedCts.Token);
                }

                isCurrentVisible = visible;
                suppressDraftAnimations = !isCurrentVisible;
                if (isCurrentVisible)
                {
                    ArrangeDrafts(useAnimation: false);
                }
            }
            finally
            {
                visibilitySemaphore.Release();
            }
        }

        public void AddDraft(Post post)
        {
            var draftView = CreateDraftView(post);
            draftViews.Insert(0, draftView);
            CompensateScrollOffsetForPrependedContent(draftView.Height);
            RegisterDraftViewScroll(draftView);
            ArrangeDrafts();
        }

        void CompensateScrollOffsetForPrependedContent(float prependedHeight)
        {
            if (prependedHeight <= 0f || scrollOffsetY <= Mathf.Epsilon)
            {
                return;
            }

            scrollOffsetY += prependedHeight;
        }

        public void RemoveDraft(Post post)
        {
            var draftView = draftViews.Find(view => view.post == post);
            if (draftView != null)
            {
                UnregisterDraftViewScroll(draftView);
                draftViews.Remove(draftView);
                Destroy(draftView.gameObject);
                ArrangeDrafts();
            }
        }

        public void ClearDrafts()
        {
            ClearDraftViewScrollSubscriptions();

            foreach (var draftView in draftViews)
            {
                Destroy(draftView.gameObject);
            }
            draftViews.Clear();
            scrollOffsetY = 0f;
            UpdateScrollBar(0f, GetViewportHeight(), 0f);
        }

        void ArrangeDrafts(bool useAnimation = true)
        {
            // 先頭要素の上端が原点になるように上から隙間無く配置する
            float topY = 0f;
            float contentHeight = GetContentHeight();
            float viewportHeight = GetViewportHeight();
            float clampedOffsetY = GetClampedScrollOffsetY();
            bool shouldUseDraftAnimation = useAnimation && CanPlayDraftAnimation();
            for (int i = 0; i < draftViews.Count; i++)
            {
                var draftView = draftViews[i];
                if (draftView == null)
                {
                    continue;
                }

                float y = topY - draftView.Height * 0.5f + clampedOffsetY;
                draftView.SetReturnPosition(0, y);
                if (!draftView.IsDragging)
                {
                    draftView.SetPosition(0, y, shouldUseDraftAnimation);
                }
                topY -= draftView.Height;
            }

            scrollOffsetY = clampedOffsetY;
            UpdateScrollBar(contentHeight, viewportHeight, clampedOffsetY);
        }

        void OnScrolled(PointerEventData eventData)
        {
            scrollOffsetY -= eventData.scrollDelta.y * wheelScrollStep;
            ArrangeDrafts();
        }

        float GetClampedScrollOffsetY()
        {
            float contentHeight = GetContentHeight();
            float viewportHeight = GetViewportHeight();
            float maxOffset = GetMaxScrollOffset(contentHeight, viewportHeight);
            return Mathf.Clamp(scrollOffsetY, 0f, maxOffset);
        }

        float GetMaxScrollOffset(float contentHeight, float viewportHeight)
        {
            return Mathf.Max(0f, contentHeight + bottomSpacingAtMaxScroll - viewportHeight);
        }

        float GetViewportHeight()
        {
            if (viewportCollider == null)
            {
                return 0f;
            }

            float worldHeight = viewportCollider.bounds.size.y;
            if (draftParent == null)
            {
                return worldHeight;
            }

            float localScaleY = Mathf.Abs(draftParent.lossyScale.y);
            if (localScaleY <= Mathf.Epsilon)
            {
                return worldHeight;
            }

            return worldHeight / localScaleY;
        }

        float GetContentHeight()
        {
            float total = 0f;
            for (int i = 0; i < draftViews.Count; i++)
            {
                var draftView = draftViews[i];
                if (draftView == null)
                {
                    continue;
                }

                total += draftView.Height;
            }

            return total;
        }

        void UpdateScrollBar(float contentHeight, float viewportHeight, float clampedOffsetY)
        {
            if (scrollBarView == null)
            {
                return;
            }

            float visualContentHeight = contentHeight + bottomSpacingAtMaxScroll;
            scrollBarView.UpdateVisual(visualContentHeight, viewportHeight, clampedOffsetY);
        }

        bool CanPlayDraftAnimation()
        {
            return !suppressDraftAnimations && isCurrentVisible;
        }

        void EnsureStandardViewAnimatorResolved()
        {
            if (standardViewAnimator != null)
            {
                return;
            }

            TryGetComponent(out standardViewAnimator);
        }

        DraftView CreateDraftView(Post post)
        {
            DraftView draftView = Instantiate(draftViewPrefab, draftParent);

            var pos = draftView.transform.localPosition;
            pos.z = -1f;  // 背景のリストビュー（Z=0）より確実に手前に配置し、Raycastを奪う
            draftView.transform.localPosition = pos;

            draftView.Initialize(post);
            return draftView;
        }

        void RegisterDraftViewScroll(DraftView draftView)
        {
            if (draftView == null)
            {
                return;
            }

            UnregisterDraftViewScroll(draftView);
            draftViewScrollSubscriptions[draftView] = draftView.OnScrolled.Subscribe(OnScrolled);
        }

        void UnregisterDraftViewScroll(DraftView draftView)
        {
            if (draftView == null)
            {
                return;
            }

            if (draftViewScrollSubscriptions.TryGetValue(draftView, out var subscription))
            {
                subscription.Dispose();
                draftViewScrollSubscriptions.Remove(draftView);
            }
        }

        void ClearDraftViewScrollSubscriptions()
        {
            foreach (var subscription in draftViewScrollSubscriptions.Values)
            {
                subscription.Dispose();
            }

            draftViewScrollSubscriptions.Clear();
        }

        void OnDestroy()
        {
            ClearDraftViewScrollSubscriptions();
            disposables.Dispose();
            visibilitySemaphore.Dispose();
        }

        void Reset()
        {
            TryGetComponent(out standardViewAnimator);
        }
    }
}
