using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class DifficultyButtonView : MonoBehaviour
    {
        public Observable<GameRuleSO> OnClicked => buttonCollider.OnClicked.Select(_ => gameRule);

        [SerializeField] PointerEventObserver buttonCollider;
        [SerializeField] GameRuleSO gameRule;
    }
}