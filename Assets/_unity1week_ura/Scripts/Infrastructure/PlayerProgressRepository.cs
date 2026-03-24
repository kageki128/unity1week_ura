using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Infrastructure
{
    public class PlayerProgressRepository : IPlayerProgressRepository
    {
        const string HowToPlaySeenKey = "how_to_play_seen";

        public UniTask<bool> HasSeenHowToPlayAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var hasSeen = PlayerPrefs.GetInt(HowToPlaySeenKey, 0) != 0;
            return UniTask.FromResult(hasSeen);
        }

        public UniTask MarkHowToPlayAsSeenAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            PlayerPrefs.SetInt(HowToPlaySeenKey, 1);
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }
    }
}
