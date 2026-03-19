using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class GameViewHub : ViewBase
    {
        [SerializeField] SmartPhoneView smartPhoneView;
        [SerializeField] ScoreView scoreView;
        [SerializeField] RemainingTimeView remainingTimeView;

        public override void Initialize()
        {
            smartPhoneView.Initialize();
            remainingTimeView.Initialize();
            scoreView.Initialize();
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {

            await smartPhoneView.ShowScreenAsync(SceneType.Game, ct);
            await scoreView.ShowAsync(ct);
            await remainingTimeView.ShowAsync(ct);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            await smartPhoneView.HideScreenAsync(SceneType.Game, ct);
            await scoreView.HideAsync(ct);
            await remainingTimeView.HideAsync(ct);
        }

        public void AddPostToTimeline(Post post)
        {
            smartPhoneView.AddPostToTimeline(post);
        }
    }
}