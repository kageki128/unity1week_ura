using System.Collections.Generic;

namespace Unity1Week_Ura.Core
{
    public class PostScoreInfo
    {
        public int PublishPoint { get; }

        public IReadOnlyList<string> WrongTexts { get; }

        public PostScoreInfo(int publishPoint, List<string> wrongTexts)
        {
            PublishPoint = publishPoint;
            WrongTexts = wrongTexts;
        }
    }
}