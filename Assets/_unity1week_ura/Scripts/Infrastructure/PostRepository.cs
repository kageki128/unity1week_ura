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
        const string CsvColumnCorrectPlayerAccount = "CorrectPlayerAccountID";
        const string CsvColumnId = "ID";
        const string CsvColumnAuthorAccountId = "AuthorAccountID";
        const string CsvColumnText = "Text";
        const string CsvColumnAttachedImageFileName = "AttachedImageFileName";
        const string CsvColumnParentPostId = "ParentPostID";
        const string CsvColumnPostType = "Type";
        const string CsvColumnDefaultLikeCount = "DefaultLikeCount";
        const string CsvColumnDefaultRepostCount = "DefaultRepostCount";

        readonly AddressableConfigSO addressableConfig;
        readonly IAccountRepository accountRepository;
        readonly AddressableSpriteLabelLoader spriteLabelLoader;
        readonly Dictionary<string, List<Post>> postsByCorrectAccountId = new(StringComparer.Ordinal);
        readonly Dictionary<string, Post> postsById = new(StringComparer.Ordinal);
        readonly SemaphoreSlim loadGate = new(1, 1);

        bool isLoaded;

        sealed class PostCsvRow
        {
            public string CorrectPlayerAccountId { get; }
            public string Id { get; }
            public string AuthorAccountId { get; }
            public string Text { get; }
            public string AttachedImageFileName { get; }
            public string ParentPostId { get; }
            public PostType Type { get; }
            public int DefaultLikeCount { get; }
            public int DefaultRepostCount { get; }

            public PostCsvRow(
                string correctPlayerAccountId,
                string id,
                string authorAccountId,
                string text,
                string attachedImageFileName,
                string parentPostId,
                PostType type,
                int defaultLikeCount,
                int defaultRepostCount)
            {
                CorrectPlayerAccountId = correctPlayerAccountId;
                Id = id;
                AuthorAccountId = authorAccountId;
                Text = text;
                AttachedImageFileName = attachedImageFileName;
                ParentPostId = parentPostId;
                Type = type;
                DefaultLikeCount = defaultLikeCount;
                DefaultRepostCount = defaultRepostCount;
            }
        }

        sealed class ResolvedPostRow
        {
            public PostCsvRow Row { get; }
            public Account CorrectPlayerAccount { get; }
            public Account Author { get; }

            public ResolvedPostRow(PostCsvRow row, Account correctPlayerAccount, Account author)
            {
                Row = row;
                CorrectPlayerAccount = correctPlayerAccount;
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

            if (!postsByCorrectAccountId.TryGetValue(playerAccount.Id, out var posts))
            {
                return new List<Post>();
            }

            return new List<Post>(posts);
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

            // リプライはID順、もしくは投稿日時順（今回はPostに日時がなければ元の要素順など）でソートするのがよいですが、
            // ひとまずCSV順（postsById.Valuesの列挙順）とします。

            return replies;
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

                var assetReference = addressableConfig.PostDatas;
                try
                {
                    TextAsset csvAsset = await AddressableAssetLoader.LoadAsync<TextAsset>(assetReference, ct);
                    var csvText = csvAsset.text;
                    ct.ThrowIfCancellationRequested();
                    var rows = await Task.Run(() => ParsePostRows(csvText), ct);
                    var spritesByFileName = await spriteLabelLoader.LoadAllByLabelAsync(addressableConfig.IconLabel, ct);
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
                        var correctPlayerAccount = await GetAccountCachedAsync(row.CorrectPlayerAccountId);
                        var author = await GetAccountCachedAsync(row.AuthorAccountId);
                        resolvedRows.Add(new ResolvedPostRow(row, correctPlayerAccount, author));
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
                        var attachedImage = AddressableSpriteLabelLoader.ResolveSprite(row.AttachedImageFileName, spritesByFileName);
                        authorByPostId.TryGetValue(row.ParentPostId, out var parentPostAuthor);

                        var property = new PostProperty(
                            resolvedRow.CorrectPlayerAccount,
                            row.Id,
                            resolvedRow.Author,
                            row.Text,
                            attachedImage,
                            row.ParentPostId,
                            parentPostAuthor,
                            row.Type);
                        var post = new Post(property, row.DefaultLikeCount, row.DefaultRepostCount, 0);

                        if (!postsByCorrectAccountId.TryGetValue(row.CorrectPlayerAccountId, out var list))
                        {
                            list = new List<Post>();
                            postsByCorrectAccountId[row.CorrectPlayerAccountId] = list;
                        }

                        list.Add(post);
                        postsById[row.Id] = post;
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

            int correctPlayerAccountColumn = -1;
            int idColumn = -1;
            int authorAccountIdColumn = -1;
            int textColumn = -1;
            int attachedImageFileNameColumn = -1;
            int parentPostIdColumn = -1;
            int postTypeColumn = -1;
            int defaultLikeCountColumn = -1;
            int defaultRepostCountColumn = -1;

            var headerColumns = lines[0].Split(',');
            for (int i = 0; i < headerColumns.Length; i++)
            {
                var column = headerColumns[i].Trim();
                if (column.Equals(CsvColumnCorrectPlayerAccount, StringComparison.OrdinalIgnoreCase))
                {
                    correctPlayerAccountColumn = i;
                }
                else if (column.Equals(CsvColumnId, StringComparison.OrdinalIgnoreCase))
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
                else if (column.Equals(CsvColumnPostType, StringComparison.OrdinalIgnoreCase))
                {
                    postTypeColumn = i;
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

            if (correctPlayerAccountColumn < 0
                || idColumn < 0
                || authorAccountIdColumn < 0
                || textColumn < 0
                || attachedImageFileNameColumn < 0
                || parentPostIdColumn < 0
                || postTypeColumn < 0
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
                    Math.Max(correctPlayerAccountColumn, idColumn),
                    Math.Max(
                        Math.Max(authorAccountIdColumn, textColumn),
                        Math.Max(
                            Math.Max(attachedImageFileNameColumn, parentPostIdColumn),
                            Math.Max(
                                postTypeColumn,
                                Math.Max(defaultLikeCountColumn, defaultRepostCountColumn)))));
                if (columns.Length <= maxRequiredColumn)
                {
                    continue;
                }

                var correctPlayerAccountId = columns[correctPlayerAccountColumn].Trim();
                var id = columns[idColumn].Trim();
                var authorAccountId = columns[authorAccountIdColumn].Trim();
                var text = columns[textColumn].Trim();
                var attachedImageFileName = columns[attachedImageFileNameColumn].Trim();
                var parentPostId = columns[parentPostIdColumn].Trim();
                var postTypeText = columns[postTypeColumn].Trim();

                if (string.IsNullOrEmpty(correctPlayerAccountId)
                    || string.IsNullOrEmpty(id)
                    || string.IsNullOrEmpty(authorAccountId)
                    || string.IsNullOrEmpty(postTypeText))
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

                if (!Enum.TryParse(postTypeText, true, out PostType type))
                {
                    continue;
                }

                rows.Add(new PostCsvRow(
                    correctPlayerAccountId,
                    id,
                    authorAccountId,
                    text,
                    attachedImageFileName,
                    parentPostId,
                    type,
                    defaultLikeCount,
                    defaultRepostCount));
            }

            return rows;
        }
    }
}
