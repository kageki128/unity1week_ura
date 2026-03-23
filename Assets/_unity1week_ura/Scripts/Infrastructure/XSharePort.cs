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
        const string DifficultyPlaceholder = "{difficulty}";
        const string ScorePlaceholder = "{score}";
        const string ReasonPlaceholder = "{reason}";
        const string UrlPlaceholder = "{url}";
        readonly GameConfigSO gameConfig;

        public XSharePort(GameConfigSO gameConfig)
        {
            this.gameConfig = gameConfig;
        }

        public string BuildResultShareText(GameResult gameResult)
        {
            if (gameResult == null)
            {
                throw new ArgumentNullException(nameof(gameResult));
            }

            var difficultyName = gameResult.GameRule == null ? "Unknown" : gameResult.GameRule.DifficultyName;
            var finishReasonText = gameConfig.GetFinishReasonText(gameResult.FinishReason);
            var shareUrl = Application.absoluteURL;
            return gameConfig.ResultText
                .Replace(DifficultyPlaceholder, difficultyName)
                .Replace(ScorePlaceholder, gameResult.Score.ToString())
                .Replace(ReasonPlaceholder, finishReasonText)
                .Replace(UrlPlaceholder, shareUrl);
        }

        public UniTask ShareResultTextAsync(string shareText, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (shareText == null)
            {
                throw new ArgumentNullException(nameof(shareText));
            }

            var encodedText = Uri.EscapeDataString(shareText);
            var url = $"{XIntentBaseUrl}?text={encodedText}";

            Debug.Log($"[XSharePort] Share URL: {url}");

            Application.OpenURL(url);
            return UniTask.CompletedTask;
        }
    }
}
