using System.Collections.Generic;
using UnityEngine;

namespace Unity1Week_Ura.Core
{
    [CreateAssetMenu(fileName = "GameRule", menuName = "Unity1Week_Ura/GameRule")]
    public class GameRuleSO : ScriptableObject
    {
        public string DifficultyId => difficultyId;
        [SerializeField] string difficultyId;

        public string DifficultyName => difficultyName;
        [SerializeField] string difficultyName = "難易度の名前";

        public float TimeLimitSeconds => timeLimitSeconds;
        [SerializeField] float timeLimitSeconds = 120.0f;

        public float PostPerSecond => postPerSecond;
        [SerializeField] float postPerSecond = 0.5f;
        public string PostPerSecondDescription => postPerSecondDescription;
        [SerializeField] string postPerSecondDescription = "流速はどのくらい？";

        public float AdvertisePostProbability => advertisePostProbability;
        [SerializeField, Range(0f, 1f)] float advertisePostProbability = 0f;

        public string AdvertisePostProbabilityDescription => advertisePostProbabilityDescription;
        [SerializeField] string advertisePostProbabilityDescription = "広告はどのくらい？";

        public IReadOnlyList<MyAccountSO> UsedAccounts => usedAccounts;
        [SerializeField] List<MyAccountSO> usedAccounts;
    }
}
