using System.Threading;
using Cysharp.Threading.Tasks;

namespace Unity1Week_Ura.Core
{
    public interface ISocialSharePort
    {
        string BuildResultShareText(GameResult gameResult);
        UniTask ShareResultTextAsync(string shareText, CancellationToken ct);
    }
}
