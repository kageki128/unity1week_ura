using System.Threading;
using Cysharp.Threading.Tasks;

namespace Unity1Week_Ura.Core
{
    public interface ISocialSharePort
    {
        UniTask ShareResultAsync(GameResult gameResult, CancellationToken ct);
    }
}