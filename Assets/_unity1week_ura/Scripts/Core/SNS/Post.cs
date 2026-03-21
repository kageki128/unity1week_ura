using System;

namespace Unity1Week_Ura.Core
{
    public class Post
    {
        public PostProperty Property { get; }
        public PostScoreInfo ScoreInfo { get; }

        readonly int defaultLikeCount;
        readonly int defaultRepostCount;

        public int LikeCount { get; private set; }
        public int RepostCount { get; private set; }
        public int ReplyCount { get; private set; }
        public bool IsLikedByPlayer { get; private set; }
        public bool IsRepostedByPlayer { get; private set; }

        public PostState State { get; private set; }
        public DateTimeOffset PublishDateTime { get; private set; }

        public Post(PostProperty property, PostScoreInfo scoreInfo, int defaultLikeCount, int defaultRepostCount, int defaultReplyCount)
        {
            Property = property;
            ScoreInfo = scoreInfo;
            this.defaultLikeCount = defaultLikeCount;
            this.defaultRepostCount = defaultRepostCount;
            LikeCount = defaultLikeCount;
            RepostCount = defaultRepostCount;
            ReplyCount = defaultReplyCount;
            IsLikedByPlayer = false;
            IsRepostedByPlayer = false;

            State = PostState.BeforeAppearing;
            PublishDateTime = DateTimeOffset.UtcNow;
        }

        public void ChangeState(PostState newState)
        {
            State = newState;
        }

        public bool ToggleLikeByPlayer()
        {
            IsLikedByPlayer = !IsLikedByPlayer;
            LikeCount += IsLikedByPlayer ? 1 : -1;
            return IsLikedByPlayer;
        }

        public bool ToggleRepostByPlayer()
        {
            IsRepostedByPlayer = !IsRepostedByPlayer;
            RepostCount += IsRepostedByPlayer ? 1 : -1;
            return IsRepostedByPlayer;
        }

        public void ResetPlayerAction()
        {
            IsLikedByPlayer = false;
            IsRepostedByPlayer = false;
            LikeCount = defaultLikeCount;
            RepostCount = defaultRepostCount;
        }
    }
}