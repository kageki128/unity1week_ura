using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class GameViewHub : ViewBase
    {
        public Observable<Post> OnDraftDroppedToPublish => smartPhoneView.OnDraftDroppedToPublish;
        public Observable<Account> OnPlayerAccountClicked => smartPhoneView.OnPlayerAccountClicked;
        
        [SerializeField] SmartPhoneView smartPhoneView;
        [SerializeField] DraftListView draftListView;
        [SerializeField] ScoreView scoreView;
        [SerializeField] RemainingTimeView remainingTimeView;

        public override void Initialize()
        {
            smartPhoneView.Initialize();
            remainingTimeView.Initialize();
            scoreView.Initialize();
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {

            await smartPhoneView.ShowSceneAsync(SceneType.Game, ct);
            await scoreView.ShowAsync(ct);
            await remainingTimeView.ShowAsync(ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            await smartPhoneView.HideSceneAsync(SceneType.Game, ct);
            await scoreView.HideAsync(ct);
            await remainingTimeView.HideAsync(ct);
        }

        public void AddPostToTimeline(Post post) => smartPhoneView.AddPostToTimeline(post);
        public void ClearTimeline() => smartPhoneView.ClearTimeline();
        public void SetPlayerAccounts(IReadOnlyList<Account> accounts) => smartPhoneView.SetPlayerAccounts(accounts);
        public void SetSelectedPlayerAccount(Account account) => smartPhoneView.SetSelectedPlayerAccount(account);

        public void AddDraft(Post post) => draftListView.AddDraft(post);
        public void RemoveDraft(Post post) => draftListView.RemoveDraft(post);
        public void ClearDrafts() => draftListView.ClearDrafts();

        public void SetScore(int score) => scoreView.SetScore(score);
        public void SetRemainingTime(float remainingTime) => remainingTimeView.SetRemainingTime(remainingTime);
    }
}