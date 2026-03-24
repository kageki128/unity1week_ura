using System.Threading;
using Cysharp.Threading.Tasks;

namespace Unity1Week_Ura.Core
{
    public interface IPlayerProgressRepository
    {
        UniTask<bool> HasSeenHowToPlayAsync(CancellationToken ct);
        UniTask MarkHowToPlayAsSeenAsync(CancellationToken ct);
    }
}
