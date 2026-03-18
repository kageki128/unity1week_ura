using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Unity1Week_Ura.Core
{
    public interface IPostRepository
    {
        UniTask<List<Post>> GetPostsByCorrectPlayerAccountAsync(Account playerAccount);
    }
}