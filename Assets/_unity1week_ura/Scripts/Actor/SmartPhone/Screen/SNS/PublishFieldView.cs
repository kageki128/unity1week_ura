using R3;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    public class PublishFieldView : MonoBehaviour
    {
        public Observable<Post> OnDraftDropped => onDraftDropped;
        public Observable<PointerEventData> OnScrolled => pointerEventObserver.OnScrolled;
        readonly Subject<Post> onDraftDropped = new();

        public float Width => viewArranger.Width;
        public float Height => viewArranger.Height;

        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] SpriteRenderer accountIcon;
        [SerializeField] ViewArranger viewArranger;

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

            draftView.MarkAsDroppedOnPublishField();
            onDraftDropped.OnNext(draftView.post);
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
            onDraftDropped.OnCompleted();
            onDraftDropped.Dispose();
            disposables.Dispose();
        }
    }
}
