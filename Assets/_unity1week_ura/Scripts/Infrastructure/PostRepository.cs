using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Infrastructure
{
    public class PostRepository : IPostRepository
    {
        const string CsvColumnCorrectPlayerAccount = "CorrectPlayerAccount";
        const string CsvColumnId = "ID";
        const string CsvColumnAuthorAccountId = "AuthorAccountID";
        const string CsvColumnText = "Text";
        const string CsvColumnAttachedImageFileName = "AttachedImageFileName";
        const string CsvColumnParentPostId = "ParentPostID";
        const string CsvColumnPostType = "PostType";
        const string CsvColumnPublishPoint = "PublishPoint";
        const string CsvColumnLikePoint = "LikePoint";
        const string CsvColumnRepostPoint = "RepostPoint";
        const string CsvColumnDefaultLikeCount = "DefaultLikeCount";
        const string CsvColumnDefaultRepostCount = "DefaultRepostCount";
        const string CsvColumnWrongTextPrefix = "WrongText";

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
            public int PublishPoint { get; }
            public int LikePoint { get; }
            public int RepostPoint { get; }
            public List<string> WrongTexts { get; }
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
                int publishPoint,
                int likePoint,
                int repostPoint,
                List<string> wrongTexts,
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
                PublishPoint = publishPoint;
                LikePoint = likePoint;
                RepostPoint = repostPoint;
                WrongTexts = wrongTexts;
                DefaultLikeCount = defaultLikeCount;
                DefaultRepostCount = defaultRepostCount;
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
                    var rows = ParsePostRows(csvAsset.text);
                    var spritesByFileName = await spriteLabelLoader.LoadAllByLabelAsync(addressableConfig.IconLabel, ct);

                    foreach (var row in rows)
                    {
                        ct.ThrowIfCancellationRequested();
                        var correctPlayerAccount = await accountRepository.GetAccount(row.CorrectPlayerAccountId, ct);
                        var author = await accountRepository.GetAccount(row.AuthorAccountId, ct);
                        var attachedImage = AddressableSpriteLabelLoader.ResolveSprite(row.AttachedImageFileName, spritesByFileName);

                        var property = new PostProperty(
                            correctPlayerAccount,
                            row.Id,
                            author,
                            row.Text,
                            attachedImage,
                            row.ParentPostId,
                            row.Type);
                        var scoreInfo = new PostScoreInfo(row.PublishPoint, row.LikePoint, row.RepostPoint, row.WrongTexts);
                        var post = new Post(property, scoreInfo, row.DefaultLikeCount, row.DefaultRepostCount, 0);

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
            int publishPointColumn = -1;
            int likePointColumn = -1;
            int repostPointColumn = -1;
            int defaultLikeCountColumn = -1;
            int defaultRepostCountColumn = -1;
            var wrongTextColumns = new List<int>();

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
                else if (column.Equals(CsvColumnPublishPoint, StringComparison.OrdinalIgnoreCase))
                {
                    publishPointColumn = i;
                }
                else if (column.Equals(CsvColumnLikePoint, StringComparison.OrdinalIgnoreCase))
                {
                    likePointColumn = i;
                }
                else if (column.Equals(CsvColumnRepostPoint, StringComparison.OrdinalIgnoreCase))
                {
                    repostPointColumn = i;
                }
                else if (column.Equals(CsvColumnDefaultLikeCount, StringComparison.OrdinalIgnoreCase))
                {
                    defaultLikeCountColumn = i;
                }
                else if (column.Equals(CsvColumnDefaultRepostCount, StringComparison.OrdinalIgnoreCase))
                {
                    defaultRepostCountColumn = i;
                }
                else if (column.StartsWith(CsvColumnWrongTextPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    wrongTextColumns.Add(i);
                }
            }

            if (correctPlayerAccountColumn < 0
                || idColumn < 0
                || authorAccountIdColumn < 0
                || textColumn < 0
                || attachedImageFileNameColumn < 0
                || parentPostIdColumn < 0
                || postTypeColumn < 0
                || publishPointColumn < 0
                || likePointColumn < 0
                || repostPointColumn < 0
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
                                Math.Max(postTypeColumn, publishPointColumn),
                                Math.Max(
                                    Math.Max(likePointColumn, repostPointColumn),
                                    Math.Max(defaultLikeCountColumn, defaultRepostCountColumn))))));
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

                if (!int.TryParse(columns[publishPointColumn].Trim(), out int publishPoint))
                {
                    continue;
                }

                if (!int.TryParse(columns[likePointColumn].Trim(), out int likePoint))
                {
                    continue;
                }

                if (!int.TryParse(columns[repostPointColumn].Trim(), out int repostPoint))
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

                var wrongTexts = new List<string>();
                foreach (int columnIndex in wrongTextColumns)
                {
                    if (columns.Length <= columnIndex)
                    {
                        continue;
                    }

                    var wrongText = columns[columnIndex].Trim();
                    if (string.IsNullOrEmpty(wrongText))
                    {
                        continue;
                    }

                    wrongTexts.Add(wrongText);
                }

                rows.Add(new PostCsvRow(
                    correctPlayerAccountId,
                    id,
                    authorAccountId,
                    text,
                    attachedImageFileName,
                    parentPostId,
                    type,
                    publishPoint,
                    likePoint,
                    repostPoint,
                    wrongTexts,
                    defaultLikeCount,
                        defaultRepostCount));
            }

            return rows;
        }
    }
}
