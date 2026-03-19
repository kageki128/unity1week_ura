using R3;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    [RequireComponent(typeof(Collider2D))]
    public class PointerEventObserver : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        // ポインターがクリックされたときに発行される。
        public Observable<PointerEventData> OnClicked => onClicked;

        // ポインターがこのオブジェクトの領域に入ったときに発行される。
        public Observable<PointerEventData> OnPointerEntered => onPointerEntered;

        // ポインターがこのオブジェクトの領域から出たときに発行される。
        public Observable<PointerEventData> OnPointerExited => onPointerExited;

        // ポインター押下が開始されたときに発行される。
        public Observable<PointerEventData> OnPointerDowned => onPointerDowned;

        // ポインター押下が解除されたときに発行される。
        public Observable<PointerEventData> OnPointerUpped => onPointerUpped;

        // ドラッグ開始時に発行される。
        public Observable<PointerEventData> OnBeginDragged => onBeginDragged;

        // ドラッグ中に継続して発行される。
        public Observable<PointerEventData> OnDragged => onDragged;

        // ドラッグ終了時に発行される。
        public Observable<PointerEventData> OnEndDragged => onEndDragged;

        readonly Subject<PointerEventData> onClicked = new();
        readonly Subject<PointerEventData> onPointerEntered = new();
        readonly Subject<PointerEventData> onPointerExited = new();
        readonly Subject<PointerEventData> onPointerDowned = new();
        readonly Subject<PointerEventData> onPointerUpped = new();
        readonly Subject<PointerEventData> onBeginDragged = new();
        readonly Subject<PointerEventData> onDragged = new();
        readonly Subject<PointerEventData> onEndDragged = new();

        public void OnPointerClick(PointerEventData eventData)
        {
            onClicked.OnNext(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            onPointerEntered.OnNext(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onPointerExited.OnNext(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            onPointerDowned.OnNext(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onPointerUpped.OnNext(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            onBeginDragged.OnNext(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            onDragged.OnNext(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            onEndDragged.OnNext(eventData);
        }

        void OnDestroy()
        {
            onClicked.OnCompleted();
            onClicked.Dispose();

            onPointerEntered.OnCompleted();
            onPointerEntered.Dispose();

            onPointerExited.OnCompleted();
            onPointerExited.Dispose();

            onPointerDowned.OnCompleted();
            onPointerDowned.Dispose();

            onPointerUpped.OnCompleted();
            onPointerUpped.Dispose();

            onBeginDragged.OnCompleted();
            onBeginDragged.Dispose();

            onDragged.OnCompleted();
            onDragged.Dispose();

            onEndDragged.OnCompleted();
            onEndDragged.Dispose();
        }
    }
}