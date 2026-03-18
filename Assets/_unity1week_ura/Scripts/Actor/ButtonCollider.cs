using R3;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    public class ButtonCollider : MonoBehaviour, IPointerClickHandler
    {
        public Observable<Unit> OnClicked => onClicked;
        readonly Subject<Unit> onClicked = new();

        public void OnPointerClick(PointerEventData eventData)
        {
            onClicked.OnNext(Unit.Default);

            Debug.Log($"Clicked: {gameObject.name}");
        }

        void OnDestroy()
        {
            onClicked.OnCompleted();
            onClicked.Dispose();
        }
    }
}