using System.Threading;
using Cysharp.Threading.Tasks;

namespace Unity1Week_Ura.Core
{
    public interface IHighScoreRepository
    {
        UniTask<int> GetHighScoreAsync(GameRuleSO gameRule, CancellationToken ct);
        UniTask<int> SaveHighScoreIfHigherAsync(GameRuleSO gameRule, int score, CancellationToken ct);
    }
}
