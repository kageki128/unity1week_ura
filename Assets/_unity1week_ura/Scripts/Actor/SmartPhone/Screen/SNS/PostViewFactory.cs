using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class PostViewFactory : MonoBehaviour
    {
        [SerializeField] GameObject postViewPrefab;

        public PostViewFactory(GameObject postViewPrefab)
        {
            this.postViewPrefab = postViewPrefab;
        }

        public PostView Create(Post post, Transform parent)
        {
            var postViewObject = Object.Instantiate(postViewPrefab, parent);
            var postView = postViewObject.GetComponent<PostView>();
            postView.Initialize(post);
            return postView;
        }
    }
}