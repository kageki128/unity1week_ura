using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using unityroom.Api;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Infrastructure
{
    public class UnityroomScoreboardRepository : IUnityroomScoreboardRepository
    {
        const int Scoreboard1No = 1;
        const int Scoreboard2No = 2;

        readonly IHighScoreRepository highScoreRepository;
        readonly GameConfigSO gameConfig;

        public UnityroomScoreboardRepository(IHighScoreRepository highScoreRepository, GameConfigSO gameConfig)
        {
            this.highScoreRepository = highScoreRepository;
            this.gameConfig = gameConfig;
        }

        public async UniTask SendScoreboardsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var client = UnityroomApiClient.Instance;
            if (client == null)
            {
                Debug.LogWarning("[unityroom] UnityroomApiClient が見つからないためランキング送信をスキップします。");
                return;
            }

            var scoreboard1Score = await BuildSummedHighScoreAsync(gameConfig?.Scoreboard1GameRules, ct);
            client.SendScore(Scoreboard1No, scoreboard1Score, ScoreboardWriteMode.HighScoreDesc);

            var scoreboard2Score = await BuildSummedHighScoreAsync(gameConfig?.Scoreboard2GameRules, ct);
            client.SendScore(Scoreboard2No, scoreboard2Score, ScoreboardWriteMode.HighScoreDesc);
        }

        async UniTask<int> BuildSummedHighScoreAsync(IReadOnlyList<GameRuleSO> gameRules, CancellationToken ct)
        {
            if (gameRules == null || gameRules.Count == 0)
            {
                return 0;
            }

            var totalScore = 0;
            HashSet<string> registeredDifficultyIds = new(System.StringComparer.Ordinal);

            for (var i = 0; i < gameRules.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var gameRule = gameRules[i];
                if (gameRule == null)
                {
                    continue;
                }

                var difficultyId = gameRule.DifficultyId;
                if (string.IsNullOrWhiteSpace(difficultyId))
                {
                    difficultyId = gameRule.name;
                }

                if (string.IsNullOrWhiteSpace(difficultyId))
                {
                    continue;
                }

                if (!registeredDifficultyIds.Add(difficultyId.Trim()))
                {
                    continue;
                }

                var highScore = await highScoreRepository.GetHighScoreAsync(gameRule, ct);
                totalScore += Mathf.Max(highScore, 0);
            }

            return Mathf.Max(totalScore, 0);
        }
    }
}
