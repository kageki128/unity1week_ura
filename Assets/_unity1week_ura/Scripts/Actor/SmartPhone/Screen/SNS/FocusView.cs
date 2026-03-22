using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    public class FocusView : MonoBehaviour
    {
        public Observable<Post> OnPostClicked => onPostClicked;
        public Observable<Post> OnLikedByPlayer => onLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => onRepostedByPlayer;

        [Header("Components")]
        [SerializeField] Transform mainPostParent;
        [SerializeField] PublishFieldView publishFieldView;

        [Header("References")]
        [SerializeField] PostViewFactory postViewFactory;
        [SerializeField] Transform timelinePostParent;
        [SerializeField] Transform topEdgeTransform;
        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] Collider2D viewportCollider;
        [SerializeField] ScrollBarView scrollBarView;

        [Header("Settings")]
        [SerializeField] float wheelScrollStep = 0.45f;
        [SerializeField] float bottomSpacingAtMaxScroll = 1f;

        readonly List<PostView> replyPostViews = new();
        readonly CompositeDisposable disposables = new();
        readonly CompositeDisposable replyPostViewEventDisposables = new();
        readonly Subject<Post> onPostClicked = new();
        readonly Subject<Post> onLikedByPlayer = new();
        readonly Subject<Post> onRepostedByPlayer = new();
        PostView mainPostView;
        float scrollOffsetY;
        CancellationTokenSource cts;

        public void Initialize()
        {
            disposables.Clear();
            scrollBarView?.Initialize();
            publishFieldView?.Initialize();

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

        public async UniTask SetupAsync(Post mainPost, CancellationToken ct)
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var linkedCt = cts.Token;

            ClearPosts();

            var mainPostViewParent = mainPostParent != null ? mainPostParent : timelinePostParent;
            mainPostView = postViewFactory.Create(mainPost, mainPostViewParent);
            mainPostView.OnPostClicked.Subscribe(onPostClicked.OnNext).AddTo(replyPostViewEventDisposables);
            mainPostView.OnLikedByPlayer.Subscribe(onLikedByPlayer.OnNext).AddTo(replyPostViewEventDisposables);
            mainPostView.OnRepostedByPlayer.Subscribe(onRepostedByPlayer.OnNext).AddTo(replyPostViewEventDisposables);
            mainPostView.OnScrolled.Subscribe(OnScrolled).AddTo(replyPostViewEventDisposables);

            publishFieldView.gameObject.SetActive(true);

            // リプライを取得して生成
            var replies = await postViewFactory.CreateRepliesAsync(mainPost, timelinePostParent, linkedCt);
            linkedCt.ThrowIfCancellationRequested();

            foreach (var replyView in replies)
            {
                replyView.OnPostClicked.Subscribe(onPostClicked.OnNext).AddTo(replyPostViewEventDisposables);
                replyView.OnLikedByPlayer.Subscribe(onLikedByPlayer.OnNext).AddTo(replyPostViewEventDisposables);
                replyView.OnRepostedByPlayer.Subscribe(onRepostedByPlayer.OnNext).AddTo(replyPostViewEventDisposables);
                replyView.OnScrolled.Subscribe(OnScrolled).AddTo(replyPostViewEventDisposables);
                replyPostViews.Add(replyView);
            }

            ArrangeElements();
        }

        public void ClearPosts()
        {
            replyPostViewEventDisposables.Clear();

            if (mainPostView != null && mainPostView.gameObject != null)
            {
                Destroy(mainPostView.gameObject);
            }
            mainPostView = null;

            foreach (var replyView in replyPostViews)
            {
                if (replyView != null && replyView.gameObject != null)
                {
                    Destroy(replyView.gameObject);
                }
            }
            replyPostViews.Clear();

            publishFieldView.gameObject.SetActive(false);

            scrollOffsetY = 0f;
            UpdateScrollBar(0f, GetViewportHeight(), 0f);
        }

        void ArrangeElements()
        {
            float topY = topEdgeTransform != null ? topEdgeTransform.localPosition.y : 0f;
            float contentHeight = GetContentHeight();
            float viewportHeight = GetViewportHeight();
            float clampedOffsetY = GetClampedScrollOffsetY();

            // 1. メインポストの配置
            if (mainPostView != null)
            {
                float y = topY - mainPostView.Height * 0.5f + clampedOffsetY;
                mainPostView.SetPosition(0, y);
                topY -= mainPostView.Height;
            }

            // 2. PublishFieldの配置
            if (publishFieldView.gameObject.activeSelf)
            {
                float y = topY - publishFieldView.Height * 0.5f + clampedOffsetY;
                publishFieldView.SetPosition(0, y);
                topY -= publishFieldView.Height;
            }

            // 3. 返信ポストの配置
            for (int i = 0; i < replyPostViews.Count; i++)
            {
                var postView = replyPostViews[i];
                float y = topY - postView.Height * 0.5f + clampedOffsetY;
                postView.SetPosition(0, y);
                topY -= postView.Height;
            }

            scrollOffsetY = clampedOffsetY;
            UpdateScrollBar(contentHeight, viewportHeight, clampedOffsetY);
        }

        void OnScrolled(PointerEventData eventData)
        {
            scrollOffsetY -= eventData.scrollDelta.y * wheelScrollStep;
            ArrangeElements();
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
            if (timelinePostParent == null)
            {
                return worldHeight;
            }

            float localScaleY = Mathf.Abs(timelinePostParent.lossyScale.y);
            if (localScaleY <= Mathf.Epsilon)
            {
                return worldHeight;
            }

            return worldHeight / localScaleY;
        }

        float GetContentHeight()
        {
            float total = 0f;
            
            if (mainPostView != null)
            {
                total += mainPostView.Height;
            }

            if (publishFieldView.gameObject.activeSelf)
            {
                total += publishFieldView.Height;
            }

            for (int i = 0; i < replyPostViews.Count; i++)
            {
                total += replyPostViews[i].Height;
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

        void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
            
            disposables.Dispose();
            replyPostViewEventDisposables.Dispose();
            onPostClicked.OnCompleted();
            onPostClicked.Dispose();
            onLikedByPlayer.OnCompleted();
            onLikedByPlayer.Dispose();
            onRepostedByPlayer.OnCompleted();
            onRepostedByPlayer.Dispose();
        }
    }
}
