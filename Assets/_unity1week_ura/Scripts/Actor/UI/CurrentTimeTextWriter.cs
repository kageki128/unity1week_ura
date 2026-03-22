using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    [RequireComponent(typeof(TMP_Text))]
    public class CurrentTimeTextWriter : MonoBehaviour
    {
        [SerializeField] TMP_Text timeText;
        [SerializeField] float refreshIntervalSeconds = 1f;

        const string TimeFormat = "HH:mm";

        CancellationTokenSource updateCancellationTokenSource;

        void Awake()
        {
            timeText ??= GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            StartRefreshLoop();
        }

        void OnDisable()
        {
            StopRefreshLoop();
        }

        void OnDestroy()
        {
            StopRefreshLoop();
        }

        void OnValidate()
        {
            timeText ??= GetComponent<TMP_Text>();
            refreshIntervalSeconds = Mathf.Max(0.1f, refreshIntervalSeconds);
        }

        void Reset()
        {
            timeText = GetComponent<TMP_Text>();
        }

        public void RefreshNow()
        {
            if (timeText == null)
            {
                return;
            }

            timeText.text = DateTime.Now.ToString(TimeFormat);
        }

        void StartRefreshLoop()
        {
            StopRefreshLoop();
            updateCancellationTokenSource = new CancellationTokenSource();
            RefreshLoopAsync(updateCancellationTokenSource.Token).Forget();
        }

        void StopRefreshLoop()
        {
            if (updateCancellationTokenSource == null)
            {
                return;
            }

            updateCancellationTokenSource.Cancel();
            updateCancellationTokenSource.Dispose();
            updateCancellationTokenSource = null;
        }

        async UniTask RefreshLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    RefreshNow();
                    await UniTask.Delay(TimeSpan.FromSeconds(refreshIntervalSeconds), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
