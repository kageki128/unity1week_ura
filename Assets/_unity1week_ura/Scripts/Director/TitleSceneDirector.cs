using System.Threading;
using Cysharp.Threading.Tasks;

namespace Unity1Week_Ura.Director
{
    public class TitleSceneDirector : ISceneDirector
    {
        public void Initialize()
        {
        }

        public UniTask EnterAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }

        public void Tick()
        {
        }

        public UniTask ExitAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }
    }
}
