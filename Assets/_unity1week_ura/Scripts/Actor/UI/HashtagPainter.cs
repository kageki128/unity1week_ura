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
