using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class GamePhoneScreenView : PhoneScreenViewBase
    {
        public Observable<Post> OnDraftDroppedToPublish => publishFieldView.OnDraftDropped;
        public Observable<Account> OnPlayerAccountClicked => playerAccountListView.OnClicked;
        public Observable<Post> OnLikedByPlayer => timelineView.OnLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => timelineView.OnRepostedByPlayer;

        [SerializeField] TimelineView timelineView;
        [SerializeField] PublishFieldView publishFieldView;
        [SerializeField] PlayerAccountListView playerAccountListView;

        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);

            timelineView.Initialize();
            publishFieldView.Initialize();
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            await screenTransitionViewHub.HideAsync(ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            await screenTransitionViewHub.ShowAsync(ScreenTransitionType.CircleWipe, ct);
            gameObject.SetActive(false);
        }

        public void AddPost(Post post) => timelineView.AddPost(post);
        public void ClearPosts() => timelineView.ClearPosts();
        public void SetPlayerAccounts(IReadOnlyList<Account> accounts)
        {
            playerAccountListView.SetPlayerAccounts(accounts);
            Account initialAccount = accounts != null && accounts.Count > 0 ? accounts[0] : null;
            publishFieldView.SetCurrentPlayerAccount(initialAccount);
        }
        public void SetSelectedPlayerAccount(Account account)
        {
            playerAccountListView.SetSelectedPlayerAccount(account);
            publishFieldView.SetCurrentPlayerAccount(account);
        }
    }
}
