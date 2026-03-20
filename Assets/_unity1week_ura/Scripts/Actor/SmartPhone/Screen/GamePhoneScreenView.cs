using System.Collections.Generic;
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
        public Observable<Account> OnPlayerAccountClicked => playerAccountListView.OnClicked;

        [SerializeField] TimelineView timelineView;
        [SerializeField] PublishFieldView publishFieldView;
        [SerializeField] PlayerAccountListView playerAccountListView;

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
        public void SetPlayerAccounts(IReadOnlyList<Account> accounts) => playerAccountListView.SetPlayerAccounts(accounts);
        public void SetSelectedPlayerAccount(Account account) => playerAccountListView.SetSelectedPlayerAccount(account);
    }
}
