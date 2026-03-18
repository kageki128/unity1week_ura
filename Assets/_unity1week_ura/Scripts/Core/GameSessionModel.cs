using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Codice.Utils;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Unity1Week_Ura.Core
{
    public class GameSessionModel
    {
        public ReadOnlyReactiveProperty<float> RemainingTimeSeconds => remainingTimeSeconds;
        readonly ReactiveProperty<float> remainingTimeSeconds = new(0);

        public ReadOnlyReactiveProperty<int> Score => score;
        readonly ReactiveProperty<int> score = new(0);

        public ReadOnlyReactiveProperty<GameState> CurrentGameState => currentGameState;
        readonly ReactiveProperty<GameState> currentGameState = new(GameState.Ready);

        readonly IAccountRepository accountRepository;
        readonly IPostRepository postRepository;

        GameRuleSO gameRule;
        IReadOnlyList<Account> playerAccounts = new List<Account>();
        List<Post> beforeAppearingPosts = new();

        public GameSessionModel(GameRuleSO defaultGameRule, IAccountRepository accountRepository, IPostRepository postRepository)
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

            remainingTimeSeconds.Value = Mathf.Max(remainingTimeSeconds.Value - deltaTime, 0);
            if (remainingTimeSeconds.Value <= 0)
            {
                currentGameState.Value = GameState.Finished;
            }
        }
    }
}