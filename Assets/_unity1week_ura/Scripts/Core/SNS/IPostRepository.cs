using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Unity1Week_Ura.Core
{
    public interface IPostRepository
    {
        UniTask<Post> GetPost(string postId, CancellationToken ct);
        UniTask<List<Post>> GetPostsByCorrectPlayerAccountAsync(Account playerAccount, CancellationToken ct);
    }
}