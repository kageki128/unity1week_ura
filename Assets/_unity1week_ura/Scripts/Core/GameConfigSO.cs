using UnityEngine;

namespace Unity1Week_Ura.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Unity1Week_Ura/GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        public SceneType InitialSceneType => initialSceneType;
        [SerializeField] SceneType initialSceneType;
        public GameRuleSO InitialGameRule => initialGameRule;
        [SerializeField] GameRuleSO initialGameRule;
        public string ResultText => resultText;
        [SerializeField, TextArea(3, 10)] string resultText = "『裏垢まねじめんと！』の {difficulty} に挑戦したよ〜📱✨\nスコアは {score} だった！誤爆しないように垢切り替えるの大変🥹💦\n{url}\n#unity1week #裏垢まねじめんと！";
        public int PostPoint => postPoint;
        [SerializeField] int postPoint = 10;
        public int ReplyPoint => replyPoint;
        [SerializeField] int replyPoint = 30;
        public int LikePoint => likePoint;
        [SerializeField] int likePoint = 10;
        public int RepostPoint => repostPoint;
        [SerializeField] int repostPoint = 50;
    }
}
