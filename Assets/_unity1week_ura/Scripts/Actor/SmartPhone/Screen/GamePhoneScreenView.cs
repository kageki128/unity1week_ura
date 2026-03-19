using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class GamePhoneScreenView : ViewBase
    {
        public Observable<Post> OnDraftDroppedToPublish => publishFieldView.OnDraftDropped;

        [SerializeField] TimelineView timelineView;
        [SerializeField] PublishFieldView publishFieldView;

        public override void Initialize()
        {
            publishFieldView.Initialize();
            gameObject.SetActive(false);
        }

        public override UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync(CancellationToken ct)
        {
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        public void AddPost(Post post) => timelineView.AddPost(post);
        public void ClearPosts() => timelineView.ClearPosts();
    }
}
