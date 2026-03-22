using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class FocusGameSubScreenView : PhoneScreenViewBase
    {
        public Observable<Unit> OnTimelineButtonClicked => onTimelineButtonClicked;
        public Observable<Post> OnPostClicked => onPostClicked;
        public Observable<Post> OnLikedByPlayer => focusView.OnLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => focusView.OnRepostedByPlayer;
        public Observable<Post> OnDraftDropped => focusView.OnDraftDropped;

        [SerializeField] ButtonView timelineButtonView;
        [SerializeField] FocusView focusView;

        readonly Stack<Post> focusPostStack = new();
        readonly Subject<Unit> onTimelineButtonClicked = new();
        readonly Subject<Post> onPostClicked = new();
        readonly CompositeDisposable disposables = new();
        bool isNavigating;

        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);
            disposables.Clear();
            timelineButtonView.OnClicked
                .Where(_ => !isNavigating)
                .Subscribe(onTimelineButtonClicked.OnNext)
                .AddTo(disposables);
            focusView.Initialize();
            focusView.OnPostClicked
                .Where(_ => !isNavigating)
                .Subscribe(onPostClicked.OnNext)
                .AddTo(disposables);
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            isNavigating = true;

            try
            {
                var postToShow = GetCurrentFocusedPost();
                if (postToShow != null)
                {
                    await focusView.SetupAsync(postToShow, ct);
                }

                await screenTransitionViewHub.HideAsync(ct);
            }
            finally
            {
                isNavigating = false;
            }
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            isNavigating = true;
            try
            {
                await screenTransitionViewHub.ShowAsync(ScreenTransitionType.WhiteFade, ct);
                ClearFocusStack();
                focusView.ClearPosts();
                gameObject.SetActive(false);
            }
            finally
            {
                isNavigating = false;
            }
        }

        public void SetPost(Post post)
        {
            ClearFocusStack();
            if (post != null)
            {
                focusPostStack.Push(post);
            }
        }

        public async UniTask FocusPostAsync(Post post, CancellationToken ct)
        {
            if (post == null)
            {
                return;
            }

            if (IsCurrentFocusedPost(post))
            {
                return;
            }

            focusPostStack.Push(post);

            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            await TransitionWithinFocusAsync(post, ct);
        }

        public async UniTask<bool> BackToPreviousFocusAsync(CancellationToken ct)
        {
            if (focusPostStack.Count == 0)
            {
                return false;
            }

            focusPostStack.Pop();
            var previousPost = GetCurrentFocusedPost();
            if (previousPost == null)
            {
                return false;
            }

            if (gameObject.activeInHierarchy)
            {
                await TransitionWithinFocusAsync(previousPost, ct);
            }

            return true;
        }

        public void SetCurrentPlayerAccount(Account account)
        {
            focusView.SetCurrentPlayerAccount(account);
        }

        Post GetCurrentFocusedPost()
        {
            if (!focusPostStack.TryPeek(out var currentPost))
            {
                return null;
            }

            return currentPost;
        }

        bool IsCurrentFocusedPost(Post post)
        {
            return IsSamePost(GetCurrentFocusedPost(), post);
        }

        bool IsSamePost(Post a, Post b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            return string.Equals(a.Property?.Id, b.Property?.Id, StringComparison.Ordinal);
        }

        void ClearFocusStack()
        {
            focusPostStack.Clear();
        }

        async UniTask TransitionWithinFocusAsync(Post post, CancellationToken ct)
        {
            isNavigating = true;
            try
            {
                await screenTransitionViewHub.ShowAsync(ScreenTransitionType.WhiteFade, ct);
                await focusView.SetupAsync(post, ct);
                await screenTransitionViewHub.HideAsync(ct);
            }
            finally
            {
                isNavigating = false;
            }
        }

        void OnDestroy()
        {
            disposables.Dispose();
            onTimelineButtonClicked.OnCompleted();
            onTimelineButtonClicked.Dispose();
            onPostClicked.OnCompleted();
            onPostClicked.Dispose();
        }
    }
}
