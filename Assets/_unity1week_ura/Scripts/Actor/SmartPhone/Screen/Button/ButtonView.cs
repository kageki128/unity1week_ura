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
        [SerializeField] bool playHoverSE = true;
        [SerializeField] bool playClickSE = true;
        [SerializeField] SEType hoverSEType = SEType.ButtonHover;
        [SerializeField] SEType clickSEType = SEType.ButtonClick;

        readonly CompositeDisposable disposables = new();

        void Awake()
        {
            if (buttonCollider == null)
            {
                return;
            }

            if (playHoverSE)
            {
                buttonCollider.OnPointerEntered.Subscribe(_ => PlaySE(hoverSEType)).AddTo(disposables);
            }

            if (playClickSE)
            {
                buttonCollider.OnClicked.Subscribe(_ => PlaySE(clickSEType)).AddTo(disposables);
            }
        }

        public void SetInteractable(bool isInteractable)
        {
            if (buttonCollider == null)
            {
                return;
            }

            buttonCollider.enabled = isInteractable;
            if (buttonCollider.TryGetComponent<Collider2D>(out var collider))
            {
                collider.enabled = isInteractable;
            }
        }

        void OnDestroy()
        {
            disposables.Dispose();
        }

        void PlaySE(SEType seType)
        {
            AudioPlayer.Current?.PlaySE(seType);
        }
    }
}
