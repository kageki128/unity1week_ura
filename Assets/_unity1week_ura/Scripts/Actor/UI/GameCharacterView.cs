using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Unity1Week_Ura.Actor
{
    [DisallowMultipleComponent]
    public class GameCharacterView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PlayerAccountCharacterSpritesSO playerAccountCharacterSprites;
        [SerializeField] SpriteRenderer targetSpriteRenderer;
        [SerializeField] Image targetImage;

        [Header("State Sprites")]
        [SerializeField] Sprite scorePenaltySprite;
        [SerializeField] Sprite failureSprite;

        [Header("Penalty")]
        [SerializeField, Min(0f)] float penaltyDisplaySeconds = 1f;

        [Header("Normal Random Switch")]
        [SerializeField, Min(0.05f)] float randomSwitchIntervalMinSeconds = 0.8f;
        [SerializeField, Min(0.05f)] float randomSwitchIntervalMaxSeconds = 1.8f;

        Account currentPlayerAccount;
        Sprite currentNormalSprite;
        int currentScore;
        bool hasScore;
        bool isPenaltyActive;
        bool isFailureActive;

        CancellationTokenSource randomSwitchCancellationTokenSource;
        CancellationTokenSource penaltyCancellationTokenSource;

        public void Initialize()
        {
            if (targetSpriteRenderer == null)
            {
                targetSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            currentPlayerAccount = null;
            currentNormalSprite = null;
            currentScore = 0;
            hasScore = false;
            isPenaltyActive = false;
            isFailureActive = false;

            CancelPenaltyTimer();
            UpdateDisplayedSprite(forceRefreshNormalSprite: true);
            RestartRandomSwitchLoopIfNeeded();
        }

        public void PrepareForNewGame()
        {
            currentPlayerAccount = null;
            isFailureActive = false;
            isPenaltyActive = false;
            hasScore = false;
            currentScore = 0;
            currentNormalSprite = null;
            CancelPenaltyTimer();
            UpdateDisplayedSprite(forceRefreshNormalSprite: true);
        }

        public void SetSelectedPlayerAccount(Account account)
        {
            currentPlayerAccount = account;
            currentNormalSprite = null;
            UpdateDisplayedSprite(forceRefreshNormalSprite: true);
        }

        public void SetScore(int score)
        {
            if (!hasScore)
            {
                hasScore = true;
                currentScore = score;
                return;
            }

            bool isDecreased = score < currentScore;
            currentScore = score;

            if (!isDecreased)
            {
                return;
            }

            ActivatePenaltySprite();
        }

        public void SetFinishReason(FinishReason finishReason)
        {
            bool shouldShowFailure = finishReason != FinishReason.None && finishReason != FinishReason.TimeUp;
            if (isFailureActive == shouldShowFailure)
            {
                return;
            }

            isFailureActive = shouldShowFailure;
            if (isFailureActive)
            {
                isPenaltyActive = false;
                CancelPenaltyTimer();
            }

            UpdateDisplayedSprite(forceRefreshNormalSprite: false);
        }

        void OnEnable()
        {
            RestartRandomSwitchLoopIfNeeded();
            UpdateDisplayedSprite(forceRefreshNormalSprite: false);
        }

        void OnDisable()
        {
            CancelRandomSwitchLoop();
            CancelPenaltyTimer();
        }

        void OnDestroy()
        {
            CancelRandomSwitchLoop();
            CancelPenaltyTimer();
        }

        void ActivatePenaltySprite()
        {
            if (scorePenaltySprite == null || isFailureActive)
            {
                return;
            }

            isPenaltyActive = true;
            UpdateDisplayedSprite(forceRefreshNormalSprite: false);
            StartPenaltyTimer();
        }

        void StartPenaltyTimer()
        {
            CancelPenaltyTimer();
            penaltyCancellationTokenSource = new CancellationTokenSource();
            HidePenaltySpriteAfterDelayAsync(penaltyCancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid HidePenaltySpriteAfterDelayAsync(CancellationToken ct)
        {
            try
            {
                var delay = TimeSpan.FromSeconds(Mathf.Max(0f, penaltyDisplaySeconds));
                await UniTask.Delay(delay, cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (ct.IsCancellationRequested)
            {
                return;
            }

            isPenaltyActive = false;
            UpdateDisplayedSprite(forceRefreshNormalSprite: false);
        }

        void RestartRandomSwitchLoopIfNeeded()
        {
            CancelRandomSwitchLoop();
            randomSwitchCancellationTokenSource = new CancellationTokenSource();
            RandomSwitchLoopAsync(randomSwitchCancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid RandomSwitchLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var delaySeconds = GetRandomSwitchIntervalSeconds();
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (ct.IsCancellationRequested || isFailureActive || isPenaltyActive)
                {
                    continue;
                }

                if (!TryGetCurrentAccountSpriteEntry(out var entry))
                {
                    continue;
                }

                var nextSprite = PickRandomSprite(entry.Sprites, currentNormalSprite);
                if (nextSprite == null || nextSprite == currentNormalSprite)
                {
                    continue;
                }

                currentNormalSprite = nextSprite;
                ApplySprite(nextSprite);
            }
        }

        float GetRandomSwitchIntervalSeconds()
        {
            var minSeconds = Mathf.Max(0.05f, randomSwitchIntervalMinSeconds);
            var maxSeconds = Mathf.Max(0.05f, randomSwitchIntervalMaxSeconds);
            if (maxSeconds < minSeconds)
            {
                (minSeconds, maxSeconds) = (maxSeconds, minSeconds);
            }

            return UnityEngine.Random.Range(minSeconds, maxSeconds);
        }

        void UpdateDisplayedSprite(bool forceRefreshNormalSprite)
        {
            if (isFailureActive && failureSprite != null)
            {
                ApplySprite(failureSprite);
                return;
            }

            if (isPenaltyActive && scorePenaltySprite != null)
            {
                ApplySprite(scorePenaltySprite);
                return;
            }

            if (!TryGetCurrentAccountSpriteEntry(out var entry))
            {
                currentNormalSprite = null;
                ApplySprite(null);
                return;
            }

            if (forceRefreshNormalSprite || currentNormalSprite == null || !ContainsSprite(entry.Sprites, currentNormalSprite))
            {
                currentNormalSprite = PickRandomSprite(entry.Sprites, null);
            }

            ApplySprite(currentNormalSprite);
        }

        bool TryGetCurrentAccountSpriteEntry(out PlayerAccountCharacterSpritesSO.AccountSpriteEntry entry)
        {
            entry = null;
            if (playerAccountCharacterSprites == null || currentPlayerAccount == null)
            {
                return false;
            }

            return playerAccountCharacterSprites.TryGetEntry(currentPlayerAccount.Id, out entry);
        }

        static bool ContainsSprite(IReadOnlyList<Sprite> entries, Sprite sprite)
        {
            if (entries == null || sprite == null)
            {
                return false;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry == sprite)
                {
                    return true;
                }
            }

            return false;
        }

        static Sprite PickRandomSprite(IReadOnlyList<Sprite> entries, Sprite excludeSprite)
        {
            if (entries == null)
            {
                return null;
            }

            var validCount = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (excludeSprite != null && entry == excludeSprite)
                {
                    continue;
                }

                validCount++;
            }

            if (validCount <= 0)
            {
                if (excludeSprite == null)
                {
                    return null;
                }

                return PickRandomSprite(entries, null);
            }

            var targetIndex = UnityEngine.Random.Range(0, validCount);
            var currentIndex = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (excludeSprite != null && entry == excludeSprite)
                {
                    continue;
                }

                if (currentIndex == targetIndex)
                {
                    return entry;
                }

                currentIndex++;
            }

            return null;
        }

        void ApplySprite(Sprite sprite)
        {
            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.sprite = sprite;
            }

            if (targetImage != null)
            {
                targetImage.sprite = sprite;
                targetImage.enabled = sprite != null;
            }
        }

        void CancelRandomSwitchLoop()
        {
            if (randomSwitchCancellationTokenSource == null)
            {
                return;
            }

            randomSwitchCancellationTokenSource.Cancel();
            randomSwitchCancellationTokenSource.Dispose();
            randomSwitchCancellationTokenSource = null;
        }

        void CancelPenaltyTimer()
        {
            if (penaltyCancellationTokenSource == null)
            {
                return;
            }

            penaltyCancellationTokenSource.Cancel();
            penaltyCancellationTokenSource.Dispose();
            penaltyCancellationTokenSource = null;
        }
    }
}
