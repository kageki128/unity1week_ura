using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class DraftViewFactory : MonoBehaviour
    {
        [SerializeField] GameObject draftViewPrefab;
        [SerializeField] Transform draftParent;

        public DraftView Create(Post post)
        {
            var draftViewObject = Instantiate(draftViewPrefab, draftParent);
            var draftView = draftViewObject.GetComponent<DraftView>();
            draftView.Initialize(post);
            return draftView;
        }
    }
}