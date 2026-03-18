using System.Collections.Generic;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class TimelineView : MonoBehaviour
    {
        [SerializeField] Transform postParent;
        [SerializeField] PostViewFactory postViewFactory;

        readonly List<PostView> postViews = new();

        public void Initialize(PostViewFactory postViewFactory)
        {
            this.postViewFactory = postViewFactory;
            ClearPosts();
        }

        public void AddPost(Post post)
        {
            var postView = postViewFactory.Create(post, postParent);
            postViews.Add(postView);
            ArrangePosts();
        }

        void ArrangePosts()
        {
            // リストの新しい順に上から隙間無く配置する
            for (int i = 0; i < postViews.Count; i++)
            {
                float y = -i * postViews[i].Height;
                postViews[i].SetPosition(0, y);
            }
        }

        void ClearPosts()
        {
            foreach (var postView in postViews)
            {
                Destroy(postView.gameObject);
            }
            postViews.Clear();
        }
    }
}