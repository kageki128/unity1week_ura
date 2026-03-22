using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity1Week_Ura.Core;
using UnityEngine;
using VContainer;

namespace Unity1Week_Ura.Actor
{
    public class PostViewFactory : MonoBehaviour
    {
        [SerializeField] PostView postViewPrefab;

        IPostRepository postRepository;

        [Inject]
        public void Construct(IPostRepository postRepository)
        {
            this.postRepository = postRepository;
        }

        public PostView Create(Post post, Transform parent)
        {
            PostView postView = Instantiate(postViewPrefab, parent);
            postView.Initialize(post);
            return postView;
        }

        public async UniTask<List<PostView>> CreateRepliesAsync(Post parentPost, Transform parent, CancellationToken ct)
        {
            var replies = await postRepository.GetRepliesAsync(parentPost.Property.Id, ct);
            var postViews = new List<PostView>();
            foreach (var reply in replies)
            {
                if (reply.State != PostState.Published)
                {
                    continue;
                }

                var view = Create(reply, parent);
                postViews.Add(view);
            }
            return postViews;
        }

        public async UniTask<List<PostView>> CreateAncestorPostsAsync(Post childPost, Transform parent, CancellationToken ct)
        {
            var ancestors = await postRepository.GetAncestorPostsAsync(childPost.Property.Id, ct);

            var postViews = new List<PostView>();
            foreach (var ancestor in ancestors)
            {
                if (ancestor.State != PostState.Published)
                {
                    continue;
                }

                var view = Create(ancestor, parent);
                postViews.Add(view);
            }

            return postViews;
        }
    }
}
