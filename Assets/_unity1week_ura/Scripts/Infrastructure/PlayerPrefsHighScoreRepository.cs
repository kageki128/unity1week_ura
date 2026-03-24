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
            return LoadHighScoreAsync(BuildPlayerPrefsKey(gameRule), ct);
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

            return BuildPlayerPrefsKey(difficultyId);
        }

        static string BuildPlayerPrefsKey(string difficultyId)
        {
            if (string.IsNullOrWhiteSpace(difficultyId))
            {
                throw new ArgumentException("Difficulty id is empty.", nameof(difficultyId));
            }

            return $"{KeyPrefix}:{difficultyId.Trim()}";
        }

        static UniTask<int> LoadHighScoreAsync(string playerPrefsKey, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.FromResult(PlayerPrefs.GetInt(playerPrefsKey, 0));
        }
    }
}
