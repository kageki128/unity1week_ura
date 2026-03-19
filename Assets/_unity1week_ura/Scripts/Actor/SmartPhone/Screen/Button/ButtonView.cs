using R3;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ButtonView : MonoBehaviour
    {
        public Observable<Unit> OnClicked => buttonCollider.OnClicked;
        [SerializeField] ButtonCollider buttonCollider;
    }
}