using R3;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ButtonView : MonoBehaviour
    {
        public Observable<Unit> OnClicked => buttonCollider.OnClicked.Select(_ => Unit.Default);

        [SerializeField] PointerEventObserver buttonCollider;
    }
}