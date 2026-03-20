using System.Collections.Generic;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class DraftListView : MonoBehaviour
    {
        [SerializeField] DraftView draftViewPrefab;
        [SerializeField] Transform draftParent;

        readonly List<DraftView> draftViews = new();

        public void AddDraft(Post post)
        {
            var draftView = CreateDraftView(post);
            draftViews.Add(draftView);
            ArrangeDrafts();
        }

        public void RemoveDraft(Post post)
        {
            var draftView = draftViews.Find(view => view.post == post);
            if (draftView != null)
            {
                draftViews.Remove(draftView);
                Destroy(draftView.gameObject);
                ArrangeDrafts();
            }
        }

        public void ClearDrafts()
        {
            foreach (var draftView in draftViews)
            {
                Destroy(draftView.gameObject);
            }
            draftViews.Clear();
        }

        void ArrangeDrafts()
        {
            // リストの新しい順に上から隙間無く配置する
            for (int i = 0; i < draftViews.Count; i++)
            {
                float y = -i * draftViews[i].Height;
                draftViews[i].SetPosition(0, y);
            }
        }

        DraftView CreateDraftView(Post post)
        {
            DraftView draftView = Instantiate(draftViewPrefab, draftParent);
            draftView.Initialize(post);
            return draftView;
        }
    }
}