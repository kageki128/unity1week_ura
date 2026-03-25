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
            Debug.Log("[U1W-DIAG][TL-001] Timeline.LoadAsync start");
            publishedPosts.Clear();
            draftPosts.Clear();

            // プレイヤーのアカウントをロード
            List<string> playerAccountIds = gameRule.UsedAccounts.Select(accountSO => accountSO.Id).ToList();
            Debug.Log($"[U1W-DIAG][TL-010] Player account load start count={playerAccountIds.Count}");
            var loadedAccounts = new List<Account>(playerAccountIds.Count);
            foreach (var accountId in playerAccountIds)
            {
                ct.ThrowIfCancellationRequested();
                loadedAccounts.Add(await accountRepository.GetAccount(accountId, ct));
            }

            Debug.Log($"[U1W-DIAG][TL-011] Player account load complete count={loadedAccounts.Count}");
            playerAccounts = loadedAccounts;
            selectedPlayerAccount.Value = playerAccounts.FirstOrDefault();

            // ポストをロード
            Debug.Log($"[U1W-DIAG][TL-020] Post load start accountCount={playerAccounts.Count}");
            var loadedPostsByAccount = new List<List<Post>>(playerAccounts.Count);
            foreach (var account in playerAccounts)
            {
                ct.ThrowIfCancellationRequested();
                loadedPostsByAccount.Add(await postRepository.GetPostsByCorrectPlayerAccountAsync(account, ct));
            }

            Debug.Log($"[U1W-DIAG][TL-021] Post load complete bucketCount={loadedPostsByAccount.Count}");
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

            Debug.Log($"[U1W-DIAG][TL-030] Post split complete normal={beforeAppearingNormalPosts.Count} advertise={advertisePosts.Count}");

            foreach (var post in beforeAppearingNormalPosts)
            {
                post.ResetPlayerAction();
            }

            foreach (var post in advertisePosts)
            {
                post.ResetPlayerAction();
            }

            Debug.Log("[U1W-DIAG][TL-040] Timeline.LoadAsync complete");
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

            _ = TrySupplyPostInternal(gameRule);
        }

        public bool TrySupplyPostGuaranteed(GameRuleSO gameRule)
        {
            if (beforeAppearingNormalPosts.Count == 0 && advertisePosts.Count == 0)
            {
                return false;
            }

            return TrySupplyPostInternal(gameRule);
        }

        bool TrySupplyPostInternal(GameRuleSO gameRule)
        {
            if (beforeAppearingNormalPosts.Count == 0 && advertisePosts.Count == 0)
            {
                return false;
            }

            bool useAdvertisePost = advertisePosts.Count > 0
                && Random.value < Mathf.Clamp01(gameRule.AdvertisePostProbability);
            if (useAdvertisePost)
            {
                if (TrySupplyPostFrom(advertisePosts, false))
                {
                    return true;
                }

                return TrySupplyPostFrom(beforeAppearingNormalPosts, true);
            }

            if (TrySupplyPostFrom(beforeAppearingNormalPosts, true))
            {
                return true;
            }

            return false;
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

            List<int> replyPostIndexes = canAppearPostIndexes
                .Where(index => !string.IsNullOrEmpty(sourcePosts[index].Property.ParentPostId))
                .ToList();
            if (replyPostIndexes.Count > 0)
            {
                canAppearPostIndexes = replyPostIndexes;
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

        public bool TryPublishDraft(Post post, Post focusedPost = null)
        {
            if (!draftPosts.Contains(post))
            {
                throw new System.ArgumentException("指定されたPostはドラフトの中に存在しません。", nameof(post));   
            }

            Account currentAccount = selectedPlayerAccount.CurrentValue;
            bool isPlayerAccountCorrect = IsPlayerAccountCorrectForPost(currentAccount, post);
            bool shouldOverrideReplyTarget = CanOverrideReplyTarget(post, focusedPost);
            bool isCorrectAction = isPlayerAccountCorrect && !shouldOverrideReplyTarget;

            draftPosts.Remove(post);
            var postToPublish = isCorrectAction
                ? post
                : BuildPublishedPostForCurrentAction(post, currentAccount, focusedPost);
            postToPublish.ChangeState(PostState.Published);
            publishedPosts.Add(postToPublish);

            return isCorrectAction;
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

        static bool CanOverrideReplyTarget(Post post, Post focusedPost)
        {
            if (post?.Type != PostType.Reply)
            {
                return false;
            }

            var focusedPostId = focusedPost?.Property?.Id;
            if (string.IsNullOrEmpty(focusedPostId))
            {
                return false;
            }

            return !string.Equals(post.Property.ParentPostId, focusedPostId, System.StringComparison.Ordinal);
        }

        static Post BuildPublishedPostForCurrentAction(Post draftPost, Account currentAccount, Post focusedPost)
        {
            if (draftPost == null)
            {
                throw new System.ArgumentNullException(nameof(draftPost));
            }

            var originalProperty = draftPost.Property;
            var publishedAuthor = currentAccount ?? originalProperty.Author;

            var parentPostId = originalProperty.ParentPostId;
            var parentPostAuthor = originalProperty.ParentPostAuthor;
            if (CanOverrideReplyTarget(draftPost, focusedPost))
            {
                parentPostId = focusedPost.Property.Id;
                parentPostAuthor = focusedPost.Property.Author;
            }

            var publishedProperty = new PostProperty(
                originalProperty.CorrectPlayerAccount,
                originalProperty.Id,
                publishedAuthor,
                originalProperty.Text,
                originalProperty.AttachedImage,
                parentPostId,
                parentPostAuthor
            );

            return new Post(
                publishedProperty,
                draftPost.LikeCount,
                draftPost.RepostCount,
                draftPost.ReplyCount
            );
        }
    }
}
