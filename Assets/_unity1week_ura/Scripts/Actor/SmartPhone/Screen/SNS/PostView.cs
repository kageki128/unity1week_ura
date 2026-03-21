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

        [SerializeField] ViewArranger viewArranger;
        [SerializeField] SpriteRenderer frameImage;
        [SerializeField] SpriteRenderer iconImage;
        [SerializeField] TMP_Text headerText;
        [SerializeField] Color subTextColor;
        [SerializeField] int subTextFontSizeOffset = 3;
        [SerializeField] Color repltTextColor = new Color32(0xEE, 0x5A, 0x7F, 0xFF);
        [SerializeField] TMP_Text advertisementText;
        [SerializeField] TMP_Text contentText;
        [SerializeField] TMP_Text repostCountText;
        [SerializeField] TMP_Text likeCountText;
        [SerializeField] float defaultContentHeight = 0.6f;

        public void Initialize(Post post)
        {
            var property = post.Property;
            var subTextColorHex = $"#{ColorUtility.ToHtmlStringRGBA(subTextColor)}";
            var repltTextColorHex = $"#{ColorUtility.ToHtmlStringRGBA(repltTextColor)}";

            iconImage.sprite = property.Author.Icon;
            headerText.text = $"{property.Author.Name}<size={subTextFontSizeOffset}><color={subTextColorHex}>　@{property.Author.Id}　{post.PublishDateTime:yyyy/MM/dd HH:mm}</color></size>";

            advertisementText.gameObject.SetActive(property.Type == PostType.Advertisement);

            if (string.IsNullOrEmpty(property.ParentPostId))
            {
                contentText.text = property.Text;
            }
            else
            {
                var replyText = $"返信先: @{property.ParentPostId} さん";
                contentText.text = $"<color={repltTextColorHex}>{replyText}</color>\n{property.Text}";
            }
            repostCountText.text = post.RepostCount.ToString();
            likeCountText.text = post.LikeCount.ToString();

            this.post = post;

            AdjustLayout();
        }

        public void SetPosition(float x, float y)
        {
            viewArranger.SetPosition(x, y);
        }

        void AdjustLayout()
        {
            // テキストメッシュを強制更新して実際の描画サイズを取得
            contentText.ForceMeshUpdate();
            float renderedHeight = contentText.renderedHeight;

            // 1行時のデフォルト高さとの差分を計算
            float extraHeight = Mathf.Max(0f, renderedHeight - defaultContentHeight);
            if (extraHeight <= 0f) return;

            float halfExtra = extraHeight * 0.5f;

            // 全ての子オブジェクトを自動的に判別してオフセット
            foreach (Transform child in transform)
            {
                if (child == frameImage.transform)
                {
                    continue; // Frameはスケール変更で対応するため除外
                }

                var pos = child.localPosition;
                // 中心(Y=0)以上のオブジェクトは上へ、未満のオブジェクトは下へ広げる
                if (pos.y >= 0f)
                {
                    pos.y += halfExtra;
                }
                else
                {
                    pos.y -= halfExtra;
                }
                child.localPosition = pos;
            }

            // Frameは原点に固定したまま、スケールYのみ拡大（上下対称に広がる）
            var frameTransform = frameImage.transform;
            var frameScale = frameTransform.localScale;
            frameScale.y += extraHeight;
            frameTransform.localScale = frameScale;
        }
    }
}