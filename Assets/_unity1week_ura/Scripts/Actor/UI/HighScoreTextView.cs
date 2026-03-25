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

                var highScore = ScoreFormatter.Clamp(await highScoreRepository.GetHighScoreAsync(gameRule, ct));
                totalScore = ScoreFormatter.ClampTotal(totalScore + highScore);
            }

            SetScore(totalScore, registeredDifficultyIds.Count > 1);
            return totalScore;
        }

        public void SetScore(int score)
        {
            SetScore(score, ShouldUseTotalScoreFormat(gameRules));
        }

        void SetScore(int score, bool useTotalScoreFormat)
        {
            if (scoreText == null)
            {
                return;
            }

            var formattedScore = useTotalScoreFormat
                ? ScoreFormatter.FormatTotal(score)
                : ScoreFormatter.Format(score);
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

        static bool ShouldUseTotalScoreFormat(IReadOnlyList<GameRuleSO> rules)
        {
            if (rules == null || rules.Count <= 1)
            {
                return false;
            }

            HashSet<string> registeredDifficultyIds = new(StringComparer.Ordinal);
            for (var i = 0; i < rules.Count; i++)
            {
                var gameRule = rules[i];
                if (gameRule == null || string.IsNullOrWhiteSpace(gameRule.DifficultyId))
                {
                    continue;
                }

                registeredDifficultyIds.Add(gameRule.DifficultyId.Trim());
                if (registeredDifficultyIds.Count > 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
