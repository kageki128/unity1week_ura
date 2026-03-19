using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class PostViewFactory : MonoBehaviour
    {
        [SerializeField] GameObject postViewPrefab;
        [SerializeField] Transform timelinePostParent;

        public PostViewFactory(GameObject postViewPrefab)
        {
            this.postViewPrefab = postViewPrefab;
        }

        public PostView Create(Post post)
        {
            var postViewObject = Instantiate(postViewPrefab, timelinePostParent);
            var postView = postViewObject.GetComponent<PostView>();
            postView.Initialize(post);
            return postView;
        }
    }
}