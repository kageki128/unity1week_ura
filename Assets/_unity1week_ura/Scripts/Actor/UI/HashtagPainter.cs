using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class HashtagPainter : MonoBehaviour
    {
        [SerializeField] TMP_Text targetText;
        [SerializeField] Color hashtagColor = new Color32(0xEE, 0x5A, 0x7F, 0xFF);

        ITextPreprocessor upstreamPreprocessor;
        HashtagTextPreprocessor hashtagPreprocessor;

        void Awake()
        {
            targetText ??= GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            Attach();
        }

        void OnDisable()
        {
            Detach();
        }

        void OnDestroy()
        {
            Detach();
        }

        void OnValidate()
        {
            targetText ??= GetComponent<TMP_Text>();
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            Refresh();
        }

        void Reset()
        {
            targetText = GetComponent<TMP_Text>();
        }

        public void Refresh()
        {
            if (targetText == null)
            {
                return;
            }

            var current = targetText.text;
            targetText.text = current;
        }

        void Attach()
        {
            if (targetText == null)
            {
                return;
            }

            if (hashtagPreprocessor != null && targetText.textPreprocessor == hashtagPreprocessor)
            {
                return;
            }

            upstreamPreprocessor = targetText.textPreprocessor;
            hashtagPreprocessor = new HashtagTextPreprocessor(this, upstreamPreprocessor);
            targetText.textPreprocessor = hashtagPreprocessor;
            Refresh();
        }

        void Detach()
        {
            if (targetText == null || hashtagPreprocessor == null)
            {
                return;
            }

            if (targetText.textPreprocessor == hashtagPreprocessor)
            {
                targetText.textPreprocessor = upstreamPreprocessor;
                Refresh();
            }

            hashtagPreprocessor = null;
            upstreamPreprocessor = null;
        }

        string PaintHashtags(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            var builder = new StringBuilder(source.Length + 32);
            var openColorTag = $"<color=#{ColorUtility.ToHtmlStringRGBA(hashtagColor)}>";
            const string closeColorTag = "</color>";

            for (var i = 0; i < source.Length; i++)
            {
                var current = source[i];

                if (current == '<')
                {
                    var closeTagIndex = source.IndexOf('>', i);
                    if (closeTagIndex < 0)
                    {
                        builder.Append(source, i, source.Length - i);
                        break;
                    }

                    builder.Append(source, i, closeTagIndex - i + 1);
                    i = closeTagIndex;
                    continue;
                }

                if (!IsHashtagStart(source, i))
                {
                    if (IsUrlStart(source, i))
                    {
                        var urlEndIndex = i;
                        while (urlEndIndex < source.Length && IsUrlCharacter(source[urlEndIndex]))
                        {
                            urlEndIndex++;
                        }

                        urlEndIndex = TrimUrlEndIndex(source, i, urlEndIndex);
                        if (urlEndIndex > i)
                        {
                            builder.Append(openColorTag);
                            builder.Append(source, i, urlEndIndex - i);
                            builder.Append(closeColorTag);
                            i = urlEndIndex - 1;
                            continue;
                        }
                    }

                    builder.Append(current);
                    continue;
                }

                var endIndex = i + 1;
                while (endIndex < source.Length && IsHashtagCharacter(source[endIndex]))
                {
                    endIndex++;
                }

                if (endIndex <= i + 1)
                {
                    builder.Append(current);
                    continue;
                }

                builder.Append(openColorTag);
                builder.Append(source, i, endIndex - i);
                builder.Append(closeColorTag);
                i = endIndex - 1;
            }

            return builder.ToString();
        }

        bool IsUrlStart(string source, int index)
        {
            if (!IsUrlBoundary(source, index))
            {
                return false;
            }

            return StartsWithIgnoreCase(source, index, "https://")
                || StartsWithIgnoreCase(source, index, "http://")
                || StartsWithIgnoreCase(source, index, "www.");
        }

        bool IsUrlBoundary(string source, int index)
        {
            if (index <= 0)
            {
                return true;
            }

            return !IsUrlCharacter(source[index - 1]);
        }

        bool StartsWithIgnoreCase(string source, int index, string value)
        {
            if (index + value.Length > source.Length)
            {
                return false;
            }

            return string.Compare(source, index, value, 0, value.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        int TrimUrlEndIndex(string source, int startIndex, int endIndex)
        {
            while (endIndex > startIndex && IsTrailingUrlCharacter(source[endIndex - 1]))
            {
                endIndex--;
            }

            return endIndex;
        }

        bool IsTrailingUrlCharacter(char value)
        {
            return value is '.' or ',' or '!' or '?' or ')' or ']' or '}' or '、' or '。' or '」' or '』' or '】';
        }

        bool IsUrlCharacter(char value)
        {
            if (char.IsLetterOrDigit(value))
            {
                return true;
            }

            return value is '-' or '.' or '_' or '~'
                or ':' or '/' or '?' or '#'
                or '[' or ']' or '@'
                or '!' or '$' or '&' or '\'' or '(' or ')' or '*'
                or '+' or ',' or ';' or '=' or '%';
        }

        bool IsHashtagStart(string source, int index)
        {
            if (source[index] != '#')
            {
                return false;
            }

            if (index + 1 >= source.Length || !IsHashtagCharacter(source[index + 1]))
            {
                return false;
            }

            if (index == 0)
            {
                return true;
            }

            return !IsHashtagCharacter(source[index - 1]);
        }

        bool IsHashtagCharacter(char value)
        {
            if (char.IsLetterOrDigit(value) || value == '_' || value == 'ー')
            {
                return true;
            }

            var category = char.GetUnicodeCategory(value);
            return category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark;
        }

        class HashtagTextPreprocessor : ITextPreprocessor
        {
            readonly HashtagPainter owner;
            readonly ITextPreprocessor upstream;

            public HashtagTextPreprocessor(HashtagPainter owner, ITextPreprocessor upstream)
            {
                this.owner = owner;
                this.upstream = upstream;
            }

            public string PreprocessText(string text)
            {
                var upstreamProcessed = upstream?.PreprocessText(text) ?? text;
                return owner.PaintHashtags(upstreamProcessed);
            }
        }
    }
}
