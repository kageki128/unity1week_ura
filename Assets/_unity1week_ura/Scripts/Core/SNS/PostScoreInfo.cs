using System.Collections.Generic;

namespace Unity1Week_Ura.Core
{
    public class PostScoreInfo
    {
        public int PublishPoint { get; }
        public int LikePoint { get; }
        public int RepostPoint { get; }

        public IReadOnlyList<string> WrongTexts { get; }

        public PostScoreInfo(int publishPoint, int likePoint, int repostPoint, List<string> wrongTexts)
        {
            PublishPoint = publishPoint;
            LikePoint = likePoint;
            RepostPoint = repostPoint;
            WrongTexts = wrongTexts;
        }
    }
}