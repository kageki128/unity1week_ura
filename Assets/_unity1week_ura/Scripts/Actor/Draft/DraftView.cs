using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class DraftView : MonoBehaviour
    {
        public Post post { get; private set; }
        public float Width => frameImage.bounds.size.x;
        public float Height => frameImage.bounds.size.y;

        [SerializeField] SpriteRenderer frameImage;
        [SerializeField] TMP_Text contentText;

        public void Initialize(Post post)
        {
            contentText.text = post.Property.Text;
            this.post = post;
        }

        public void SetPosition(float x, float y)
        {
            transform.localPosition = new Vector3(x, y, 0f);
        }
    }
}