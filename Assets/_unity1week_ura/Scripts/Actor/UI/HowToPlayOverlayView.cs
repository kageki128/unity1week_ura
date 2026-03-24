using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity1Week_Ura.Actor
{
    [DisallowMultipleComponent]
    public class HowToPlayOverlayView : AnimationViewBase
    {
        public Observable<Unit> OnShown => onShown;
        public Observable<Unit> OnHidden => onHidden;

        [Header("Data")]
        [SerializeField] HowToPlayPagesSO howToPlayPages;

        [Header("References")]
        [SerializeField] StandardViewAnimator standardViewAnimator;
        [SerializeField] TMP_Text pageTitleText;
        [SerializeField] TMP_Text pageText;
        [SerializeField] Image pageImage;

        [Header("Buttons")]
        [SerializeField] ButtonView previousButtonView;
        [SerializeField] ButtonView nextButtonView;
        [SerializeField] ButtonView closeButtonView;

        readonly CompositeDisposable disposables = new();
        readonly Subject<Unit> onShown = new();
        readonly Subject<Unit> onHidden = new();
        readonly SemaphoreSlim transitionSemaphore = new(1, 1);

        int currentPageIndex;
        bool isShown;

        public override void Initialize()
        {
            disposables.Clear();

            if (standardViewAnimator == null)
            {
                standardViewAnimator = GetComponent<StandardViewAnimator>();
            }

            previousButtonView?.OnClicked.Subscribe(_ => ShowPreviousPage()).AddTo(disposables);
            nextButtonView?.OnClicked.Subscribe(_ => ShowNextPage()).AddTo(disposables);
            closeButtonView?.OnClicked.Subscribe(_ => HideAsync(destroyCancellationToken).Forget()).AddTo(disposables);

            currentPageIndex = 0;
            ApplyPageContent();
            standardViewAnimator?.Initialize();

            if (standardViewAnimator == null)
            {
                gameObject.SetActive(false);
            }

            isShown = false;
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            await transitionSemaphore.WaitAsync(ct);
            try
            {
                currentPageIndex = 0;
                ApplyPageContent();

                if (isShown)
                {
                    return;
                }

                if (standardViewAnimator != null)
                {
                    await standardViewAnimator.ShowAsync(ct);
                }
                else
                {
                    gameObject.SetActive(true);
                }

                isShown = true;
                onShown.OnNext(Unit.Default);
            }
            finally
            {
                transitionSemaphore.Release();
            }
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            await transitionSemaphore.WaitAsync(ct);
            try
            {
                if (!isShown && !gameObject.activeSelf)
                {
                    return;
                }

                if (standardViewAnimator != null)
                {
                    await standardViewAnimator.HideAsync(ct);
                }
                else
                {
                    gameObject.SetActive(false);
                }

                if (!isShown)
                {
                    return;
                }

                isShown = false;
                onHidden.OnNext(Unit.Default);
            }
            finally
            {
                transitionSemaphore.Release();
            }
        }

        void ShowPreviousPage()
        {
            if (!isShown)
            {
                return;
            }

            var pageCount = GetPageCount();
            if (pageCount <= 0)
            {
                return;
            }

            currentPageIndex = Mathf.Clamp(currentPageIndex - 1, 0, pageCount - 1);
            ApplyPageContent();
        }

        void ShowNextPage()
        {
            if (!isShown)
            {
                return;
            }

            var pageCount = GetPageCount();
            if (pageCount <= 0)
            {
                return;
            }

            currentPageIndex = Mathf.Clamp(currentPageIndex + 1, 0, pageCount - 1);
            ApplyPageContent();
        }

        int GetPageCount()
        {
            var pages = howToPlayPages?.Pages;
            return pages?.Count ?? 0;
        }

        void ApplyPageContent()
        {
            var pageCount = GetPageCount();
            if (pageCount <= 0)
            {
                if (pageTitleText != null)
                {
                    pageTitleText.text = string.Empty;
                }

                if (pageText != null)
                {
                    pageText.text = string.Empty;
                }

                if (pageImage != null)
                {
                    pageImage.sprite = null;
                    pageImage.enabled = false;
                }

                SetNavigationButtons(canGoPrevious: false, canGoNext: false);
                return;
            }

            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, pageCount - 1);
            var page = howToPlayPages.Pages[currentPageIndex];

            if (pageTitleText != null)
            {
                var title = page?.Title ?? string.Empty;
                pageTitleText.text = $"{title} ({currentPageIndex + 1}/{pageCount})";
            }

            if (pageText != null)
            {
                pageText.text = page?.Text ?? string.Empty;
            }

            if (pageImage != null)
            {
                pageImage.sprite = page?.Image;
                pageImage.enabled = page?.Image != null;
            }

            var canGoPrevious = currentPageIndex > 0;
            var canGoNext = currentPageIndex < pageCount - 1;
            SetNavigationButtons(canGoPrevious, canGoNext);
        }

        void SetNavigationButtons(bool canGoPrevious, bool canGoNext)
        {
            SetButtonVisible(previousButtonView, canGoPrevious);
            SetButtonVisible(nextButtonView, canGoNext);
        }

        static void SetButtonVisible(ButtonView buttonView, bool isVisible)
        {
            if (buttonView == null)
            {
                return;
            }

            buttonView.gameObject.SetActive(isVisible);
            buttonView.SetInteractable(isVisible);
        }

        void OnDestroy()
        {
            disposables.Dispose();
            transitionSemaphore.Dispose();
            onShown.OnCompleted();
            onHidden.OnCompleted();
            onShown.Dispose();
            onHidden.Dispose();
        }
    }
}
