using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Infrastructure
{
    public class PlayerPrefsHighScoreRepository : IHighScoreRepository
    {
        const string KeyPrefix = "high_score";

        public UniTask<int> GetHighScoreAsync(GameRuleSO gameRule, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var key = BuildPlayerPrefsKey(gameRule);
            return UniTask.FromResult(PlayerPrefs.GetInt(key, 0));
        }

        public UniTask<int> SaveHighScoreIfHigherAsync(GameRuleSO gameRule, int score, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var key = BuildPlayerPrefsKey(gameRule);
            var currentHighScore = PlayerPrefs.GetInt(key, 0);
            var clampedScore = Mathf.Max(score, 0);

            if (clampedScore <= currentHighScore)
            {
                return UniTask.FromResult(currentHighScore);
            }

            PlayerPrefs.SetInt(key, clampedScore);
            PlayerPrefs.Save();
            return UniTask.FromResult(clampedScore);
        }

        static string BuildPlayerPrefsKey(GameRuleSO gameRule)
        {
            if (gameRule == null)
            {
                throw new ArgumentNullException(nameof(gameRule));
            }

            var difficultyId = gameRule.DifficultyId;
            if (string.IsNullOrWhiteSpace(difficultyId))
            {
                difficultyId = gameRule.name;
            }

            if (string.IsNullOrWhiteSpace(difficultyId))
            {
                throw new InvalidOperationException("Difficulty id is empty.");
            }

            return $"{KeyPrefix}:{difficultyId.Trim()}";
        }
    }
}
