using TMPro;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class HighScoreTextView : MonoBehaviour
    {
        public IReadOnlyList<GameRuleSO> GameRules => gameRules;

        [SerializeField] List<GameRuleSO> gameRules = new();
        [SerializeField] TMP_Text scoreText;
        [SerializeField] string scoreTemplate = "{score}";

        public async UniTask<int> LoadHighScoreAsync(IHighScoreRepository highScoreRepository, CancellationToken ct)
        {
            if (highScoreRepository == null)
            {
                throw new ArgumentNullException(nameof(highScoreRepository));
            }

            if (gameRules == null || gameRules.Count == 0)
            {
                SetScore(0);
                return 0;
            }

            var totalScore = 0;
            HashSet<string> registeredDifficultyIds = new(StringComparer.Ordinal);

            for (var i = 0; i < gameRules.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var gameRule = gameRules[i];
                if (gameRule == null)
                {
                    continue;
                }

                var difficultyId = ResolveDifficultyId(gameRule);
                if (!registeredDifficultyIds.Add(difficultyId))
                {
                    continue;
                }

                var highScore = Mathf.Max(await highScoreRepository.GetHighScoreAsync(gameRule, ct), 0);
                totalScore += highScore;
            }

            SetScore(totalScore);
            return totalScore;
        }

        public void SetScore(int score)
        {
            if (scoreText == null)
            {
                return;
            }

            var formattedScore = ScoreFormatter.Format(Mathf.Max(score, 0));
            if (string.IsNullOrEmpty(scoreTemplate))
            {
                scoreText.text = formattedScore;
                return;
            }

            scoreText.text = scoreTemplate.Replace("{score}", formattedScore);
        }

        void Reset()
        {
            if (scoreText == null)
            {
                TryGetComponent(out scoreText);
            }
        }

        static string ResolveDifficultyId(GameRuleSO gameRule)
        {
            if (gameRule == null)
            {
                throw new ArgumentNullException(nameof(gameRule));
            }

            var difficultyId = gameRule.DifficultyId;
            if (string.IsNullOrWhiteSpace(difficultyId))
            {
                throw new InvalidOperationException($"Difficulty id is empty. GameRuleSO: {gameRule.name}");
            }

            return difficultyId.Trim();
        }
    }
}
