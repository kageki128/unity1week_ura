using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using ObservableCollections;
using R3;
using UnityEngine;

namespace Unity1Week_Ura.Core
{
    public class Timeline
    {
        public IReadOnlyList<Account> PlayerAccounts => playerAccounts;
        IReadOnlyList<Account> playerAccounts = new List<Account>();

        public ReadOnlyReactiveProperty<Account> SelectedPlayerAccount => selectedPlayerAccount;
        readonly ReactiveProperty<Account> selectedPlayerAccount = new();

        public IReadOnlyObservableList<Post> PublishedPosts => publishedPosts;
        readonly ObservableList<Post> publishedPosts = new(new List<Post>());

        public IReadOnlyObservableList<Post> DraftPosts => draftPosts;
        readonly ObservableList<Post> draftPosts = new(new List<Post>());

        List<Post> beforeAppearingPosts = new(new List<Post>());

        readonly IAccountRepository accountRepository;
        readonly IPostRepository postRepository;

        public Timeline(IAccountRepository accountRepository, IPostRepository postRepository)
        {
            this.accountRepository = accountRepository;
            this.postRepository = postRepository;
        }

        public async UniTask LoadAsync(GameRuleSO gameRule, CancellationToken ct)
        {
            publishedPosts.Clear();
            draftPosts.Clear();

            // プレイヤーのアカウントをロード
            List<string> playerAccountIds = gameRule.UsedAccounts.Select(accountSO => accountSO.Id).ToList();
            Account[] loadedAccounts = await UniTask.WhenAll(
                playerAccountIds.Select(accountId => accountRepository.GetAccount(accountId, ct))
            );
            playerAccounts = loadedAccounts;
            selectedPlayerAccount.Value = playerAccounts.FirstOrDefault();

            // ポストをロード
            List<Post>[] loadedPostsByAccount = await UniTask.WhenAll(
                playerAccounts.Select(account => postRepository.GetPostsByCorrectPlayerAccountAsync(account, ct))
            );
            beforeAppearingPosts = loadedPostsByAccount.SelectMany(posts => posts).ToList();
        }

        public void TrySupplyPost(GameRuleSO gameRule, float deltaTime)
        {
            if (beforeAppearingPosts.Count == 0)
            {
                return;
            }

            float supplyProbability = Mathf.Clamp01(gameRule.PostPerSecond * deltaTime);
            if (Random.value > supplyProbability)
            {
                return;
            }

            List<int> canAppearPostIndexes = beforeAppearingPosts
                .Select((post, index) => new { post, index })
                .Where(x => CanAppear(x.post))
                .Select(x => x.index)
                .ToList();

            if (canAppearPostIndexes.Count == 0)
            {
                return;
            }

            int appearedPostIndex = canAppearPostIndexes[Random.Range(0, canAppearPostIndexes.Count)];
            Post appearedPost = beforeAppearingPosts[appearedPostIndex];
            beforeAppearingPosts.RemoveAt(appearedPostIndex);

            bool isPlayerAccountPost = playerAccounts.Any(account => account.Id == appearedPost.Property.Author.Id);
            if (isPlayerAccountPost)
            {
                appearedPost.ChangeState(PostState.Draft);
                draftPosts.Add(appearedPost);
                return;
            }

            appearedPost.ChangeState(PostState.Published);
            publishedPosts.Add(appearedPost);
        }

        public bool TryPublishDraft(Post post)
        {
            if (!draftPosts.Contains(post))
            {
                throw new System.ArgumentException("指定されたPostはドラフトの中に存在しません。", nameof(post));   
            }

            draftPosts.Remove(post);
            post.ChangeState(PostState.Published);
            publishedPosts.Add(post);

            // 正誤判定
            Account currentAccount = selectedPlayerAccount.CurrentValue;
            return post.Property.CorrectPlayerAccount.Id == currentAccount.Id;
        }

        public void SetCurrentPlayerAccount(Account account)
        {
            bool canSelect = playerAccounts.Any(playerAccount => playerAccount.Id == account.Id);
            if (!canSelect)
            {
                throw new System.ArgumentException("指定されたアカウントはプレイヤーの使用可能アカウントではありません。", nameof(account));
            }

            selectedPlayerAccount.Value = account;
        }

        bool CanAppear(Post post)
        {
            string parentPostId = post.Property.ParentPostId;
            if (string.IsNullOrEmpty(parentPostId))
            {
                return true;
            }

            return publishedPosts.Any(p => p.Property.Id == parentPostId && p.State == PostState.Published);
        }
    }
}