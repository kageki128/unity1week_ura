using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using ObservableCollections;
using UnityEngine;

namespace Unity1Week_Ura.Core
{
    public class Timeline
    {
        public IReadOnlyList<Account> PlayerAccounts => playerAccounts;
        IReadOnlyList<Account> playerAccounts = new List<Account>();

        public IReadOnlyObservableList<Post> PublishedPosts => publishedPosts;
        readonly ObservableList<Post> publishedPosts = new();

        public IReadOnlyObservableList<Post> DraftPosts => draftPosts;
        readonly ObservableList<Post> draftPosts = new();

        List<Post> beforeAppearingPosts = new();

        readonly IAccountRepository accountRepository;
        readonly IPostRepository postRepository;

        public Timeline(IAccountRepository accountRepository, IPostRepository postRepository)
        {
            this.accountRepository = accountRepository;
            this.postRepository = postRepository;
        }

        public async UniTask LoadAsync(GameRuleSO gameRule, CancellationToken ct)
        {
            List<string> playerAccountIds = gameRule.UsedAccounts.Select(accountSO => accountSO.Id).ToList();
            Account[] loadedAccounts = await UniTask.WhenAll(
                playerAccountIds.Select(accountId => accountRepository.GetAccount(accountId, ct))
            );
            playerAccounts = loadedAccounts;

            List<Post>[] loadedPostsByAccount = await UniTask.WhenAll(
                playerAccounts.Select(account => postRepository.GetPostsByCorrectPlayerAccountAsync(account, ct))
            );
            beforeAppearingPosts = loadedPostsByAccount.SelectMany(posts => posts).ToList();
            publishedPosts.Clear();
            draftPosts.Clear();

            foreach (Post post in beforeAppearingPosts)
            {
                Debug.Log($"[Timeline] ロードしたポスト: {post.Property.Id} by {post.Property.Author.Name}");
            }
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