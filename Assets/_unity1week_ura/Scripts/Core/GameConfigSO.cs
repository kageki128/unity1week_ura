using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity1Week_Ura.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Unity1Week_Ura/GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        [Serializable]
        public class FinishReasonTextEntry
        {
            public FinishReason Reason => reason;
            [SerializeField] FinishReason reason;
            public string Text => text;
            [SerializeField, TextArea(2, 4)] string text;
        }

        public SceneType InitialSceneType => initialSceneType;
        [SerializeField] SceneType initialSceneType;

        public GameRuleSO InitialGameRule => initialGameRule;
        [SerializeField] GameRuleSO initialGameRule;

        public string ResultText => resultText;
        [SerializeField, TextArea(3, 10)] string resultText = "『裏垢まねじめんと！』の {difficulty} に挑戦したよ〜📱✨\nスコアは {score} ！{reason}\n{url}\n#unity1week #裏垢まねじめんと";

        public IReadOnlyList<GameRuleSO> Scoreboard1GameRules => scoreboard1GameRules;
        [SerializeField] List<GameRuleSO> scoreboard1GameRules = new();

        public IReadOnlyList<GameRuleSO> Scoreboard2GameRules => scoreboard2GameRules;
        [SerializeField] List<GameRuleSO> scoreboard2GameRules = new();

        [SerializeField] List<FinishReasonTextEntry> finishReasonTexts = new();

        public int PostPoint => postPoint;
        [SerializeField] int postPoint = 10;

        public int ReplyPoint => replyPoint;
        [SerializeField] int replyPoint = 30;

        public int LikePoint => likePoint;
        [SerializeField] int likePoint = 10;

        public int RepostPoint => repostPoint;
        [SerializeField] int repostPoint = 50;

        public string GetFinishReasonText(FinishReason reason)
        {
            for (var i = 0; i < finishReasonTexts.Count; i++)
            {
                var entry = finishReasonTexts[i];
                if (entry != null && entry.Reason == reason && !string.IsNullOrEmpty(entry.Text))
                {
                    return entry.Text;
                }
            }

            return reason switch
            {
                FinishReason.TimeUp => "誤爆しないで最後までやりきったよ〜😎✨",
                FinishReason.WrongAccountLike => "いいね誤爆しちゃった🥹💦",
                FinishReason.WrongAccountRepost => "リポスト誤爆しちゃった🥹💦",
                FinishReason.WrongAccountNormalPost => "投稿誤爆しちゃった🥹💦",
                FinishReason.WrongAccountReplyPost => "返信誤爆しちゃった🥹💦",
                FinishReason.WrongReplyTarget => "返信相手間違えちゃった🥹💦",
                FinishReason.InvalidNormalDraftPublish => "投稿操作ミスしちゃった🥹💦",
                FinishReason.InvalidReplyDraftPublish => "返信操作ミスしちゃった🥹💦",
                _ => "不明な理由で終了しちゃった🥹💦"
            };
        }
    }
}
