using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using ObservableCollections;
using R3;
using UnityEngine;

namespace Unity1Week_Ura.Core
{
    public class GameSession
    {
        public ReadOnlyReactiveProperty<float> RemainingTimeSeconds => remainingTimeSeconds;
        readonly ReactiveProperty<float> remainingTimeSeconds = new(0);

        public ReadOnlyReactiveProperty<int> Score => score;
        readonly ReactiveProperty<int> score = new(0);

        public ReadOnlyReactiveProperty<GameState> CurrentGameState => currentGameState;
        readonly ReactiveProperty<GameState> currentGameState = new(GameState.Ready);

        public IReadOnlyList<Account> PlayerAccounts => playerAccounts;
        IReadOnlyList<Account> playerAccounts = new List<Account>();

        public IReadOnlyObservableList<Post> PublishedPosts => publishedPosts;
        readonly ObservableList<Post> publishedPosts = new();
        public IReadOnlyObservableList<Post> DraftPosts => draftPosts;
        readonly ObservableList<Post> draftPosts = new();
        List<Post> beforeAppearingPosts = new();
        
        readonly IAccountRepository accountRepository;
        readonly IPostRepository postRepository;

        GameRuleSO gameRule;
        
        public GameSession(GameRuleSO defaultGameRule, IAccountRepository accountRepository, IPostRepository postRepository)
        {
            this.accountRepository = accountRepository;
            this.postRepository = postRepository;
            gameRule = defaultGameRule;
        }

        public void SetNewGameRule(GameRuleSO newGameRule)
        {
            if(currentGameState.CurrentValue != GameState.Ready && currentGameState.CurrentValue != GameState.Finished)
            {
                return;
            }

            gameRule = newGameRule;
        }

        public async UniTask LoadNewGame(CancellationToken ct)
        {
            if(currentGameState.CurrentValue != GameState.Ready && currentGameState.CurrentValue != GameState.Finished)
            {
                return;
            }

            // 使用するアカウントを読み込む
            List<string> playerAccountIds = gameRule.UsedAccounts.Select(accountSO => accountSO.Id).ToList();
            Account[] loadedAccounts = await UniTask.WhenAll(
                playerAccountIds.Select(accountId => accountRepository.GetAccount(accountId, ct))
            );
            playerAccounts = loadedAccounts;

            // 使用するポストを読み込む
            List<Post>[] loadedPostsByAccount = await UniTask.WhenAll(
                playerAccounts.Select(account => postRepository.GetPostsByCorrectPlayerAccountAsync(account, ct))
            );
            beforeAppearingPosts = loadedPostsByAccount.SelectMany(posts => posts).ToList();
            publishedPosts.Clear();
            draftPosts.Clear();
            // debug
            foreach (var post in beforeAppearingPosts)
            {
                Debug.Log($"[GameSession] ロードしたポスト: {post.Property.Id} by {post.Property.Author.Name}");
            }

            remainingTimeSeconds.Value = gameRule.TimeLimitSeconds;
            score.Value = 0;
            currentGameState.Value = GameState.Pause;
        }

        public void Play()
        {
            if (currentGameState.CurrentValue != GameState.Pause)
            {
                return;
            }

            currentGameState.Value = GameState.Playing;
        }

        public void Pause()
        {
            if (currentGameState.CurrentValue != GameState.Playing)
            {
                return;
            }

            currentGameState.Value = GameState.Pause;
        }

        public void Proceed(float deltaTime)
        {
            if (currentGameState.CurrentValue != GameState.Playing)
            {
                return;
            }

            TrySupplyPost(deltaTime);

            remainingTimeSeconds.Value = Mathf.Max(remainingTimeSeconds.Value - deltaTime, 0);
            if (remainingTimeSeconds.Value <= 0)
            {
                currentGameState.Value = GameState.Finished;
            }
        }

        void TrySupplyPost(float deltaTime)
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