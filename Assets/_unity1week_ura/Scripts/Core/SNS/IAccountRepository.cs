using System.Threading;
using Cysharp.Threading.Tasks;

namespace Unity1Week_Ura.Core
{
    public interface IAccountRepository
    {
        UniTask<Account> GetAccount(string accountId, CancellationToken ct);
    }
}