using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Infrastructure
{
    public class PostRepository : IPostRepository
    {
        const string CsvColumnId = "ID";
        const string CsvColumnAuthorAccountId = "AuthorAccountID";
        const string CsvColumnText = "Text";
        const string CsvColumnAttachedImageFileName = "AttachedImageFileName";
        const string CsvColumnParentPostId = "ParentPostID";
        const string CsvColumnDefaultLikeCount = "DefaultLikeCount";
        const string CsvColumnDefaultRepostCount = "DefaultRepostCount";

        readonly AddressableConfigSO addressableConfig;
        readonly IAccountRepository accountRepository;
        readonly AddressableSpriteLabelLoader spriteLabelLoader;
        readonly Dictionary<string, List<Post>> postsByCorrectAccountId = new(StringComparer.Ordinal);
        readonly List<Post> postsForAnyPlayer = new();
        readonly Dictionary<string, Post> postsById = new(StringComparer.Ordinal);
        readonly SemaphoreSlim loadGate = new(1, 1);

        bool isLoaded;

        sealed class PostCsvRow
        {
            public string Id { get; }
            public string AuthorAccountId { get; }
            public string Text { get; }
            public string AttachedImageFileName { get; }
            public string ParentPostId { get; }
            public int DefaultLikeCount { get; }
            public int DefaultRepostCount { get; }

            public PostCsvRow(
                string id,
                string authorAccountId,
                string text,
                string attachedImageFileName,
                string parentPostId,
                int defaultLikeCount,
                int defaultRepostCount)
            {
                Id = id;
                AuthorAccountId = authorAccountId;
                Text = text;
                AttachedImageFileName = attachedImageFileName;
                ParentPostId = parentPostId;
                DefaultLikeCount = defaultLikeCount;
                DefaultRepostCount = defaultRepostCount;
            }
        }

        sealed class ResolvedPostRow
        {
            public PostCsvRow Row { get; }
            public Account Author { get; }

            public ResolvedPostRow(PostCsvRow row, Account author)
            {
                Row = row;
                Author = author;
            }
        }

        public PostRepository(AddressableConfigSO addressableConfig, IAccountRepository accountRepository, AddressableSpriteLabelLoader spriteLabelLoader)
        {
            if (addressableConfig == null)
            {
                throw new ArgumentNullException(nameof(addressableConfig));
            }

            if (accountRepository == null)
            {
                throw new ArgumentNullException(nameof(accountRepository));
            }

            if (spriteLabelLoader == null)
            {
                throw new ArgumentNullException(nameof(spriteLabelLoader));
            }

            this.addressableConfig = addressableConfig;
            this.accountRepository = accountRepository;
            this.spriteLabelLoader = spriteLabelLoader;
        }

        public async UniTask<List<Post>> GetPostsByCorrectPlayerAccountAsync(Account playerAccount, CancellationToken ct)
        {
            if (playerAccount == null)
            {
                throw new ArgumentNullException(nameof(playerAccount));
            }

            if (string.IsNullOrEmpty(playerAccount.Id))
            {
                throw new ArgumentException("playerAccount.Id is null or empty.", nameof(playerAccount));
            }

            await EnsurePostsLoadedAsync(ct);

            var result = new List<Post>();
            if (postsByCorrectAccountId.TryGetValue(playerAccount.Id, out var posts))
            {
                result.AddRange(posts);
            }

            result.AddRange(postsForAnyPlayer);
            return result;
        }

        public async UniTask<Post> GetPost(string postId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(postId))
            {
                throw new ArgumentException("postId is null or empty.", nameof(postId));
            }

            await EnsurePostsLoadedAsync(ct);

            if (postsById.TryGetValue(postId, out var post))
            {
                return post;
            }

            throw new KeyNotFoundException($"Post not found. postId: {postId}");
        }

        public async UniTask<List<Post>> GetRepliesAsync(string postId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(postId))
            {
                throw new ArgumentException("postId is null or empty.", nameof(postId));
            }

            await EnsurePostsLoadedAsync(ct);

            var replies = new List<Post>();
            foreach (var post in postsById.Values)
            {
                if (post.Property.ParentPostId == postId)
                {
                    replies.Add(post);
                }
            }

            return replies;
        }

        public async UniTask<List<Post>> GetAncestorPostsAsync(string postId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(postId))
            {
                throw new ArgumentException("postId is null or empty.", nameof(postId));
            }

            await EnsurePostsLoadedAsync(ct);

            if (!postsById.TryGetValue(postId, out var currentPost))
            {
                throw new KeyNotFoundException($"Post not found. postId: {postId}");
            }

            var ancestors = new List<Post>();
            var visitedPostIds = new HashSet<string>(StringComparer.Ordinal);
            string parentPostId = currentPost.Property.ParentPostId;

            while (!string.IsNullOrEmpty(parentPostId))
            {
                ct.ThrowIfCancellationRequested();
                if (!visitedPostIds.Add(parentPostId))
                {
                    break;
                }

                if (!postsById.TryGetValue(parentPostId, out var parentPost))
                {
                    break;
                }

                ancestors.Add(parentPost);
                parentPostId = parentPost.Property.ParentPostId;
            }

            ancestors.Reverse();
            return ancestors;
        }

        async UniTask EnsurePostsLoadedAsync(CancellationToken ct)
        {
            if (isLoaded)
            {
                return;
            }

            await loadGate.WaitAsync(ct).AsUniTask();
            try
            {
                if (isLoaded)
                {
                    return;
                }

                if (addressableConfig.PostDatas == null || !addressableConfig.PostDatas.RuntimeKeyIsValid())
                {
                    throw new InvalidOperationException("AddressableConfigSO.PostDatas is not assigned.");
                }
                if (addressableConfig.AttachedImageLabel == null || !addressableConfig.AttachedImageLabel.RuntimeKeyIsValid())
                {
                    throw new InvalidOperationException("AddressableConfigSO.AttachedImageLabel is not assigned.");
                }

                var assetReference = addressableConfig.PostDatas;
                try
                {
                    var loadedPostsByCorrectAccountId = new Dictionary<string, List<Post>>(StringComparer.Ordinal);
                    var loadedPostsForAnyPlayer = new List<Post>();
                    var loadedPostsById = new Dictionary<string, Post>(StringComparer.Ordinal);
                    TextAsset csvAsset = await AddressableAssetLoader.LoadAsync<TextAsset>(assetReference, ct);
                    var csvText = csvAsset.text;
                    ct.ThrowIfCancellationRequested();
                    var rows = await Task.Run(() => ParsePostRows(csvText), ct);
                    var attachedImagesByFileName = await spriteLabelLoader.LoadAllByLabelAsync(addressableConfig.AttachedImageLabel, ct);
                    var accountCache = new Dictionary<string, Account>(StringComparer.Ordinal);
                    var resolvedRows = new List<ResolvedPostRow>(rows.Count);

                    async UniTask<Account> GetAccountCachedAsync(string accountId)
                    {
                        if (accountCache.TryGetValue(accountId, out var cachedAccount))
                        {
                            return cachedAccount;
                        }

                        var loadedAccount = await accountRepository.GetAccount(accountId, ct);
                        accountCache[accountId] = loadedAccount;
                        return loadedAccount;
                    }

                    foreach (var row in rows)
                    {
                        ct.ThrowIfCancellationRequested();
                        var author = await GetAccountCachedAsync(row.AuthorAccountId);
                        resolvedRows.Add(new ResolvedPostRow(row, author));
                    }

                    var authorByPostId = new Dictionary<string, Account>(StringComparer.Ordinal);
                    foreach (var resolvedRow in resolvedRows)
                    {
                        authorByPostId[resolvedRow.Row.Id] = resolvedRow.Author;
                    }

                    foreach (var resolvedRow in resolvedRows)
                    {
                        ct.ThrowIfCancellationRequested();
                        var row = resolvedRow.Row;
                        var correctPlayerAccountId = resolvedRow.Author.RelatedPlayerAccountId;
                        Account correctPlayerAccount = null;
                        if (!string.IsNullOrEmpty(correctPlayerAccountId))
                        {
                            correctPlayerAccount = await GetAccountCachedAsync(correctPlayerAccountId);
                        }

                        var attachedImage = AddressableSpriteLabelLoader.ResolveSprite(row.AttachedImageFileName, attachedImagesByFileName);
                        authorByPostId.TryGetValue(row.ParentPostId, out var parentPostAuthor);

                        var property = new PostProperty(
                            correctPlayerAccount,
                            row.Id,
                            resolvedRow.Author,
                            row.Text,
                            attachedImage,
                            row.ParentPostId,
                            parentPostAuthor);
                        var post = new Post(property, row.DefaultLikeCount, row.DefaultRepostCount, 0);

                        if (string.IsNullOrEmpty(correctPlayerAccountId))
                        {
                            loadedPostsForAnyPlayer.Add(post);
                        }
                        else
                        {
                            if (!loadedPostsByCorrectAccountId.TryGetValue(correctPlayerAccountId, out var list))
                            {
                                list = new List<Post>();
                                loadedPostsByCorrectAccountId[correctPlayerAccountId] = list;
                            }

                            list.Add(post);
                        }

                        loadedPostsById[row.Id] = post;
                    }

                    postsByCorrectAccountId.Clear();
                    foreach (var postsPair in loadedPostsByCorrectAccountId)
                    {
                        postsByCorrectAccountId[postsPair.Key] = postsPair.Value;
                    }

                    postsForAnyPlayer.Clear();
                    postsForAnyPlayer.AddRange(loadedPostsForAnyPlayer);

                    postsById.Clear();
                    foreach (var postPair in loadedPostsById)
                    {
                        postsById[postPair.Key] = postPair.Value;
                    }

                    isLoaded = true;
                }
                finally
                {
                    assetReference.ReleaseAsset();
                }
            }
            finally
            {
                loadGate.Release();
            }
        }

        List<PostCsvRow> ParsePostRows(string csvText)
        {
            if (string.IsNullOrWhiteSpace(csvText))
            {
                throw new InvalidOperationException("Posts csv is empty.");
            }

            var lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1)
            {
                throw new InvalidOperationException("Posts csv does not contain data rows.");
            }

            int idColumn = -1;
            int authorAccountIdColumn = -1;
            int textColumn = -1;
            int attachedImageFileNameColumn = -1;
            int parentPostIdColumn = -1;
            int defaultLikeCountColumn = -1;
            int defaultRepostCountColumn = -1;

            var headerColumns = lines[0].Split(',');
            for (int i = 0; i < headerColumns.Length; i++)
            {
                var column = headerColumns[i].Trim();
                if (column.Equals(CsvColumnId, StringComparison.OrdinalIgnoreCase))
                {
                    idColumn = i;
                }
                else if (column.Equals(CsvColumnAuthorAccountId, StringComparison.OrdinalIgnoreCase))
                {
                    authorAccountIdColumn = i;
                }
                else if (column.Equals(CsvColumnText, StringComparison.OrdinalIgnoreCase))
                {
                    textColumn = i;
                }
                else if (column.Equals(CsvColumnAttachedImageFileName, StringComparison.OrdinalIgnoreCase))
                {
                    attachedImageFileNameColumn = i;
                }
                else if (column.Equals(CsvColumnParentPostId, StringComparison.OrdinalIgnoreCase))
                {
                    parentPostIdColumn = i;
                }
                else if (column.Equals(CsvColumnDefaultLikeCount, StringComparison.OrdinalIgnoreCase))
                {
                    defaultLikeCountColumn = i;
                }
                else if (column.Equals(CsvColumnDefaultRepostCount, StringComparison.OrdinalIgnoreCase))
                {
                    defaultRepostCountColumn = i;
                }
            }

            if (idColumn < 0
                || authorAccountIdColumn < 0
                || textColumn < 0
                || attachedImageFileNameColumn < 0
                || parentPostIdColumn < 0
                || defaultLikeCountColumn < 0
                || defaultRepostCountColumn < 0)
            {
                throw new InvalidOperationException("Posts csv header is invalid.");
            }

            var rows = new List<PostCsvRow>(lines.Length - 1);

            for (int i = 1; i < lines.Length; i++)
            {
                var columns = lines[i].Split(',');
                int maxRequiredColumn = Math.Max(
                    idColumn,
                    Math.Max(
                        Math.Max(authorAccountIdColumn, textColumn),
                        Math.Max(
                            Math.Max(attachedImageFileNameColumn, parentPostIdColumn),
                            Math.Max(defaultLikeCountColumn, defaultRepostCountColumn))));
                if (columns.Length <= maxRequiredColumn)
                {
                    continue;
                }

                var id = columns[idColumn].Trim();
                var authorAccountId = columns[authorAccountIdColumn].Trim();
                var text = columns[textColumn].Trim();
                var attachedImageFileName = columns[attachedImageFileNameColumn].Trim();
                var parentPostId = columns[parentPostIdColumn].Trim();

                if (string.IsNullOrEmpty(id)
                    || string.IsNullOrEmpty(authorAccountId))
                {
                    continue;
                }

                if (!int.TryParse(columns[defaultLikeCountColumn].Trim(), out int defaultLikeCount))
                {
                    continue;
                }

                if (!int.TryParse(columns[defaultRepostCountColumn].Trim(), out int defaultRepostCount))
                {
                    continue;
                }

                rows.Add(new PostCsvRow(
                    id,
                    authorAccountId,
                    text,
                    attachedImageFileName,
                    parentPostId,
                    defaultLikeCount,
                    defaultRepostCount));
            }

            return rows;
        }
    }
}
