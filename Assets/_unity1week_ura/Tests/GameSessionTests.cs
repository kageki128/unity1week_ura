using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Tests
{
    public class GameSessionTests
    {
        [Test]
        public void LikePostByPlayer_AnyPlayerRelatedNormalPost_CanScoreWithAnyAccount()
        {
            var publicAccount = new Account("Public", "Public", null, AccountType.Normal, "Public");
            var selfieAccount = new Account("Selfie", "Selfie", null, AccountType.Normal, "Selfie");
            var anyRelatedAuthor = new Account("npc", "Npc", null, AccountType.Normal, string.Empty);

            var session = CreateSession(
                CreateGameRule("Public", "Selfie"),
                new Dictionary<string, Account>(StringComparer.Ordinal)
                {
                    { publicAccount.Id, publicAccount },
                    { selfieAccount.Id, selfieAccount },
                    { anyRelatedAuthor.Id, anyRelatedAuthor }
                },
                new Dictionary<string, List<Post>>(StringComparer.Ordinal));

            session.LoadNewGame(default).GetAwaiter().GetResult();
            session.Play();
            session.SetCurrentPlayerAccount(selfieAccount);

            var post = CreatePost("p-like-any", anyRelatedAuthor, null, null);
            session.LikePostByPlayer(post);

            Assert.That(GetReactiveCurrentValue<int>(session, "Score"), Is.EqualTo(10));
            Assert.That(GetReactiveCurrentValue<GameState>(session, "CurrentGameState"), Is.EqualTo(GameState.Playing));
        }

        [Test]
        public void LikePostByPlayer_AdvertisePost_SubtractsLikePoint()
        {
            var publicAccount = new Account("Public", "Public", null, AccountType.Normal, "Public");
            var selfieAccount = new Account("Selfie", "Selfie", null, AccountType.Normal, "Selfie");
            var advertiseAuthor = new Account("ad", "Ad", null, AccountType.Advertise, string.Empty);

            var session = CreateSession(
                CreateGameRule("Public", "Selfie"),
                new Dictionary<string, Account>(StringComparer.Ordinal)
                {
                    { publicAccount.Id, publicAccount },
                    { selfieAccount.Id, selfieAccount },
                    { advertiseAuthor.Id, advertiseAuthor }
                },
                new Dictionary<string, List<Post>>(StringComparer.Ordinal));

            session.LoadNewGame(default).GetAwaiter().GetResult();
            session.Play();
            session.SetCurrentPlayerAccount(selfieAccount);

            var post = CreatePost("p-like-ad", advertiseAuthor, null, null);
            session.LikePostByPlayer(post);

            Assert.That(GetReactiveCurrentValue<int>(session, "Score"), Is.EqualTo(-10));
            Assert.That(GetReactiveCurrentValue<GameState>(session, "CurrentGameState"), Is.EqualTo(GameState.Playing));
        }

        [Test]
        public void RepostByPlayer_AdvertisePost_SubtractsRepostPoint()
        {
            var publicAccount = new Account("Public", "Public", null, AccountType.Normal, "Public");
            var selfieAccount = new Account("Selfie", "Selfie", null, AccountType.Normal, "Selfie");
            var advertiseAuthor = new Account("ad", "Ad", null, AccountType.Advertise, string.Empty);

            var session = CreateSession(
                CreateGameRule("Public", "Selfie"),
                new Dictionary<string, Account>(StringComparer.Ordinal)
                {
                    { publicAccount.Id, publicAccount },
                    { selfieAccount.Id, selfieAccount },
                    { advertiseAuthor.Id, advertiseAuthor }
                },
                new Dictionary<string, List<Post>>(StringComparer.Ordinal));

            session.LoadNewGame(default).GetAwaiter().GetResult();
            session.Play();
            session.SetCurrentPlayerAccount(selfieAccount);

            var post = CreatePost("p-repost-ad", advertiseAuthor, null, null);
            session.RepostByPlayer(post);

            Assert.That(GetReactiveCurrentValue<int>(session, "Score"), Is.EqualTo(-50));
            Assert.That(GetReactiveCurrentValue<GameState>(session, "CurrentGameState"), Is.EqualTo(GameState.Playing));
        }

        [Test]
        public void TryPublishReplyDraft_WhenFocusedPostIsAdvertise_SubtractsReplyPoint()
        {
            var publicAccount = new Account("Public", "Public", null, AccountType.Normal, "Public");
            var advertiseAuthor = new Account("ad", "Ad", null, AccountType.Advertise, string.Empty);

            var focusedPost = CreatePost("p-target-ad", advertiseAuthor, null, null);
            var replyDraft = CreatePost("p-reply", publicAccount, publicAccount, focusedPost.Property.Id);
            var postsByAccountId = new Dictionary<string, List<Post>>(StringComparer.Ordinal)
            {
                { publicAccount.Id, new List<Post> { focusedPost, replyDraft } }
            };

            var session = CreateSession(
                CreateGameRule("Public"),
                new Dictionary<string, Account>(StringComparer.Ordinal)
                {
                    { publicAccount.Id, publicAccount },
                    { advertiseAuthor.Id, advertiseAuthor }
                },
                postsByAccountId);

            session.LoadNewGame(default).GetAwaiter().GetResult();
            session.Play();
            session.Proceed(1f);
            session.Proceed(1f);

            session.TryPublishReplyDraft(new ReplyDraftPublishRequest(replyDraft, focusedPost));

            Assert.That(GetReactiveCurrentValue<int>(session, "Score"), Is.EqualTo(-30));
            Assert.That(GetReactiveCurrentValue<GameState>(session, "CurrentGameState"), Is.EqualTo(GameState.Playing));
        }

        static GameSession CreateSession(
            GameRuleSO gameRule,
            Dictionary<string, Account> accountsById,
            Dictionary<string, List<Post>> postsByAccountId)
        {
            var config = CreateGameConfig(gameRule);
            var accountRepository = new StubAccountRepository(accountsById);
            var postRepository = new StubPostRepository(postsByAccountId);
            return new GameSession(config, accountRepository, postRepository, new StubSocialSharePort());
        }

        static GameRuleSO CreateGameRule(params string[] playerAccountIds)
        {
            var gameRule = ScriptableObject.CreateInstance<GameRuleSO>();
            SetField(gameRule, "difficultyName", "Test");
            SetField(gameRule, "timeLimitSeconds", 120f);
            SetField(gameRule, "postPerSecond", 1f);

            var usedAccounts = new List<MyAccountSO>(playerAccountIds.Length);
            foreach (var accountId in playerAccountIds)
            {
                var myAccount = ScriptableObject.CreateInstance<MyAccountSO>();
                SetField(myAccount, "id", accountId);
                usedAccounts.Add(myAccount);
            }

            SetField(gameRule, "usedAccounts", usedAccounts);
            return gameRule;
        }

        static GameConfigSO CreateGameConfig(GameRuleSO gameRule)
        {
            var config = ScriptableObject.CreateInstance<GameConfigSO>();
            SetField(config, "initialGameRule", gameRule);
            SetField(config, "postPoint", 10);
            SetField(config, "replyPoint", 30);
            SetField(config, "likePoint", 10);
            SetField(config, "repostPoint", 50);
            return config;
        }

        static Post CreatePost(string postId, Account author, Account correctPlayerAccount, string parentPostId)
        {
            var property = new PostProperty(
                correctPlayerAccount,
                postId,
                author,
                "test",
                null,
                parentPostId,
                null);
            return new Post(property, 0, 0, 0);
        }

        static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(instance.GetType().Name, fieldName);
            }

            field.SetValue(instance, value);
        }

        static T GetReactiveCurrentValue<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new MissingMemberException(instance.GetType().Name, propertyName);
            }

            var reactive = property.GetValue(instance);
            if (reactive == null)
            {
                throw new NullReferenceException($"{propertyName} is null.");
            }

            var currentValueProperty = reactive.GetType().GetProperty("CurrentValue", BindingFlags.Instance | BindingFlags.Public);
            if (currentValueProperty == null)
            {
                throw new MissingMemberException(reactive.GetType().Name, "CurrentValue");
            }

            return (T)currentValueProperty.GetValue(reactive);
        }

        sealed class StubAccountRepository : IAccountRepository
        {
            readonly Dictionary<string, Account> accountsById;

            public StubAccountRepository(Dictionary<string, Account> accountsById)
            {
                this.accountsById = accountsById;
            }

            public UniTask<Account> GetAccount(string accountId, CancellationToken ct)
            {
                if (!accountsById.TryGetValue(accountId, out var account))
                {
                    throw new KeyNotFoundException(accountId);
                }

                return UniTask.FromResult(account);
            }
        }

        sealed class StubPostRepository : IPostRepository
        {
            readonly Dictionary<string, List<Post>> postsByAccountId;
            readonly Dictionary<string, Post> postsById;

            public StubPostRepository(Dictionary<string, List<Post>> postsByAccountId)
            {
                this.postsByAccountId = postsByAccountId;
                postsById = new Dictionary<string, Post>(StringComparer.Ordinal);
                foreach (var posts in postsByAccountId.Values)
                {
                    foreach (var post in posts)
                    {
                        postsById[post.Property.Id] = post;
                    }
                }
            }

            public UniTask<Post> GetPost(string postId, CancellationToken ct)
            {
                if (!postsById.TryGetValue(postId, out var post))
                {
                    throw new KeyNotFoundException(postId);
                }

                return UniTask.FromResult(post);
            }

            public UniTask<List<Post>> GetPostsByCorrectPlayerAccountAsync(Account playerAccount, CancellationToken ct)
            {
                if (!postsByAccountId.TryGetValue(playerAccount.Id, out var posts))
                {
                    return UniTask.FromResult(new List<Post>());
                }

                return UniTask.FromResult(new List<Post>(posts));
            }

            public UniTask<List<Post>> GetRepliesAsync(string postId, CancellationToken ct)
            {
                var replies = new List<Post>();
                foreach (var post in postsById.Values)
                {
                    if (string.Equals(post.Property.ParentPostId, postId, StringComparison.Ordinal))
                    {
                        replies.Add(post);
                    }
                }

                return UniTask.FromResult(replies);
            }

            public UniTask<List<Post>> GetAncestorPostsAsync(string postId, CancellationToken ct)
            {
                var ancestors = new List<Post>();
                var visited = new HashSet<string>(StringComparer.Ordinal);
                if (!postsById.TryGetValue(postId, out var currentPost))
                {
                    return UniTask.FromResult(ancestors);
                }

                var parentPostId = currentPost.Property.ParentPostId;
                while (!string.IsNullOrEmpty(parentPostId))
                {
                    if (!visited.Add(parentPostId))
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
                return UniTask.FromResult(ancestors);
            }
        }

        sealed class StubSocialSharePort : ISocialSharePort
        {
            public string BuildResultShareText(GameResult gameResult) => string.Empty;
            public UniTask ShareResultTextAsync(string shareText, CancellationToken ct) => UniTask.CompletedTask;
        }
    }
}
