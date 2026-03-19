using R3;
using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    public class DraftView : MonoBehaviour
    {
        public Post post { get; private set; }
        public float Width => frameImage.bounds.size.x;
        public float Height => frameImage.bounds.size.y;

        [SerializeField] SpriteRenderer frameImage;
        [SerializeField] TMP_Text contentText;
        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] Collider2D dragCollider;

        Vector3 originalLocalPosition;
        bool droppedOnPublishField = false;

        readonly CompositeDisposable disposables = new();

        public void Initialize(Post post)
        {
            contentText.text = post.Property.Text;
            this.post = post;

            disposables.Clear();
            pointerEventObserver.OnBeginDragged.Subscribe(OnBeginDrag).AddTo(disposables);
            pointerEventObserver.OnDragged.Subscribe(OnDrag).AddTo(disposables);
            pointerEventObserver.OnEndDragged.Subscribe(OnEndDrag).AddTo(disposables);
        }

        public void SetPosition(float x, float y)
        {
            transform.localPosition = new Vector3(x, y, 0f);
        }

        public void MarkAsDroppedOnPublishField()
        {
            droppedOnPublishField = true;
        }

        void OnBeginDrag(PointerEventData eventData)
        {
            originalLocalPosition = transform.localPosition;
            droppedOnPublishField = false;
            // ドラッグ中はColliderを無効化して、PublishFieldViewへのレイキャストを遮蔽しない
            dragCollider.enabled = false;
        }

        void OnDrag(PointerEventData eventData)
        {
            var worldPosition = Camera.main.ScreenToWorldPoint(eventData.position);
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        }

        void OnEndDrag(PointerEventData eventData)
        {
            dragCollider.enabled = true;

            if (!droppedOnPublishField)
            {
                // PublishFieldView以外にドロップされた場合は元の位置に戻す
                transform.localPosition = originalLocalPosition;
            }
        }

        void OnDestroy()
        {
            disposables.Dispose();
        }
    }
}