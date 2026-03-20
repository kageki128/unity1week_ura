using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class RemainingTimeView : AnimationViewBase
    {
        [SerializeField] TMP_Text remainingTimeText;

        const string Format = @"mm\:ss";

        public override void Initialize()
        {
            gameObject.SetActive(false);
        }

        public override UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync(CancellationToken ct)
        {
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        public void SetRemainingTime(float remainingTime)
        {
            var timeSpan = TimeSpan.FromSeconds(remainingTime);
            remainingTimeText.text = timeSpan.ToString(Format);
        }
    }
}