using System.Collections.Generic;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class TimelineView : MonoBehaviour
    {
        [SerializeField] PostView postViewPrefab;
        [SerializeField] Transform timelinePostParent;

        readonly List<PostView> postViews = new();

        public void AddPost(Post post)
        {
            var postView = CreatePostView(post);
            postViews.Add(postView);
            ArrangePosts();
        }

        public void ClearPosts()
        {
            foreach (var postView in postViews)
            {
                Destroy(postView.gameObject);
            }
            postViews.Clear();
        }

        void ArrangePosts()
        {
            // リストの新しい順に上から隙間無く配置する
            float topY = 0f;
            for (int i = 0; i < postViews.Count; i++)
            {
                var postView = postViews[i];
                float y = topY - postView.Height * 0.5f;
                postView.SetPosition(0, y);
                topY -= postView.Height;
            }
        }

        PostView CreatePostView(Post post)
        {
            PostView postView = Instantiate(postViewPrefab, timelinePostParent);
            postView.Initialize(post);
            return postView;
        }
    }
}