using System.Threading;
using Cysharp.Threading.Tasks;

namespace Unity1Week_Ura.Director
{
    public interface ISceneDirector
    {
        void Initialize();
        UniTask EnterAsync(CancellationToken ct);
        void Tick();
        UniTask ExitAsync(CancellationToken ct);
    }
}