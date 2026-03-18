using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class DifficultyButtonView : MonoBehaviour
    {
        public Observable<GameRuleSO> OnClicked => onClicked;
        readonly Subject<GameRuleSO> onClicked = new();

        [SerializeField] ButtonCollider buttonCollider;
        [SerializeField] GameRuleSO gameRule;

        void Awake()
        {
            buttonCollider.OnClicked.Subscribe(_ => onClicked.OnNext(gameRule)).AddTo(this);
        }

        void OnDestroy()
        {
            onClicked.OnCompleted();
            onClicked.Dispose();
        }
    }
}