using System.Collections.Generic;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    public class TimelineView : MonoBehaviour
    {
        public Observable<Post> OnLikedByPlayer => onLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => onRepostedByPlayer;

        [SerializeField] PostView postViewPrefab;
        [SerializeField] Transform timelinePostParent;
        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] Collider2D viewportCollider;
        [SerializeField] float wheelScrollStep = 0.45f;

        readonly List<PostView> postViews = new();
        readonly CompositeDisposable disposables = new();
        readonly CompositeDisposable postViewEventDisposables = new();
        readonly Subject<Post> onLikedByPlayer = new();
        readonly Subject<Post> onRepostedByPlayer = new();
        float scrollOffsetY;

        public void Initialize()
        {
            disposables.Clear();

            if (pointerEventObserver == null)
            {
                return;
            }

            if (viewportCollider == null)
            {
                pointerEventObserver.TryGetComponent(out viewportCollider);
            }

            pointerEventObserver.OnScrolled.Subscribe(OnScrolled).AddTo(disposables);
        }

        public void AddPost(Post post)
        {
            var postView = CreatePostView(post);
            postViews.Add(postView);
            ArrangePosts();
        }

        public void ClearPosts()
        {
            postViewEventDisposables.Clear();

            foreach (var postView in postViews)
            {
                Destroy(postView.gameObject);
            }
            postViews.Clear();
            scrollOffsetY = 0f;
        }

        void ArrangePosts()
        {
            // リストの新しい順に上から隙間無く配置する
            float topY = 0f;
            float clampedOffsetY = GetClampedScrollOffsetY();
            for (int i = 0; i < postViews.Count; i++)
            {
                var postView = postViews[i];
                float y = topY - postView.Height * 0.5f + clampedOffsetY;
                postView.SetPosition(0, y);
                topY -= postView.Height;
            }

            scrollOffsetY = clampedOffsetY;
        }

        PostView CreatePostView(Post post)
        {
            PostView postView = Instantiate(postViewPrefab, timelinePostParent);
            postView.Initialize(post);
            postView.OnLikedByPlayer.Subscribe(onLikedByPlayer.OnNext).AddTo(postViewEventDisposables);
            postView.OnRepostedByPlayer.Subscribe(onRepostedByPlayer.OnNext).AddTo(postViewEventDisposables);
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
            float maxOffset = Mathf.Max(0f, contentHeight - viewportHeight);
            return Mathf.Clamp(scrollOffsetY, 0f, maxOffset);
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
            for (int i = 0; i < postViews.Count; i++)
            {
                total += postViews[i].Height;
            }

            return total;
        }

        void OnDestroy()
        {
            disposables.Dispose();
            postViewEventDisposables.Dispose();
            onLikedByPlayer.OnCompleted();
            onLikedByPlayer.Dispose();
            onRepostedByPlayer.OnCompleted();
            onRepostedByPlayer.Dispose();
        }
    }
}