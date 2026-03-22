using R3;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    public class PublishFieldView : MonoBehaviour
    {
        public Observable<Post> OnNormalDraftDropped => onNormalDraftDropped;
        public Observable<Post> OnReplyDraftDropped => onReplyDraftDropped;
        public Observable<PointerEventData> OnScrolled => pointerEventObserver.OnScrolled;
        readonly Subject<Post> onNormalDraftDropped = new();
        readonly Subject<Post> onReplyDraftDropped = new();

        public float Width => viewArranger.Width;
        public float Height => viewArranger.Height;

        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] SpriteRenderer accountIcon;
        [SerializeField] ViewArranger viewArranger;
        [SerializeField] PostType acceptedPostType = PostType.Normal;

        readonly CompositeDisposable disposables = new();

        public void Initialize()
        {
            disposables.Clear();    
            pointerEventObserver.OnDropped.Subscribe(OnDrop).AddTo(disposables);
        }

        void OnDrop(PointerEventData eventData)
        {
            // ドラッグ元のDraftViewを取得
            var draggedObject = eventData.pointerDrag;
            if (draggedObject == null)
            {
                // ドロップされたオブジェクトがない場合は無視
                return;
            }

            if (!draggedObject.TryGetComponent<DraftView>(out var draftView))
            {
                // ドロップされたオブジェクトがDraftViewでない場合は無視
                return;
            }

            var post = draftView.post;
            if (post == null || post.Type != acceptedPostType)
            {
                return;
            }

            draftView.MarkAsDroppedOnPublishField();
            if (post.Type == PostType.Reply)
            {
                onReplyDraftDropped.OnNext(post);
                return;
            }

            onNormalDraftDropped.OnNext(post);
        }

        public void SetCurrentPlayerAccount(Account account)
        {
            if (accountIcon == null)
            {
                return;
            }

            accountIcon.sprite = account?.Icon;
        }

        public void SetPosition(float x, float y, bool useAnimation = true) => viewArranger.SetPosition(x, y, useAnimation);

        void OnDestroy()
        {
            onNormalDraftDropped.OnCompleted();
            onNormalDraftDropped.Dispose();
            onReplyDraftDropped.OnCompleted();
            onReplyDraftDropped.Dispose();
            disposables.Dispose();
        }
    }
}
