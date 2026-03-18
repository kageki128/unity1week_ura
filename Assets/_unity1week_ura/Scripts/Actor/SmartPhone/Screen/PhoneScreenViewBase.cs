using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public abstract class PhoneScreenViewBase : MonoBehaviour
    {
        public abstract void Initialize();
        public abstract UniTask ShowAsync();
        public abstract UniTask HideAsync();
    }
}