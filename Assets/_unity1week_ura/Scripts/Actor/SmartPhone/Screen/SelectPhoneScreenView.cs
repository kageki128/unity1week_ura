using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class SelectPhoneScreenView : PhoneScreenViewBase
    {
        public Observable<GameRuleSO> OnDifficultyButtonClicked => onDifficultyButtonClicked;
        Observable<GameRuleSO> onDifficultyButtonClicked;

        [SerializeField] List<DifficultyButtonView> difficultyButtons;
        
        public override void Initialize()
        {
            onDifficultyButtonClicked = Observable.Merge(difficultyButtons.Select(button => button.OnClicked).ToArray());
            gameObject.SetActive(false);
        }

        public override UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync(CancellationToken ct)
        {
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }
    }
}
