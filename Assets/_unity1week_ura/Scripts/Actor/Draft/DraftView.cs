using R3;
using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Unity1Week_Ura.Actor
{
    public class DraftView : MonoBehaviour
    {
        const int DraggingSortingOrderOffset = 3;

        public Post post { get; private set; }
        public float Width => viewArranger.Width;
        public float Height => viewArranger.Height;
        public bool IsDragging { get; private set; }
        public Observable<PointerEventData> OnScrolled => onScrolled;

        [FormerlySerializedAs("sizeCalculator")]
        [SerializeField] ViewArranger viewArranger;
        [SerializeField] SpriteRenderer frameImage;
        [SerializeField] SpriteRenderer subFrameImage;
        [SerializeField] TMP_Text contentText;
        [SerializeField] Color replyTextColor = new Color32(0xEE, 0x5A, 0x7F, 0xFF);
        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] Collider2D dragCollider;

        Vector3 returnLocalPosition;
        bool droppedOnPublishField = false;
        Vector2 frameImageBaseSize;
        Vector2 subFrameImageBaseSize;
        Vector3 frameImageBaseScale;
        Vector3 subFrameImageBaseScale;
        Vector2 dragColliderBaseSize;
        float baseContentRenderedHeight;
        bool hasCachedFrameBaseValues;
        bool hasCachedSortingOrders;
        int frameImageBaseSortingOrder;
        int subFrameImageBaseSortingOrder;
        int contentTextBaseSortingOrder;
        TextMeshPro contentTextMesh;
        Renderer contentTextRenderer;

        readonly CompositeDisposable disposables = new();
        readonly Subject<PointerEventData> onScrolled = new();

        public void Initialize(Post post)
        {
            CacheFrameBaseValuesIfNeeded();
            CacheSortingOrdersIfNeeded();

            contentText.text = BuildContentText(post.Property);
            this.post = post;
            returnLocalPosition = transform.localPosition;
            AdjustLayout();
            SetDraggingSortingOrder(false);

            disposables.Clear();
            if (pointerEventObserver != null)
            {
                pointerEventObserver.OnPointerEntered.Subscribe(_ => PlaySE(SEType.ButtonHover)).AddTo(disposables);
                pointerEventObserver.OnPointerDowned.Subscribe(_ => PlaySE(SEType.ButtonClick)).AddTo(disposables);
                pointerEventObserver.OnScrolled.Subscribe(onScrolled.OnNext).AddTo(disposables);
                pointerEventObserver.OnBeginDragged.Subscribe(OnBeginDrag).AddTo(disposables);
                pointerEventObserver.OnDragged.Subscribe(OnDrag).AddTo(disposables);
                pointerEventObserver.OnEndDragged.Subscribe(OnEndDrag).AddTo(disposables);
            }
        }

        string BuildContentText(PostProperty property)
        {
            if (string.IsNullOrEmpty(property.ParentPostId))
            {
                return property.Text;
            }

            var parentAccountId = property.ParentPostAuthor?.Id;
            if (string.IsNullOrEmpty(parentAccountId))
            {
                parentAccountId = property.ParentPostId;
            }

            var replyTextColorHex = $"#{ColorUtility.ToHtmlStringRGBA(replyTextColor)}";
            var replyText = $"返信先: @{parentAccountId} さん";
            return $"<color={replyTextColorHex}>{replyText}</color>\n{property.Text}";
        }

        public void SetPosition(float x, float y, bool useAnimation = true)
        {
            viewArranger.SetPosition(x, y, useAnimation);
        }

        public void SetReturnPosition(float x, float y)
        {
            returnLocalPosition = new Vector3(x, y, transform.localPosition.z);
        }

        public void MarkAsDroppedOnPublishField()
        {
            droppedOnPublishField = true;
        }

        void CacheFrameBaseValuesIfNeeded()
        {
            if (hasCachedFrameBaseValues)
            {
                return;
            }

            if (contentText != null)
            {
                contentText.ForceMeshUpdate();
                baseContentRenderedHeight = contentText.renderedHeight;
            }

            if (frameImage != null)
            {
                frameImageBaseSize = frameImage.size;
                frameImageBaseScale = frameImage.transform.localScale;
            }

            if (subFrameImage != null)
            {
                subFrameImageBaseSize = subFrameImage.size;
                subFrameImageBaseScale = subFrameImage.transform.localScale;
            }

            if (dragCollider is BoxCollider2D boxCollider)
            {
                dragColliderBaseSize = boxCollider.size;
            }

            hasCachedFrameBaseValues = true;
        }

        void CacheSortingOrdersIfNeeded()
        {
            if (hasCachedSortingOrders)
            {
                return;
            }

            if (frameImage != null)
            {
                frameImageBaseSortingOrder = frameImage.sortingOrder;
            }

            if (subFrameImage != null)
            {
                subFrameImageBaseSortingOrder = subFrameImage.sortingOrder;
            }

            if (contentText != null)
            {
                contentTextMesh = contentText as TextMeshPro;
                if (contentTextMesh != null)
                {
                    contentTextBaseSortingOrder = contentTextMesh.sortingOrder;
                }
                else
                {
                    contentTextRenderer = contentText.GetComponent<Renderer>();
                    if (contentTextRenderer != null)
                    {
                        contentTextBaseSortingOrder = contentTextRenderer.sortingOrder;
                    }
                }
            }

            hasCachedSortingOrders = true;
        }

        void AdjustLayout()
        {
            contentText.ForceMeshUpdate();
            float renderedHeight = contentText.renderedHeight;
            
            // TextはY=0の中央配置となっているため、テキスト行数増減分をそのままフレーム枠の増減へ送る。
            // Draftの初期フレームサイズより文字数が少ない場合も枠を縮めるため、Mathf.Max(0, ...)の制限を外すことで上下の余白を一定に保ちます。
            float extraHeight = renderedHeight - baseContentRenderedHeight;

            ApplyHeightExtension(frameImage, frameImageBaseSize, frameImageBaseScale, extraHeight);
            ApplyHeightExtension(subFrameImage, subFrameImageBaseSize, subFrameImageBaseScale, extraHeight);

            if (dragCollider is BoxCollider2D boxCollider)
            {
                var size = dragColliderBaseSize;
                float scaleY = Mathf.Abs(boxCollider.transform.localScale.y) > Mathf.Epsilon ? Mathf.Abs(boxCollider.transform.localScale.y) : 1f;
                size.y += extraHeight / scaleY;
                boxCollider.size = size;
            }
        }

        void ApplyHeightExtension(SpriteRenderer frame, Vector2 baseSize, Vector3 baseScale, float extraHeight)
        {
            if (frame == null)
            {
                return;
            }

            if (frame.drawMode == SpriteDrawMode.Sliced || frame.drawMode == SpriteDrawMode.Tiled)
            {
                var targetSize = baseSize;
                float scaleY = Mathf.Abs(baseScale.y) > Mathf.Epsilon ? Mathf.Abs(baseScale.y) : 1f;
                targetSize.y = baseSize.y + extraHeight / scaleY;
                frame.size = targetSize;
                return;
            }

            var targetScale = baseScale;
            targetScale.y = baseScale.y + extraHeight;
            frame.transform.localScale = targetScale;
        }

        void OnBeginDrag(PointerEventData eventData)
        {
            viewArranger.StopAnimations();
            IsDragging = true;
            droppedOnPublishField = false;
            SetDraggingSortingOrder(true);
            // ドラッグ中はColliderを無効化して、PublishFieldViewへのレイキャストを遮蔽しない
            if (dragCollider != null)
            {
                dragCollider.enabled = false;
            }
        }

        void OnDrag(PointerEventData eventData)
        {
            var worldPosition = Camera.main.ScreenToWorldPoint(eventData.position);
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        }

        void OnEndDrag(PointerEventData eventData)
        {
            RestoreDragState();

            if (!droppedOnPublishField)
            {
                // PublishFieldView以外にドロップされた場合は元の位置に戻す
                SetPosition(returnLocalPosition.x, returnLocalPosition.y);
            }
        }

        void RestoreDragState()
        {
            IsDragging = false;
            SetDraggingSortingOrder(false);
            if (dragCollider != null)
            {
                dragCollider.enabled = true;
            }
        }

        void SetDraggingSortingOrder(bool isDragging)
        {
            int offset = isDragging ? DraggingSortingOrderOffset : 0;

            if (frameImage != null)
            {
                frameImage.sortingOrder = frameImageBaseSortingOrder + offset;
            }

            if (subFrameImage != null)
            {
                subFrameImage.sortingOrder = subFrameImageBaseSortingOrder + offset;
            }

            if (contentTextMesh != null)
            {
                contentTextMesh.sortingOrder = contentTextBaseSortingOrder + offset;
            }
            else if (contentTextRenderer != null)
            {
                contentTextRenderer.sortingOrder = contentTextBaseSortingOrder + offset;
            }
        }

        void OnDisable()
        {
            // ウィンドウ外でマウスアップした場合などにOnEndDragが来ないケースでも掴める状態に戻す
            RestoreDragState();
        }

        void OnDestroy()
        {
            disposables.Dispose();
            onScrolled.OnCompleted();
            onScrolled.Dispose();
        }

        void PlaySE(SEType seType)
        {
            AudioPlayer.Current?.PlaySE(seType);
        }
    }
}
