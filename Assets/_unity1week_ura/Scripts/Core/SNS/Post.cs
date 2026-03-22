using System;
using R3;

namespace Unity1Week_Ura.Core
{
    public class Post
    {
        public PostProperty Property { get; }
        public PostType Type => string.IsNullOrEmpty(Property.ParentPostId) ? PostType.Normal : PostType.Reply;
        public bool IsReply => Type == PostType.Reply;

        readonly int defaultLikeCount;
        readonly int defaultRepostCount;

        public int LikeCount { get; private set; }
        public int RepostCount { get; private set; }
        public int ReplyCount { get; private set; }
        public ReadOnlyReactiveProperty<bool> IsLikedByPlayer => isLikedByPlayer;
        readonly ReactiveProperty<bool> isLikedByPlayer = new(false);
        public ReadOnlyReactiveProperty<bool> IsRepostedByPlayer => isRepostedByPlayer;
        readonly ReactiveProperty<bool> isRepostedByPlayer = new(false);
        public Account RepostedByAccount { get; private set; }

        public PostState State { get; private set; }
        public DateTimeOffset PublishDateTime { get; private set; }

        public Post(PostProperty property, int defaultLikeCount, int defaultRepostCount, int defaultReplyCount)
        {
            Property = property;
            this.defaultLikeCount = defaultLikeCount;
            this.defaultRepostCount = defaultRepostCount;
            LikeCount = defaultLikeCount;
            RepostCount = defaultRepostCount;
            ReplyCount = defaultReplyCount;
            isLikedByPlayer.Value = false;
            isRepostedByPlayer.Value = false;
            RepostedByAccount = null;

            State = PostState.BeforeAppearing;
            PublishDateTime = DateTimeOffset.Now;
        }

        public void ChangeState(PostState newState)
        {
            State = newState;
            if (newState == PostState.Published)
            {
                PublishDateTime = DateTimeOffset.Now;
            }
        }

        public bool ToggleLikeByPlayer()
        {
            bool isActive = !isLikedByPlayer.CurrentValue;
            LikeCount += isActive ? 1 : -1;
            isLikedByPlayer.Value = isActive;
            return isActive;
        }

        public bool ToggleRepostByPlayer()
        {
            bool isActive = !isRepostedByPlayer.CurrentValue;
            RepostCount += isActive ? 1 : -1;
            isRepostedByPlayer.Value = isActive;
            return isActive;
        }

        public void ResetPlayerAction()
        {
            isLikedByPlayer.Value = false;
            isRepostedByPlayer.Value = false;
            LikeCount = defaultLikeCount;
            RepostCount = defaultRepostCount;
            RepostedByAccount = null;
        }

        public void MarkAsRepost(Account repostedByAccount)
        {
            RepostedByAccount = repostedByAccount;
            PublishDateTime = DateTimeOffset.Now;
        }
    }
}
