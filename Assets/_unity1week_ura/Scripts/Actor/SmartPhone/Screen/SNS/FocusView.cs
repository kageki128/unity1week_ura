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
    public class FocusView : MonoBehaviour
    {
        public Observable<Post> OnPostClicked => onPostClicked;
        public Observable<Post> OnLikedByPlayer => onLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => onRepostedByPlayer;
        public Observable<ReplyDraftPublishRequest> OnReplyDraftDropped => onReplyDraftDropped;

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

        readonly List<PostView> ancestorPostViews = new();
        readonly List<PostView> replyPostViews = new();
        readonly CompositeDisposable disposables = new();
        readonly CompositeDisposable postViewEventDisposables = new();
        readonly Subject<Post> onPostClicked = new();
        readonly Subject<Post> onLikedByPlayer = new();
        readonly Subject<Post> onRepostedByPlayer = new();
        readonly Subject<ReplyDraftPublishRequest> onReplyDraftDropped = new();
        PostView mainPostView;
        Post currentFocusedPost;
        float scrollOffsetY;
        float mainPostTopAlignOffsetY;
        CancellationTokenSource cts;
        Account currentPlayerAccount;

        public void Initialize()
        {
            disposables.Clear();
            scrollBarView?.Initialize();
            publishFieldView?.Initialize();
            if (publishFieldView != null)
            {
                publishFieldView.OnScrolled.Subscribe(OnScrolled).AddTo(disposables);
                publishFieldView.OnReplyDraftDropped
                    .Select(replyDraft => new ReplyDraftPublishRequest(replyDraft, currentFocusedPost))
                    .Subscribe(onReplyDraftDropped.OnNext)
                    .AddTo(disposables);
            }

            if (pointerEventObserver == null)
            {
                UpdateScrollBar(0f, GetViewportHeight(), 0f);
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
            currentFocusedPost = mainPost;

            var mainPostViewParent = mainPostParent != null ? mainPostParent : timelinePostParent;
            mainPostView = postViewFactory.Create(mainPost, mainPostViewParent);
            mainPostView.OnPostClicked.Subscribe(onPostClicked.OnNext).AddTo(postViewEventDisposables);
            mainPostView.OnLikedByPlayer.Subscribe(onLikedByPlayer.OnNext).AddTo(postViewEventDisposables);
            mainPostView.OnRepostedByPlayer.Subscribe(onRepostedByPlayer.OnNext).AddTo(postViewEventDisposables);
            mainPostView.OnScrolled.Subscribe(OnScrolled).AddTo(postViewEventDisposables);

            if (publishFieldView != null)
            {
                publishFieldView.gameObject.SetActive(true);
                publishFieldView.SetCurrentPlayerAccount(currentPlayerAccount);
            }

            var ancestors = await postViewFactory.CreateAncestorPostsAsync(mainPost, mainPostViewParent, linkedCt);
            linkedCt.ThrowIfCancellationRequested();
            for (int i = 0; i < ancestors.Count; i++)
            {
                RegisterPostView(ancestors[i]);
                ancestorPostViews.Add(ancestors[i]);
            }

            var replies = await postViewFactory.CreateRepliesAsync(mainPost, timelinePostParent, linkedCt);
            linkedCt.ThrowIfCancellationRequested();
            for (int i = 0; i < replies.Count; i++)
            {
                if (ContainsReplyPost(replies[i].post))
                {
                    continue;
                }

                RegisterPostView(replies[i]);
                replyPostViews.Add(replies[i]);
            }

            mainPostTopAlignOffsetY = GetAncestorContentHeight();
            scrollOffsetY = mainPostTopAlignOffsetY;
            ArrangeElements(useAnimation: false);
        }

        public void ClearPosts()
        {
            postViewEventDisposables.Clear();

            DestroyPostView(mainPostView);
            mainPostView = null;

            for (int i = 0; i < ancestorPostViews.Count; i++)
            {
                DestroyPostView(ancestorPostViews[i]);
            }
            ancestorPostViews.Clear();

            for (int i = 0; i < replyPostViews.Count; i++)
            {
                DestroyPostView(replyPostViews[i]);
            }
            replyPostViews.Clear();

            if (publishFieldView != null)
            {
                publishFieldView.gameObject.SetActive(false);
            }

            scrollOffsetY = 0f;
            mainPostTopAlignOffsetY = 0f;
            currentFocusedPost = null;
            UpdateScrollBar(0f, GetViewportHeight(), 0f);
        }

        void RegisterPostView(PostView postView)
        {
            postView.OnPostClicked.Subscribe(onPostClicked.OnNext).AddTo(postViewEventDisposables);
            postView.OnLikedByPlayer.Subscribe(onLikedByPlayer.OnNext).AddTo(postViewEventDisposables);
            postView.OnRepostedByPlayer.Subscribe(onRepostedByPlayer.OnNext).AddTo(postViewEventDisposables);
            postView.OnScrolled.Subscribe(OnScrolled).AddTo(postViewEventDisposables);
        }

        void DestroyPostView(PostView postView)
        {
            if (postView == null || postView.gameObject == null)
            {
                return;
            }

            postView.StopAnimations();
            Destroy(postView.gameObject);
        }

        public void SetCurrentPlayerAccount(Account account)
        {
            currentPlayerAccount = account;
            publishFieldView?.SetCurrentPlayerAccount(account);
        }

        public void AddPublishedPost(Post post)
        {
            if (!CanDisplayAsReplyForCurrentFocus(post))
            {
                return;
            }

            if (ContainsReplyPost(post))
            {
                return;
            }

            var replyView = postViewFactory.Create(post, timelinePostParent);
            RegisterPostView(replyView);
            replyPostViews.Insert(0, replyView);
            ArrangeElements();
        }

        void ArrangeElements(bool useAnimation = true)
        {
            float viewportTopY = topEdgeTransform != null ? topEdgeTransform.localPosition.y : 0f;
            float topY = viewportTopY;
            float contentHeight = GetContentHeight();
            float viewportHeight = GetViewportHeight();
            float clampedOffsetY = GetClampedScrollOffsetY();

            for (int i = 0; i < ancestorPostViews.Count; i++)
            {
                var ancestorView = ancestorPostViews[i];
                float y = topY - ancestorView.Height * 0.5f + clampedOffsetY;
                ancestorView.SetPosition(0, y, useAnimation);
                UpdatePostClickability(ancestorView, y, viewportTopY, viewportHeight);
                topY -= ancestorView.Height;
            }

            if (mainPostView != null)
            {
                float y = topY - mainPostView.Height * 0.5f + clampedOffsetY;
                mainPostView.SetPosition(0, y, useAnimation);
                UpdatePostClickability(mainPostView, y, viewportTopY, viewportHeight);
                topY -= mainPostView.Height;
            }

            if (publishFieldView != null && publishFieldView.gameObject.activeSelf)
            {
                float y = topY - publishFieldView.Height * 0.5f + clampedOffsetY;
                publishFieldView.SetPosition(0, y, useAnimation);
                topY -= publishFieldView.Height;
            }

            for (int i = 0; i < replyPostViews.Count; i++)
            {
                var replyView = replyPostViews[i];
                float y = topY - replyView.Height * 0.5f + clampedOffsetY;
                replyView.SetPosition(0, y, useAnimation);
                UpdatePostClickability(replyView, y, viewportTopY, viewportHeight);
                topY -= replyView.Height;
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
            float defaultMaxOffset = Mathf.Max(0f, contentHeight + bottomSpacingAtMaxScroll - viewportHeight);
            return Mathf.Max(mainPostTopAlignOffsetY, defaultMaxOffset);
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

            total += GetAncestorContentHeight();

            if (mainPostView != null)
            {
                total += mainPostView.Height;
            }

            if (publishFieldView != null && publishFieldView.gameObject.activeSelf)
            {
                total += publishFieldView.Height;
            }

            for (int i = 0; i < replyPostViews.Count; i++)
            {
                total += replyPostViews[i].Height;
            }

            return total;
        }

        float GetAncestorContentHeight()
        {
            float total = 0f;
            for (int i = 0; i < ancestorPostViews.Count; i++)
            {
                total += ancestorPostViews[i].Height;
            }

            return total;
        }

        void UpdateScrollBar(float contentHeight, float viewportHeight, float clampedOffsetY)
        {
            if (scrollBarView == null)
            {
                return;
            }

            float maxOffset = GetMaxScrollOffset(contentHeight, viewportHeight);
            float visualContentHeight = viewportHeight + maxOffset;
            scrollBarView.UpdateVisual(visualContentHeight, viewportHeight, clampedOffsetY);
        }

        void UpdatePostClickability(PostView postView, float centerY, float viewportTopY, float viewportHeight)
        {
            if (postView == null)
            {
                return;
            }

            if (viewportCollider == null)
            {
                postView.SetInteractable(true);
                postView.SetInteractionClip(float.PositiveInfinity, float.NegativeInfinity);
                return;
            }

            float halfHeight = postView.Height * 0.5f;
            float postTopY = centerY + halfHeight;
            float postBottomY = centerY - halfHeight;
            float viewportBottomY = viewportTopY - viewportHeight;

            bool isWithinViewport = postBottomY < viewportTopY && postTopY > viewportBottomY;
            postView.SetInteractable(isWithinViewport);
            if (isWithinViewport)
            {
                postView.SetInteractionClip(viewportTopY, viewportBottomY);
            }
        }

        bool CanDisplayAsReplyForCurrentFocus(Post post)
        {
            if (post == null || post.State != PostState.Published || post.Type != PostType.Reply)
            {
                return false;
            }

            if (currentFocusedPost?.Property == null)
            {
                return false;
            }

            var currentFocusId = currentFocusedPost.Property.Id;
            if (string.IsNullOrEmpty(currentFocusId))
            {
                return false;
            }

            return string.Equals(post.Property.ParentPostId, currentFocusId, StringComparison.Ordinal);
        }

        bool ContainsReplyPost(Post post)
        {
            if (post?.Property == null || string.IsNullOrEmpty(post.Property.Id))
            {
                return false;
            }

            for (int i = 0; i < replyPostViews.Count; i++)
            {
                var replyView = replyPostViews[i];
                if (replyView?.post?.Property == null)
                {
                    continue;
                }

                if (string.Equals(replyView.post.Property.Id, post.Property.Id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();

            disposables.Dispose();
            postViewEventDisposables.Dispose();
            onPostClicked.OnCompleted();
            onPostClicked.Dispose();
            onLikedByPlayer.OnCompleted();
            onLikedByPlayer.Dispose();
            onRepostedByPlayer.OnCompleted();
            onRepostedByPlayer.Dispose();
            onReplyDraftDropped.OnCompleted();
            onReplyDraftDropped.Dispose();
        }
    }
}
