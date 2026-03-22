namespace Unity1Week_Ura.Core
{
    public class ReplyDraftPublishRequest
    {
        public Post ReplyDraft { get; }
        public Post FocusedPost { get; }

        public ReplyDraftPublishRequest(Post replyDraft, Post focusedPost)
        {
            ReplyDraft = replyDraft;
            FocusedPost = focusedPost;
        }
    }
}
