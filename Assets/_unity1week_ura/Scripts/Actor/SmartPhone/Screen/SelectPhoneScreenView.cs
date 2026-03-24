using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class SelectPhoneScreenView : PhoneScreenViewBase
    {
        public Observable<GameRuleSO> OnGameStartButtonClicked => onGameStartButtonClicked;
        public Observable<Unit> OnBackToTitleButtonClicked => backToTitleButton.OnClicked;
        Observable<GameRuleSO> onGameStartButtonClicked;

        [SerializeField] List<DifficultyButtonView> difficultyButtons;
        [SerializeField] ButtonView gameStartButton;
        [SerializeField] ButtonView backToTitleButton;
        [SerializeField] DifficultyInfoView difficultyInfoView;
        [SerializeField] List<HighScoreTextView> highScoreTextViews;
        [SerializeField] TimedViewAnimationPlayer timedViewAnimationPlayer;

        readonly Subject<GameRuleSO> gameStartButtonClickedSubject = new();
        readonly CompositeDisposable disposables = new();
        DifficultyButtonView selectedDifficultyButton;

        public override void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            base.Initialize(screenTransitionViewHub);

            disposables.Clear();
            selectedDifficultyButton = null;
            onGameStartButtonClicked = gameStartButtonClickedSubject;
            difficultyInfoView?.SetGameRule(null);
            SetAllHighScoreTexts(0);

            var availableButtons = difficultyButtons == null
                ? System.Array.Empty<DifficultyButtonView>()
                : difficultyButtons.Where(button => button != null).ToArray();
            foreach (var button in availableButtons)
            {
                button.Initialize();
                button.SetSelected(false);
                button.OnClicked.Subscribe(_ => SelectDifficultyButton(button)).AddTo(disposables);
            }

            var firstButton = availableButtons.FirstOrDefault();
            if (firstButton != null)
            {
                SelectDifficultyButton(firstButton);
            }

            if (gameStartButton != null)
            {
                gameStartButton.OnClicked.Subscribe(_ =>
                {
                    if (selectedDifficultyButton == null)
                    {
                        return;
                    }

                    gameStartButtonClickedSubject.OnNext(selectedDifficultyButton.GameRule);
                }).AddTo(disposables);
            }

            UpdateGameStartButtonInteractable();
            timedViewAnimationPlayer?.InitializeRegisteredViews();
            gameObject.SetActive(false);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);

            if (timedViewAnimationPlayer == null || !timedViewAnimationPlayer.HasShowAnimations)
            {
                await screenTransitionViewHub.HideAsync(ct);
                return;
            }

            await UniTask.WhenAll(
                screenTransitionViewHub.HideAsync(ct),
                timedViewAnimationPlayer.PlayShowAsync(ct));
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            if (timedViewAnimationPlayer == null || !timedViewAnimationPlayer.HasHideAnimations)
            {
                await screenTransitionViewHub.ShowAsync(ScreenTransitionType.AppIconLaunchPortrait, ct);
                gameObject.SetActive(false);
                return;
            }

            await UniTask.WhenAll(
                screenTransitionViewHub.ShowAsync(ScreenTransitionType.AppIconLaunchPortrait, ct),
                timedViewAnimationPlayer.PlayHideAsync(ct));

            gameObject.SetActive(false);
        }

        void SelectDifficultyButton(DifficultyButtonView difficultyButton)
        {
            if (difficultyButton == null)
            {
                return;
            }

            if (selectedDifficultyButton != null && selectedDifficultyButton != difficultyButton)
            {
                selectedDifficultyButton.SetSelected(false);
            }

            selectedDifficultyButton = difficultyButton;
            selectedDifficultyButton.SetSelected(true);
            difficultyInfoView?.SetGameRule(selectedDifficultyButton.GameRule);
            UpdateGameStartButtonInteractable();
        }

        void UpdateGameStartButtonInteractable()
        {
            gameStartButton?.SetInteractable(selectedDifficultyButton != null);
        }

        public async UniTask LoadHighScoresAsync(IHighScoreRepository highScoreRepository, CancellationToken ct)
        {
            if (highScoreRepository == null)
            {
                throw new ArgumentNullException(nameof(highScoreRepository));
            }

            if (highScoreTextViews == null || highScoreTextViews.Count == 0)
            {
                return;
            }

            for (var i = 0; i < highScoreTextViews.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var highScoreTextView = highScoreTextViews[i];
                if (highScoreTextView == null)
                {
                    continue;
                }

                try
                {
                    await highScoreTextView.LoadHighScoreAsync(highScoreRepository, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    highScoreTextView.SetScore(0);
                }
            }
        }

        void SetAllHighScoreTexts(int score)
        {
            if (highScoreTextViews != null)
            {
                for (var i = 0; i < highScoreTextViews.Count; i++)
                {
                    var highScoreTextView = highScoreTextViews[i];
                    highScoreTextView?.SetScore(score);
                }
            }
        }

        void OnDestroy()
        {
            disposables.Dispose();
            gameStartButtonClickedSubject.Dispose();
        }
    }
}
