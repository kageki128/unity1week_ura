using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Infrastructure
{
    public class XSharePort : ISocialSharePort
    {
        const string XIntentBaseUrl = "https://x.com/intent/post";

        public UniTask ShareResultAsync(GameResult gameResult, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (gameResult == null)
            {
                throw new ArgumentNullException(nameof(gameResult));
            }

            var difficultyName = gameResult.GameRule == null ? "Unknown" : gameResult.GameRule.name;
            var text = $"I scored {gameResult.Score} points! Difficulty: {difficultyName} #unity1week";
            var encodedText = Uri.EscapeDataString(text);
            var url = $"{XIntentBaseUrl}?text={encodedText}";

            Debug.Log($"[XSharePort] Share URL: {url}");

            Application.OpenURL(url);
            return UniTask.CompletedTask;
        }
    }
}
