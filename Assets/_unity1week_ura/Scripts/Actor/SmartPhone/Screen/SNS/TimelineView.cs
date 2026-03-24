using System.Collections.Generic;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    public class TimelineView : MonoBehaviour
    {
        public Observable<Post> OnPostClicked => onPostClicked;
        public Observable<Post> OnLikedByPlayer => onLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => onRepostedByPlayer;

        [SerializeField] PostViewFactory postViewFactory;
        [SerializeField] Transform timelinePostParent;
        [SerializeField] PublishFieldView publishFieldView;
        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] Collider2D viewportCollider;
        [SerializeField] ScrollBarView scrollBarView;
        [SerializeField] float wheelScrollStep = 0.45f;
        [SerializeField] float bottomSpacingAtMaxScroll = 1f;

        readonly List<PostView> postViews = new();
        readonly List<Post> pendingPosts = new();
        readonly CompositeDisposable disposables = new();
        readonly CompositeDisposable postViewEventDisposables = new();
        readonly Subject<Post> onPostClicked = new();
        readonly Subject<Post> onLikedByPlayer = new();
        readonly Subject<Post> onRepostedByPlayer = new();
        float scrollOffsetY;

        public void Initialize()
        {
            disposables.Clear();
            scrollBarView?.Initialize();

            if (publishFieldView == null && transform.parent != null)
            {
                publishFieldView = transform.parent.GetComponentInChildren<PublishFieldView>(true);
            }

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

        public void AddPost(Post post)
        {
            if (!gameObject.activeInHierarchy)
            {
                pendingPosts.Add(post);
                return;
            }

            AddPostInternal(post);
            ArrangePosts();
        }

        public void FlushPendingPosts(bool useAnimation = true)
        {
            for (int i = 0; i < pendingPosts.Count; i++)
            {
                AddPostInternal(pendingPosts[i]);
            }

            pendingPosts.Clear();
            ArrangePosts(useAnimation);
        }

        public void ClearPosts()
        {
            postViewEventDisposables.Clear();

            foreach (var postView in postViews)
            {
                if (postView == null || postView.gameObject == null)
                {
                    continue;
                }

                postView.StopAnimations();
                Destroy(postView.gameObject);
            }

            postViews.Clear();
            pendingPosts.Clear();
            scrollOffsetY = 0f;
            UpdateScrollBar(0f, GetViewportHeight(), 0f);
        }

        void AddPostInternal(Post post)
        {
            var postView = CreatePostView(post);
            postViews.Insert(0, postView);
        }

        void ArrangePosts(bool useAnimation = true)
        {
            float topY = 0f;
            float contentHeight = GetContentHeight();
            float viewportHeight = GetViewportHeight();
            float clampedOffsetY = GetClampedScrollOffsetY();
            float viewportTopY = GetViewportTopY();
            float viewportBottomY = GetViewportBottomY();
            float interactionTopY = GetInteractionTopY(viewportTopY);

            for (int i = 0; i < postViews.Count; i++)
            {
                var postView = postViews[i];
                float y = topY - postView.Height * 0.5f + clampedOffsetY;
                postView.SetPosition(0, y, useAnimation);
                UpdatePostInteractability(postView, y, interactionTopY, viewportBottomY);
                topY -= postView.Height;
            }

            scrollOffsetY = clampedOffsetY;
            UpdateScrollBar(contentHeight, viewportHeight, clampedOffsetY);
        }

        PostView CreatePostView(Post post)
        {
            PostView postView = postViewFactory.Create(post, timelinePostParent);
            postView.OnPostClicked.Subscribe(onPostClicked.OnNext).AddTo(postViewEventDisposables);
            postView.OnLikedByPlayer.Subscribe(onLikedByPlayer.OnNext).AddTo(postViewEventDisposables);
            postView.OnRepostedByPlayer.Subscribe(onRepostedByPlayer.OnNext).AddTo(postViewEventDisposables);
            postView.OnScrolled.Subscribe(OnScrolled).AddTo(postViewEventDisposables);
            return postView;
        }

        void OnScrolled(PointerEventData eventData)
        {
            scrollOffsetY -= eventData.scrollDelta.y * wheelScrollStep;
            ArrangePosts();
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

        float GetViewportTopY()
        {
            if (viewportCollider == null)
            {
                return 0f;
            }

            if (timelinePostParent == null)
            {
                return viewportCollider.bounds.max.y;
            }

            var parentPosition = timelinePostParent.position;
            var topWorldPosition = new Vector3(parentPosition.x, viewportCollider.bounds.max.y, parentPosition.z);
            return timelinePostParent.InverseTransformPoint(topWorldPosition).y;
        }

        float GetViewportBottomY()
        {
            if (viewportCollider == null)
            {
                return 0f;
            }

            if (timelinePostParent == null)
            {
                return viewportCollider.bounds.min.y;
            }

            var parentPosition = timelinePostParent.position;
            var bottomWorldPosition = new Vector3(parentPosition.x, viewportCollider.bounds.min.y, parentPosition.z);
            return timelinePostParent.InverseTransformPoint(bottomWorldPosition).y;
        }

        float GetContentHeight()
        {
            float total = 0f;
            for (int i = 0; i < postViews.Count; i++)
            {
                total += postViews[i].Height;
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

        float GetInteractionTopY(float viewportTopY)
        {
            if (publishFieldView == null || !publishFieldView.isActiveAndEnabled || !publishFieldView.gameObject.activeInHierarchy)
            {
                return viewportTopY;
            }

            if (!publishFieldView.TryGetColliderWorldBounds(out var bounds))
            {
                return viewportTopY;
            }

            if (timelinePostParent == null)
            {
                return Mathf.Min(viewportTopY, bounds.min.y);
            }

            var parentPosition = timelinePostParent.position;
            var publishFieldBottomWorldPosition = new Vector3(parentPosition.x, bounds.min.y, parentPosition.z);
            float publishFieldBottomY = timelinePostParent.InverseTransformPoint(publishFieldBottomWorldPosition).y;
            return Mathf.Min(viewportTopY, publishFieldBottomY);
        }

        void UpdatePostInteractability(PostView postView, float centerY, float interactionTopY, float viewportBottomY)
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
            bool isWithinViewport = postBottomY < interactionTopY && postTopY > viewportBottomY;
            postView.SetInteractable(isWithinViewport);
            if (isWithinViewport)
            {
                postView.SetInteractionClip(interactionTopY, viewportBottomY);
            }
        }

        void OnDestroy()
        {
            disposables.Dispose();
            postViewEventDisposables.Dispose();
            onPostClicked.OnCompleted();
            onPostClicked.Dispose();
            onLikedByPlayer.OnCompleted();
            onLikedByPlayer.Dispose();
            onRepostedByPlayer.OnCompleted();
            onRepostedByPlayer.Dispose();
        }
    }
}
