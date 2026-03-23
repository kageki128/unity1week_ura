using System.Globalization;
using System.Text;
using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class DifficultyInfoView : MonoBehaviour
    {
        [SerializeField] TMP_Text detailText;
        [SerializeField] Color labelColor = Color.white;
        [SerializeField] Color valueColor = Color.white;

        public void SetGameRule(GameRuleSO gameRule)
        {
            if (detailText == null)
            {
                return;
            }

            detailText.richText = true;
            detailText.text = BuildDetailText(gameRule, labelColor, valueColor);
        }

        static string BuildDetailText(GameRuleSO gameRule, Color labelColor, Color valueColor)
        {
            if (gameRule == null)
            {
                return string.Empty;
            }

            var timeText = ConvertDigitsToFullWidth(FormatTimeMinutes(gameRule.TimeLimitSeconds));
            var speedText = ConvertDigitsToFullWidth(string.IsNullOrWhiteSpace(gameRule.PostPerSecondDescription)
                ? "-"
                : gameRule.PostPerSecondDescription);
            var adsText = ConvertDigitsToFullWidth(string.IsNullOrWhiteSpace(gameRule.AdvertisePostProbabilityDescription)
                ? "-"
                : gameRule.AdvertisePostProbabilityDescription);
            var accountCountText = ConvertDigitsToFullWidth((gameRule.UsedAccounts?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
            var labelColorCode = ColorUtility.ToHtmlStringRGBA(labelColor);
            var valueColorCode = ColorUtility.ToHtmlStringRGBA(valueColor);

            var builder = new StringBuilder();
            AppendLine(builder, "時間", $"　{timeText}分", labelColorCode, valueColorCode);
            AppendLine(builder, "流速", $"　{speedText}", labelColorCode, valueColorCode);
            AppendLine(builder, "広告", $"　{adsText}", labelColorCode, valueColorCode);
            AppendLine(builder, "操作垢", $"{accountCountText}つ", labelColorCode, valueColorCode);
            return builder.ToString();
        }

        static void AppendLine(StringBuilder builder, string label, string value, string labelColorCode, string valueColorCode)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append("<color=#")
                .Append(labelColorCode)
                .Append('>')
                .Append(label)
                .Append(":</color>")
                .Append("<color=#")
                .Append(valueColorCode)
                .Append('>')
                .Append(value)
                .Append("</color>");
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
