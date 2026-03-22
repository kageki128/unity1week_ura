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

        List<Post> beforeAppearingNormalPosts = new(new List<Post>());
        List<Post> advertisePosts = new(new List<Post>());

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
            var loadedPostIds = new HashSet<string>(System.StringComparer.Ordinal);
            var mergedPosts = new List<Post>();
            foreach (var posts in loadedPostsByAccount)
            {
                foreach (var post in posts)
                {
                    if (!loadedPostIds.Add(post.Property.Id))
                    {
                        continue;
                    }

                    mergedPosts.Add(post);
                }
            }

            beforeAppearingNormalPosts = mergedPosts
                .Where(post => post.Property.Author.Type != AccountType.Advertise)
                .ToList();
            advertisePosts = mergedPosts
                .Where(post => post.Property.Author.Type == AccountType.Advertise)
                .ToList();

            foreach (var post in beforeAppearingNormalPosts)
            {
                post.ResetPlayerAction();
            }

            foreach (var post in advertisePosts)
            {
                post.ResetPlayerAction();
            }
        }

        public void TrySupplyPost(GameRuleSO gameRule, float deltaTime)
        {
            if (beforeAppearingNormalPosts.Count == 0 && advertisePosts.Count == 0)
            {
                return;
            }

            float supplyProbability = Mathf.Clamp01(gameRule.PostPerSecond * deltaTime);
            if (Random.value > supplyProbability)
            {
                return;
            }

            bool useAdvertisePost = advertisePosts.Count > 0
                && Random.value < Mathf.Clamp01(gameRule.AdvertisePostProbability);
            if (useAdvertisePost)
            {
                if (TrySupplyPostFrom(advertisePosts, false))
                {
                    return;
                }

                _ = TrySupplyPostFrom(beforeAppearingNormalPosts, true);
                return;
            }

            if (TrySupplyPostFrom(beforeAppearingNormalPosts, true))
            {
                return;
            }

            _ = TrySupplyPostFrom(advertisePosts, false);
        }

        bool TrySupplyPostFrom(List<Post> sourcePosts, bool removeAfterSupply)
        {
            if (sourcePosts.Count == 0)
            {
                return false;
            }

            List<int> canAppearPostIndexes = sourcePosts
                .Select((post, index) => new { post, index })
                .Where(x => CanAppear(x.post))
                .Select(x => x.index)
                .ToList();

            if (canAppearPostIndexes.Count == 0)
            {
                return false;
            }

            int appearedPostIndex = canAppearPostIndexes[Random.Range(0, canAppearPostIndexes.Count)];
            Post appearedPost = sourcePosts[appearedPostIndex];
            if (removeAfterSupply)
            {
                sourcePosts.RemoveAt(appearedPostIndex);
            }

            SupplyPost(appearedPost);
            return true;
        }

        void SupplyPost(Post appearedPost)
        {
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
            return IsPlayerAccountCorrectForPost(currentAccount, post);
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

        public bool IsCurrentPlayerAccountCorrectForAction(Post post)
        {
            if (post == null)
            {
                return false;
            }

            Account currentAccount = selectedPlayerAccount.CurrentValue;
            if (currentAccount == null)
            {
                return false;
            }

            return IsPlayerAccountCorrectForPost(currentAccount, post);
        }

        public void AddRepostToTimeline(Post originalPost)
        {
            if (originalPost == null)
            {
                throw new System.ArgumentNullException(nameof(originalPost));
            }

            Account currentAccount = selectedPlayerAccount.CurrentValue;
            if (currentAccount == null)
            {
                throw new System.InvalidOperationException("現在のプレイヤーアカウントが選択されていません。");
            }

            originalPost.MarkAsRepost(currentAccount);
            publishedPosts.Add(originalPost);
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

        static bool IsPlayerAccountCorrectForPost(Account playerAccount, Post post)
        {
            if (playerAccount == null || post == null)
            {
                return false;
            }

            if (IsRelatedToAnyPlayer(post))
            {
                return true;
            }

            var correctPlayerAccount = post.Property.CorrectPlayerAccount;
            if (correctPlayerAccount == null || string.IsNullOrEmpty(correctPlayerAccount.Id))
            {
                return false;
            }

            return correctPlayerAccount.Id == playerAccount.Id;
        }

        static bool IsRelatedToAnyPlayer(Post post)
        {
            if (post?.Property?.Author == null)
            {
                return false;
            }

            return string.IsNullOrEmpty(post.Property.Author.RelatedPlayerAccountId);
        }
    }
}
