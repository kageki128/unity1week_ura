using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public abstract class PhoneScreenViewBase : MonoBehaviour
    {
        protected ScreenTransitionViewHub screenTransitionViewHub;

        public virtual void Initialize(ScreenTransitionViewHub screenTransitionViewHub)
        {
            this.screenTransitionViewHub = screenTransitionViewHub;
        }

        public abstract UniTask ShowAsync(CancellationToken ct);
        public abstract UniTask HideAsync(CancellationToken ct);
    }
}