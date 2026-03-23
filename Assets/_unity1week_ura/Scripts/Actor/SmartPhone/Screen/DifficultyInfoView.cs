using System.Globalization;
using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class DifficultyInfoView : MonoBehaviour
    {
        [SerializeField] TMP_Text detailText;

        public void SetGameRule(GameRuleSO gameRule)
        {
            if (detailText == null)
            {
                return;
            }

            detailText.text = ConvertDigitsToFullWidth(BuildDetailText(gameRule));
        }

        static string BuildDetailText(GameRuleSO gameRule)
        {
            if (gameRule == null)
            {
                return string.Empty;
            }

            var timeText = FormatTimeMinutes(gameRule.TimeLimitSeconds);
            var speedText = string.IsNullOrWhiteSpace(gameRule.PostPerSecondDescription)
                ? "-"
                : gameRule.PostPerSecondDescription;
            var adsText = string.IsNullOrWhiteSpace(gameRule.AdvertisePostProbabilityDescription)
                ? "-"
                : gameRule.AdvertisePostProbabilityDescription;
            var accountCount = gameRule.UsedAccounts?.Count ?? 0;

            return
                $"時間: 　{timeText}分\n" +
                $"流速: 　{speedText}\n" +
                $"広告: 　{adsText}\n" +
                $"操作垢: {accountCount}つ";
        }

        static string FormatTimeMinutes(float timeLimitSeconds)
        {
            var minutes = timeLimitSeconds / 60f;
            var roundedMinutes = Mathf.Round(minutes);

            if (Mathf.Approximately(minutes, roundedMinutes))
            {
                return roundedMinutes.ToString("0", CultureInfo.InvariantCulture);
            }

            return minutes.ToString("0.#", CultureInfo.InvariantCulture);
        }

        static string ConvertDigitsToFullWidth(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (c >= '0' && c <= '9')
                {
                    chars[i] = (char)('０' + (c - '0'));
                }
            }

            return new string(chars);
        }
    }
}
