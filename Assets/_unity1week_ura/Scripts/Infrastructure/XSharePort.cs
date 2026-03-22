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
        const string UrlPlaceholder = "{url}";
        readonly GameConfigSO gameConfig;

        public XSharePort(GameConfigSO gameConfig)
        {
            this.gameConfig = gameConfig;
        }

        public UniTask ShareResultAsync(GameResult gameResult, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (gameResult == null)
            {
                throw new ArgumentNullException(nameof(gameResult));
            }

            var difficultyName = gameResult.GameRule == null ? "Unknown" : gameResult.GameRule.DifficultyName;
            var shareUrl = Application.absoluteURL;
            var text = gameConfig.ResultText
                .Replace(DifficultyPlaceholder, difficultyName)
                .Replace(ScorePlaceholder, gameResult.Score.ToString())
                .Replace(UrlPlaceholder, shareUrl);
            var encodedText = Uri.EscapeDataString(text);
            var url = $"{XIntentBaseUrl}?text={encodedText}";

            Debug.Log($"[XSharePort] Share URL: {url}");

            Application.OpenURL(url);
            return UniTask.CompletedTask;
        }
    }
}
