using R3;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ButtonCollider : MonoBehaviour
    {
        public Observable<Unit> OnClicked => onClicked;
        readonly Subject<Unit> onClicked = new();

        void OnMouseDown()
        {
            onClicked.OnNext(Unit.Default);
        }

        void OnDestroy()
        {
            onClicked.OnCompleted();
            onClicked.Dispose();
        }
    }
}