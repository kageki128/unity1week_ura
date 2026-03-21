using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Unity1Week_Ura.Actor
{
    public class PostView : MonoBehaviour
    {
        public Post post { get; private set; }
        public float Width => viewArranger.Width;
        public float Height => viewArranger.Height;

        [FormerlySerializedAs("sizeCalculator")]
        [SerializeField] ViewArranger viewArranger;
        [SerializeField] SpriteRenderer frameImage;
        [SerializeField] SpriteRenderer iconImage;
        [SerializeField] TMP_Text authorNameText;
        [SerializeField] TMP_Text authorIdText;
        [SerializeField] TMP_Text dateText;
        [SerializeField] TMP_Text advertisementText;
        [SerializeField] TMP_Text parentText;
        [SerializeField] TMP_Text contentText;
        [SerializeField] TMP_Text replyCountText;
        [SerializeField] TMP_Text repostCountText;
        [SerializeField] TMP_Text likeCountText;

        public void Initialize(Post post)
        {
            var property = post.Property;

            iconImage.sprite = property.Author.Icon;
            authorNameText.text = property.Author.Name;
            authorIdText.text = $"@{property.Author.Id}";
            dateText.text = post.PublishDateTime.ToString("yyyy/MM/dd HH:mm");

            advertisementText.gameObject.SetActive(property.Type == PostType.Advertisement);

            if (string.IsNullOrEmpty(property.ParentPostId))
            {
                parentText.gameObject.SetActive(false);
                parentText.text = string.Empty;
            }
            else
            {
                parentText.gameObject.SetActive(true);
                parentText.text = $"Re: {property.ParentPostId}";
            }

            contentText.text = property.Text;
            replyCountText.text = post.ReplyCount.ToString();
            repostCountText.text = post.RepostCount.ToString();
            likeCountText.text = post.LikeCount.ToString();

            this.post = post;
        }

        public void SetPosition(float x, float y)
        {
            viewArranger.SetPosition(x, y);
        }
    }
}