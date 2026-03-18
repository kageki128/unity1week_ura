using System;

namespace Unity1Week_Ura.Core
{
    public class Post
    {
        public PostProperty Property { get; }
        public PostScoreInfo ScoreInfo { get; }

        public int LikeCount { get; private set; }  
        public int RepostCount { get; private set; }
        public int ReplyCount { get; private set; }
        public PostState State { get; private set; }
        public DateTimeOffset PublishDateTime { get; private set; }

        public Post(PostProperty property, PostScoreInfo scoreInfo, int defaultLikeCount, int defaultRepostCount, int defaultReplyCount)
        {
            Property = property;
            ScoreInfo = scoreInfo;
            LikeCount = defaultLikeCount;
            RepostCount = defaultRepostCount;
            ReplyCount = defaultReplyCount;

            State = PostState.BeforeAppeared;
            PublishDateTime = DateTimeOffset.UtcNow;
        }
    }
}