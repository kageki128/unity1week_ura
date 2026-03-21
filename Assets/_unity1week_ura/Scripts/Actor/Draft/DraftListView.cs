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
            draftViews.Insert(0, draftView);
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
            // 先頭要素の上端が原点になるように上から隙間無く配置する
            float topY = 0f;
            for (int i = 0; i < draftViews.Count; i++)
            {
                var draftView = draftViews[i];
                if (draftView == null)
                {
                    continue;
                }

                float y = topY - draftView.Height * 0.5f;
                draftView.SetReturnPosition(0, y);
                if (!draftView.IsDragging)
                {
                    draftView.SetPosition(0, y);
                }
                topY -= draftView.Height;
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