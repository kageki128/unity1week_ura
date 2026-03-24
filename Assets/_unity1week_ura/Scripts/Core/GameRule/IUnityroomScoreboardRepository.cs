using System.Threading;
using Cysharp.Threading.Tasks;

namespace Unity1Week_Ura.Core
{
    public interface IUnityroomScoreboardRepository
    {
        UniTask SendScoreboardsAsync(CancellationToken ct);
    }
}
