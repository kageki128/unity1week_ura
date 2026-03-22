using R3;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    public class ButtonView : MonoBehaviour
    {
        public Observable<Unit> OnClicked => buttonCollider.OnClicked.Select(_ => Unit.Default);
        public Observable<PointerEventData> OnScrolled => buttonCollider.OnScrolled;

        [SerializeField] PointerEventObserver buttonCollider;
    }
}